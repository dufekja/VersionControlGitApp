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

using MenuItem = System.Windows.Controls.MenuItem;
using System.Collections.ObjectModel;

namespace VersionControlGitApp {
    public partial class MainWindow : Window {
        
        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));

        private readonly LocalRepoDB repoDB;
        public static User user;
        public static List<UserRepository> userRepos;

        public Collection<Thread> RunningThreadsCollection { get; set; }

        public MainWindow(string token) {
            InitializeComponent();

            RunningThreadsCollection = new Collection<Thread>();

            repoDB = new LocalRepoDB();
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            user = client.User.Current().Result;
            MainWindowUI.InitUIElements(this, user, repoDB);

            // get path from pathlabel
            string path = PathLabel.Text.ToString();

            // set branch
            MainWindowUI.ChangeCommitButtonBranch(path);

            // if there is repo then watch for changes
            if (path != "") {
                ConsoleLogger.Success("MainWindow", $"Iniciace sledovacího vlákna pro {GitMethods.GetNameFromPath(path)}");
                Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                repoChangesThread.Start();              
                RunningThreadsCollection.Add(repoChangesThread);

                // load repo branches
                Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(path, this));

                // async listener for changes in selected file
                Task.Run(() => AsyncListener());
            }   
        }

        // add already created repository to sqlite db
        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            MainWindowController.AddLocalRepositoryCommand(repoDB);
        }

        // clone repository window
        private void CloneRepository(object sender, RoutedEventArgs e) {
            new CloneRepoWindow(repoDB, client, this).Show();   
        }

        private void FetchExternalRepository(object sender, RoutedEventArgs e) {
            if (GitMethods.IsRepo(PathLabel.Text.ToString()))
                Dispatcher.Invoke(() => GitMethods.Fetch(PathLabel.Text.ToString(), client));
        }

        private void PushLocalRepository(object sender, RoutedEventArgs e) {
            if (GitMethods.IsRepo(PathLabel.Text.ToString()))
                Dispatcher.Invoke(() => GitMethods.Push(PathLabel.Text.ToString(), client));      
        }

        private void PullExternalRepository(object sende, RoutedEventArgs e) {
            if (GitMethods.IsRepo(PathLabel.Text.ToString()))
                Dispatcher.Invoke(() => GitMethods.Pull(PathLabel.Text.ToString(), client));
        }

        private void NewRepository(object sender, RoutedEventArgs e) {
            MainWindowController.NewRepositoryCommand(repoDB);
        }

        private void CommitRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            Dispatcher.Invoke(() => MainWindowController.CommitRepositoryCommand(repoPath, this));
        }

        private void RemoveRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            MainWindowController.RemoveRepositoryCommand(repoPath, repoDB, this);
        }

        private void DeleteRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "" && GitMethods.IsRepo(repoPath) && Directory.Exists(repoPath)) {
                
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"Do you want to delete {GitMethods.GetNameFromPath(repoPath)} ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes) {
                    Directory.Delete(repoPath);
                    ConsoleLogger.UserPopup("Delete Confirmation", $"{GitMethods.GetNameFromPath(repoPath)} deleted");
                }
            }
        }

        private void CreateNewBranch(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "") {
                List<string> lines = GitMethods.GetBranches(repoPath);

                try {
                    if (lines[0] != null) {
                        new BranchEditWindow(lines, repoPath, "create", this).Show();
                    }
                } catch { }
            } else {
                ConsoleLogger.UserPopup("Branch", "Repository must be selected first");
            }
        }

        private void ChangeBranch(object sender, RoutedEventArgs e) {
            if (PathLabel.Text.ToString() != "") {
                MenuItem item = sender as MenuItem;
                string name = item.Header.ToString();
                string repoPath = PathLabel.Text.ToString();

                MainWindowController.ChangeBranchCommand(name, repoPath);
                Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, this));
                Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath));
            } else {
                ConsoleLogger.UserPopup("Branch", "Repository must be selected first");
            }
        }

        private void RenameCurrentBranch(object sender, RoutedEventArgs e) {
            if (PathLabel.Text.ToString() != "") {
                string repoPath = PathLabel.Text.ToString();
                string currentBranch = GitMethods.GetCurrentBranch(repoPath);

                List<string> lines = GitMethods.GetBranches(PathLabel.Text.ToString());

                try {
                    if (lines[0] != null) {
                        if (currentBranch != "master") {
                            new BranchEditWindow(lines, PathLabel.Text.ToString(), "rename", this).Show();
                        } else
                            ConsoleLogger.UserPopup("Branch rename", "Can't rename branch master");
                    }
                } catch { }
            } else {
                ConsoleLogger.UserPopup("Branch", "Repository must be selected first");
            }
        }

        private void MergeCurrentBranch(object sender, RoutedEventArgs e) {

            if (PathLabel.Text.ToString() != "") {
                string repoPath = PathLabel.Text.ToString();
                string currentBranch = GitMethods.GetCurrentBranch(repoPath);
                List<string> lines = GitMethods.GetBranches(PathLabel.Text.ToString());
                MenuItem item = sender as MenuItem;
                string branch = item.Header.ToString();

                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"Do you want to merge {currentBranch} to {branch} ?", "Merge confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes) {
                    MainWindowController.MergeCurrentBranchCommand(branch, currentBranch, repoPath);
                    Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, this));
                    Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath));
                }
            } else {
                ConsoleLogger.UserPopup("Branch", "Repository must be selected first");
            }
        }

        private void DeleteCurrentBranch(object sender, RoutedEventArgs e) {
            if (PathLabel.Text.ToString() != "") {
                string repoPath = PathLabel.Text.ToString();
                string currentBranch = GitMethods.GetCurrentBranch(repoPath);
                bool deleted = false;

                if (currentBranch != "master") {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"Do you want to delete {currentBranch} ?", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                    if (messageBoxResult == MessageBoxResult.Yes && currentBranch != "master") {
                        Cmd.Run("checkout master", repoPath);
                        bool success = Cmd.Run($"branch -D {currentBranch}", repoPath);
                        if (success)
                            deleted = true;
                    }

                    if (deleted)
                        ConsoleLogger.UserPopup("Delete Confirmation", $"{currentBranch} deleted");
                    else
                        ConsoleLogger.UserPopup("Delete Confirmation", "There was an error");
                } else {
                    ConsoleLogger.UserPopup("Delete branch", "Can't delete branch master");
                }

                Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, this));
                Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath));
            } else {
                ConsoleLogger.UserPopup("Branch", "Repository must be selected first");
            }
        }

        // trigger when selection changed in RepoListBox
        private void RepoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            // get new selected repo name
            string repoName = "";
            if ((ListBoxItem)RepoListBox.SelectedItem != null)
                repoName = ((ListBoxItem)RepoListBox.SelectedItem).Content.ToString();

            if (repoName != "") {
                // PathLabel change
                List<Repo> repos = repoDB.FindByName(repoName);
                if (repos != null && repos.Count > 0) {
                    string path = repos[0].Path.ToString();
                    PathLabel.Text = path;

                    if (RunningThreadsCollection.Count > 2 || RunningThreadsCollection != null) {

                        // delete all running threads
                        AbortWasteThreads();

                        // start new repo watching thread
                        Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                        repoChangesThread.Start();
                        RunningThreadsCollection.Add(repoChangesThread);
                        ConsoleLogger.Success("MainWindow.RepoListBox_SelectionChanged", "WaitForChangesOnRepo thread started");
                    }
                }
            }
        }

        // thread watching repositories
        private void AsyncListener() {
            while (true) {
                Thread.Sleep(1000);

                // delete removed folders from db
                List<string> deletedRepos = repoDB.Refresh();
                if (deletedRepos != null) {
                    Dispatcher.Invoke(() => RepoListBox.Items.Clear());
                    Dispatcher.Invoke(() => MainWindowUI.ListBoxLoad());
                }
            }
        }

        private void AddTrackedFiles(string path, List<string> untrackedFiles) {
            bool state = true;

            foreach (string file in untrackedFiles) {
                string command = "add " + '"' + file.Trim() + '"';
                state = Cmd.Run(command, path);
                ConsoleLogger.Success("MainWindow.AddTrackedFiles", $"File: {file} now tracked");
            }

            if (state == true) {

                AbortWasteThreads();

                Dispatcher.Invoke(() => MainWindowUI.FilesToCommitRefresh(path, this));

                Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                repoChangesThread.Start();
                RunningThreadsCollection.Add(repoChangesThread);

                ConsoleLogger.Success("MainWindow.AddTrackedFiles", "WaitForChangesOnRepo thread started");
            }

        }

        // thread watching files in selected repo
        private void WaitForChangesOnRepo(string path) {
            while (true) {
                Thread.Sleep(1000);

                // If untracked files in currently watched repo -> track them
                List<string> untrackedFiles = Cmd.UntrackedFiles(path);
                if (untrackedFiles != null) {
                    Thread addTrackedFilesThread = new Thread(() => AddTrackedFiles(path, untrackedFiles));
                    addTrackedFilesThread.Start();
                    RunningThreadsCollection.Add(addTrackedFilesThread);
                    break;
                }
            }
        }

        private void FilesToCommit_SelectionChanged(object sender, SelectionChangedEventArgs e) {

            if (FilesToCommit.SelectedItem != null) {
                string fileName = ((ComboBoxItem)FilesToCommit.SelectedItem).Content.ToString();
                string path = PathLabel.Text.ToString();

                if (File.Exists($@"{path}\{fileName}")) {
                    string text = GitMethods.GetAllFileChanges(fileName, path);
                    FileContent.Text = text;
                } else {
                    ConsoleLogger.Popup("MainWindow", $"File: {fileName} no exists");
                }
                
            }
        }

        public void AbortWasteThreads() {
            if (RunningThreadsCollection.Count > 2) {
                foreach (Thread t in RunningThreadsCollection) {
                    ConsoleLogger.Info("MainWindow.AbortWasteThreads", t.ThreadState.ToString());
                }
            }
        }

        private void OpenStatistics(object sender, RoutedEventArgs e) {
            new StatisticsWindow(this, client, repoDB).Show();
        }

        private void Window_Minimized(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        private void Window_Closed(object sender, RoutedEventArgs e) {
            Environment.Exit(Environment.ExitCode);
        }

        private void DragWindownOnMouseDown(object sender, MouseButtonEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    
    }
}
