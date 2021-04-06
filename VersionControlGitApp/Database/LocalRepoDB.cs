using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp.Database {

    /// <summary>
    /// Repositories table structure
    /// </summary>
    [Table("Repositories")]
    public class Repo {
        [PrimaryKey, AutoIncrement] public int ID { get; set; }
        [NotNull, MaxLength(40)] public string Name { get; set; }
        [NotNull, MaxLength(200)] public string Path { get; set; }
        [NotNull, MaxLength(100)] public string User { get; set; }
    }

    public class LocalRepoDB {
        public static SQLiteConnection database = new SQLiteConnection($@"{Config.DATAPATH}\repos.db3");

        /// <summary>
        /// Init repository database
        /// </summary>
        public void InitDB() {
            database.CreateTable<Repo>();
        }

        /// <summary>
        /// Refresh database and return stored data
        /// </summary>
        /// <param name="user">Current logged user</param>
        /// <returns>Returns list of repositories</returns>
        public List<string> Refresh(string user) {
            List<Repo> repoList = ReadDB(user);
            List<string> deletedRepos = new List<string>();

            foreach (Repo repo in repoList) {
                string repoPath = repo.Path;
                if (!Directory.Exists(repoPath)) {
                    deletedRepos.Add(repo.Name);
                    DeleteByPath(repoPath);   
                } 
            }
            if (deletedRepos.Count == 0)
                deletedRepos = null;

            return deletedRepos;
        }

        /// <summary>
        /// Save repository to DB
        /// </summary>
        /// <param name="repo">Repository object</param>
        public void WriteDB(Repo repo) {
            if (FindByName(repo.Name) == null) {
                database.Insert(repo);
                ConsoleLogger.Success("LocalRepoDB", "New repository added");
            } else {
                ConsoleLogger.Warning("LocalRepoDB", "Repository already created");
            }
        }

        /// <summary>
        /// Find repository by name
        /// </summary>
        /// <param name="name">Repository name</param>
        /// <returns>Returns list of found repositories</returns>
        public List<Repo> FindByName(string name) {
            var result = database.Query<Repo>($"SELECT * FROM Repositories WHERE Name='{name}'");
            if (result.Count == 0) {
                return null;
            } else {
                return result;
            }
        }

        /// <summary>
        /// Delete repository by name
        /// </summary>
        /// <param name="name">Repository name</param>
        public void DeleteByName(string name) {
            try {
                database.Query<Repo>($"DELETE FROM Repositories WHERE Name='{name}'");
            } catch {
               
            }
        }

        /// <summary>
        /// Delete repository by path
        /// </summary>
        /// <param name="path">Repository path</param>
        public void DeleteByPath(string path) {
            try {
                database.Query<Repo>($"DELETE FROM Repositories WHERE Path='{path}'");
            } catch {
               
            }
        }

        /// <summary>
        /// Read database
        /// </summary>
        /// <param name="user">Currently logged user</param>
        /// <returns>Returns list of found repositories</returns>
        public List<Repo> ReadDB(string user) {
            List<Repo> list = new List<Repo>();
            try {
                var result = database.Query<Repo>($"SELECT * FROM Repositories WHERE User='{user}'");
                foreach (Repo repo in result) {
                    list.Add(repo);
                }
            } catch {
                return null;
            }
           
            return list;
        }
    }
}
