using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp.Controllers {
    public static class Cmd {

        public static bool Run(string command, string dir) {

            bool state = true;

            if (command != "")
                try {
                    ProcessStartInfo startInfo = new ProcessStartInfo {
                        FileName = Config.GITEXE,
                        Arguments = command,
                        CreateNoWindow = true,
                        WorkingDirectory = dir,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    Process process = Process.Start(startInfo);

                    process.WaitForExit();
                } catch {
                    state = false;
                }
            return state;
        }

        public static List<string> RunAndRead(string command, string dir) {

            List<string> output = new List<string>();
            if (command != "") {
                try {

                    ProcessStartInfo startInfo = new ProcessStartInfo() {
                        FileName = Config.GITEXE,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false,
                        WorkingDirectory = dir,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        Arguments = command
                    };

                    Process process = new Process {
                        StartInfo = startInfo
                    };

                    process.Start();
                    string line = process.StandardOutput.ReadLine();
                    while (line != null) {
                        output.Add(line);
                        line = process.StandardOutput.ReadLine();
                    }
                    process.WaitForExit();
                } catch {
                    output = null;
                }

            }

            return output;
        }

        public static string Explode(string text, string firstDelimeter, string secondDelimeter) {
            string[] arr = text.Split(new string[] { firstDelimeter }, StringSplitOptions.None);
            string[] arr2 = arr[1].Split(new string[] { secondDelimeter }, StringSplitOptions.None);
            return arr2[0];
        }

        public static List<string> UntrackedFiles(string path) {
            List<string> modifiedFiles = RunAndRead("status --porcelain", path);
            List<string> output = new List<string>();

            foreach (string line in modifiedFiles) {
                output.Add(line.Substring(3));
            }

            return output;
        }
        
        public static List<string> ModifiedFiles(string path) {
            List<string> files = RunAndRead("status --porcelain", path);
            List<string> output = new List<string>();
            bool wasModified = false;

            if (files != null) {
                foreach (string line in files) {
                    if (line.Contains("M ")) {
                        output.Add(line.Replace("M ", "").Trim());
                        wasModified = true;
                    } else if (line.Contains("MM ")) {
                        output.Add(line.Replace("MM", "").Trim());
                        wasModified = true;
                    } else if (line.Contains("AM ")) {
                        output.Add(line.Replace("AM", "").Trim());
                        wasModified = true;
                    }
                }
            }

            if (wasModified)
                return output;
            else
                return null;
        }

        public static void AddFile(List<string> files, string path) {
            foreach (string file in files) {
                Cmd.Run($"add {file}", path);
            }
        }

        public static List<string> RemovedFiles(string path) {
            List<string> files = Cmd.RunAndRead("status --porcelain", path);
            List<string> output = new List<string>();
            bool wasModified = false;

            foreach (string line in files) {
                if (line.Contains("D ")) {
                    output.Add(line.Replace("D ", "").Trim());
                    wasModified = true;
                } else if (line.Contains("MD ")) {
                    output.Add(line.Replace("MD ", "").Trim());
                    wasModified = true;
                }
            }

            if (wasModified)
                return output;
            else
                return null;
        }

        public static void PushRepo(GitHubClient client, string name, string path) {

            int counter = 0;
            while (true) {
                Thread.Sleep(1000);
                bool repoExists = GithubController.RepoExists(client, name);

                if (repoExists || counter > 10)
                    break;
                else
                    counter++;
            }

            
            if (counter <= 10) {
                User user = client.User.Current().Result;
                string externalRepoPath = $"{Config.GetGithubPath()}{user.Login}/{name}.git";

                Cmd.Run($"remote add origin {externalRepoPath}", path);
                Cmd.Run($"push -u origin master", path);

                ConsoleLogger.UserPopup("Repository push success", $"Data pushed from {path} to {externalRepoPath}");
            } else {
                ConsoleLogger.UserPopup("Repository push error", $"Failed to push data to external repository");
            }
        }

        public static void PullRepo(GitHubClient client, string path) {

            string name = GitMethods.GetNameFromPath(path);
            bool repoExists = GithubController.RepoExists(client, name);

            if (repoExists) {
                User user = client.User.Current().Result;
                string externalRepoPath = $"{Config.GetGithubPath()}{user.Login}/{name}.git";

                Cmd.Run("pull", path);
                ConsoleLogger.UserPopup("Repository pull", $"Pulled from {externalRepoPath} to {path}");

            } else {
                ConsoleLogger.UserPopup("Repository pull", $"Repozitář neexistuje");
            }
        }
    }
}
