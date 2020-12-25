using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static void InitUIElements(MainWindow _win, User _user, LocalRepoDB _repoDB) {

            win = _win;
            user = _user;
            repoDB = _repoDB;

            win.PathLabel.Text = "";
            win.UserName.Text = user.Name;

            LoadUserAvatar();
            ListBoxLoad();
        }

        public static void LoadUserAvatar() {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(user.AvatarUrl);
            bi.EndInit();
            win.UserImage.Source = bi;
        }

        public static void LoadPathLabel(string path) {
            ListBoxItem item = new ListBoxItem {
                Content = GitMethods.GetNameFromPath(path),
                IsSelected = true
            };

            win.RepoListBox.Items.Add(item);
            win.PathLabel.Text = path;
        }

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

            //disable with one branch
            if (win.ChangeBranchMenuItem.Items.Count > 0)
                win.ChangeBranchMenuItem.IsEnabled = true;
            else
                win.ChangeBranchMenuItem.IsEnabled = false;

            if (win.MergeBranchMenuItem.Items.Count > 0)
                win.MergeBranchMenuItem.IsEnabled = true;
            else
                win.MergeBranchMenuItem.IsEnabled = false;


        }

        public static void ChangeCommitButtonBranch(string path) {
            win.CommitButton.Content = "Commit to " + GitMethods.GetCurrentBranch(path);
        }

        public static void ListBoxLoad() {

            List<Repo> localRepos = repoDB.ReadDB();
            bool isSelected = false;

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

        public static void FilesToCommitRefresh(string path, MainWindow win) {

            List<string> modifiedFiles = Cmd.UntrackedFiles(path);

            if (modifiedFiles != null) {

                string selected = "";
                if (win.FilesToCommit.SelectedItem != null) {
                    selected = ((ComboBoxItem)win.FilesToCommit.SelectedItem).Content.ToString();
                }

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

        public static void ClearCommitAndContext(MainWindow win) {
            win.CommitSummary.Text = "";
            win.CommitDescription.Text = "";
            win.FileContent.Text = "";
        }

    }
}
