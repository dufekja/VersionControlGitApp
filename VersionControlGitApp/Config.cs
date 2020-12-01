using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp {
    public static class Config {

        private const string GITHUB_PATH = "https://github.com/";

        public static string GetGithubPath() {
            return GITHUB_PATH;
        }

    }
}
