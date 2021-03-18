using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp.Controllers {
    public static class GithubController {

        /// <summary>
        /// Authenticate as github user using private token
        /// </summary>
        /// <param name="client">Github client object</param>
        /// <param name="token">User private token</param>
        /// <returns>Return authenticated Github client object</returns>
        public static GitHubClient Authenticate(GitHubClient client, string token, MainWindow win) {
            win.Dispatcher.Invoke(() => ConsoleLogger.StatusBarUpdate("Authenticating user", win));

            try {
                var tokenAuth = new Credentials(token, AuthenticationType.Oauth);
                client.Credentials = tokenAuth;
                win.Dispatcher.Invoke(() => ConsoleLogger.StatusBarUpdate("User successfully authenticated", win));
            } catch {
                win.Dispatcher.Invoke(() => ConsoleLogger.StatusBarUpdate("There was an error with authenticating", win));
            }

            return client;
        }

        /// <summary>
        /// Get list of authenticated user repositories
        /// </summary>
        /// <param name="client">Authenticated Github client object</param>
        /// <returns>Return List of UserRepository objects</returns>
        public static List<UserRepository> GetAllRepos(GitHubClient client) {
            List<UserRepository> userRepos = new List<UserRepository>();
            var repoList = client.Repository.GetAllForCurrent().Result;

            foreach (Repository repo in repoList) {
                UserRepository userRepo = new UserRepository(repo);
                userRepos.Add(userRepo);
            }
            return userRepos;
        }

        /// <summary>
        /// Method to check if repository exists
        /// </summary>
        /// <param name="client">GitHubClient authenticated object</param>
        /// <param name="name">Repository name</param>
        /// <returns></returns>
        public static bool RepoExists(GitHubClient client, string name) {
            IReadOnlyList<Repository> repos = client.Repository.GetAllForCurrent().Result;

            foreach (Repository repo in repos) {
                if (repo.Name == name)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Method to set user globals into system
        /// </summary>
        /// <param name="user"></param>
        /// <param name="path"></param>
        public static void SetGlobals(User user, string path) {
            string login = user.Login.ToString();
            string email = user.Email.ToString();

            Cmd.Run("config --global user.name " + login, path);
            Cmd.Run("config --global user.email " + email, path);
        }

    }
}
