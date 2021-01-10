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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp {
    public partial class TokenWindow : Window {

        private PrivateTokenDB tokenDB;
        public static string user;

        public TokenWindow() {

            user = System.Windows.Forms.SystemInformation.UserName;
            tokenDB = new PrivateTokenDB();
            tokenDB.InitDB();

            List<Token> tokens = tokenDB.FindTokensByUser(user);
            if (tokens != null) {
                foreach (Token token in tokens) {
                    if (token.IsActive)
                        GoMainWindow(token.Value);
                }
            }
            

            InitializeComponent();
        }

        private void GoMainWindow(string token) {
            new MainWindow(token).Show();
            this.Close();
        }

        private void SubmitToken(object sender, RoutedEventArgs e) {
            string token = TokenTextBox.Text.ToLower();

            if (token.Length == 40) {
                if (tokenDB.FindTokenByValue(token) == null) {
                    Token activeToken = tokenDB.GetActiveToken(user);
                    bool isActive = false;

                    if (activeToken == null)
                        isActive = true;

                    tokenDB.WriteToken(token, user, isActive);
                    GoMainWindow(token);
                } else {
                    ConsoleLogger.UserPopup("Submit token", "Token already exists");
                }
            } else {
                ConsoleLogger.UserPopup("Submit token", "Token must be 40 characters long");
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
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
