using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace VersionControlGitApp.Logging {
    public static class ConsoleLogger {
        
        /// <summary>
        /// Success log in code
        /// </summary>
        /// <param name="file">Position of logging</param>
        /// <param name="text">Shown message</param>
        public static void Success(string file, string text) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Log.Success.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Warning log in code
        /// </summary>
        /// <param name="file">Position of logging</param>
        /// <param name="text">Shown message</param>
        public static void Warning(string file, string text) {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Log.Warning.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Info log in code
        /// </summary>
        /// <param name="file">Position of logging</param>
        /// <param name="text">Shown message</param>
        public static void Info(string file, string text) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Log.Info.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Error log in code
        /// </summary>
        /// <param name="file">Position of logging</param>
        /// <param name="text">Shown message</param>
        public static void Error(string file, string text) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Log.Error.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Messagebox user popup
        /// </summary>
        /// <param name="header">Messagebox header</param>
        /// <param name="text">Messagebox text</param>
        public static void UserPopup(string header, string text) {
            MessageBox.Show(text, header);
        }

        /// <summary>
        /// Mainwindow statusbar update
        /// </summary>
        /// <param name="text">Text to show</param>
        /// <param name="win">Reference to main window thread instance</param>
        public static void StatusBarUpdate(string text, MainWindow win) {
            win.Dispatcher.Invoke(() => win.ActionStatusBarLabel.Content = text.ToString());
        }


    }
}
