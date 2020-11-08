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
        public static GitHubClient Authenticate(GitHubClient client, string token) {
            ConsoleLogger.Info("GithubController", "Authenticating client");
            var tokenAuth = new Credentials(token, AuthenticationType.Oauth);
            client.Credentials = tokenAuth;
            return client;
        }

        /// <summary>
        /// Get list of authenticated user repositories
        /// </summary>
        /// <param name="client">Authenticated Github client object</param>
        /// <returns>Return List of UserRepository objects</returns>
        public static List<UserRepository> GetAllRepos(GitHubClient client) {
            ConsoleLogger.Info("GithubController", "Retrieving client repositories");
            List<UserRepository> userRepos = new List<UserRepository>();
            var repoList = client.Repository.GetAllForCurrent().Result;

            foreach (Repository repo in repoList) {
                UserRepository userRepo = new UserRepository(repo);
                userRepos.Add(userRepo);
            }
            return userRepos;
        }

    }
}
