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
using LiveCharts.Defaults;
using static VersionControlGitApp.Config;

namespace VersionControlGitApp.Windows {
    public partial class StatisticsWindow : Window {

        public static MainWindow mainWin;
        public static GitHubClient client;
        public static LocalRepoDB repoDB;
        public static User user;
        public static string header;
        public static string currentRepo;
        public static string currentRepoPath;

        public SeriesCollection SeriesCollection { get; set; }

        public StatisticsWindow(MainWindow _win, GitHubClient _client, User _user, LocalRepoDB _repoDB, string _header, string _currentRepoPath) {
            InitializeComponent();

            // series for pie chart
            SeriesCollection = new SeriesCollection();
            DataContext = this;

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

                // remove pie chart 
                PieChart chart = (PieChart)this.MainGrid.FindName("Piechart");
                this.MainGrid.Children.Remove(chart);

;                GenerateRepoData();
            }

        }

        private void GenerateUserData() {
            
            // set public and private repo labels count
            Task.Run(() => Dispatcher.Invoke(() => SetRepoLabelsText(
                $"Owned public repositories: {user.PublicRepos}", 
                $"Owned private repositories: {user.TotalPrivateRepos}")));

            // calculate commit activity for each repo
            Task.Run(() => Dispatcher.Invoke(() => SetStatsLabel()));

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

            if (repositories != null) {
                foreach (string[] repo in repositories) {
                    //adding values or series will update and animate the chart automatically

                    ObservableValue commits = new ObservableValue(int.Parse(repo[1])); 

                    var pie = new PieSeries() {
                        Title = repo[0],
                        Values = new ChartValues<ObservableValue> { commits },
                        DataLabels = true
                    };

                    SeriesCollection.Add(pie);
                }
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_CHART_RELATED, "There are no repositories on user Github");
            }
  
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

                // generate from github

                //Dispatcher.Invoke(() => StatsLabel.Content = $"{currentRepo} - {repoID}");
                //client.Repository.Statistics.GetCommitActivity();
            } else {

                // generate from local git func
                string text = "";
                List<string> lines = Cmd.RunAndRead("log", currentRepoPath);
             
                //Dispatcher.Invoke(() => StatsLabel.Content = "");

                foreach (string line in lines) {
                    text += line + "\n";
                    //Dispatcher.Invoke(() => StatsLabel.Content += line + "\n");
                }

                ConsoleLogger.UserPopup("diwhq", text);

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
