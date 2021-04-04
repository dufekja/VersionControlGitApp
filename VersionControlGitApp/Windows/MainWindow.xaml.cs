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
using System.Linq;
using System.Windows.Media;
using System.Windows.Documents;

namespace VersionControlGitApp {
    public partial class MainWindow : Window {
        
        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));
        private readonly LocalRepoDB repoDB = new LocalRepoDB();
        private readonly PrivateTokenDB tokenDB;
        public static List<UserRepository> userRepos;
        public static User user;
        public static string loggedUser;

        public static Thread repoChangesThread;
        public static RepoChangesThreadState newRepoChangesThreadState = RepoChangesThreadState.New;

        /// <summary>
        /// Main window constructor
        /// </summary>
        /// <param name="token">Token value</param>
        /// <param name="_tokenDB">Instance of token database</param>
        public MainWindow(string token, PrivateTokenDB _tokenDB) {
            InitializeComponent();

            // init databases
            tokenDB = _tokenDB;
            repoDB.InitDB();
            loggedUser = SystemInformation.UserName;
            
            // auth user using token
            client = GithubController.Authenticate(client, token, this);

            // get user based on token and set name + picture
            try {
                user = client.User.Current().Result;
            } catch { user = null; ISGITHUBUSER = false; }

            MainWindowUI.InitUIElements(this, user, repoDB, loggedUser);

            // get path from pathlabel
            string path = PathLabel.Text.ToString();

            // set global git name and email
            //GithubController.SetGlobals(user, path);

            // if there is repo then watch for changes
            if (path != "") {

                // branches and headers enable
                MainWindowUI.ChangeCommitButtonBranch(path, this);
                MainWindowUI.ChangeAllBranchToolsStatus(path, this);
                MainWindowUI.RepoStatsBlocked(path, this);

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

        /// <summary>
        /// add local repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            MainWindowController.AddLocalRepositoryCommand(repoDB, this);
        }

        /// <summary>
        /// clone repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void CloneRepository(object sender, RoutedEventArgs e) {
            try {
                var repoList = client.Repository.GetAllForCurrent().Result;
                new CloneRepoWindow(repoDB, client, this).Show();
            } catch {
                ConsoleLogger.UserPopup(HEADERMSG_PULL_REPO, "Unable to clone due to token permissions");
            }
        }

        /// <summary>
        /// fetch repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void FetchExternalRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                ConsoleLogger.StatusBarUpdate("Fetching external repository", this);
                Task.Run(() => GitMethods.Fetch(repoPath, client, this));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_FETCH_REPO, USERMSG_SELECTREPO);
            }     
        }

        /// <summary>
        /// push repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void PushLocalRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Task.Run(() => GitMethods.Push(repoPath, client, this));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_PUSH_REPO, USERMSG_SELECTREPO);
            }
        }

        /// <summary>
        /// pull repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void PullExternalRepository(object sende, RoutedEventArgs e) { 
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Task.Run(() => GitMethods.Pull(repoPath, client, this));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_PULL_REPO, USERMSG_SELECTREPO);
            }
                
        }

        /// <summary>
        /// new repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void NewRepository(object sender, RoutedEventArgs e) {
            MainWindowController.NewRepositoryCommand(repoDB);
        }

        /// <summary>
        /// commit repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void CommitRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Dispatcher.Invoke(() => MainWindowController.CommitRepositoryCommand(repoPath, this));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, USERMSG_SELECTREPO);
            }
        }

        /// <summary>
        /// remove repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void RemoveRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                MainWindowController.RemoveRepositoryCommand(repoPath, repoDB, this);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, USERMSG_SELECTREPO);
            }   
        }

        /// <summary>
        /// delete repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void DeleteRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Dispatcher.Invoke(() => MainWindowController.DeleteRepositoryCommand(repoPath));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_DELETE_REPO, USERMSG_SELECTREPO);
            }
                
        }

        /// <summary>
        /// create new branch action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void CreateNewBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (GitMethods.IsRepo(repoPath)) {
                Dispatcher.Invoke(() => MainWindowController.CreateNewBranchCommand(repoPath, this));
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        /// <summary>
        /// change currently selected branch action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
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

        /// <summary>
        /// rename currently selected branch
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void RenameCurrentBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "") {
                MainWindowController.RenameCurrentBranchCommand(repoPath, this);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        /// <summary>
        /// merge currently selected branch into selected
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
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

        /// <summary>
        /// delete currently selected branch
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void DeleteCurrentBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "") {
                MainWindowController.DeleteCurrentBranchCommand(repoPath, this);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, USERMSG_SELECTREPO);
            }
        }

        /// <summary>
        /// trigger when selection changed in RepoListBox
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
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

        /// <summary>
        /// change repo watching thread to watch new repository
        /// </summary>
        /// <param name="repoName">Repository name</param>
        private void ChangeThreadWithNewRepo(string repoName) {
            List<Repo> repos = repoDB.FindByName(repoName);
            if (repos != null) {

                // set new path label and clear all old repo data
                string repoPath = repos[0].Path.ToString();
                Dispatcher.Invoke(() => MainWindowUI.SetDataForNewRepo(repoName, repoPath, this));
                MainWindowUI.ChangeAllBranchToolsStatus(repoPath, this);
                MainWindowUI.RepoStatsBlocked(repoPath, this);

                // start new thread which will watch new repo
                newRepoChangesThreadState = RepoChangesThreadState.New;

                // watch new repo based on new pathlabel
                string repo = "";
                Dispatcher.Invoke(() => repo = PathLabel.Text.ToString());
                repoChangesThread = new Thread(() => WaitForChangesOnRepo(repo));
                repoChangesThread.Start();

                ConsoleLogger.Success("MainWindow.RepoListBox_SelectionChanged", "WaitForChangesOnRepo thread started");
            }
        }

        /// <summary>
        /// thread watching repositories
        /// </summary>
        private void AllReposListener() {
            while (true) {
                Thread.Sleep(2500);

                // refresh lisbox if there are deleted repositories
                List<string> deletedRepos = repoDB.Refresh(loggedUser);
                if (deletedRepos != null) {
                    Dispatcher.Invoke(() => RepoListBox.Items.Clear());
                    Dispatcher.Invoke(() => MainWindowUI.ListBoxLoad());
                    Dispatcher.Invoke(() => MainWindowUI.ClearRepoPathOnEmpty(this));
                }
            }
        }

        /// <summary>
        /// add tracked files using git add
        /// </summary>
        /// <param name="untrackedFiles">List of all untracked files</param>
        /// <param name="path">Current repository path</param>
        private void AddTrackedFiles(List<string> untrackedFiles, string path) {

            // add each untracked file to index of watched files
            foreach (string file in untrackedFiles) {
                string command = "add " + '"' + file.Trim() + '"';
                Cmd.Run(command, path);
            }

            try {
                // refresh UI with new files to commit
                Dispatcher.Invoke(() => MainWindowUI.FilesToCommitRefresh(this));
            } catch {}

            ConsoleLogger.StatusBarUpdate("Waiting on changes in repository", this);
        }

        /// <summary>
        /// thread watching files in selected repo
        /// </summary>
        /// <param name="path">Current repository path</param>
        private void WaitForChangesOnRepo(string path) {
            ConsoleLogger.Info("MainWindow.WaitForChangesOnRepo", $"Called with state: {newRepoChangesThreadState}");

            // change global thread state
            if (newRepoChangesThreadState == RepoChangesThreadState.New) {
                newRepoChangesThreadState = RepoChangesThreadState.Repeating;
            }
                
            while (newRepoChangesThreadState == RepoChangesThreadState.Repeating) {
                // wait for 2.5 seconds
                Thread.Sleep(2500);

                // if there are untracked files in currently watched repo -> track them
                List<string> untrackedFiles = Cmd.UntrackedFiles(path);
                if (untrackedFiles != null) {
                    AddTrackedFiles(untrackedFiles, path);
                } else {
                    ConsoleLogger.Info("MainWindow.WaitForChangesOnRepo", $"No untracked files");
                }
            }

        }

        /// <summary>
        /// run if user select new file contents to show
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void FilesToCommit_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (FilesToCommit.SelectedItem != null) {
                
                // get selected filename 
                string fileName = ((ComboBoxItem)FilesToCommit.SelectedItem).Content.ToString();
                string path = PathLabel.Text.ToString();

                // remove tags from name
                if (fileName.Contains('"')) {
                    fileName = fileName.Substring(1, fileName.Length - 2);
                }

                ConsoleLogger.StatusBarUpdate($"Showing {fileName} content", this);

                // added file exists check
                if (File.Exists($@"{path}\{fileName}")) {

                    // gel all file changes
                    List<string[]> textInput = GitMethods.GetAllFileChanges(fileName, path);

                    FileContent.Blocks.Clear();

                    // run trought file changes
                    var color = Brushes.GhostWhite;
                    foreach (var item in textInput) {
                        // choose color based on symbol
                        if (item[2] == "-") {
                            color = Brushes.Red;
                        } else if (item[2] == "+") {
                            color = Brushes.Green;
                        } else {
                            color = Brushes.GhostWhite;
                        }

                        // create richboxtext elements 
                        Paragraph paragraph = new Paragraph() { Margin = new Thickness(2) };
                        Run lineNumber = new Run() { Text = $"{item[0]}", Foreground = Brushes.GhostWhite };
                        Run textFormat = new Run() {Text = $"{item[1]}", Foreground = color};

                        // add formats to paragraph and insert into richbox
                        paragraph.Inlines.Add(lineNumber);
                        paragraph.Inlines.Add(textFormat);
                        FileContent.Blocks.Add(paragraph);

                    }

                } else {
                    ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, $"File: {fileName} don't exists");
                    FilesToCommit.SelectedItem = null;
                }
                
            }
        }

        /// <summary>
        /// Open statistics window action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void OpenStatistics(object sender, RoutedEventArgs e) {
            string header = ((MenuItem)sender).Header.ToString();
            string repoPath = PathLabel.Text.ToString();

            // check clicked header name
            if (header == "Repository") {
                if (repoPath != "") {
                    // open statistic for repository
                    new StatisticsWindow(this, client, user, repoDB, header, repoPath).Show();
                } else {
                    ConsoleLogger.UserPopup("Repository statistics", "You need to select repository first");
                }
                    
            } else {
                // open statistics for user
                new StatisticsWindow(this, client, user, repoDB, header, repoPath).Show();
            }
                
        }

        /// <summary>
        /// Update private token action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void UpdatePrivateToken(object sender, RoutedEventArgs e) {
            string token = TokenTextBox.Text.ToString();
            TokenTextBox.Text = "";

            if (token != "" && token.Length == 40) {
                // update token in database
                bool updated = GitMethods.UpdatePrivateTokenCommand(token, tokenDB);

                if (updated) {
                    // authenticate user with new token
                    Dispatcher.Invoke(() => client = GithubController.Authenticate(client, token, this));
                    ConsoleLogger.UserPopup("Private token", "Private token updated and set to active");

                    // check user token validity and update UIelements
                    try {
                        user = client.User.Current().Result;
                        MainWindowUI.InitUIElements(this, user, repoDB, loggedUser);
                    } catch { user = null; }
                }

            } else {
                ConsoleLogger.UserPopup("Private token", "Please insert valid token");
            }    
        }

        /// <summary>
        /// Minimize window action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void Window_Minimized(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Close window action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void Window_Closed(object sender, RoutedEventArgs e) {
            Environment.Exit(Environment.ExitCode);
        }

        /// <summary>
        /// Drag window on mouse down action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void DragWindownOnMouseDown(object sender, MouseButtonEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    
    }
}
