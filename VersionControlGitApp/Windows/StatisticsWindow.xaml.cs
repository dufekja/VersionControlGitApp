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
using System.Threading;

namespace VersionControlGitApp.Windows {
    public partial class StatisticsWindow : Window {

        public static MainWindow mainWin;
        public static GitHubClient client;
        public static LocalRepoDB repoDB;
        public static User user;
        public static string header;
        public static string currentRepo;
        public static string currentRepoPath;

        public SeriesCollection PieSeriesCollection { get; set; }

        // column chart data
        public SeriesCollection ColumnSeriesCollection { get; set; }
        public List<string> Labels { get; set; }
        public Func<int, string> Formatter { get; set; }

        public static Thread chartThread;

        /// <summary>
        /// Statistics window constructor
        /// </summary>
        /// <param name="_win">Reference to instance of mainwindow thread</param>
        /// <param name="_client">Github client instance</param>
        /// <param name="_user">User instance</param>
        /// <param name="_repoDB">Reference to repository database instance</param>
        /// <param name="_header">Type of statistics to show</param>
        /// <param name="_currentRepoPath">Currently selected repository</param>
        public StatisticsWindow(MainWindow _win, GitHubClient _client, User _user, LocalRepoDB _repoDB, string _header, string _currentRepoPath) {

            mainWin = _win;
            client = _client;
            user = _user;
            repoDB = _repoDB;
            header = _header;
            currentRepoPath = _currentRepoPath;
            currentRepo = GitMethods.GetNameFromPath(_currentRepoPath);

            InitializeComponent();

            // in case of user statistics
            if (header == "User") {

                // series for pie chart
                PieSeriesCollection = new SeriesCollection();
                
                // remove column chart 
                CartesianChart chart = (CartesianChart)MainGrid.FindName("Columnchart");
                MainGrid.Children.Remove(chart);

                chartThread = new Thread(() => GenerateUserData());
                chartThread.Start();

            // in case of repository statistics
            } else if (header == "Repository") {

                // remove pie chart 
                PieChart chart = (PieChart)MainGrid.FindName("Piechart");
                MainGrid.Children.Remove(chart);

                chartThread = new Thread(() => GenerateRepoData());
                chartThread.Start();
                
            }

        }

        /// <summary>
        /// Call methods to obtain data for user statistics
        /// </summary>
        private void GenerateUserData() {

            // set public and private repo labels count
            Dispatcher.Invoke(() => SetRepoLabelsText(
                $"Owned public repositories: {user.PublicRepos}", 
                $"Owned private repositories: {user.TotalPrivateRepos}"));

            // calculate commit activity for each repo
            Dispatcher.Invoke(() => SetPieChart());
        }

        /// <summary>
        /// Returns coommits made in repository by year timespan
        /// </summary>
        /// <returns>Returns list with repo tuples</returns>
        private List<string[]> GenerateYearlyUserCommitActivity() {
            int commitCount = 0;
            IReadOnlyList<Repository> repos = client.Repository.GetAllForCurrent().Result;
            if (repos == null)
                return null;

            List<string[]> reposWithActivity = new List<string[]>();
            foreach (var repo in repos) {

                // get activity for past year
                var yearCommitActivity = client.Repository.Statistics.GetCommitActivity(repo.Id).Result;
                int totalYearActivity = 0;

                // add week activity
                foreach (var week in yearCommitActivity.Activity) {
                    totalYearActivity += week.Total;
                }

                // add year activity for repostiory to commit count
                commitCount += totalYearActivity;

                // create repository tuples with name and commits for past year
                string[] repoTuple = new string[2];
                repoTuple[0] = $"{repo.Name}";
                repoTuple[1] = $"{totalYearActivity}";

                reposWithActivity.Add(repoTuple);
            }

            // set commit count text to commit count
            SetCommitCount(commitCount);

            return reposWithActivity;
        }

