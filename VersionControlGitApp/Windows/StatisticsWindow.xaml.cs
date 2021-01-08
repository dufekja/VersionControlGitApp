using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp.Windows {
    public partial class StatisticsWindow : Window {

        public static MainWindow mainWin;
        public static GitHubClient client;
        public static LocalRepoDB repoDB;
        public static User user;
        public static string header;

        public StatisticsWindow(MainWindow _win, GitHubClient _client, User _user, LocalRepoDB _repoDB, string _header) {
            InitializeComponent();

            mainWin = _win;
            client = _client;
            user = _user;
            repoDB = _repoDB;
            header = _header;


            if (header == "User") {
                GenerateUserData();
            } else if (header == "Repository") {
                GenerateRepoData();
            }

        }

        private void GenerateUserData() {
            SetRepoLabelsText($"Public repositories: {user.PublicRepos}", $"Private repositories: {user.TotalPrivateRepos}");
        }

        private void GenerateRepoData() {
            SetRepoLabelsText("", "");
        }

        private void SetRepoLabelsText(string publicLabel, string privateLabel) {
            PublicReposLabel.Content = $"{publicLabel}";
            PrivateReposLabel.Content = $"{privateLabel}";
        }

        private void Window_Minimized(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        private void Window_Closed(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void DragWindownOnMouseDown(object sender, MouseButtonEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}
