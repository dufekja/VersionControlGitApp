using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;
using static VersionControlGitApp.Config;

namespace VersionControlGitApp.Controllers {
    public static class GitMethods {

        /// <summary>
        /// Add local repository to local repo db 
        /// </summary>
        /// <param name="path">Path to repository folder</param>
        /// <param name="repoDB">Initiated repodDB object</param>
        public static bool AddLocalRepo(string path, LocalRepoDB repoDB) {

            bool created = false;
            bool exist = false; 
            List<Repo> repos = repoDB.FindByName(GetNameFromPath(path));

            if (repos != null)
                foreach (Repo repo in repos) {
                    if (repo.Path == path)
                        exist = true;
                }

            if (IsRepo(path) && !exist) {
                Repo repo = new Repo() {
                    Name = GetNameFromPath(path),
                    Path = path,
                    User = SystemInformation.UserName
                };
                repoDB.WriteDB(repo);
                created = true;
            }

            return created;
        }

        /// <summary>
        /// Get word after last slash in given url
        /// </summary>
        /// <param name="path">Url to get name from</param>
        /// <returns>Returns string of last word from url</returns>
        public static string GetNameFromPath(string path) {
            string[] arr = path.Split(Convert.ToChar(92));
            return arr[arr.Length - 1];
        }

        /// <summary>
        /// Get last word from given URL
        /// </summary>
        /// <param name="URL">URL string </param>
        /// <returns>Returns last word string</returns>
        public static string GetNameFromURL(string URL) {
            string[] arr = URL.Split('/');
            arr = arr[arr.Length - 1].Split('.');
            return arr[0];
        }

        /// <summary>
        /// Check if directory contains .git folder
        /// </summary>
        /// <param name="path">Path of given folder</param>
        /// <returns>Returns bool if folder is repo</returns>
        public static bool IsRepo(string path) {
            bool status = false;
            if (Directory.Exists(path + @"\.git")) {
                status = true;
            }
            return status;
        }

        /// <summary>
        /// Clone repository from external source
        /// </summary>
        /// <param name="URL">Url of external repo</param>
        /// <param name="path">Path where to clone repo</param>
        /// <param name="repoDB">Initiated LocalRepoDB object</param>
        public static void Clone(string URL, string path, LocalRepoDB repoDB) {

            string dirName = GetNameFromURL(URL);
            string dirPath = path + @"\" + dirName;

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string command = $"clone {URL} {dirPath}";
            ConsoleState state = Cmd.Run(command, path);
            if (state == ConsoleState.Success) {
                AddLocalRepo(dirPath, repoDB);
            }
        }

        /// <summary>
        /// Init git repository
        /// </summary>
        /// <param name="path">Path to repository</param>
        /// <param name="repoDB">RepoDB object</param>
        public static void Init(string path, LocalRepoDB repoDB) {
            string command = $"init {path}";
            ConsoleState state = Cmd.Run(command, path);
            if (state == ConsoleState.Success) {
                AddLocalRepo(path, repoDB);
            }
        }

