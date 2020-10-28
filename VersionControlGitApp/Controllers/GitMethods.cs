using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VersionControlGitApp.Database;

namespace VersionControlGitApp.Controllers {
    public static class GitMethods {

        /// <summary>
        /// Add local repository to local repo db 
        /// </summary>
        /// <param name="path">Path to repository folder</param>
        /// <param name="repoDB">Initiated repodDB object</param>
        public static bool AddLocalRepo(string path, LocalRepoDB repoDB) {

            bool created = false;
            bool exist = false; 
            List<Repo> repos = repoDB.FindByName(GetNameFromPath(path));
            if (repos != null)
                foreach (Repo repo in repos) {
                    if (repo.Path == path)
                        exist = true;
                }

            if (IsRepo(path) && !exist) {
                Repo repo = new Repo() {
                    Name = GetNameFromPath(path),
                    Path = path
                };
                repoDB.WriteDB(repo);

                MessageBox.Show("Repozitář přidán", "Info");
                created = true;
            } else {
                MessageBox.Show("Nejdená se o repozitář", "Info");
            }

            return created;
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
        /// Get last word from given URL
        /// </summary>
        /// <param name="URL">URL string </param>
        /// <returns>Returns last word string</returns>
        public static string GetNameFromURL(string URL) {
            string[] arr = URL.Split('/');
            arr = arr[arr.Length - 1].Split('.');
            return arr[0];
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

            string dirName = GetNameFromURL(URL);

            string dirPath = path + @"\" + dirName;

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string command = $@"/C git clone {URL} {dirPath}";
            bool state = Cmd.Run(command);
            if (state == true) {
                AddLocalRepo(dirPath, repoDB);
                MessageBox.Show("Repozitář naklonován", "Info");
            }
        }

        public static void Init(string path, LocalRepoDB repoDB) {
            string command = $@"/C git init {path}";
            bool state = Cmd.Run(command);
            if (state == true) {
                AddLocalRepo(path, repoDB);
                MessageBox.Show("Repozitář vytvořen", "Info");
            } else {
                MessageBox.Show("Už to je repozitář", "Info");
            }
        }

    }
}
