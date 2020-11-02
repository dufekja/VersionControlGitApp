using Octokit;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;

namespace VersionControlGitApp {
    public partial class MainWindow : Window {

        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));

        private readonly LocalRepoDB repoDB;
        public static User user;
        public static List<UserRepository> userRepos;

        public List<Task> RunningTasks { get; set; }

        public MainWindow(string token) {
            InitializeComponent();

            RunningTasks = new List<Task>();


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
            UserName.Text = user.Name;
 
            InitialComboboxLoad();
            LoadUserAvatar();

            string path = PathLabel.Text.ToString();
            if (path != null) {
                var t = Task.Run(() => GitMethods.WaitForChangesOnRepo(this, path));
                RunningTasks.Add(t);
            }
        }

        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();
                string res = $"{result}";
                string path = fbd.SelectedPath;

                if (res == "OK") {
                    bool ok = GitMethods.AddLocalRepo(path, repoDB);

                    if (ok) {
                        LoadPathLabel(path);
                    }
                } 
            } 
        }

        private void CloneRepository(object sender, RoutedEventArgs e) {
            CloneRepoWindow window = new CloneRepoWindow(repoDB, client, this);
            window.Show();   
        }

        private void NewRepository(object sender, RoutedEventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();
                string res = $"{result}";
                string path = fbd.SelectedPath;

                if (res == "OK" && !GitMethods.IsRepo(path)) {
                    GitMethods.Init(path, repoDB);

                    LoadPathLabel(path);
                }
            }
        }

        private void RepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string repoName = ((ComboBoxItem)RepoComboBox.SelectedItem).Content.ToString();
            if (repoName != "") {

                // PathLabel change
                List<Repo> repos = repoDB.FindByName(repoName);
                if (repos != null && repos.Count != 0) {
                    string path = repos[0].Path.ToString();
                    PathLabel.Text = path;

                    Cmd.KillAllWaitingTasks(this);

                    // watch selected repo for changes
                    Task t = Task.Run(() => GitMethods.WaitForChangesOnRepo(this, path));
                    RunningTasks.Add(t);

                    FilesToCommit.Items.Clear();
                    List<string> filesForCommit = Cmd.FilesForCommit(path);
                    if (filesForCommit.Count > 0) {
                        foreach (string file in filesForCommit) {
                            FilesToCommit.Items.Add(file);
                        }
                    }
                }
            }
        }

        private void LoadUserAvatar() {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(user.AvatarUrl);
            bi.EndInit();
            UserImage.Source = bi;
        }

        private void LoadPathLabel(string path) {
            ComboBoxItem item = new ComboBoxItem {
                Content = GitMethods.GetNameFromPath(path),
                IsSelected = true
            };

            RepoComboBox.Items.Add(item);
            PathLabel.Text = path;
        }

        private void InitialComboboxLoad() {
            List<Repo> localRepos = repoDB.ReadDB();
            bool isSelected = false;

            foreach (Repo repo in localRepos) {
                ComboBoxItem item = new ComboBoxItem {
                    Content = repo.Name
                };

                if (isSelected == false) {
                    item.IsSelected = true;
                    PathLabel.Text = repo.Path;
                    isSelected = true;
                }

                RepoComboBox.Items.Add(item);
            }
        }

    }
}
