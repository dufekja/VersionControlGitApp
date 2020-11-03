using Octokit;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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

            win.UserName.Text = user.Name;

            LoadUserAvatar();
            ComboBoxLoad();
        }

        public static void LoadUserAvatar() {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(user.AvatarUrl);
            bi.EndInit();
            win.UserImage.Source = bi;
        }

        public static void LoadPathLabel(string path) {
            ComboBoxItem item = new ComboBoxItem {
                Content = GitMethods.GetNameFromPath(path),
                IsSelected = true
            };

            win.RepoComboBox.Items.Add(item);
            win.PathLabel.Text = path;
        }

        public static void ComboBoxLoad() {

            List<Repo> localRepos = repoDB.ReadDB();
            bool isSelected = false;

            if (localRepos.Count > 0) {
                foreach (Repo repo in localRepos) {
                    ComboBoxItem item = new ComboBoxItem {
                        Content = repo.Name
                    };

                    if (isSelected == false) {
                        item.IsSelected = true;
                        win.PathLabel.Text = repo.Path;
                        isSelected = true;
                    }

                    win.RepoComboBox.Items.Add(item);
                }
            }
        }
    }
}
