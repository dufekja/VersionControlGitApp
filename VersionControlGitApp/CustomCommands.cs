using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VersionControlGitApp {
    
    #region CustomCommands
    public static class CustomCommands {
        public static readonly RoutedUICommand FetchCommand = new
            RoutedUICommand("Fetch Command", "FetchCommand", typeof(CustomCommands));
    }
    #endregion
}
