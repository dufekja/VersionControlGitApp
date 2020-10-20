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
using VersionControlGitApp.Database;

namespace VersionControlGitApp {
    public partial class TokenWindow : Window {

        private PrivateTokenDB tokenDB;

        public TokenWindow() {

            tokenDB = new PrivateTokenDB();
            tokenDB.InitDB();

            string token = tokenDB.GetFirstToken();
            if (token != "") {
                GoMainWindow(token);
            }


            InitializeComponent();
        }

        private void GoMainWindow(string token) {
            MainWindow main = new MainWindow(token);
            main.Owner = this;
            main.ShowDialog();
            this.Close();
        }

        private void SubmitToken(object sender, RoutedEventArgs e) {
            string token = TokenTextBox.Text.ToLower();

            if (token.Length == 40) {
                if (tokenDB.FindTokenByValue(token) == null) {
                    tokenDB.WriteToken(token);
                    GoMainWindow(token);
                }
            } else {
                TokenTextBox.Text = "Ur token not 40 chars *crying* | haha token lenght goes Brrrrrr";
            }
        }
    }
}
