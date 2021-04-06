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
using System.Windows.Input;

using static VersionControlGitApp.Config;

namespace VersionControlGitApp {
    public partial class CloneRepoWindow : Window {

        private static LocalRepoDB repoDB;
        public static List<UserRepository> userRepos;
        public static MainWindow win;

        /// <summary>
        /// Clone repostiory window construcotr
        /// </summary>
        /// <param name="_repoDB">Repository database instance</param>
        /// <param name="client">Client instance</param>
        /// <param name="_win">Reference to mainwindow thread instance</param>
        public CloneRepoWindow(LocalRepoDB _repoDB, GitHubClient client, MainWindow _win) {
            InitializeComponent();

            repoDB = _repoDB;
            win = _win;

            // select textbox
            URL.Focus();

            // get user repositories
            userRepos = GithubController.GetAllRepos(client);
            bool isFirst = true;

            // parse them into combobox
            foreach (UserRepository repo in userRepos) {

                // combobox item instance
                ComboBoxItem item = new ComboBoxItem {
                    Content = repo.GetName(),
                    Tag = repo.GetHtmlUrl()
                };

                // set first item as selected
                if (isFirst) {
                    item.IsSelected = true;
                    isFirst = false;
                }

                ExternalRepoComboBox.Items.Add(item);
            }

        }

        /// <summary>
        /// Clone repository action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void CloneRepository(object sender, RoutedEventArgs e) {
            string url = URL.Text.ToString();

            if (url != "") {
                
                // show directory selection to user and get selected path
                using var fbd = new FolderBrowserDialog();
                DialogResult result = fbd.ShowDialog();
                string path = fbd.SelectedPath;

                if (result.ToString() == "OK" && path != "") {
                    // clone repo from Github and add it to listbox 
                    Task.Run(() => GitMethods.Clone(url, path, repoDB));
                    
                    ListBoxItem item = new ListBoxItem {
                        Content = GitMethods.GetNameFromURL(url),
                    };
                    win.RepoListBox.Items.Add(item);

                    Close();
                }
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_CLONE_REPO, STATUSMSG_SELECTREPO);
            }
           
        }

        /// <summary>
        /// Change url text when selected different repository in combobox
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void ExternalRepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string selectedRepo = ((ComboBoxItem)ExternalRepoComboBox.SelectedItem).Tag.ToString() + ".git";
            URL.Text = selectedRepo;
        }

        /// <summary>
        /// Minimize window action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void Window_Minimized(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Close window action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void Window_Closed(object sender, RoutedEventArgs e) {
            Close();
        }

        /// <summary>
        /// Drag window on mouse down action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void DragWindownOnMouseDown(object sender, MouseButtonEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
