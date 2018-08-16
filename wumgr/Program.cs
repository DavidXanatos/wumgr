using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppLog Log = new AppLog();

            WuAgent Agent = new WuAgent();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WuMgr());
        }
    }
}
