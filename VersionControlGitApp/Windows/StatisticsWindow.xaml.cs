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

        public SeriesCollection ColumnSeriesCollection { get; set; }
        public List<string> Labels { get; set; }
        public Func<int, string> Formatter { get; set; }


        public StatisticsWindow(MainWindow _win, GitHubClient _client, User _user, LocalRepoDB _repoDB, string _header, string _currentRepoPath) {

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

            InitializeComponent();

            if (header == "User") {

                // remove column chart 
                CartesianChart chart = (CartesianChart)this.MainGrid.FindName("Columnchart");
                this.MainGrid.Children.Remove(chart);

                GenerateUserData();

            } else if (header == "Repository") {

                // remove pie chart 
                PieChart chart = (PieChart)this.MainGrid.FindName("Piechart");
                this.MainGrid.Children.Remove(chart);

                ColumnSeriesCollection = new SeriesCollection();
                Labels = new List<string>();
                Formatter = value => value.ToString("N");

                GenerateRepoData();
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
            Task.Run(() => Dispatcher.Invoke(() => GenerateRepoDataFunc()));
        }

        private void GenerateRepoDataFunc() {
            string userName = user.Login.ToLower();
            string totalCommits = $"Total commits in {currentRepo}: ";
            string commitsFromLoggedUser = $"Commits from {userName}: ";

            // generate from local git func
            List<string> commits = GetCommitsFromGitLog();
            Dictionary<string, int> finalChartParse = new Dictionary<string, int>();

            if (commits != null) {
                int loggedUserCommitsCount = 0;
                foreach (string commit in commits) {
                    if (commit.Contains(userName)) {
                        loggedUserCommitsCount++;
                    }

                    string date = Cmd.Explode(commit, "Date:   ", "   ");
                    string month = date.Substring(4, 3);
                    string year = date.Substring(20, 4);
                    string key = $"{month} {year}";

                    if (!finalChartParse.ContainsKey(key)) {
                        finalChartParse.Add(key, 1);
                    } else {
                        finalChartParse[key]++;
                    }
                }

                //adding series will update and animate the chart automatically
                ColumnSeriesCollection.Add(new ColumnSeries {
                    Title = "Commits: ",
                    Values = new ChartValues<int>() { }
                });
                
                foreach (var pair in finalChartParse) {
                    Labels.Add(pair.Key);
                    ColumnSeriesCollection[0].Values.Add(pair.Value);
                }

                totalCommits += $"{commits.Count}";
                commitsFromLoggedUser += $"{loggedUserCommitsCount}";

                Dispatcher.Invoke(() => SetRepoLabelsText(totalCommits, commitsFromLoggedUser));

            } else {

                SetRepoLabelsText("There are no commits yet", "");
                CartesianChart chart = (CartesianChart)this.MainGrid.FindName("Columnchart");
                this.MainGrid.Children.Remove(chart);
            }

            
            // for reading repo stats from github
            /*IReadOnlyList<Repository> repos = client.Repository.GetAllForCurrent().Result;
            long repoID = 0;
            foreach (var repo in repos) {
                if (repo.Name == currentRepo) {
                    repoID = repo.Id;
                    break;
                }
            }

            if (repoID != 0) {

            Dispatcher.Invoke(() => StatsLabel.Content = $"{currentRepo} - {repoID}");
            client.Repository.Statistics.GetCommitActivity();*/

        }

        private List<string> GetCommitsFromGitLog() {
            List<string> lines = Cmd.RunAndRead("log", currentRepoPath);

            if (lines == null)
                return null;

            List<string> commits = new List<string>();
            string commit = "";
            foreach (string line in lines) {
                if (!line.Contains("Author:")) {
                    commit += line;
                } else {
                    commits.Add(commit);
                    commit = line;
                }
            }

            if (commit != "")
                commits.Add(commit);

            if (commits != null && commits.Count > 1) {
                commits.RemoveAt(0);
                return commits;
            }

            return null;
   
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
