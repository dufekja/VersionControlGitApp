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

namespace VersionControlGitApp.Windows {
    public partial class BranchEditWindow : Window {

        public static List<string> branches;
        public static string dirPath;

        public BranchEditWindow(List<string> _branches, string _dirPath) {
            InitializeComponent();
            branches = CleanBranches(_branches);
            dirPath = _dirPath;

            BranchesComboBox.ItemsSource = branches;
        }

        private List<string> CleanBranches(List<string> branches) {
            List<string> newBranches = new List<string>();
            foreach (string branch in branches) {
                newBranches.Add(branch.Replace('*', ' ').Trim());
            }
            return newBranches;
        }

        private void BranchesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string branch = BranchesComboBox.SelectedItem.ToString();

            if (branch != "") {
                BranchesTextBox.Text = branch.Replace('*', ' ').Trim();
            }

        }

        private void CheckoutBranchButton_Clicked(object sender, RoutedEventArgs e) {
            string branch = BranchesTextBox.Text.ToString();
            bool branchExists = false;
            
            foreach (string br in branches) {
                if (br.ToLower() == branch.ToLower()) {
                    branchExists = true;
                    break;
                } 
            }

            if (branchExists) {
                Cmd.Run($"checkout {branch}", dirPath);
                ConsoleLogger.Popup("BranchEditWindow", $"Swapped to {branch}");
            } else {
                Cmd.Run($"checkout -b {branch}", dirPath);
                ConsoleLogger.Popup("BranchEditWindow", $"Branch {branch} created");
            }

            this.Close();

        }
    }
}