        /// <summary>
        /// Get data from user and generate pie chart
        /// </summary>
        private void SetPieChart() {
            List<string[]> repositories = GenerateYearlyUserCommitActivity();

            if (repositories != null) {
                foreach (string[] repo in repositories) {

                    ObservableValue commits = new ObservableValue(int.Parse(repo[1])); 

                    // new pie chart series
                    var pie = new PieSeries() {
                        Title = repo[0],
                        Values = new ChartValues<ObservableValue> { commits },
                        DataLabels = true
                    };

                    PieSeriesCollection.Add(pie);
                }

                DataContext = this;

                Dispatcher.Invoke(() => LoadingLabel.Content = "");

            } else {
                ConsoleLogger.UserPopup(HEADERMSG_CHART_RELATED, "There are no repositories on user Github");
            }
  
        }

        /// <summary>
        /// Call methods to obtain repository data
        /// </summary>
        private void GenerateRepoData() {
            // invoke chart and repo labels info
            Dispatcher.Invoke(() => SetRepoLabelsText("", ""));
            Dispatcher.Invoke(() => GenerateRepoDataCommand());
        }

        /// <summary>
        /// Set commit count to UI text element
        /// </summary>
        private void SetCommitCount(int commitCount) {
            Dispatcher.Invoke(() => NumberOfCommits.Content = $"Commits for year: {commitCount}");
        }

        /// <summary>
        /// Obtain single repository data from git log function and fill columnt chart with it
        /// </summary>
        private void GenerateRepoDataCommand() {
           
            string totalCommits = $"Total commits in {currentRepo}: ";
            string commitsFromLoggedUser = "";
            string userName = "";

            // show user commits only if user has valid token
            if (ISGITHUBUSER) {
                userName = user.Login.ToString().ToLower();
                commitsFromLoggedUser = $"Commits from {userName}: ";
            } else {
                PrivateReposLabel.Visibility = Visibility.Hidden;
            }

            // generate from local git func
            List<string> commits = GetCommitsFromGitLog();
            Dictionary<string, int> finalChartParse = new Dictionary<string, int>();

            if (commits != null) {

                // parse text commits to dictionary
                int loggedUserCommitsCount = 0;
                foreach (string commit in commits) {
                    if (commit.Contains(userName)) {
                        loggedUserCommitsCount++;
                    }

                    // parse data from given string 
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

                // create new column series collection (same colored columns)
                ColumnSeriesCollection = new SeriesCollection {
                    new ColumnSeries {
                        Title = "Commits: ",
                        Values = new ChartValues<int> {}
                    }
                };

                Labels = new List<string> { };

                // fill in data
                foreach (var pair in finalChartParse) {
                    Labels.Add($"{pair.Key}");
                    ColumnSeriesCollection[0].Values.Add(pair.Value);
                }

                // format to Y axis
                Formatter = value => value.ToString("N");
                DataContext = this;

                // global info labels
                totalCommits += $"{commits.Count}";
                commitsFromLoggedUser += $"{loggedUserCommitsCount}";
                
                // set global labels
                Dispatcher.Invoke(() => SetRepoLabelsText(totalCommits, commitsFromLoggedUser));
                Dispatcher.Invoke(() => LoadingLabel.Content = "");

            } else {
                SetRepoLabelsText("There are no commits yet", "");
                CartesianChart chart = (CartesianChart)MainGrid.FindName("Columnchart");
                MainGrid.Children.Remove(chart);
                Dispatcher.Invoke(() => LoadingLabel.Content = "");
            }

        }

        /// <summary>
        /// Call git log and return lines crom cmd
        /// </summary>
        /// <returns>Returns list of lines from cmd</returns>
        private List<string> GetCommitsFromGitLog() {
            List<string> lines = Cmd.RunAndRead("log", currentRepoPath);

            if (lines == null)
                return null;

            List<string> commits = new List<string>();
            string commit = "";

            // add every line that contains author reference
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

        /// <summary>
        /// Method to set global info labels
        /// </summary>
        /// <param name="publicLabel">First label</param>
        /// <param name="privateLabel">Second label</param>
        private void SetRepoLabelsText(string publicLabel, string privateLabel) {
            PublicReposLabel.Content = $"{publicLabel}";
            PrivateReposLabel.Content = $"{privateLabel}";
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
