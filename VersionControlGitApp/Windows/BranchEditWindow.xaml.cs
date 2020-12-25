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

        public BranchEditWindow(List<string> _branches, string _dirPath, string _operation, MainWindow _win) {
            InitializeComponent();
            branches = CleanBranches(_branches);
            dirPath = _dirPath;
            operation = _operation;
            win = _win;

            string branch = GitMethods.GetCurrentBranch(dirPath);

            if (operation == "create") {
                BranchesLabel.Content = "Create new branch or swap to existing one";
            } else if (operation == "rename") {
                BranchesLabel.Content = $"Rename branch {branch}";
            }

        }

        private List<string> CleanBranches(List<string> branches) {
            List<string> newBranches = new List<string>();
            foreach (string branch in branches) {
                newBranches.Add(branch.Replace('*', ' ').Trim());
            }
            return newBranches;
        }

        private void CheckoutBranchButton_Clicked(object sender, RoutedEventArgs e) {
            string branch = BranchesTextBox.Text.ToString();
            string currentBranch = GitMethods.GetCurrentBranch(dirPath);
            bool branchExists = false;
            bool close = false;
            
            foreach (string br in branches) {
                if (br.ToLower() == branch.ToLower()) {
                    branchExists = true;
                    break;
                } 
            }

            if (branchExists) {
                if (currentBranch != branch) {
                    Cmd.Run($"checkout {branch}", dirPath);
                    ConsoleLogger.UserPopup("Branch swap", $"Swapped from {branch} to {branch}");
                    close = true;
                } else {
                    ConsoleLogger.UserPopup("Branch swap", $"Can't swap to same branch");
                }
            } else if (operation == "rename") {
                if (currentBranch != "master") {
                    Cmd.Run($"branch -m {branch}", dirPath);
                    ConsoleLogger.UserPopup("Branch swap", $"Renamed to {branch}");
                    close = true;
                } else {
                    ConsoleLogger.UserPopup("Branch swap", $"Can't rename branch master");
                }
            } else if (operation == "create") {
                Cmd.Run($"checkout -b {branch}", dirPath);
                ConsoleLogger.UserPopup("Branch create", $"Branch {branch} created");
                close = true;
            } else {
                ConsoleLogger.UserPopup("Branch edit", $"There was an error");
                close = true;
            }

            if (close) {
                Dispatcher.Invoke(() => MainWindowUI.LoadRepoBranches(dirPath, win));
                Dispatcher.Invoke(() => MainWindowUI.ChangeCommitButtonBranch(dirPath));
                this.Close();
            }

        }

        private void Window_Minimized(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        private void Window_Closed(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void DragWindownOnMouseDown(object sender, MouseButtonEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

    }
}
