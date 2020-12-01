using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VersionControlGitApp.Logging {
    public static class ConsoleLogger {
        
        public static void Success(string file, string text) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Log.Success.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Warning(string file, string text) {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Log.Warning.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Info(string file, string text) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Log.Info.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Error(string file, string text) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Log.Error.{file} - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Popup(string file, string text) {
            MessageBox.Show($"Popup.Info.{file} - {text}");
        }

    }
}
