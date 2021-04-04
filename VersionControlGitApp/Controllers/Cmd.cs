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
using static VersionControlGitApp.Config;

namespace VersionControlGitApp.Controllers {
    public static class Cmd {

        /// <summary>
        /// Run git async command using window terminal
        /// </summary>
        /// <param name="command">Given command</param>
        /// <param name="dir">Directory where to execute command</param>
        /// <returns>ConsoleState variable which returns Success or Error</returns>
        public static ConsoleState Run(string command, string dir) {

            // return error on empty values
            if (command == "" || dir == "")
                return ConsoleState.Error;

            // catch errors
            try {
                // process info setup
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = GITEXE,
                    Arguments = command,
                    CreateNoWindow = true,
                    WorkingDirectory = dir,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // start process and wait for exit
                Process process = Process.Start(startInfo);
                process.WaitForExit();
            } catch {
                return ConsoleState.Error;
            }

            return ConsoleState.Success;
        }

        /// <summary>
        /// Run git async command using window terminal and return output
        /// </summary>
        /// <param name="command">Given command</param>
        /// <param name="dir">Directory where to execute command</param>
        /// <returns>Returns list of output lines</returns>
        public static List<string> RunAndRead(string command, string dir) {

            // return error on empty values
            if (command == "" || dir == "")
                return null;

            List<string> output = new List<string>();
            try {
                // setup process start info
                ProcessStartInfo startInfo = new ProcessStartInfo() {
                    FileName = GITEXE,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    WorkingDirectory = dir,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    Arguments = command
                };

                // create new process with startInfo
                Process process = new Process() { StartInfo = startInfo };
                process.Start();
                
                // read console output
                string line = process.StandardOutput.ReadLine();
                while (line != null) {
                    output.Add(line);
                    line = process.StandardOutput.ReadLine();
                }
                process.WaitForExit();
            } catch {
                output = null;
            }

            return output;
        }

        /// <summary>
        /// Set attributes in subfolders so they can be deleted
        /// </summary>
        /// <param name="dir">Directory info object</param>
        public static void SetAttributesNormal(DirectoryInfo dir) {
            foreach (var subDir in dir.GetDirectories()) {
                SetAttributesNormal(subDir);
            }
                
            foreach (var file in dir.GetFiles()) {
                file.Attributes = FileAttributes.Normal;
            }
        }

        /// <summary>
        /// Parse string between 2 delimeters
        /// </summary>
        /// <param name="text">Given text</param>
        /// <param name="firstDelimeter">First delimeter</param>
        /// <param name="secondDelimeter">Second delimeter</param>
        /// <returns>Returns string between delimeters</returns>
        public static string Explode(string text, string firstDelimeter, string secondDelimeter) {
            // split string from both sides by tags
            string[] arr = text.Split(new string[] { firstDelimeter }, StringSplitOptions.None);
            string[] arr2 = arr[1].Split(new string[] { secondDelimeter }, StringSplitOptions.None);
            return arr2[0];
        }

        /// <summary>
        /// Check for unstracked files in repository
        /// </summary>
        /// <param name="path">Given path of repository</param>
        /// <returns>Returns list of untracked files</returns>
        public static List<string> UntrackedFiles(string path) {
            // run raw git sattus output command
            List<string> modifiedFiles = RunAndRead("status --porcelain", path);
            List<string> output = new List<string>();

            if (modifiedFiles == null) {
                return null;
            }

            // for each untracked file
            foreach (string line in modifiedFiles) {
                if (!line.Contains("AD ")) {
                    output.Add(line.Substring(3));
                }
            }

            return output;
        }
        
        /// <summary>
        /// Check for modified files in selected repository
        /// </summary>
        /// <param name="path">Path to selected repository</param>
        /// <returns>Returns list of modified files</returns>
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

