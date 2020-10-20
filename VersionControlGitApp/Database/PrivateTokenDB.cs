using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp.Database {
    [Table("Tokens")]
    public class Token {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Value { get; set; }
    }
    
    public class PrivateTokenDB {
        public static SQLiteConnection database = new SQLiteConnection("./tokens.db3");

        public void InitDB() {
            database.CreateTable<Token>();
        }

        public void WriteToken(string value) {
            database.Insert(new Token() {
                Value = value
            });
        }

        public string ReadTokens() {
            var tokens = database.Table<Token>();
            return tokens.First().Value;
        }

        public string GetFirstToken() {
            string token = "";
            if (database.Table<Token>().Count() != 0) {
                var tokens = database.Table<Token>();
                return tokens.First().Value;
            }
            return token;
        }

        public Token FindTokenByValue(string value) {
            var query = database.Query<Token>($"SELECT * FROM Tokens WHERE Value='{value}'");
            if (query.Count == 0) {
                return null;
            } else {
                return new Token() {
                    ID = query[0].ID,
                    Value = query[0].Value
                };
            }
        }

    }


}
