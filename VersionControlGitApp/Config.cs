using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp {
    public static class Config {

        /* State enumerators */ 
        public enum RepoChangesThreadState {
            New,
            Repeating,
            Aborting
        }

        public enum ConsoleState {
            Success,
            Error
        }

        public static string DATAPATH { get; set; }
        public static bool ISGITHUBUSER = true;

        /* Public constant variables */
        public const string GITHUB_PATH = "https://github.com/";
        public const string GITEXE = @"C:\Program Files\Git\cmd\git.exe";
        public const string CMD = @"C:\Windows\System32\cmd.exe";

        public const string HEADERMSG_GIT_STATUS = "Git status";
        public const string HEADERMSG_DELETE_CONF = "Delete confirmation";
        public const string HEADERMSG_REMOVE_CONF = "Remove confirmation";
        public const string HEADERMSG_DELETE_REPO = "Delete repository";
        public const string HEADERMSG_REMOVE_REPO = "Remove repository";
        public const string HEADERMSG_CREATE_REPO = "Create repository";
        public const string HEADERMSG_COMMIT_REPO = "Commit repository";
        public const string HEADERMSG_CLONE_REPO = "Clone repository";
        public const string HEADERMSG_PUSH_REPO = "Push repository";
        public const string HEADERMSG_PULL_REPO = "Pull repository"; 
        public const string HEADERMSG_FETCH_REPO = "Fetch repository";
        public const string HEADERMSG_BRANCH_RELATED = "Branch edit";
        public const string HEADERMSG_CHART_RELATED = "Chart loading";
        public const string HEADERMSG_STATISTICS = "Repository statistics";
        public const string HEADERMSG_TOKEN = "Private token";

        public const string ERRORMSG = "There was an error";
        public const string ERRORMSG_TOKEN = "Invalid token";
        public const string ERRORMSG_TOKEN_EXISTS = "Token already exists";
        public const string ERRORMSG_PERMISSIONS = "Unable due to token permissions";
        public const string STATUSMSG_SELECTREPO = "You must select repository first";
        public const string STATUSMSG_REPO_NOTFOUND = "Selected repository not found";
        public const string STATUSMSG_WAIT = "Waiting on changes in repository"; 
        public const string STATUSMSG_INSERT_TOKEN = "Please insert valid token";
        public const string STATUSMSG_NO_FILES = "There is nothing to commit";
        public const string STATUSMSG_PUSHING = "Pushing external repository";
        public const string STATUSMSG_PULLING = "Pulling external repository";

        public const string SUCCESSMSG_COMMIT = "Commit successful";

        public const string NEWFILE = "?? ";
        public const string MODIFIEDNEW = "M ";
        public const string MODIFIED = "MM ";
        public const string ADDMODIFIED = "AM ";
        public const string DELETED = "D ";
        public const string DELETEDMODIFIED = "DM ";

        /* Dictionary with month string to number */
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
