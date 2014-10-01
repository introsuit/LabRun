using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ScrS_viewer_
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
            Form1 form1 = new Form1();
            if (args.Length != 0) { form1.host = args[0]; }
            Application.Run(form1);

        }
    }
}
