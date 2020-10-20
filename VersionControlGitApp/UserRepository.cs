using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionControlGitApp.Controllers;

namespace VersionControlGitApp {
    public class UserRepository {

        private protected Repository repository;

        public UserRepository(Repository repository) {
            this.repository = repository;
        }

        private string GetHtmlUrl() {
            return repository.HtmlUrl;
        }

        public void Init(string path) {
            string command = $@"/C git init {path}";

            Cmd.Run(command);
        }

        public void Status(string path) {

            string command = $@"/C cd {path} && git status ";

            if (Directory.Exists($@"{path}\.git")) {
                string status = Cmd.RunAndRead(command);
                Console.Write(status);
            }

        }

        public void Clone(string path) {

            string URL = $@"{this.GetHtmlUrl()}.git";
            string command = $@"/C git clone {URL} {path}";

            Cmd.Run(command);         
        }

    }
}
