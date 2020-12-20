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

        public string GetHtmlUrl() {
            return repository.HtmlUrl;
        }

        public string GetName() {
            return repository.Name;
        }
    }
}
