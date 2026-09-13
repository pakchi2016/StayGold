using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace Orfevre
{
    public partial class App
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
    public class BuildInfo
    {
        public Dictionary<int, string> Type { get; set; }
        public Dictionary<int, string> Version { get; set; }

        public BuildInfo()
        {
            Type = new Dictionary<int, string>
                {
                    {0, "dotnet new console" },
                    {1, "dotnet new wpf" },
                    {2, "dotnet new winforms" },
                    {3, "dotnet new classlib" }

                }; 
            Version = new Dictionary<int, string>
                {
                    {0, "-f net10.0" },
                    {1, "--target-framework-override net48 -lang c#" }
                };
        }
    }
}
