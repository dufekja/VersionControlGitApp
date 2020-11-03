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

namespace VersionControlGitApp {
    public partial class MainWindow : Window {

        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));

        private readonly LocalRepoDB repoDB;
        public static User user;
        public static List<UserRepository> userRepos;

        public MainWindow(string token) {
            InitializeComponent();

            // TODO -> synchonizace více pc pomocí stejného tokenu
            // TODO -> podpora klávesových zkratek (settings)
            // TODO-> funkce status vybraného repozitáře - vytáhnout z konzole output
            // TODO -> hezčí klonovací okno
            // TODO -> stáhnout uživatelské repozitáře a udělat v clone oknu výběr z nich (předvyplnění url)
            // TODO -> výpis v okně repozitářů lokálních
            // TODO -> výpis v okně externích repozitářů
            // TODO -> sledovat změny v lokálním repozitáři
            // TODO -> základní práce s vybraným repozitářem (commit, branches)
            // TODO -> projet celou db a kouknout jestli existují foldery -> smazat ty co nejsou

            repoDB = new LocalRepoDB();
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            user = client.User.Current().Result;

            MainWindowUI.InitUIElements(this, user, repoDB);



            string path = PathLabel.Text.ToString();
            if (path != null) {
                Task.Run(() => GitMethods.WaitForChangesOnRepo(this, path));
            }

            Task.Run(() => AsyncListener());

        }

        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string res = $"{result}";
            string path = fbd.SelectedPath;

            if (res == "OK") {
                bool ok = GitMethods.AddLocalRepo(path, repoDB);
                Console.WriteLine($"\n\n{ok}\n\n");
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
                    /* Task.Run(() => GitMethods.WaitForChangesOnRepo(this, path));

                    FilesToCommit.Items.Clear();
                    List<string> filesForCommit = Cmd.FilesForCommit(path);
                    if (filesForCommit.Count > 0) {
                        foreach (string file in filesForCommit) {
                            FilesToCommit.Items.Add(file);
                        }
                    }*/
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
                    Console.WriteLine("\n\nnekdo smazal repo omg brrr");
                }

            }
        }

    }
}
