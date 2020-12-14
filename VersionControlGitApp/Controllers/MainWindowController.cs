using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;
using VersionControlGitApp.UIelements;

namespace VersionControlGitApp.Controllers {
    public static class MainWindowController {

        public static void AddLocalRepositoryCommand(LocalRepoDB repoDB) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string res = $"{result}";
            string path = fbd.SelectedPath;

            if (res == "OK") {
                bool ok = GitMethods.AddLocalRepo(path, repoDB);
                if (ok == true) {
                    MainWindowUI.LoadPathLabel(path);
                }
            }
        }

        public static void NewRepositoryCommand(LocalRepoDB repoDB) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string res = $"{result}";
            string repoPath = fbd.SelectedPath;

            if (res == "OK" && !GitMethods.IsRepo(repoPath)) {
                GitMethods.Init(repoPath, repoDB);
                MainWindowUI.LoadPathLabel(repoPath);
            }
        }

        public static void CommitRepositoryCommand(string repoPath, MainWindow win) {
            if (repoPath != "" && GitMethods.IsRepo(repoPath)) {

                string summary = win.CommitSummary.Text.ToString();
                string desc = win.CommitDescription.Text.ToString();

                GitMethods.Commit(repoPath, summary, desc, win);

                MainWindowUI.ClearCommitAndContext(win);
                
            }
        }

        public static void RemoveRepositoryCommand(string repoPath, LocalRepoDB repoDB, MainWindow win) {
            if (repoPath != "" && GitMethods.IsRepo(repoPath)) {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"Do you want to remove {GitMethods.GetNameFromPath(repoPath)} from list?", "Remove Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes) {
                    repoDB.DeleteByPath(repoPath);
                    ConsoleLogger.UserPopup("Remove Confirmation", $"{GitMethods.GetNameFromPath(repoPath)} removed");

                    win.Dispatcher.Invoke(() => win.RepoListBox.Items.Clear());
                    win.Dispatcher.Invoke(() => MainWindowUI.ListBoxLoad());
                    win.PathLabel.Text = "";
                }
            }
        }
    }
}
