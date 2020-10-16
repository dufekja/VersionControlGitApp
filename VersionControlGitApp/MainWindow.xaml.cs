using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace VersionControlGitApp {
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public static GitHubClient client = new GitHubClient(new ProductHeaderValue("VersionControlGitApp"));
        public static string token = "63fc1bdad3fb1e6cb40a16cdf49acf6ebb247e37";
        
        public static User user;
        public static List<UserRepository> userRepos;

        public MainWindow() {
            InitializeComponent();

            // auth user using token
            client = GithubController.Authenticate(client, token);
            user = client.User.Current().Result;

            // get all of user's repositories info
            userRepos = GithubController.GetAllRepos(client);

            string path = @"C:\Users\jandu\Desktop\repo";

            //userRepos[0].Status(path);


        }

    }
}
