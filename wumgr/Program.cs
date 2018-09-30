using Microsoft.Win32;
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
using System.Collections.Specialized;

namespace wumgr
{
    static class Program
    {
        public static string[] args = null;
        public static bool mConsole = false;
        public static string mVersion = "0.0";
        public static string mName = "Update Manager for Windows";
        private static string nTaskName = "WuMgrNoUAC";
        public static string appPath = "";
        private static string mINIPath = "";
        public static WuAgent Agent = null;

        public const Int32 WM_USER = 0x0400;
        public const Int32 WM_APP = 0x0800; // to 0xBFFF
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, String lpWindowName);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int SendMessageTimeout(IntPtr windowHandle, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

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

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            mVersion = fvi.FileMajorPart + "." + fvi.FileMinorPart;

            AppLog Log = new AppLog();
            AppLog.Line(MiscFunc.fmt("{0}, Version v{1} by David Xanatos", mName, mVersion));
            AppLog.Line(MiscFunc.fmt("This Tool is Open Source under the GNU General Public License, Version 3\r\n"));

            appPath = Path.GetDirectoryName(Application.ExecutablePath);

            if (!TestArg("-NoUAC"))
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        AppLog.Line(MiscFunc.fmt("Application is already running. Only one instance of this application is allowed"));
                        //IntPtr WindowToFind = FindWindow(null, Program.mName);
                        IntPtr result = IntPtr.Zero;
                        if (SendMessageTimeout(process.MainWindowHandle, WM_APP, IntPtr.Zero, IntPtr.Zero, 0, 3000, out result) == 0)
                        {
                            MessageBox.Show(MiscFunc.fmt("Application is already running, but not responding.\r\nClose it using a task manager and restart."));
                        }
                        //SetForegroundWindow(process.MainWindowHandle);
                        return;
                    }
                }
            }


            mINIPath = appPath + @"\wumgr.ini";

            /*switch(FileOps.TestFileAdminSec(mINIPath))
            {
                case 0:
                    AppLog.Line(MiscFunc.fmt("Warning wumgr.ini was writable by non administrative users, it was renamed to wumgr.ini.old and replaced with a empty one.\r\n"));
                    if (!FileOps.MoveFile(mINIPath, mINIPath + ".old", true))
                        return;
                    goto case 2;
                case 2: // file missing, create
                    FileOps.SetFileAdminSec(mINIPath);
                    break;
                case 1: // every thign's fine ini file is only writable by admins
                    break;
            }*/

#if DEBUG
            Test();
#endif

            if (IsAdministrator() == false)
            {
                Console.WriteLine("Trying to get admin privilegs...");
                if (!SkipUacRun())
                {
                    // Restart program and run as admin
                    var exeName = Process.GetCurrentProcess().MainModule.FileName;
                    string arguments = "\"" + string.Join("\" \"", args) + "\"";
                    ProcessStartInfo startInfo = new ProcessStartInfo(exeName, arguments);
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    try
                    {
                        Process.Start(startInfo);
                    }
                    catch
                    {
                        MessageBox.Show(MiscFunc.fmt("The {0} requirers Administrator privilegs.\r\nPlease restart the application as Administrator.\r\n\r\nYou can use the option Start->'Bypass User Account Control' to solve this issue for future startsups.", mName), mName);
                    }
                }
                Application.Exit();
                return;
            }

            Agent = new WuAgent();

            ExecOnStart();

            Agent.Init();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WuMgr());

            Agent.UnInit();

            ExecOnClose();
        }

        static private void ExecOnStart()
        {
            if (int.Parse(IniReadValue("OnStart", "EnableWuAuServ", "0")) != 0)
                Agent.EnableWuAuServ(true);

            string OnStart = IniReadValue("OnStart", "Exec", "");
            if (OnStart.Length > 0)
                DoExec(PrepExec(OnStart, MiscFunc.parseInt(IniReadValue("OnStart", "Silent", "1")) != 0), true);
        }

        static private void ExecOnClose()
        {
            string OnClose = IniReadValue("OnClose", "Exec", "");
            if (OnClose.Length > 0)
                DoExec(PrepExec(OnClose, MiscFunc.parseInt(IniReadValue("OnClose", "Silent", "1")) != 0), true);

            if (int.Parse(IniReadValue("OnClose", "DisableWuAuServ", "0")) != 0)
                Agent.EnableWuAuServ(false);

            // Note: With the UAC bypass the onclose parameter can be used for a local privilege escalation exploit
            if (!TestArg("-NoUAC"))
            {
                for (int i = 0; i < Program.args.Length; i++)
                {
                    if (Program.args[i].Equals("-onclose", StringComparison.CurrentCultureIgnoreCase))
                        DoExec(PrepExec(Program.args[++i], true));
                }
            }
        }

        static public ProcessStartInfo PrepExec(string command, bool silent = true)
        {
            // -onclose """cm d.exe"" /c ping 10.70.0.1" -test
            int pos = -1;
            if (command.Length > 0 && command.Substring(0, 1) == "\"")
            {
                command = command.Remove(0, 1).Trim();
                pos = command.IndexOf("\"");
            }
            else
                pos = command.IndexOf(" ");

            string exec;
            string arguments = "";
            if (pos != -1)
            {
                exec = command.Substring(0, pos);
                arguments = command.Substring(pos + 1).Trim();
            }
            else
                exec = command;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = exec;
            startInfo.Arguments = arguments;
            if (silent)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
            }
            return startInfo;
        }

        static public bool DoExec(ProcessStartInfo startInfo, bool wait = false)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo = startInfo;
                proc.EnableRaisingEvents = true;
                proc.Start();
                if (wait)
                    proc.WaitForExit();
            }
            catch
            {
                return false;
            }
            return true;

        }

        static private void Test()
        {
            
        }

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        public static void IniWriteValue(string Section, string Key, string Value, string INIPath = null)
        {
            WritePrivateProfileString(Section, Key, Value, INIPath != null ? INIPath : mINIPath);
        }

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, [In, Out] char[] retVal, int size, string filePath);
        public static string IniReadValue(string Section, string Key, string Default = "", string INIPath = null)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(Section, Key, Default, chars, 8193, INIPath != null ? INIPath : mINIPath);
            return new String(chars, 0, size);
        }

        public static string[] IniEnumSections(string INIPath = null)
        {
            char[] chars = new char[8193];
            int size = GetPrivateProfileString(null, null, null, chars, 8193, INIPath != null ? INIPath : mINIPath);
            return new String(chars, 0, size).Split('\0');
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

        public static string GetArg(string name)
        {
            for (int i = 0; i < Program.args.Length; i++)
            {
                if (Program.args[i].Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    string temp = Program.args[i + 1];
                    if (temp.Length > 0 && temp[0] != '-')
                        return temp;
                    return "";
                }
            }
            return null;
        }

        public static void AutoStart(bool enable)
        {
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enable)
            {
                string value = "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"" + " -tray";
                subKey.SetValue("wumgr", value);
            }
            else
                subKey.DeleteValue("wumgr", false);
        }

        public static bool IsAutoStart()
        { 
            var subKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return (subKey != null && subKey.GetValue("wumgr") != null);
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
            catch {}
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
                    action.WorkingDirectory = appPath;
                    action.Arguments = "-NoUAC $(Arg0)";

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
                AppLog.Line(MiscFunc.fmt("SkipUacEnable Error {0}", err.ToString()));
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
                AppLog.Line(MiscFunc.fmt("SkipUacRun Error {0}", err.ToString()));
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
