using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionControlGitApp.Logging {
    public static class ConsoleLogger {
        
        public static void Success(string text) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Log.Success - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Warning(string text) {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Log.Warning - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Info(string text) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Log.Info - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Error(string text) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Log.Error - {text}");
            Console.ForegroundColor = ConsoleColor.White;
        }

    }
}
