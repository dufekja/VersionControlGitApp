using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;
using Octokit;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp {
    public partial class CloneRepoWindow : Window {

        private static LocalRepoDB repoDB;
        public static List<UserRepository> userRepos;
        public static MainWindow win;

        public CloneRepoWindow(LocalRepoDB _repoDB, GitHubClient client, MainWindow _win) {
            InitializeComponent();

            repoDB = _repoDB;
            win = _win;

            URL.Focus();

            userRepos = GithubController.GetAllRepos(client);
            foreach (UserRepository repo in userRepos) {
                ComboBoxItem item = new ComboBoxItem {
                    Content = repo.GetName(),
                    Tag = repo.GetHtmlUrl()
                };

                ExternalRepoComboBox.Items.Add(item);
            }

        }

        private void CloneRepository(object sender, RoutedEventArgs e) {
            string url = URL.Text.ToString();

            if (url != "") {
                using (var fbd = new FolderBrowserDialog()) {
                    DialogResult result = fbd.ShowDialog();
                    string res = $"{result}";
                    string path = fbd.SelectedPath;

                    if (res == "OK" && url != "" && path != "") {
                        Task.Run(() => GitMethods.Clone(url, path, repoDB));
                        ComboBoxItem item = new ComboBoxItem {
                            Content = GitMethods.GetNameFromURL(url),
                            IsSelected = true
                        };

                        win.RepoListBox.Items.Add(item);
                        win.PathLabel.Text = path + @"\" + GitMethods.GetNameFromURL(url);
                        this.Close();
                    }
                }
            } else {
                ConsoleLogger.UserPopup("Clone", "You must select repository first");
            }
           
        }

        private void ExternalRepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string selectedRepo = ((ComboBoxItem)ExternalRepoComboBox.SelectedItem).Tag.ToString() + ".git";
            URL.Text = selectedRepo;
        }
    }
}
