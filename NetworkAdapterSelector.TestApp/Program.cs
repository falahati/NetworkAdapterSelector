using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace NetworkAdapterSelector.TestApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if (DEBUG)
            if (Debugger.IsAttached)
            {
                Process.Start("NetworkAdapterSelector.Hook.exe",
                    "-d -n \"{39F954B1-F286-4156-8AF0-88FD057B3B91}\" -a " + Process.GetCurrentProcess().Id);
            }
#endif
            Application.Run(new Form1());
        }
    }
}
