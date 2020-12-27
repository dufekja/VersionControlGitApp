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
using VersionControlGitApp.Windows;
using static VersionControlGitApp.Config;

namespace VersionControlGitApp.Controllers {
    public static class MainWindowController {

        /// <summary>
        /// Add already created repository based on .git folder inside
        /// </summary>
        /// <param name="repoDB">Instance of repository database</param>
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

        /// <summary>
        /// Initialize new repository in folder which is not already repository
        /// </summary>
        /// <param name="repoDB">Instance of repository database</param>
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

        /// <summary>
        /// Get commit summary then commit changes
        /// </summary>
        /// <param name="repoPath">Path to repository</param>
        /// <param name="win">Mainwindow window object</param>
        public static void CommitRepositoryCommand(string repoPath, MainWindow win) {
            if (repoPath != "" && GitMethods.IsRepo(repoPath)) {

                string summary = win.CommitSummary.Text.ToString();
                string desc = win.CommitDescription.Text.ToString();

                GitMethods.Commit(repoPath, summary, desc, win);

                MainWindowUI.ClearCommitAndContext(win); 
            }
        }

        /// <summary>
        /// Remove repository from repository listbox
        /// </summary>
        /// <param name="repoPath">Path to repository</param>
        /// <param name="repoDB">Instance of repository database</param>
        /// <param name="win">Mainwindow window object</param>
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

        /// <summary>
        /// Remove repository from listbox and move it into device trash bin
        /// </summary>
        /// <param name="repoPath">Repository path</param>
        public static void DeleteRepositoryCommand(string repoPath) {
            if (repoPath != "" && GitMethods.IsRepo(repoPath) && Directory.Exists(repoPath)) {

                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $"Do you want to delete {GitMethods.GetNameFromPath(repoPath)} ?",
                    "Delete Confirmation",
                    System.Windows.MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes) {
                    Directory.Delete(repoPath);
                    ConsoleLogger.UserPopup("Delete Confirmation", $"{GitMethods.GetNameFromPath(repoPath)} deleted");
                }
            }
        }

        /// <summary>
        /// Change to selected branch
        /// </summary>
        /// <param name="branch">Name of selected branch</param>
        /// <param name="repoPath">Repository path</param>
        /// <param name="win">MainWindow window object</param>
        public static void ChangeBranchCommand(string branch, string repoPath, MainWindow win) {
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);

            ConsoleState state = ConsoleState.Error; 
            state = Cmd.Run($"checkout {branch}", repoPath);
            win.Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, win));
            win.Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath, win));
            
            if (state == ConsoleState.Success && currentBranch != branch)
                ConsoleLogger.UserPopup("Branch swap", $"Swapped to branch '{branch}'");
            else
                ConsoleLogger.UserPopup("Branch swap", $"Can't swap to same branch");
        }

        /// <summary>
        /// Open window to create new branch
        /// </summary>
        /// <param name="repoPath">Repository path</param>
        /// <param name="win">MainWindow window object</param>
        public static void CreateNewBranchCommand(string repoPath, MainWindow win) {
            List<string> lines = GitMethods.GetBranches(repoPath);

            if (lines != null) {
                new BranchEditWindow(lines, repoPath, "create", win).Show();
            }

        }

        public static void RenameCurrentBranchCommand(string repoPath, MainWindow win) {
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);
            List<string> lines = GitMethods.GetBranches(repoPath);

            try {
                if (lines[0] != null) {
                    if (currentBranch != "master") {
                        new BranchEditWindow(lines, repoPath, "rename", win).Show();
                    } else
                        ConsoleLogger.UserPopup("Branch rename", "Can't rename branch master");
                }
            } catch { }

        }

        public static void MergeCurrentBranchCommand(string repoPath, string branch, MainWindow win) {
            List<string> lines = GitMethods.GetBranches(repoPath);
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);

            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                $"Do you want to merge {currentBranch} into {branch} ?",
                "Merge confirmation",
                MessageBoxButton.YesNo);

            if (messageBoxResult == MessageBoxResult.Yes) {
                ConsoleState state = ConsoleState.Error;

                if (Cmd.Run($"checkout {branch}", repoPath) == ConsoleState.Success)
                    state = Cmd.Run($"merge {currentBranch}", repoPath);

                if (state == ConsoleState.Success) {
                    ConsoleLogger.UserPopup("Branch merge", $"{currentBranch} merged to {branch}");
                    win.Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, win));
                    win.Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath, win));
                } else {
                    ConsoleLogger.UserPopup("Branch merge", $"Can't merge to {branch}");
                }   
            }

        }


    }
}
