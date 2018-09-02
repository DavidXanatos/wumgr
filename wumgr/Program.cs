using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskScheduler;

namespace wumgr
{
    static class Program
    {
        public static String fmt(string str, params object[] args)
        {
            return string.Format(str, args);
        }

        public static string[] args = null;
        public static bool mConsole = false;
        public static string mVersion = "0.0";
        public static string mName = "Windows Update Manager";
        private static string nTaskName = "WuMgrNoUAC";

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Program.args = args;

            mConsole = WinConsole.Initialize(TestArg("-console"));

            if (TestArg("-help") || TestArg("/?"))
            {
                ShowHelp();
                return;
            }

            Console.WriteLine("Starting...");

            AppLog Log = new AppLog();

#if DEBUG
            Test();
#endif

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            mVersion = fvi.FileMajorPart + "." + fvi.FileMinorPart;

            if (IsAdministrator() == false)
            {
                Console.WriteLine("Trying to get admin privilegs...");
                if (!SkipUacRun())
                {
                    // Restart program and run as admin
                    var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    string arguments = "\"" + string.Join("\" \"", args) + "\"";
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeName, arguments);
                    startInfo.Verb = "runas";
                    System.Diagnostics.Process.Start(startInfo);
                }
                Application.Exit();
                return;
            }

            Console.WriteLine("Alloc AppLog...");

            WuAgent Agent = new WuAgent();

            Console.WriteLine("Alloc WuAgent...");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WuMgr());

            for (int i = 0; i < Program.args.Length; i++)
            {
                if (Program.args[i].Equals("-onclose", StringComparison.CurrentCultureIgnoreCase))
                    Process.Start(Program.args[++i]);
            }
        }

        static private void Test()
        {
            return;
        }

        private static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool TestArg(string name)
        {
            for (int i = 0; i < Program.args.Length; i++)
            {
                if (Program.args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        static public bool IsSkipUacRun()
        {
            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(@"\"); // root
                IRegisteredTask task = folder.GetTask(nTaskName);
                return task != null;
            }
            catch (Exception err){}
            return false;
        }

        static public bool SkipUacEnable(bool is_enable)
        {
            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(@"\"); // root
                if (is_enable)
                {
                    ITaskDefinition task = service.NewTask(0);
                    task.RegistrationInfo.Author = "WuMgr";
                    task.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_HIGHEST;

                    task.Settings.AllowHardTerminate = false;
                    task.Settings.StartWhenAvailable = false;
                    task.Settings.DisallowStartIfOnBatteries = false;
                    task.Settings.StopIfGoingOnBatteries = false;
                    task.Settings.MultipleInstances = _TASK_INSTANCES_POLICY.TASK_INSTANCES_PARALLEL;
                    task.Settings.ExecutionTimeLimit = "PT0S";

                    IExecAction action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                    action.Path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    action.WorkingDirectory = Directory.GetCurrentDirectory();
                    action.Arguments = "$(Arg0)";

                    IRegisteredTask registered_task = folder.RegisterTaskDefinition(nTaskName, task, (int)_TASK_CREATION.TASK_CREATE_OR_UPDATE, null, null, _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN);

                    return registered_task != null;
                }
                else
                {
                    folder.DeleteTask(nTaskName, 0);
                    return true;
                }
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("SkipUacEnable Error {0}", err.ToString()));
                return false;
            }
        }

        static public bool SkipUacRun()
        {
            try
            {
                TaskScheduler.TaskScheduler service = new TaskScheduler.TaskScheduler();
                service.Connect();
                ITaskFolder folder = service.GetFolder(@"\"); // root
                IRegisteredTask task = folder.GetTask(nTaskName);

                IExecAction action = (IExecAction)task.Definition.Actions[1];
                if (action.Path.Equals(System.Reflection.Assembly.GetExecutingAssembly().Location, StringComparison.CurrentCultureIgnoreCase))
                {
                    string arguments = "\"" + string.Join("\" \"", args) + "\"";

                    IRunningTask running_Task = task.RunEx(arguments, (int)_TASK_RUN_FLAGS.TASK_RUN_NO_FLAGS, 0, null);

                    for (int i = 0; i < 5; i++)
                    {
                        Thread.Sleep(250);
                        running_Task.Refresh();
                        _TASK_STATE state = running_Task.State;
                        if (state == _TASK_STATE.TASK_STATE_RUNNING || state == _TASK_STATE.TASK_STATE_READY || state == _TASK_STATE.TASK_STATE_DISABLED)
                        {
                            if (state == _TASK_STATE.TASK_STATE_RUNNING || state == _TASK_STATE.TASK_STATE_READY)
                                return true;
                            break;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                AppLog.Line(Program.fmt("SkipUacRun Error {0}", err.ToString()));
            }
            return false;
        }

        private static void ShowHelp()
        {
            string Message = "Available command line options\r\n";
            string[] Help = {"-tray\t\tStart in Tray",
                                    "-onclose [cmd]\tExecute commands when closing",
                                    "-update\t\tSearch for updates on start",
                                    "-console\t\tshow console (for debugging)",
                                    "-help\t\tShow this help message" };
            if (!mConsole)
                MessageBox.Show(Message + string.Join("\r\n", Help));
            else
            {
                Console.WriteLine(Message);
                for (int j = 0; j < Help.Length; j++)
                    Console.WriteLine(" " + Help[j]);
            }
        }
    }
}
