using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp.UIelements {
    public static class MainWindowUI {

        public static MainWindow win;
        public static User user;
        public static LocalRepoDB repoDB;
        public static string loggedUser;

        /// <summary>
        /// Initialize mainwindow UI elements
        /// </summary>
        /// <param name="_win">Reference to main window thread instance</param>
        /// <param name="_user">User instance</param>
        /// <param name="_repoDB">Repository database instance</param>
        /// <param name="_loggedUser">Logged user name</param>
        public static void InitUIElements(MainWindow _win, User _user, LocalRepoDB _repoDB, string _loggedUser) {

            win = _win;
            user = _user;
            repoDB = _repoDB;
            loggedUser = _loggedUser;

            win.PathLabel.Text = "";
            win.UserName.Text = user.Name;

            LoadUserAvatar();
            ListBoxLoad();
        }

        /// <summary>
        /// Load user image from github
        /// </summary>
        public static void LoadUserAvatar() {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(user.AvatarUrl);
            bi.EndInit();
            win.UserImage.Source = bi;
        }

        /// <summary>
        /// Change path label in main window
        /// </summary>
        /// <param name="path">Path to repository </param>
        public static void LoadPathLabel(string path) {
            ListBoxItem item = new ListBoxItem {
                Content = GitMethods.GetNameFromPath(path),
                IsSelected = true
            };

            win.RepoListBox.Items.Add(item);
            win.PathLabel.Text = path;
        }

        /// <summary>
        /// Refresh repository branches in tool panel
        /// </summary>
        /// <param name="path">Path to repository</param>
        /// <param name="win">Main window thread instance</param>
        public static void LoadRepoBranches(string path, MainWindow win) {
            win.MergeBranchMenuItem.Items.Clear();
            win.ChangeBranchMenuItem.Items.Clear();

            List<string> lines = GitMethods.GetBranches(path);
            string currentBranch = GitMethods.GetCurrentBranch(path);

            // fill rename and merge items
            if (lines != null) {
                foreach (string line in lines) {
                    if (line.Replace("*", "").Trim() != currentBranch) {
                        MenuItem item = new MenuItem() {
                            Header = line.Replace("*", "").Trim(),
                            FontSize = 14.0,
                        };
                        win.ChangeBranchMenuItem.Items.Add(item);

                        item = new MenuItem() {
                            Header = line.Replace("*", "").Trim(),
                            FontSize = 14.0,
                        };
                        win.MergeBranchMenuItem.Items.Add(item);
                    }
                }
            }

            // disable with one branch
            if (win.ChangeBranchMenuItem.Items.Count > 0) {
                win.ChangeBranchMenuItem.IsEnabled = true;
            } else {
                win.ChangeBranchMenuItem.IsEnabled = false;
            }
                
            if (win.MergeBranchMenuItem.Items.Count > 0) {
                win.MergeBranchMenuItem.IsEnabled = true;
            } else {
                win.MergeBranchMenuItem.IsEnabled = false;
            }
                
        }

        /// <summary>
        /// Refresh commit button label
        /// </summary>
        /// <param name="path">Repository path</param>
        /// <param name="win">Main window thread instance</param>
        public static void ChangeCommitButtonBranch(string path, MainWindow win) {
            win.CommitButton.Content = "Commit to " + GitMethods.GetCurrentBranch(path);
        }

        /// <summary>
        /// Block repository stats if no commits action
        /// </summary>
        /// <param name="repo">Currently selected repository</param>
        /// <param name="win">Main window thread instance</param>
        public static void RepoStatsBlocked(string repo, MainWindow win) {
            // repository stats status
            if (Cmd.HaveCommits(repo))
               win.Dispatcher.Invoke(() => win.RepositoryStatsMenuItem.IsEnabled = true);
            else
                win.Dispatcher.Invoke(() => win.RepositoryStatsMenuItem.IsEnabled = false);
        }

        /// <summary>
        /// Load repository list box
        /// </summary>
        public static void ListBoxLoad() {

            // get list of local repos from database
            List<Repo> localRepos = repoDB.ReadDB(loggedUser);
            bool isSelected = false;

            // fill listbox with data
            if (localRepos.Count > 0) {
                win.RepoListBox.Items.Clear();
                foreach (Repo repo in localRepos) {
                    ListBoxItem item = new ListBoxItem {
                        Content = repo.Name
                    };

                    if (isSelected == false) {
                        item.IsSelected = true;
                        win.PathLabel.Text = repo.Path;
                        isSelected = true;
                    }

                    win.RepoListBox.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Enable or disable branch actions
        /// </summary>
        /// <param name="repo">Currently selected repository</param>
        /// <param name="win">Main window thread instance</param>
        public static void ChangeAllBranchToolsStatus(string path, MainWindow win) {
            bool haveCommits = Cmd.HaveCommits(path);

            if (haveCommits) {
                win.Dispatcher.Invoke(() => win.AllBranchToolsMenuItem.IsEnabled = true);
            } else {
                win.Dispatcher.Invoke(() => win.AllBranchToolsMenuItem.IsEnabled = false);
            }
        }

        /// <summary>
        /// Clear path label if no repositories in listbox
        /// </summary>
        /// <param name="win">Main window thread instance</param>
        public static void ClearRepoPathOnEmpty(MainWindow win) {
            if (win.RepoListBox.Items.Count == 0) {
                win.PathLabel.Text = "";
            }
        }

        /// <summary>
        /// Files to commit refresh action
        /// </summary>
        /// <param name="win">Main window thread instance</param>
        public static void FilesToCommitRefresh(MainWindow win) {

            string path = "";
            win.Dispatcher.Invoke(() => path = win.PathLabel.Text.ToString());

            List<string> modifiedFiles = Cmd.UntrackedFiles(path);

            if (modifiedFiles != null) {

                string selected = "";
                if (win.FilesToCommit.SelectedItem != null) {
                    selected = ((ComboBoxItem)win.FilesToCommit.SelectedItem).Content.ToString();
                }

                // fill combobox with files to commit
                List<ComboBoxItem> newComboBoxItems = new List<ComboBoxItem>();
                foreach (string name in modifiedFiles) {
                    bool isSelected = false;
                    if (name == selected) {
                        isSelected = true;
                    }
                    newComboBoxItems.Add(new ComboBoxItem() {
                        Content = name,
                        IsSelected = isSelected
                    });
                }

                win.FilesToCommit.Items.Clear();
                foreach (ComboBoxItem newItem in newComboBoxItems) {
                    win.FilesToCommit.Items.Add(newItem);
                }
            }
        }

        /// <summary>
        /// Clear context of commit summary and description
        /// </summary>
        /// <param name="win">Main window thread instance</param>
        public static void ClearCommitAndContext(MainWindow win) {
            win.CommitSummary.Text = "";
            win.CommitDescription.Text = "";
            win.FileContent.Blocks.Clear();
        }

        /// <summary>
        /// On repository change action
        /// </summary>
        /// <param name="repoName">New repository name</param>
        /// <param name="repoPath">New repository path</param>
        /// <param name="win">Main window thread instance</param>
        public static void SetDataForNewRepo(string repoName, string repoPath, MainWindow win) {
            win.PathLabel.Text = repoPath;
            win.FileContent.Blocks.Clear();
            FilesToCommitRefresh(win);
            ChangeCommitButtonBranch(repoPath, win);
            LoadRepoBranches(repoPath, win);

            ConsoleLogger.StatusBarUpdate($"Changed to repository: {repoName}", win);
        }

    }
}
