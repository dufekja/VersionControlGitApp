using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VersionControlGitApp.Database;
using VersionControlGitApp.Logging;
using VersionControlGitApp.UIelements;

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

        public static void Status(string path) {
            List<string> untrackedFiles = Cmd.UntrackedFiles(path);

        }

        public static void Commit(string path, string msg, string desc) {

            List<string> lines = Cmd.RunAndRead("status --porcelain", path);
            bool uncommitedFiles = false;

            foreach (string line in lines) {
                if (line.Contains("A  "))
                    uncommitedFiles = true;
            }

            if (msg != "" && uncommitedFiles == true) {
                if (desc != "")
                    msg += $" -m {desc}";
                string command = $"commit -m {msg}";

                bool state = Cmd.Run(command, path);
                if (state)
                    ConsoleLogger.Success("GitMethods", "Files commited");
            }
        }

        public static void Push(string path, User user, GitHubClient client) {


            // TODO -> zmenit var cesta na path a integrovat tuto metodu s tlacitkem (async)

            string cesta = @"C:\Users\jandu\Desktop\diowjdwq";
            string name = GetNameFromPath(cesta);
            string externalRepoPath = @"https://github.com/";

            bool repoExists = GithubController.RepoExists(client, name);
            if (!repoExists)
                client.Repository.Create(new NewRepository(name));

            externalRepoPath += $"{user.Login}/{name}.git";

            Cmd.Run($"remote add origin {externalRepoPath}", path);
            Cmd.Run($"push -u origin master", path);

            ConsoleLogger.Success("GitMethods", $"Pushed from {cesta} to {externalRepoPath}");
        }

        public static bool Fetch(string path) {
            string name = GetNameFromPath(path);

            return true;
        }

    }
}
