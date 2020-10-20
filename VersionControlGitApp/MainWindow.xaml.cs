using Octokit;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;

namespace VersionControlGitApp {
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));

        private LocalRepoDB repoDB;


        public static User user;
        public static List<UserRepository> userRepos;

        public MainWindow(string token) {
            InitializeComponent();

            repoDB = new LocalRepoDB();
            repoDB.InitDB();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            user = client.User.Current().Result;

            Console.WriteLine($"\n Token{token} \n");


            // get all of user's repositories info
            //userRepos = GithubController.GetAllRepos(client);

            //userRepos[0].Status(path);

            /** Init flow
             * Connect to db
             * Load all saved repos to Repo list
             * Async render from repo list to app
             */

            List<Repo> list = repoDB.ReadDB();
            foreach (Repo x in list) {
                Console.WriteLine($"\n{x.Path}");
            }

        }

        public void AddLocalRepo(string path) {
            if (IsRepo(path) == true) {
                Repo repo = new Repo() {
                    Name = GetNameFromPath(path),
                    Path = path
                };
                repoDB.WriteDB(repo);
                Console.WriteLine("\nKlasik");
            } else {
                Console.WriteLine("\nNormal folder goes Brrrrr");
            }
        }

        public string GetNameFromPath(string path) {
            string[] arr = path.Split(Convert.ToChar(92));
            return arr[arr.Length - 1];
        }

        public bool IsRepo(string path) {

            bool status = false;
            if (Directory.Exists(path + @"\.git")) {
                status = true;
            }
            return status;
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
                    AddLocalRepo(fbd.SelectedPath);
                } 
            }
        }
    }
}
