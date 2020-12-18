using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp {
    public static class Config {

        private const string GITHUB_PATH = "https://github.com/";
        private const string NEWFILE_SYMBOL = "?? ";
        private const string MODIFIED_SYMBOL = "?? ";
        private const string NECO = "?? ";

        public static string GetGithubPath() {
            return GITHUB_PATH;
        }

        public static string GetNewfileSymbol() {
            return NEWFILE_SYMBOL;
        }

    }
}
