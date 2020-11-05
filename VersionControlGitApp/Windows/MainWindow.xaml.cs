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

namespace VersionControlGitApp {
    public partial class MainWindow : Window {

        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));

        private readonly LocalRepoDB repoDB;
        public static User user;
        public static List<UserRepository> userRepos;

        public List<Thread> RunningThreadsList { get; set; }

        public MainWindow(string token) {
            InitializeComponent();

            // TODO -> pčedělat combobox s repos na list nebo vypsat změny pod
            // TODO -> barevné logování do externí konzole
            // TODO -> základní práce s vybraným repozitářem (commit, branches)
            // TODO -> podpora klávesových zkratek (settings)
            // TODO -> heyží okno pro token
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
                ConsoleLogger.Success("start - initial files tracker thread");                
                RunningThreadsList.Add(repoChangesThread);
            }

            // async task for deleting files
            Task.Run(() => AsyncListener());
        }

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

        private void CloneRepository(object sender, RoutedEventArgs e) {
            CloneRepoWindow window = new CloneRepoWindow(repoDB, client, this);
            window.Show();   
        }

        private void NewRepository(object sender, RoutedEventArgs e) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string res = $"{result}";
            string path = fbd.SelectedPath;

            if (res == "OK" && !GitMethods.IsRepo(path)) {
                GitMethods.Init(path, repoDB);
                MainWindowUI.LoadPathLabel(path);
            }
        }

        private void RepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string repoName = "";
            if ((ComboBoxItem)RepoComboBox.SelectedItem != null)
                repoName = ((ComboBoxItem)RepoComboBox.SelectedItem).Content.ToString();
            if (repoName != "") {
                // PathLabel change
                List<Repo> repos = repoDB.FindByName(repoName);
                if (repos != null && repos.Count > 0) {
                    string path = repos[0].Path.ToString();
                    PathLabel.Text = path;

                    // watch selected repo for changes
                    if (RunningThreadsList.Count > 0) {
                        ConsoleLogger.Warning("Mazání všech vláken");
                        foreach (Thread t in RunningThreadsList) {
                            t.Abort();
                        }

                        Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                        repoChangesThread.Start();
                        ConsoleLogger.Success("start vlákna na základě přepnutí selekce");
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
                    Dispatcher.Invoke((Action)(() => RepoComboBox.Items.Clear()));
                    Dispatcher.Invoke((Action)(() => MainWindowUI.ComboBoxLoad()));
                }
            }
        }

        private void AddTrackedFiles(string path) {
            List<string> untrackedFiles = Cmd.UntrackedFiles(path);
            bool state = true;

            foreach (string file in untrackedFiles) {
                string command = $@"add {file.Trim()}";
                state = Cmd.Run(command, path);
                ConsoleLogger.Success($"File: {file} now tracked");
            }

            if (state == true) {
                if (RunningThreadsList.Count > 0) {
                    Console.WriteLine("/n mazání všech vláken");
                    foreach (Thread t in RunningThreadsList) {
                        t.Abort();
                    }
                }

                Dispatcher.Invoke((Action)(() => MainWindowUI.FilesToCommitRefresh(path)));
                Thread repoChangesThread = new Thread(() => WaitForChangesOnRepo(path));
                repoChangesThread.Start();
                ConsoleLogger.Success("start vlákna na základě nových změn");
                RunningThreadsList.Add(repoChangesThread);
            }
        }

        private void WaitForChangesOnRepo(string path) {
            while (true) {
                Thread.Sleep(2000);

                List<string> untrackedFiles = Cmd.UntrackedFiles(path);
                if (untrackedFiles != null) {
                    Task.Run(() => AddTrackedFiles(path));
                    break;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            Environment.Exit(Environment.ExitCode);
        }
    }
}
