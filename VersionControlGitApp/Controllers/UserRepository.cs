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

        /// <summary>
        /// User repository construcotr
        /// </summary>
        /// <param name="repository">Repository name</param>
        public UserRepository(Repository repository) {
            this.repository = repository;
        }

        /// <summary>
        /// Get repository url
        /// </summary>
        /// <returns>Returns url string</returns>
        public string GetHtmlUrl() {
            return repository.HtmlUrl;
        }

        /// <summary>
        /// Get repository name
        /// </summary>
        /// <returns>Returns repository name string</returns>
        public string GetName() {
            return repository.Name;
        }
    }
}
