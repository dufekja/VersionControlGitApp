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
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Logging;
using VersionControlGitApp.UIelements;

namespace VersionControlGitApp.Windows {
    public partial class BranchEditWindow : Window {

        public static List<string> branches;
        public static string dirPath;
        public static string operation;
        public static MainWindow win;

        /// <summary>
        /// Branch edit window constructor
        /// </summary>
        /// <param name="_branches">List of all repository branches</param>
        /// <param name="_dirPath">Repository path</param>
        /// <param name="_operation">Operation which will be executed</param>
        /// <param name="_win">Instance to mainwindow thread</param>
        public BranchEditWindow(List<string> _branches, string _dirPath, string _operation, MainWindow _win) {
            InitializeComponent();
            branches = CleanBranches(_branches);
            dirPath = _dirPath;
            operation = _operation;
            win = _win;

            // get selected branch
            string branch = GitMethods.GetCurrentBranch(dirPath);

            // change label based on operation
            if (operation == "create") {
                BranchesLabel.Content = "Create new branch or swap to existing one";
            } else if (operation == "rename") {
                BranchesLabel.Content = $"Rename branch {branch}";
            }

        }

        /// <summary>
        /// Remove * from branches
        /// </summary>
        /// <param name="branches">List of branches</param>
        /// <returns>Returns cleaned list of branches</returns>
        private List<string> CleanBranches(List<string> branches) {
            List<string> newBranches = new List<string>();

            // remove selcted branch status from selected branch
            foreach (string branch in branches) {
                newBranches.Add(branch.Replace('*', ' ').Trim());
            }
            return newBranches;
        }

        /// <summary>
        /// Checkout branch action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void CheckoutBranchButton_Clicked(object sender, RoutedEventArgs e) {
            string branch = BranchesTextBox.Text.ToString();
            string currentBranch = GitMethods.GetCurrentBranch(dirPath);
            bool branchExists = false;
            bool close = false;
            
            // check if branch exists
            foreach (string br in branches) {
                if (br.ToLower() == branch.ToLower()) {
                    branchExists = true;
                    break;
                } 
            }

            // in case of branche swapping
            if (branchExists && currentBranch != null) {
                if (currentBranch != branch) {
                    Cmd.Run($"checkout {branch}", dirPath);
                    ConsoleLogger.UserPopup("Branch swap", $"Swapped from {branch} to {branch}");
                    close = true;
                } else {
                    ConsoleLogger.UserPopup("Branch swap", $"Can't swap to same branch");
                }

            // in case of rename current branch
            } else if (operation == "rename") {
                if (currentBranch != "master") {
                    Cmd.Run($"branch -m {branch}", dirPath);
                    ConsoleLogger.UserPopup("Branch swap", $"Renamed to {branch}");
                    close = true;
                } else {
                    ConsoleLogger.UserPopup("Branch swap", $"Can't rename branch master");
                }

            // in case of creating new branch
            } else if (operation == "create") {
                Cmd.Run($"checkout -b {branch}", dirPath);
                ConsoleLogger.UserPopup("Branch create", $"Branch {branch} created");
                close = true;
            } else {
                ConsoleLogger.UserPopup("Branch edit", $"There was an error\n Try to commit changes first");
                Dispatcher.Invoke(() => win.CommitButton.Content = "Commit to master");
                close = true;
            }

            // update main window and close branch edit window
            if (close) {
                Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(dirPath, win));
                Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(dirPath, win));
                Close();
            }

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
