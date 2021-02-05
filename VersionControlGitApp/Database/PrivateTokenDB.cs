using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp.Database {
    [Table("Tokens")]
    public class Token {
        [PrimaryKey, AutoIncrement] public int ID { get; set; }
        [NotNull, MaxLength(25)] public string User { get; set; }
        [NotNull, MaxLength(40), Unique] public string Value { get; set; }
        [NotNull] public int IsActive { get; set; }
    }
    
    public class PrivateTokenDB {
        public static SQLiteConnection database = new SQLiteConnection("./tokens.db3");

        /// <summary>
        /// Init token database
        /// </summary>
        public void InitDB() {
            database.CreateTable<Token>();
        }

        /// <summary>
        /// Save token into db
        /// </summary>
        /// <param name="value"></param>
        /// <param name="user"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        public bool WriteToken(string value, string user, int isActive) {
            try {
                database.Insert(new Token() {
                    Value = value,
                    User = user,
                    IsActive = isActive
                });
                return true;
            } catch {
                return false;
            }


        }

        public Token GetFirstToken(string user) {
            if (database.Table<Token>().Count() != 0) {
                var tokens = database.Table<Token>();
                return tokens.First();
            }
            return null;
        }

        public List<Token> FindTokensByUser(string user) {
            var result = database.Query<Token>($"SELECT * FROM Tokens WHERE User='{user}'");
            if (result.Count == 0) {
                return null;
            } else {
                return result;
            }
        }

        public Token GetActiveToken(string user) {
            var result = database.Query<Token>($"SELECT * FROM Tokens WHERE User='{user}'");

            if (result.Count > 0) {
                foreach (Token token in result) {
                    if (token.IsActive == 1) {
                        return token;
                    }
                }
            }
            return null;
        }

        public Token FindTokenByValue(string value) {
            var result = database.Query<Token>($"SELECT * FROM Tokens WHERE Value='{value}'");
            if (result.Count == 0) {
                return null;
            } else {
                return new Token() {
                    ID = result[0].ID,
                    Value = result[0].Value,
                    User = result[0].User,
                    IsActive = result[0].IsActive
                };
            }
        }

        public bool UpdateTokenByValue(string value, int isActive) {
            try {
                database.Query<Token>($"UPDATE Tokens SET IsActive='{isActive}' WHERE Value='{value}'");
                return true;
            } catch {
                return false;
            }
            
        }

    }


}
