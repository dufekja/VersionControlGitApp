using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp.Controllers {
    public static class Cmd {

        /// <summary>
        /// Run cmd commnad
        /// </summary>
        /// <param name="command">String of given commnad</param>
        /// <returns>Returns command run state (success/fail)</returns>
        public static bool Run(string command) {

            bool state = true;

            if (command != "")
                try {
                    var process = new ProcessStartInfo();
                    process.FileName = "cmd.exe";
                    process.Arguments = command;
                    process.WindowStyle = ProcessWindowStyle.Hidden;
                    var proc = Process.Start(process);

                    proc.WaitForExit();

                } catch {
                    state = false;
                }

            return state;
        }

        /// <summary>
        /// Run commnad and read its output
        /// </summary>
        /// <param name="command">String of given commnad</param>
        /// <returns>Returns string of commnad output</returns>
        public static string RunAndRead(string command) {

            string output = "";
            if (command != "") {
                try {
                    Process proc = new Process();
                    proc.StartInfo.FileName = "cmd.exe";
                    proc.StartInfo.Arguments = command;
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.Start();

                    output = proc.StandardOutput.ReadToEnd();
                } catch {
                    output = "";
                }
            }
    
            return output; 
        }
    }
}
