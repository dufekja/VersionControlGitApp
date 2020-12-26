using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp {
    public static class Config {

        private const string GITHUB_PATH = "https://github.com/";
        public const string GITEXE = "git.exe";

        public const string NEWFILE = "?? ";
        public const string MODIFIEDNEW = "M ";
        public const string MODIFIED = "MM ";
        public const string ADDMODIFIED = "AM ";
        public const string DELETED = "D ";
        public const string DELETEDMODIFIED = "DM ";

        public const string USERMSG_SELECTREPO = "You must select repository first";

        public static string GetGithubPath() {
            return GITHUB_PATH;
        }

    }
}
