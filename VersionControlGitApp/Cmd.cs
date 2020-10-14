using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp {
    public static class Cmd {
        public static bool Run(string command) {

            bool state = true;

            if (command != "")
                try {
                    var proc = new ProcessStartInfo();
                    proc.FileName = "cmd.exe";
                    proc.Arguments = command;
                    proc.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(proc);
                } catch {
                    state = false;
                }

            return state;
        }

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
