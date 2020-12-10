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

namespace VersionControlGitApp {
    public partial class MainWindow : Window {

        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));

        private readonly LocalRepoDB repoDB;
        public static User user;
        public static List<UserRepository> userRepos;

        public List<Thread> RunningThreadsList { get; set; }

        public MainWindow(string token) {
            InitializeComponent();

            // TODO -> předělat combobox s repos na list nebo vypsat změny pod
            // TODO -> barevné logování do externí konzole
            // TODO -> větve
            // TODO -> porovnávání změn 
            // TODO -> podpora klávesových zkratek (settings)
            // TODO -> hezčí okno pro token
            // TODO -> synchonizace více pc pomocí stejného tokenu

            RunningThreadsList = new List<Thread>();

            repoDB = new LocalRepoDB();
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            user = client.User.Current().Result;

            MainWindowUI.InitUIElements(this, user, repoDB);

            string path = PathLabel.Text.ToString();
            if (path != null) {
                Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                repoChangesThread.Start();
                ConsoleLogger.Success("MainWindow", "Iniciace vlákna");                
                RunningThreadsList.Add(repoChangesThread);
            }

            // async task for deleting files
            Task.Run(() => AsyncListener());
        }

        // add already created repository to sqlite db
        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string res = $"{result}";
            string path = fbd.SelectedPath;

            if (res == "OK") {
                bool ok = GitMethods.AddLocalRepo(path, repoDB);
                if (ok == true)
                    MainWindowUI.LoadPathLabel(path);
            }
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
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string res = $"{result}";
            string repoPath = fbd.SelectedPath;

            if (res == "OK" && !GitMethods.IsRepo(repoPath)) {
                GitMethods.Init(repoPath, repoDB);
                MainWindowUI.LoadPathLabel(repoPath);
            }
        }

        private void CommitRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();

            if (repoPath != "" && GitMethods.IsRepo(repoPath)) {
                string summary = CommitSummary.Text.ToString();
                string desc = CommitDescription.Text.ToString();
                GitMethods.Commit(repoPath, summary, desc);

                CommitSummary.Text = "";
                CommitDescription.Text = "";
            }
        }

        private void RemoveRepository(object sender, RoutedEventArgs e) {
            string repoPath = PathLabel.Text.ToString();
            if (repoPath != "" && GitMethods.IsRepo(repoPath)) {
                repoDB.DeleteByPath(repoPath);

                Dispatcher.Invoke((Action)(() => RepoListBox.Items.Clear()));
                Dispatcher.Invoke((Action)(() => MainWindowUI.ListBoxLoad()));
                PathLabel.Text = "";

                ConsoleLogger.Popup("MainWindow", $"Removerepo - {repoPath}");
            }
            
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
            List<string> lines = GitMethods.GetBranches(PathLabel.Text.ToString());

            try {
                if (lines[0] != null) {
                    new BranchEditWindow(lines, PathLabel.Text.ToString()).Show();
                }
            } catch {

            }
        }

        private void RenameCurrentBranch(object sender, RoutedEventArgs e) {
            // TODO -> RenameCurrentBranch
            string repoPath = PathLabel.Text.ToString();
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);

            ConsoleLogger.Popup("MainWindow", $"rename branch {currentBranch}");
        }

        private void MergeCurrentBranch(object sender, RoutedEventArgs e) {
            // TODO -> MergeCurrentBranch

            string repoPath = PathLabel.Text.ToString();
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);
            ConsoleLogger.Popup("MainWindow", $"merge branch {currentBranch} to other branch");
        }

        private void DeleteCurrentBranch(object sender, RoutedEventArgs e) {
            // TODO -> DeleteCurrentBranch
            string repoPath = PathLabel.Text.ToString();
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);
            bool deleted = false;

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
                ConsoleLogger.UserPopup("Delete Confirmation", "Something goes brrrrrr");
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

                    // watch selected repo for changes
                    if (RunningThreadsList.Count > 0) {

                        // delete all running threads
                        ConsoleLogger.Warning("MainWindow", "Mazání všech vláken");
                        foreach (Thread t in RunningThreadsList) {
                            t.Abort();
                        }

                        // start new repo watching thread
                        Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                        repoChangesThread.Start();
                        ConsoleLogger.Success("MainWindow", "Start nového vlákna");
                        RunningThreadsList.Add(repoChangesThread);
                    }
                }
            }
        }

        private void AsyncListener() {
            while (true) {
                Thread.Sleep(1000);

                // delete removed folders from db
                List<string> deletedRepos = repoDB.Refresh();
                if (deletedRepos != null) {
                    Dispatcher.Invoke((Action)(() => RepoListBox.Items.Clear()));
                    Dispatcher.Invoke((Action)(() => MainWindowUI.ListBoxLoad()));
                }
            }
        }

        private void AddTrackedFiles(string path) {
            List<string> untrackedFiles = Cmd.UntrackedFiles(path);
            bool state = true;

            foreach (string file in untrackedFiles) {
                string command = $@"add {file.Trim()}";
                state = Cmd.Run(command, path);
                ConsoleLogger.Success("MainWindow", $"File: {file} now tracked");
            }

            if (state == true) {
                if (RunningThreadsList[0] != null) {
                    try {
                        foreach (Thread t in RunningThreadsList) {
                            t.Abort();
                        }
                    } catch {
                        ConsoleLogger.Error("MainWindow", "Abort všech vláken selhal");
                    } 
                }

                Dispatcher.Invoke((Action)(() => MainWindowUI.FilesToCommitRefresh(path)));
                Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                repoChangesThread.Start();
                ConsoleLogger.Success("MainWindow", "Start nového vlákna");
                RunningThreadsList.Add(repoChangesThread);
            }
        }

        private void WaitForChangesOnRepo(string path) {
            while (true) {
                Thread.Sleep(1000);

                // If untracked files in currently watched repo -> track them
                List<string> untrackedFiles = Cmd.UntrackedFiles(path);
                if (untrackedFiles != null) {
                    Task.Run(() => AddTrackedFiles(path));
                    break;
                }
            }
        }

        private void FilesToCommit_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (FilesToCommit.SelectedItem != null) {
                string fileName = FilesToCommit.SelectedItem.ToString();
                FileContent.Text = File.ReadAllText($@"{PathLabel.Text}\{fileName}");
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            Environment.Exit(Environment.ExitCode);
        }
    }
}
