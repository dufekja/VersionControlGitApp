using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp.Database {

    /// <summary>
    /// Tokens table structure
    /// </summary>
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
        /// <param name="value">Token value</param>
        /// <param name="user">Username</param>
        /// <param name="isActive">Is active int (0,1)</param>
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

        /// <summary>
        /// Get first token of usser
        /// </summary>
        /// <param name="user">Username</param>
        /// <returns>Returns token object</returns>
        public Token GetFirstToken(string user) {
            if (database.Table<Token>().Count() != 0) {
                var tokens = database.Table<Token>();
                return tokens.First();
            }
            return null;
        }

        /// <summary>
        /// Find token by user
        /// </summary>
        /// <param name="user">Username</param>
        /// <returns>Returns list of all user tokens</returns>
        public List<Token> FindTokensByUser(string user) {
            var result = database.Query<Token>($"SELECT * FROM Tokens WHERE User='{user}'");
            if (result.Count == 0) {
                return null;
            } else {
                return result;
            }
        }

        /// <summary>
        /// Get user active token
        /// </summary>
        /// <param name="user">Username</param>
        /// <returns>Return current user active token</returns>
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

        /// <summary>
        /// Find token by inserted value
        /// </summary>
        /// <param name="value">Token value</param>
        /// <returns>Returns token object or null</returns>
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

        /// <summary>
        /// Update token isActive value by token value
        /// </summary>
        /// <param name="value">Value of token</param>
        /// <param name="isActive">Bool of new value</param>
        /// <returns>Returns bool with success</returns>
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
