using Octokit;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string newRepoName { get; set; }

        public MainWindow(string token) {
            InitializeComponent();

            newRepoName = "";

            // TODO -> synchonizace více pc pomocí stejného tokenu
            // TODO -> podpora klávesových zkratek (settings)
            // TODO-> funkce status vybraného repozitáře - vytáhnout z konzole output
            // TODO -> hezčí klonovací okno
            // TODO -> stáhnout uživatelské repozitáře a udělat v clone oknu výběr z nich (předvyplnění url)
            // TODO -> výpis v okně repozitářů lokálních
            // TODO -> výpis v okně externích repozitářů
            // TODO -> sledovat změny v lokálním repozitáři
            // TODO -> základní práce s vybraným repozitářem (commit, branches)

            repoDB = new LocalRepoDB();
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            user = client.User.Current().Result;
            UserName.Text = user.Name;
            LoadUserAvatar();

            // get all of user's repositories info
            // userRepos = GithubController.GetAllRepos(client);

            List<Repo> localRepos = repoDB.ReadDB();

            bool isSelected = false;
            string selectedPath = "";
            foreach (Repo repo in localRepos) {
                ComboBoxItem item = new ComboBoxItem {
                    Content = repo.Name
                };

                if (!isSelected)
                    item.IsSelected = true;
                    selectedPath = repo.Path;
                    isSelected = true;

                RepoComboBox.Items.Add(item);
            }
            PathLabel.Text = selectedPath;
        }

        /// <summary>
        /// Add local repository to db using button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddLocalRepository(object sender, RoutedEventArgs e) {
            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();
                string res = $"{result}";
                string path = fbd.SelectedPath;

                if (res == "OK") {
                    bool ok = GitMethods.AddLocalRepo(path, repoDB);

                    if (ok) {
                        ComboBoxItem item = new ComboBoxItem {
                            Content = GitMethods.GetNameFromPath(path),
                            IsSelected = true
                        };

                        RepoComboBox.Items.Add(item);
                        PathLabel.Text = path;
                    }
                } 
            } 
        }

        /// <summary>
        /// Show clone repository window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

                    Console.WriteLine("Gut init");

                    string name = GitMethods.GetNameFromPath(path);
                    ComboBoxItem item = new ComboBoxItem {
                        Content = name,
                        IsSelected = true
                    };

                    RepoComboBox.Items.Add(item);
                    PathLabel.Text = path;
                }
            }
        }

        private void RepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string repoName = ((ComboBoxItem)RepoComboBox.SelectedItem).Content.ToString();
            Console.WriteLine(repoName);
            if (repoName != "") {
                List<Repo> repos = repoDB.FindByName(repoName);
                if (repos != null && repos.Count != 0) {
                    string path = repos[0].Path.ToString();
                    PathLabel.Text = path;
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
    }
}
