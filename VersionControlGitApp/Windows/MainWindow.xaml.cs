using Octokit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;
using VersionControlGitApp.UIelements;
using VersionControlGitApp.Logging;
using System.IO;
using System.Windows.Input;
using VersionControlGitApp.Windows;

using static VersionControlGitApp.Config;
using MenuItem = System.Windows.Controls.MenuItem;

namespace VersionControlGitApp {
    public partial class MainWindow : Window {
        
        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));
        private readonly LocalRepoDB repoDB = new LocalRepoDB();
        public static List<UserRepository> userRepos;
        public static User user;        

        public static Thread repoChangesThread;
        public static RepoChangesThreadState newRepoChangesThreadState = RepoChangesThreadState.New;
        

        public MainWindow(string token) {
            InitializeComponent();

            // init repository database
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token, this);

            // get current token
            //client.Connection.Credentials.GetToken()

            // get user based on token and set name + picture
            user = client.User.Current().Result;
            MainWindowUI.InitUIElements(this, user, repoDB);

            // get path from pathlabel
            string path = PathLabel.Text.ToString();

            // if there is repo then watch for changes
            if (path != "") {

                // set commit button branch
                MainWindowUI.ChangeCommitButtonBranch(path, this);

                // start thread to watch selected repo
                repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                repoChangesThread.Start();              

                ConsoleLogger.StatusBarUpdate($"{GitMethods.GetNameFromPath(path)} is now watched for changes", this);

                // load repo branches
                Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(path, this));
            }

            // async listener for changes in selected file
            Task.Run(() => AllReposListener());
        }

        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            MainWindowController.AddLocalRepositoryCommand(repoDB);
        }

        private void CloneRepository(object sender, RoutedEventArgs e) {
            new CloneRepoWindow(repoDB, client, this).Show();   
        }

        private void FetchExternalRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                ConsoleLogger.StatusBarUpdate("Fetching external repository", this);
                Task.Run(() => GitMethods.Fetch(repoPath, client, this));
            } else
                ConsoleLogger.UserPopup(HEADERMSG_FETCH_REPO, USERMSG_SELECTREPO);
        }

        private void PushLocalRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Task.Run(() => GitMethods.Push(repoPath, client, this));
            } else
                ConsoleLogger.UserPopup(HEADERMSG_PUSH_REPO, USERMSG_SELECTREPO);
        }

        private void PullExternalRepository(object sende, RoutedEventArgs e) { 
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Task.Run(() => GitMethods.Pull(repoPath, client, this));
            } else
                ConsoleLogger.UserPopup(HEADERMSG_PULL_REPO, USERMSG_SELECTREPO);
        }

        private void NewRepository(object sender, RoutedEventArgs e) {
            MainWindowController.NewRepositoryCommand(repoDB);
        }

        private void CommitRepository(object sender, RoutedEventArgs e) {
            if (GitMethods.IsRepo(PathLabel.Text.ToString()))
                Dispatcher.Invoke(() => MainWindowController.CommitRepositoryCommand(PathLabel.Text.ToString(), this));
            else
                ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, USERMSG_SELECTREPO);
        }

        private void RemoveRepository(object sender, RoutedEventArgs e) {
            MainWindowController.RemoveRepositoryCommand(PathLabel.Text.ToString(), repoDB, this);
        }

        private void DeleteRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) 
                Dispatcher.Invoke(() => MainWindowController.DeleteRepositoryCommand(repoPath));
            else
                ConsoleLogger.UserPopup(HEADERMSG_DELETE_REPO, USERMSG_SELECTREPO);
        }

        private void CreateNewBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Dispatcher.Invoke(() => MainWindowController.CreateNewBranchCommand(repoPath, this));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        private void ChangeBranch(object sender, RoutedEventArgs e) {

            // get repopath and clicked menuitem branch name
            string repoPath = PathLabel.Text.ToString();
            MenuItem item = sender as MenuItem;
            string branch = item.Header.ToString();

            if (repoPath != "" && branch != "") {
                MainWindowController.ChangeBranchCommand(repoPath, branch, this);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        private void RenameCurrentBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "") {
                MainWindowController.RenameCurrentBranchCommand(repoPath, this);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        private void MergeCurrentBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            MenuItem item = sender as MenuItem;
            string branch = item.Header.ToString();

            if (repoPath != "" && branch != "") {

                MainWindowController.MergeCurrentBranchCommand(repoPath, branch, this);
                
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        private void DeleteCurrentBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "") {
                MainWindowController.DeleteCurrentBranchCommand(repoPath, this);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        // trigger when selection changed in RepoListBox
        private void RepoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            // get new selected repo name
            if ((ListBoxItem)RepoListBox.SelectedItem != null) {
                string repoName = ((ListBoxItem)RepoListBox.SelectedItem).Content.ToString();
                if (repoName != "") {
                    // let running thread execute remaining code
                    newRepoChangesThreadState = RepoChangesThreadState.Aborting;
                    Task.Run(() => ChangeThreadWithNewRepo(repoName));
                }   
            }
        }

        private void ChangeThreadWithNewRepo(string repoName) {
            List<Repo> repos = repoDB.FindByName(repoName);
            if (repos != null) {

                // set new path label and clear all old repo data
                string repoPath = repos[0].Path.ToString();
                Dispatcher.Invoke(() => MainWindowUI.SetDataForNewRepo(repoName, repoPath, this));

                // start new thread which will watch new repo
                newRepoChangesThreadState = RepoChangesThreadState.New;
                string repo = "";
                Dispatcher.Invoke(() => repo = PathLabel.Text.ToString());
                repoChangesThread = new Thread(() => WaitForChangesOnRepo(repo));
                repoChangesThread.Start();

                ConsoleLogger.Success("MainWindow.RepoListBox_SelectionChanged", "WaitForChangesOnRepo thread started");
            }
        }

        // thread watching repositories
        private void AllReposListener() {
            while (true) {
                Thread.Sleep(2500);

                // refresh lisbox if there are deleted repositories
                List<string> deletedRepos = repoDB.Refresh();
                if (deletedRepos != null) {
                    Dispatcher.Invoke(() => RepoListBox.Items.Clear());
                    Dispatcher.Invoke(() => MainWindowUI.ListBoxLoad());
                }
            }
        }

        private void AddTrackedFiles(List<string> untrackedFiles, string path) {

            foreach (string file in untrackedFiles) {
                string command = "add " + '"' + file.Trim() + '"';
                Cmd.Run(command, path);
            }

            Dispatcher.Invoke(() => MainWindowUI.FilesToCommitRefresh(this));

            ConsoleLogger.StatusBarUpdate("Waiting on changes in repository", this);
        }

        // thread watching files in selected repo
        private void WaitForChangesOnRepo(string path) {
            ConsoleLogger.Info("MainWindow.WaitForChangesOnRepo", $"Called with state: {newRepoChangesThreadState.ToString()}");

            if (newRepoChangesThreadState == RepoChangesThreadState.New)
                newRepoChangesThreadState = RepoChangesThreadState.Repeating;

            while (newRepoChangesThreadState == RepoChangesThreadState.Repeating) {
                ConsoleLogger.Info("MainWindow.WaitForChangesOnRepo", $"Running state: {newRepoChangesThreadState} on {path}");
                Thread.Sleep(2500);

                // If there are untracked files in currently watched repo -> track them
                List<string> untrackedFiles = Cmd.UntrackedFiles(path);
                if (untrackedFiles != null) {
                    AddTrackedFiles(untrackedFiles, path);
                } else {
                    ConsoleLogger.Info("MainWindow.WaitForChangesOnRepo", $"No untracked files");
                }
            }

        }

        private void FilesToCommit_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (FilesToCommit.SelectedItem != null) {
                string fileName = ((ComboBoxItem)FilesToCommit.SelectedItem).Content.ToString();
                string path = PathLabel.Text.ToString();

                ConsoleLogger.StatusBarUpdate($"Showing {fileName} content", this);

                if (File.Exists($@"{path}\{fileName}")) {
                    string text = GitMethods.GetAllFileChanges(fileName, path);
                    FileContent.Text = text;

                } else {
                    ConsoleLogger.UserPopup("Error", $"File: {fileName} don't exists");
                }
                
            }
        }

        private void OpenStatistics(object sender, RoutedEventArgs e) {
            string header = ((MenuItem)sender).Header.ToString();
            new StatisticsWindow(this, client, user, repoDB, header).Show();
        }

        private void Window_Minimized(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        private void Window_Closed(object sender, RoutedEventArgs e) {
            Environment.Exit(Environment.ExitCode);
        }

        private void DragWindownOnMouseDown(object sender, MouseButtonEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    
    }
}
