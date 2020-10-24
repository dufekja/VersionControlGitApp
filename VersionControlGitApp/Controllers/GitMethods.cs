using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionControlGitApp.Database;

namespace VersionControlGitApp.Controllers {
    public static class GitMethods {

        /// <summary>
        /// Add local repository to local repo db 
        /// </summary>
        /// <param name="path">Path to repository folder</param>
        /// <param name="repoDB">Initiated repodDB object</param>
        public static void AddLocalRepo(string path, LocalRepoDB repoDB) {
            if (IsRepo(path) == true) {
                Repo repo = new Repo() {
                    Name = GetNameFromPath(path),
                    Path = path
                };
                repoDB.WriteDB(repo);
                Console.WriteLine("\n\nKlasik\n");
            } else {
                Console.WriteLine("\nNormal folder goes Brrrrr");
            }
        }

        /// <summary>
        /// Get word after last slash in given url
        /// </summary>
        /// <param name="path">Url to get name from</param>
        /// <returns>Returns string of last word from url</returns>
        public static string GetNameFromPath(string path) {
            string[] arr = path.Split(Convert.ToChar(92));
            return arr[arr.Length - 1];
        }

        /// <summary>
        /// Check if directory contains .git folder
        /// </summary>
        /// <param name="path">Path of given folder</param>
        /// <returns>Returns bool if folder is repo</returns>
        public static bool IsRepo(string path) {

            bool status = false;
            if (Directory.Exists(path + @"\.git")) {
                status = true;
            }
            return status;
        }

        /// <summary>
        /// Clone repository from external source
        /// </summary>
        /// <param name="URL">Url of external repo</param>
        /// <param name="path">Path where to clone repo</param>
        /// <param name="repoDB">Initiated LocalRepoDB object</param>
        public static void Clone(string URL, string path, LocalRepoDB repoDB) {

            string command = $@"/C git clone {URL} {path}";
            bool state = Cmd.Run(command);
            if (state == true) {
                AddLocalRepo(path, repoDB);
            }
        }
    }
}
