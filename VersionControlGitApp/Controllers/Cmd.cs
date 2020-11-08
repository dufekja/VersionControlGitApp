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
            List<string> output = Cmd.RunAndRead("ls-files . --exclude-standard --others", path);
            return output;
        }

        public static List<string> FilesForCommit(string path) {
            List<string> output = Cmd.RunAndRead("status", path);
            List<string> files = new List<string>();

            if (files != null) {
                foreach (string line in output) {
                    if (line.Contains("new file:   ")) {
                        string file = Explode(line, "new file:   ", ".txt") + ".txt";
                        files.Add(file);
                    }
                }
            }
            
            return files;
        }
    }
}
