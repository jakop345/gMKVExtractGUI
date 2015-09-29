using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace gMKVToolnix
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // If on Linux, set the enironmnet variables for locale to C (default locale)
            if (gMKVHelper.IsLinux)
            {
                Environment.SetEnvironmentVariable("LC_ALL", "C", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("LANG", "C", EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("LC_MESSAGES", "C", EnvironmentVariableTarget.Process);
            }
            Application.Run(new frmMain());
        }
    }
}
