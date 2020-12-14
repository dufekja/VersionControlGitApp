using Octokit;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;

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
            win.FilesToCommit.Items.Clear();
            List<string> filesForCommit = Cmd.FilesForCommit(path, win);
            if (filesForCommit.Count > 0) {
                foreach (string file in filesForCommit) {
                    win.FilesToCommit.Items.Add(file);
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
