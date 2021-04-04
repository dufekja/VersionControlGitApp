using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VersionControlGitApp.Controllers;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp {
    public partial class TokenWindow : Window {

        private PrivateTokenDB tokenDB;
        public static string user;

        /// <summary>
        /// Token window construcotr
        /// </summary>
        public TokenWindow() {
            // check git version
            List<string> output = Cmd.RunAndRead("--version", @"\");
            if (output == null) {
                ConsoleLogger.UserPopup("Git status", "Please install git first");
                Close();
            }

            // get logged user and init token database 
            user = SystemInformation.UserName;

            // set DB files path
            string appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Cmd.SetDBPath(appdataFolder + @"\VersionControlGitApp");

            // initialize token database
            tokenDB = new PrivateTokenDB();
            tokenDB.InitDB();

            // if user have active token then go to main window
            List<Token> tokens = tokenDB.FindTokensByUser(user);
            if (tokens != null) {
                foreach (Token token in tokens) {
                    if (token.IsActive == 1)
                        GoMainWindow(token.Value);
                }
            }

            InitializeComponent();
        }

        /// <summary>
        /// Go to main window action
        /// </summary>
        /// <param name="token">Token value</param>
        private void GoMainWindow(string token) {
            new MainWindow(token, tokenDB).Show();
            Close();
        }

        /// <summary>
        /// Submit new token action
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void SubmitToken(object sender, RoutedEventArgs e) {
            string token = TokenTextBox.Text.ToLower();

            // check token lenght
            if (token.Length == 40) {

                // if token is not in database
                if (tokenDB.FindTokenByValue(token) == null) {
                    Token activeToken = tokenDB.GetActiveToken(user);
                    int isActive = 0;

                    if (activeToken == null) {
                        isActive = 1;
                    }

                    // save token and redirect to mainwindow
                    tokenDB.WriteToken(token, user, isActive);
                    GoMainWindow(token);
                } else {
                    ConsoleLogger.UserPopup("Submit token", "Token already exists");
                }
            } else {
                ConsoleLogger.UserPopup("Submit token", "Token must be 40 characters long");
            }
        }

        /// <summary>
        /// Request navigation to website
        /// </summary>
        /// <param name="sender">Object that triggered action</param>
        /// <param name="e">All added arguments</param>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
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
