using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp {
    public static class Config {

        public enum RepoChangesThreadState {
            New,
            Repeating,
            Aborting
        }

        public enum ConsoleState {
            Success,
            Error
        }

        public const string GITHUB_PATH = "https://github.com/";
        public const string GITEXE = "git.exe";

        public const string USERMSG_SELECTREPO = "You must select repository first";
        public const string HEADERMSG_DELETE_CONF = "Delete confirmation";
        public const string HEADERMSG_REMOVE_CONF = "Remove confirmation";
        public const string HEADERMSG_DELETE_REPO = "Delete repository";
        public const string HEADERMSG_REMOVE_REPO = "Remove repository";
        public const string HEADERMSG_CREATE_REPO = "Create repository";
        public const string HEADERMSG_COMMIT_REPO = "Commit repository";
        public const string HEADERMSG_PUSH_REPO = "Push repository";
        public const string HEADERMSG_PULL_REPO = "Pull repository"; 
        public const string HEADERMSG_FETCH_REPO = "Fetch repository";
        public const string HEADERMSG_BRANCH_RELATED = "Branch edit";
        public const string HEADERMSG_CHART_RELATED = "Chart loading";

        public const string ERROR_MSG = "There was an error";

        public const string NEWFILE = "?? ";
        public const string MODIFIEDNEW = "M ";
        public const string MODIFIED = "MM ";
        public const string ADDMODIFIED = "AM ";
        public const string DELETED = "D ";
        public const string DELETEDMODIFIED = "DM ";

        public static Dictionary<string, int> monthsIndexes = new Dictionary<string, int>() {
            { "Jan", 1 },
            { "Feb", 2 },
            { "Mar", 3 },
            { "Apr", 4 },
            { "May", 5 },
            { "Jun", 6 },
            { "Jul", 7 },
            { "Aug", 8 },
            { "Sep", 9 },
            { "Oct", 10 },
            { "Nov", 11 },
            { "Dec", 12 },
        };

    }
}
