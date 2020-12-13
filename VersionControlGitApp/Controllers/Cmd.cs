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
                        FileName = "git.exe",
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
                        FileName = "git.exe",
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
            List<string> output = RunAndRead("ls-files . --exclude-standard --others", path);
            return output;
        }
        
        public static List<string> ModifiedFiles(string path) {
            List<string> files = RunAndRead("status --porcelain", path);
            List<string> output = new List<string>();
            bool wasModified = false;

            foreach (string line in files) {
                if (line.Contains("M ")) {
                    output.Add(line.Replace("M ","").Trim());
                    wasModified = true;
                }
            }

            if (wasModified)
                return output;
            else
                return null;
        }

        public static void AddFile(List<string> files, string path) {
            foreach (string file in files) {
                ConsoleLogger.Info("Cmd", file);
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
                }
            }

            if (wasModified)
                return output;
            else
                return null;
        }

        public static void RemoveFile(List<string> files, string path) {
            foreach (string file in files) {
                Cmd.Run($"rm {file}", path);
            }
        }

        public static List<string> FilesForCommit(string path, MainWindow win) {
            List<string> output = Cmd.RunAndRead("status", path);
            List<string> files = new List<string>();

            if (output != null) {
                foreach (string line in output) {
                    if (line.Contains("new file:   ")) {
                        string file = line.Replace("new file:", "").Trim();

                        ConsoleLogger.Info("Cmd", $"file: {file}");
                        if (File.Exists($@"{win.PathLabel.Text}\{file}")) {
                            files.Add(file);
                        }
                            
                    }
                }
            }
            
            return files;
        }
    }
}