            if (wasModified) {
                return output;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Set database path to appdata (each user have theirs DB)
        /// </summary>
        public static void SetDBPath(string path) {
            // set datapath to local user
            DATAPATH = path;

            // create app directory if there is none yet
            if (!Directory.Exists(DATAPATH)) {
                Directory.CreateDirectory(DATAPATH);
            }
        }

        /// <summary>
        /// Add file to watching index
        /// </summary>
        /// <param name="files">List of files</param>
        /// <param name="path">Path to selected repository</param>
        public static void AddFile(List<string> files, string path) {
            foreach (string file in files) {
                Run($"add {file}", path);
            }
        }

        /// <summary>
        /// Remove files that was removed
        /// </summary>
        /// <param name="path">Selected repository</param>
        /// <returns>Returns list of deleted files</returns>
        public static List<string> RemovedFiles(string path) {
            List<string> files = RunAndRead("status --porcelain", path);
            List<string> output = new List<string>();
            bool wasModified = false;

            foreach (string line in files) {
                if (line.Contains("AD ")) {
                    output.Add(line.Replace("AD ", "").Trim());
                    wasModified = true;
                } else if (line.Contains("D ")) {
                    output.Add(line.Replace("D ", "").Trim());
                    wasModified = true;
                } else if (line.Contains("MD ")) {
                    output.Add(line.Replace("MD ", "").Trim());
                    wasModified = true;
                } 
            }

            if (wasModified) {
                return output;
            } else {
                return null;
            }
        }

        /// <summary>
        /// Push selected repository to Github
        /// </summary>
        /// <param name="client">Github client object</param>
        /// <param name="name">Repository name</param>
        /// <param name="path">Repository path</param>
        public static void PushRepo(GitHubClient client, string name, string path, MainWindow win) {

            // wait 5 seconds for connection in repo pushing
            int counter = 0;
            while (true) {
                Thread.Sleep(1000);
                bool repoExists = GithubController.RepoExists(client, name);

                win.Dispatcher.Invoke(() => ConsoleLogger.StatusBarUpdate($"Pushing repository | Time: {counter}s", win));

                if (repoExists || counter > 5) {
                    break;
                } else {
                    counter++;
                }
                    
            }

            // after 5 seconds
            if (counter < 5) {
                // get user instance and get external repository path
                User user = client.User.Current().Result;
                string externalRepoPath = $"{GITHUB_PATH}{user.Login}/{name}.git";

                // run git commands to link and push repository
                Run($"remote add origin {externalRepoPath}", path);
                Run($"push -u origin master", path);

                ConsoleLogger.UserPopup(HEADERMSG_PUSH_REPO, $"Data pushed from {path} to {externalRepoPath}");
            } else {
                ConsoleLogger.UserPopup(HEADERMSG_PUSH_REPO, $"Failed to push data to external repository");
            }
        }

        /// <summary>
        /// Pull selected repository from Github
        /// </summary>
        /// <param name="client">Github client object</param>
        /// <param name="path">External repository path</param>
        public static void PullRepo(GitHubClient client, string path, MainWindow win) {

            // wait 5 seconds for connection in repo pulling
            int counter = 0;
            string name = GitMethods.GetNameFromPath(path);
            while (true) {
                Thread.Sleep(1000);
                bool repoExists = GithubController.RepoExists(client, name);

                win.Dispatcher.Invoke(() => ConsoleLogger.StatusBarUpdate($"Pulling repository | Time: {counter}s", win));

                if (repoExists || counter > 5) {
                    break;
                } else {
                    counter++;
                }
                    
            }
            
            if (counter < 5) {
                User user = client.User.Current().Result;
                string externalRepoPath = $"{GITHUB_PATH}{user.Login}/{name}.git";

                // run pull git command
                Run("pull", path);
                ConsoleLogger.UserPopup(HEADERMSG_PULL_REPO, $"Pulled from {externalRepoPath} to {path}");

            } else {
                ConsoleLogger.UserPopup(HEADERMSG_PULL_REPO, $"Failed to pull data from external repository");
            }
        }

        /// <summary>
        /// Checks if repository has commits
        /// </summary>
        /// <param name="path">Repository path</param>
        /// <returns>Returns bool if repository have commits</returns>
        public static bool HaveCommits(string path) {
            // return log output
            List<string> lines = RunAndRead("log", path);
            bool haveCommits = false;

            // search for selected substring
            if (lines != null) {
                foreach (string line in lines) {
                    if (!line.Contains("fatal: ")) {
                        haveCommits = true;
                    }
                }      
            }

            return haveCommits;
        }

    }
}
