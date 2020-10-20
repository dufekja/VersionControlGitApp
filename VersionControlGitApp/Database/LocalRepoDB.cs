using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp.Database {
    [Table("Repositories")]
    public class Repo {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class LocalRepoDB {
        public static SQLiteConnection database = new SQLiteConnection("./repos.db3");

        public void InitDB() {
            database.CreateTable<Repo>();
        }

        public void WriteDB(Repo repo) {
            var obj = FindByName(repo.Name);
            if (obj == null) {
                database.Insert(repo);
                Console.WriteLine("\n New Klasik repo added");
            } else {
                Console.WriteLine("\n Repo klasik, but už zasunul");
            }
        }

        public Repo FindByName(string name) {
            var query = database.Query<Repo>($"SELECT * FROM Repositories WHERE Name='{name}'");
            if (query.Count == 0) {
                return null;
            } else {
                return new Repo() {
                    ID = query[0].ID,
                    Name = query[0].Name,
                    Path = query[0].Path
                };
            }
        }

        public void DeleteByName(string name) {
            try {
                database.Query<Repo>($"DELETE FROM Repositories WHERE Name='{name}'");
            } catch {
               
            }
        }

        public List<Repo> ReadDB() {
            var TBrepo = database.Table<Repo>();
            List<Repo> list = new List<Repo>();

            foreach (Repo repo in TBrepo) {
                list.Add(repo);
            }

            return list;
        }
    }
}
