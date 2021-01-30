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
        public static void AddLocalRepositoryCommand(LocalRepoDB repoDB, MainWindow win) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string path = fbd.SelectedPath;

            if (result == DialogResult.OK) {
                bool ok = GitMethods.AddLocalRepo(path, repoDB);
                if (ok == true) {
                    MainWindowUI.LoadPathLabel(path);
                    MainWindowUI.ChangeCommitButtonBranch(path, win);
                }
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_CREATE_REPO, ERROR_MSG);
            }
        }

        /// <summary>
        /// Initialize new repository in folder which is not already repository
        /// </summary>
        /// <param name="repoDB">Instance of repository database</param>
        public static void NewRepositoryCommand(LocalRepoDB repoDB) {
            using FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            string repoPath = fbd.SelectedPath;

            if (result == DialogResult.OK && !GitMethods.IsRepo(repoPath)) {
                GitMethods.Init(repoPath, repoDB);
                MainWindowUI.LoadPathLabel(repoPath);
                ConsoleLogger.UserPopup(HEADERMSG_CREATE_REPO, $"Repostiory {GitMethods.GetNameFromPath(repoPath)} added");
            } else {
                string repoName = GitMethods.GetNameFromPath(repoPath);
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $"{repoName} is already repository. Would you like to add it ?",
                    $"{repoName} is already repository",
                    MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes) {
                    bool success = GitMethods.AddLocalRepo(repoPath, repoDB);
                    if (success) {
                        MainWindowUI.LoadPathLabel(repoPath);
                        ConsoleLogger.UserPopup(HEADERMSG_CREATE_REPO, $"Repository {repoName} added");
                    } else {
                        ConsoleLogger.UserPopup(HEADERMSG_CREATE_REPO, ERROR_MSG);
                    }
                }
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
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $"Do you want to remove {GitMethods.GetNameFromPath(repoPath)} from list?",
                    HEADERMSG_REMOVE_CONF,
                    MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes) {
                    repoDB.DeleteByPath(repoPath);
                    ConsoleLogger.UserPopup(HEADERMSG_REMOVE_CONF, $"{GitMethods.GetNameFromPath(repoPath)} removed");

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
                    HEADERMSG_DELETE_CONF,
                    MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes) {
                    DirectoryInfo dir = new DirectoryInfo(repoPath);

                    if (dir.Exists) {
                        Cmd.setAttributesNormal(dir);
                        dir.Delete(true);
                    }

                    ConsoleLogger.UserPopup(HEADERMSG_DELETE_CONF, $"{GitMethods.GetNameFromPath(repoPath)} deleted");
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
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, $"Swapped to branch '{branch}'");
            else
                ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, $"Can't swap to same branch");
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

        /// <summary>
        /// Rename current selected branch
        /// </summary>
        /// <param name="repoPath">Repository path</param>
        /// <param name="win">MainWindow window object</param>
        public static void RenameCurrentBranchCommand(string repoPath, MainWindow win) {
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);
            List<string> branches = GitMethods.GetBranches(repoPath);

            try {
                if (branches[0] != null) {
                    if (currentBranch != "master") {
                        new BranchEditWindow(branches, repoPath, "rename", win).Show();
                    } else
                        ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, "Can't rename branch master");
                }
            } catch { }
        }

        /// <summary>
        /// Merge current branch to selected branch
        /// </summary>
        /// <param name="repoPath">Repository path</param>
        /// <param name="branch">Selected branch name</param>
        /// <param name="win">MainWindow window object</param>
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
                    ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, $"{currentBranch} merged to {branch}");
                    win.Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, win));
                    win.Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath, win));
                } else {
                    ConsoleLogger.UserPopup(HEADERMSG_BRANCH_RELATED, $"Cannot merge to {branch}");
                }   
            }
        }

        /// <summary>
        /// Delete current branch
        /// </summary>
        /// <param name="repoPath">Repository path</param>
        /// <param name="win">MainWindow window object</param>
        public static void DeleteCurrentBranchCommand(string repoPath, MainWindow win) {
            string currentBranch = GitMethods.GetCurrentBranch(repoPath);
            ConsoleState state = ConsoleState.Error;

            if (currentBranch != "master") {

                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $"Do you want to delete {currentBranch} ?",
                    HEADERMSG_DELETE_CONF,
                    System.Windows.MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes) {
                    Cmd.Run("checkout master", repoPath);
                    state = Cmd.Run($"branch -D {currentBranch}", repoPath);
                }

                if (state == ConsoleState.Success)
                    ConsoleLogger.UserPopup(HEADERMSG_DELETE_CONF, $"{currentBranch} deleted");
                else
                    ConsoleLogger.UserPopup(HEADERMSG_DELETE_CONF, ERROR_MSG);
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_DELETE_CONF, "Cannot delete branch master");
            }

            win.Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(repoPath, win));
            win.Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(repoPath, win));
        }


    }
}