        /// <summary>
        /// Commit all watched changes
        /// </summary>
        /// <param name="path">Path to selected repository</param>
        /// <param name="msg">Commit message</param>
        /// <param name="desc">Commit description</param>
        /// <param name="win">Reference to MainWindow object</param>
        public static void Commit(string path, string msg, string desc, MainWindow win) {
            ConsoleState state = ConsoleState.Error;
            List<string> lines = Cmd.RunAndRead("status --porcelain", path);

            if (lines != null) {
                if (msg.Length > 0) {
                    string command = "commit -m ";
                    command += '"' + msg;

                    if (desc.Length > 0)
                        command += '\n' + desc + '"';
                    else
                        command += '"';
                    state = Cmd.Run(command, path);
                }

                if (state == ConsoleState.Success)
                    ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, "Commit successful");
                else
                    ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, "There was an error");
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, "There are no files to commit");
            }  
        }

        /// <summary>
        /// User push interaction method
        /// </summary>
        /// <param name="path">Selected repository path</param>
        /// <param name="client">Github client</param>
        /// <param name="win">Reference to MainWindow object</param>
        public static void Push(string path, GitHubClient client, MainWindow win) {

            string name = GetNameFromPath(path);
            bool repoExists = GithubController.RepoExists(client, name);

            if (!repoExists) {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $"{name} not found. Would you like to create it ?",
                    $"{name} not found",
                    MessageBoxButton.YesNo);

                if (messageBoxResult == MessageBoxResult.Yes) {
                    ConsoleLogger.StatusBarUpdate("Pushing external repository", win);
                    client.Repository.Create(new NewRepository(name));
                    Task.Run(() => Cmd.PushRepo(client, name, path, win));
                }
            }
        }

        /// <summary>
        /// User pull interaction
        /// </summary>
        /// <param name="path">Selected repository path</param>
        /// <param name="client">Github client</param>
        /// <param name="win">Reference to MainWindow object</param>
        public static void Pull(string path, GitHubClient client, MainWindow win) {
            ConsoleLogger.StatusBarUpdate("Pulling external repository", win);
            Task.Run(() => Cmd.PullRepo(client, path, win));
        }

        /// <summary>
        /// Fetch selected repository
        /// </summary>
        /// <param name="path">Selected repository path</param>
        /// <param name="client">Github client</param>
        /// <param name="win">Reference to MainWindow object</param>
        public static void Fetch(string path, GitHubClient client, MainWindow win) {
            string name = GetNameFromPath(path);
            bool repoExists = GithubController.RepoExists(client, name);

            if (repoExists) {
                List<string> lines = Cmd.RunAndRead("fetch --dry-run", path);
                string output = "";
                foreach (string line in lines) {
                    output += line;
                }
                ConsoleLogger.StatusBarUpdate("External repository fetched", win);
                ConsoleLogger.UserPopup(HEADERMSG_FETCH_REPO, $"{output}");
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_FETCH_REPO, "Selected repository not found");
            }

        }

        /// <summary>
        /// Get list of branches for repository
        /// </summary>
        /// <param name="path">Repository path</param>
        /// <returns>Returns list of branches</returns>
        public static List<string> GetBranches(string path) {
            List<string> lines = Cmd.RunAndRead("branch", path);

            if (lines == null)
                return null;

            return lines;
        }

        /// <summary>
        /// Get current repository branch
        /// </summary>
        /// <param name="path">Repository path</param>
        /// <returns>Returns current repository branch</returns>
        public static string GetCurrentBranch(string path) {
            List<string> lines = GetBranches(path);
            if (lines != null)
                foreach (string line in lines) {
                    if (line.Contains("*")) {
                        return line.Replace("*", "").Trim();
                    }
                }
            return null;
        }

        /// <summary>
        /// Get all changes between last commit and current version of repository
        /// </summary>
        /// <param name="file">File name</param>
        /// <param name="path">Repostory path</param>
        /// <returns>Returns diff output</returns>
        public static string GetAllFileChanges(string file, string path) {
            string ret = "";
            List<string> diffSummary = Cmd.RunAndRead($"diff HEAD {file}", path);

            // filter changed lines
            List<string> diffSummaryOutput = new List<string>();
            bool read = false;
            foreach (string line in diffSummary) {
                if (read) {
                    if (!line.Contains("newline")) {
                        diffSummaryOutput.Add(line);
                        ret += line + "\n";
                    }
                } else if (line.Contains("@@")) {
                    read = true;
                }
            }

            List<string> outputContentList = new List<string>(File.ReadAllText($@"{path}\{file}").Split('\n'));      
            return ret;
        }

        /// <summary>
        /// Updates private token of logged user
        /// </summary>
        /// <param name="token">Private token</param>
        /// <param name="win">Reference to MainWindow object</param>
        public static bool UpdatePrivateTokenCommand(string token, PrivateTokenDB tokenDB) {

            // get active token and set it unactive
            Token tk = tokenDB.GetActiveToken(SystemInformation.UserName);
            if (tk != null) {

                if (tk.Value == token) {
                    ConsoleLogger.UserPopup("Private token", "This token is already active");
                    return false;
                }

                // update old to false
               bool updated = tokenDB.UpdateTokenByValue(tk.Value, 0);

               if (updated) {
                    Token updateToken = tokenDB.FindTokenByValue(token);

                    if (updateToken != null) {
                        tokenDB.UpdateTokenByValue(token, 1);
                    } else {
                        tokenDB.WriteToken(token, SystemInformation.UserName, 1);
                    }

                    return true;
                }
            }

            return false;
        }

    }
}
