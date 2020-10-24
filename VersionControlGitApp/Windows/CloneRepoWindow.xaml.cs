using System;
using System.Collections.Generic;
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

namespace VersionControlGitApp {
    public partial class CloneRepoWindow : Window {

        private static LocalRepoDB repoDB;

        public CloneRepoWindow(LocalRepoDB _repoDB) {
            InitializeComponent();

            repoDB = _repoDB;

        }

        private void CloneRepository(object sender, RoutedEventArgs e) {
            string url = URL.Text;

            using (var fbd = new FolderBrowserDialog()) {
                DialogResult result = fbd.ShowDialog();
                string res = $"{result}";
                string path = fbd.SelectedPath;

                if (res == "OK" && url != "" && path != "") {
                    Task.Run(() => GitMethods.Clone(url, path, repoDB));
                    this.Close();
                }
            }
        }
    }
}
