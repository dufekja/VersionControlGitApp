using Octokit;
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
using VersionControlGitApp.Logging;
using LiveCharts.Wpf;
using LiveCharts;
using VersionControlGitApp.Controllers;

namespace VersionControlGitApp.Windows {
    public partial class StatisticsWindow : Window {

        public static MainWindow mainWin;
        public static GitHubClient client;
        public static LocalRepoDB repoDB;
        public static User user;
        public static string header;
        public static string currentRepo;
        public static string currentRepoPath;

        public SeriesCollection SeriesCollection { get; private set; }
        public string[] Labels { get; private set; }

        public StatisticsWindow(MainWindow _win, GitHubClient _client, User _user, LocalRepoDB _repoDB, string _header, string _currentRepoPath) {
            InitializeComponent();

            mainWin = _win;
            client = _client;
            user = _user;
            repoDB = _repoDB;
            header = _header;
            currentRepoPath = _currentRepoPath;
            currentRepo = GitMethods.GetNameFromPath(_currentRepoPath);


            if (header == "User") {
                GenerateUserData();
            } else if (header == "Repository") {
                GenerateRepoData();
            }

        }

        private void GenerateUserData() {
            
            // set public and private repo labels count
            SetRepoLabelsText($"Public repositories: {user.PublicRepos}", 
                              $"Private repositories: {user.TotalPrivateRepos}");

            // calculate commit activity for each repo
            Task.Run(() => SetStatsLabel());

        }

        private List<string[]> GenerateYearlyUserCommitActivity() {
            IReadOnlyList<Repository> repos = client.Repository.GetAllForCurrent().Result;
            if (repos == null)
                return null;

            List<string[]> reposWithActivity = new List<string[]>();
            foreach (var repo in repos) {
                var yearCommitActivity = client.Repository.Statistics.GetCommitActivity(repo.Id).Result;
                int totalYearActivity = 0;

                foreach (var week in yearCommitActivity.Activity) {
                    totalYearActivity += week.Total;
                }
                string[] repoTuple = new string[2];
                repoTuple[0] = $"{repo.Name}";
                repoTuple[1] = $"{totalYearActivity}";

                reposWithActivity.Add(repoTuple);
            }

            return reposWithActivity;
        }

        private void SetStatsLabel() {
            List<string[]> repositories = GenerateYearlyUserCommitActivity();
            string text = "";

            /*SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Commits per year",
                    Values = new ChartValues<double> { 10, 50, 39, 50 }
                }
            };

            Labels = new[] { "Maria", "Susan", "Charles", "Frida" };

            DataContext = this;*/

            foreach (var repo in repositories) {
                text += $"{repo[0]} - {repo[1]}\n";
            }

            Dispatcher.Invoke(() => StatsLabel.Content = text);
        }

        private void GenerateRepoData() {
            SetRepoLabelsText("", "");

            Task.Run(() => GenerateRepoDataFunc());
        }

        private void GenerateRepoDataFunc() {

            IReadOnlyList<Repository> repos = client.Repository.GetAllForCurrent().Result;
            long repoID = 0;
            foreach (var repo in repos) {
                ConsoleLogger.Info("StatisticsWindow", $"{repo.Name} - {currentRepo}");
                if (repo.Name == currentRepo) {
                    repoID = repo.Id;
                    break;
                }
            }

            if (repoID != 0) {
                Dispatcher.Invoke(() => StatsLabel.Content = $"{currentRepo} - {repoID}");
                //client.Repository.Statistics.GetCommitActivity();
            } else {
                List<string> lines = Cmd.RunAndRead("log", currentRepoPath);
                Dispatcher.Invoke(() => StatsLabel.Content = "");

                foreach (string line in lines) {
                    Dispatcher.Invoke(() => StatsLabel.Content += line + "\n");
                }

            }
                

            
        }

        private void SetRepoLabelsText(string publicLabel, string privateLabel) {
            PublicReposLabel.Content = $"{publicLabel}";
            PrivateReposLabel.Content = $"{privateLabel}";
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
