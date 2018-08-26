using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace wumgr
{
    static class Program
    {
        public static String fmt(string str, params object[] args)
        {
            return string.Format(str, args);
        }

        public static string[] args = null;

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Program.args = args;

            AppLog Log = new AppLog();

            WuAgent Agent = new WuAgent();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WuMgr());

            for (int i = 0; i < Program.args.Length; i++)
            {
                if (Program.args[i].Equals("-onclose", StringComparison.CurrentCultureIgnoreCase))
                {
                    Process.Start(Program.args[++i]);
                }
            }
        }
    }
}
