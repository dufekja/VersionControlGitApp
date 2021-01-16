using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionControlGitApp.Logging;

namespace VersionControlGitApp.Database {
    [Table("Repositories")]
    public class Repo {
        [PrimaryKey, AutoIncrement] public int ID { get; set; }
        [NotNull, MaxLength(40)] public string Name { get; set; }
        [NotNull, MaxLength(200)] public string Path { get; set; }
        [NotNull, MaxLength(100)] public string User { get; set; }
    }

    public class LocalRepoDB {
        public static SQLiteConnection database = new SQLiteConnection("./repos.db3");

        public void InitDB() {
            database.CreateTable<Repo>();
        }

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

        public void WriteDB(Repo repo) {
            var obj = FindByName(repo.Name);
            if (obj == null) {
                database.Insert(repo);
                ConsoleLogger.Success("LocalRepoDB", "New repository added");
            } else {
                ConsoleLogger.Warning("LocalRepoDB", "Repository already created");
            }
        }

        public List<Repo> FindByName(string name) {
            var result = database.Query<Repo>($"SELECT * FROM Repositories WHERE Name='{name}'");
            if (result.Count == 0) {
                return null;
            } else {
                return result;
            }
        }

        public void DeleteByName(string name) {
            try {
                database.Query<Repo>($"DELETE FROM Repositories WHERE Name='{name}'");
            } catch {
               
            }
        }

        public void DeleteByPath(string path) {
            try {
                database.Query<Repo>($"DELETE FROM Repositories WHERE Path='{path}'");
            } catch {
               
            }
        }

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
