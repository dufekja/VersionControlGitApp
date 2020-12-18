using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;

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
                    Path = path
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
            bool state = Cmd.Run(command, path);
            if (state == true) {
                AddLocalRepo(dirPath, repoDB);
            }
        }

        public static void Init(string path, LocalRepoDB repoDB) {
            string command = $"init {path}";
            bool state = Cmd.Run(command, path);
            if (state == true) {
                AddLocalRepo(path, repoDB);
            }
        }

        public static void Commit(string path, string msg, string desc, MainWindow win) {
            bool state = false;
            bool tooShort = false;
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

                if (state)
                    ConsoleLogger.UserPopup("Commit", "Commit successful");
                else
                    ConsoleLogger.UserPopup("Commit", "There was an error");
            } else {
                ConsoleLogger.UserPopup("Commit", "There are no files to commit");
            }  
        }

        
        public static void Push(string path, GitHubClient client) {

            string name = GetNameFromPath(path);
            bool repoExists = GithubController.RepoExists(client, name);

            if (!repoExists) {
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show($"{name} not found. Would you like to create it ?", $"{name} not found", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes) {
                    client.Repository.Create(new NewRepository(name));
                    Task.Run(() => Cmd.PushRepo(client, name, path));
                }
            }
        }

        public static void Pull(string path, GitHubClient client) {
            Task.Run(() => Cmd.PullRepo(client, path));
        }

        public static void Fetch(string path, GitHubClient client) {
            string name = GetNameFromPath(path);
            bool repoExists = GithubController.RepoExists(client, name);

            if (repoExists) {
                List<string> lines = Cmd.RunAndRead("fetch --dry-run", path);
                string output = "";
                foreach (string line in lines) {
                    output += line;
                }
                ConsoleLogger.UserPopup("Fetch", $"{output}");
            } else {
                ConsoleLogger.UserPopup("Fetch", "Vybraný repozitář nebyl nalezen");
            }

        }

        public static List<string> GetBranches(string path) {
            List<string> lines = Cmd.RunAndRead("branch", path);

            if (lines == null)
                return null;

            return lines;
        }

        public static string GetCurrentBranch(string path) {
            List<string> lines = GetBranches(path);
            foreach (string line in lines) {
                if (line.Contains("*")) {
                    return line.Replace("*", "").Trim();
                }
            }
            return null;
        }

    }
}
