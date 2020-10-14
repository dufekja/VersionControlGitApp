using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace VersionControlGitApp {
    public static class GithubController {

        public static GitHubClient Authenticate(GitHubClient client, string token) {

            var tokenAuth = new Credentials(token, AuthenticationType.Oauth);
            client.Credentials = tokenAuth;

            return client;
        }

        public static List<UserRepository> GetAllRepos(GitHubClient client) {

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
