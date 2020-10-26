using Octokit;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;

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

            repoDB = new LocalRepoDB();
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            //user = client.User.Current().Result;


            // get all of user's repositories info
            // userRepos = GithubController.GetAllRepos(client);

            List<Repo> localRepos = repoDB.ReadDB();

            bool isSelected = false;
            foreach (Repo repo in localRepos) {
                ComboBoxItem item = new ComboBoxItem {
                    Content = repo.Name
                };

                if (!isSelected)
                    item.IsSelected = true;
                    isSelected = true;

                RepoComboBox.Items.Add(item);
            }
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
                
                if (res == "OK") {
                    GitMethods.AddLocalRepo(fbd.SelectedPath, repoDB);
                } 
            }
        }

        /// <summary>
        /// Show clone repository window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloneRepository(object sender, RoutedEventArgs e) {
            CloneRepoWindow window = new CloneRepoWindow(repoDB, client);
            window.Show();
        }
    }
}
