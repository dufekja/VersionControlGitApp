using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            // check if repo already exists in db
            if (repos != null) {
                foreach (Repo repo in repos) {
                    if (repo.Path == path) {
                        exist = true;
                    }     
                }
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

            // run repo clone
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
        /// <returns>Returns state of init command</returns>
        public static ConsoleState Init(string path, LocalRepoDB repoDB) {
            ConsoleState state = ConsoleState.Error;

            // repo must be one word
            string repo = GetNameFromPath(path);
            if (!repo.Trim().Contains(' ')) {

                string command = $"init {path}";
                state = Cmd.Run(command, path);

                if (state == ConsoleState.Success) {
                    AddLocalRepo(path, repoDB);
                }

            } else {
                ConsoleLogger.UserPopup(HEADERMSG_CREATE_REPO, "Repository cannot contain spaces");
            }

            return state;
           
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

                    // build commit string for git commit command in cmd 
                    string command = "commit -m ";
                    command += '"' + msg;

                    if (desc.Length > 0) {
                        command += '\n' + desc + '"';
                    }  else {
                        command += '"';
                    }
                        
                    state = Cmd.Run(command, path);
                }

                if (state == ConsoleState.Success) {
                    if (Cmd.HaveCommits(path)) {
                        ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, "Commit successful");
                    } else {
                        ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, "There is nothing to commit");
                    }
                } else {
                    ConsoleLogger.UserPopup(HEADERMSG_COMMIT_REPO, "There was an error");
                }   
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
                    $"{name} not found. Would you like to create it?",
                    $"{name} not found",
                    MessageBoxButton.YesNo);

                // create new repo and push it
                if (messageBoxResult == MessageBoxResult.Yes) {
                    client.Repository.Create(new NewRepository(name));

                    ConsoleLogger.StatusBarUpdate("Pushing external repository", win);
                    Task.Run(() => Cmd.PushRepo(client, name, path, win));
                }
            } else {
                ConsoleLogger.StatusBarUpdate("Pushing external repository", win);
                Task.Run(() => Cmd.PushRepo(client, name, path, win));
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
            bool hashMatch = false;

            if (repoExists) {
                // fetch external repository hash
                List<string> remoteHash = Cmd.RunAndRead("ls-remote origin -h refs/heads/master", path);
                List<string> localHash = Cmd.RunAndRead("rev-parse HEAD", path);
                
                ConsoleLogger.StatusBarUpdate("External repository fetched", win);

                if (remoteHash != null && localHash != null) {
                    // get hashes from list
                    string remoteHashRaw = remoteHash[0].Substring(0, 40);
                    string localHashRaw = localHash[0];

                    if (remoteHashRaw == localHashRaw) {
                        hashMatch = true;
                    }
                }

                if (hashMatch) {
                    ConsoleLogger.UserPopup(HEADERMSG_FETCH_REPO, $"There are no new changes in {name}");
                } else {
                    ConsoleLogger.UserPopup(HEADERMSG_FETCH_REPO, $"There are new changes in {name}");
                }

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

            // if no branches then master else go trough all branches and find *branch
            if (lines != null)
                foreach (string line in lines) {
                    if (line.Contains("*")) {
                        return line.Replace("*", "").Trim();
                    }
                }
            return "master";
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

        /// <summary>
        /// Get all changes between last commit and current version of repository
        /// </summary>
        /// <param name="file">File name</param>
        /// <param name="path">Repostory path</param>
        /// <returns>Returns diff output in list of each line - each line contains linenum, line and symbol</returns>
        public static List<string[]> GetAllFileChanges(string file, string path) {

            List<string[]> lineNumbered = new List<string[]>();

            // check for commits
            if (Cmd.HaveCommits(path)) {

                // read cmd git diff output
                List<string> diffOutput = Cmd.RunAndRead($"diff HEAD {file}", path);
                List<string> changeChunks = new List<string>();
                bool read = false;

                // remove not needed lines
                foreach (string line in diffOutput) {
                    if (read) {
                        if (!line.Contains("No newline at")) {
                            changeChunks.Add(line);
                        }
                    } else if (line.Contains("@@ -")) {
                        read = true;
                    }
                }

                // sort coloring and lines
                char prevSymbol = ' ';
                int lineNum = 1;
                foreach (string line in changeChunks) {
                    char symbol = line[0];

                    // empty symbol
                    if (symbol == ' ') {
                        lineNumbered.Add(new string[] { $"{lineNum}.", $"   {line}", " " });
                        prevSymbol = ' ';
                        lineNum++;

                        // minus symbol
                    } else if (symbol == '-') {
                        lineNumbered.Add(new string[] { $"{lineNum}.", $"   {line}", "-" });
                        lineNum++;
                        prevSymbol = '-';

                        // add symbol
                    } else if (symbol == '+') {
                        if (prevSymbol == ' ' || prevSymbol == '+') {
                            lineNumbered.Add(new string[] { $"{lineNum}.", $"   {line}", "+" });
                            lineNum++;
                        } else {
                            lineNumbered.Add(new string[] { $"{lineNum - 1}.", $"   {line}", "+" });
                        }
                        prevSymbol = '+';
                    }
                }

            } else {
                // in case of no commits show new file output
                List<string> newFile = new List<string>(File.ReadAllText($@"{path}\{file}").Split('\n'));
                int lineNum = 1;

                // add every line as new 
                foreach (string line in newFile) {
                    lineNumbered.Add(new string[] { $"{lineNum}.", $"   {line.Trim()}", "+" });
                    lineNum++;
                }
            }

            return lineNumbered;
            
        }

    }
}
