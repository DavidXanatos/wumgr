using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace wumgr
{
    class UpdateInstaller
    {
        List<MsUpdate> mUpdates = null;
        private MultiValueDictionary<string, string> mAllFiles = null;
        private int mCurrentTask = 0;
        private Thread mThread = null;
        private Dispatcher mDispatcher;
        private bool Canceled = false;
        private bool RebootRequired = false;
        private int ErrorCount = 0;
        private bool DoInstall = true;

        public UpdateInstaller()
        {
            mDispatcher = Dispatcher.CurrentDispatcher;
        }

        private void Reset()
        {
            ErrorCount = 0;
            RebootRequired = false;
            Canceled = false;
            mCurrentTask = 0;
        }

        public bool Install(List<MsUpdate> Updates, MultiValueDictionary<string, string> AllFiles)
        {
            Reset();
            mUpdates = Updates;
            mAllFiles = AllFiles;
            DoInstall = true;

            NextUpdate();
            return true;
        }

        public bool UnInstall(List<MsUpdate> Updates)
        {
            Reset();
            mUpdates = Updates;
            DoInstall = false;

            NextUpdate();
            return true;
        }

        public bool IsBusy()
        {
            return mUpdates != null;
        }

        public void CancelOperations()
        {
            Canceled = true;
        }

        private void NextUpdate()
        {
            if (!Canceled && mUpdates.Count > mCurrentTask)
            {
                int Percent = 0; // Note: there does not seem to be an easy way to get this value
                Progress?.Invoke(this, new WuAgent.ProgressArgs(mUpdates.Count, mUpdates.Count == 0 ? 0 : (100 * mCurrentTask + Percent) / mUpdates.Count, mCurrentTask + 1, Percent, mUpdates[mCurrentTask].Title));

                if (DoInstall)
                {
                    List<string> Files = mAllFiles.GetValues(mUpdates[mCurrentTask].KB);

                    mThread = new Thread(new ParameterizedThreadStart(RunInstall));
                    mThread.Start(Files);
                }
                else
                {
                    string KB = mUpdates[mCurrentTask].KB;

                    mThread = new Thread(new ParameterizedThreadStart(RunUnInstall));
                    mThread.Start(KB);
                }
                return;
            }

            FinishedEventArgs args = new FinishedEventArgs(ErrorCount, RebootRequired);
            //args.AllFiles = mAllFiles;
            args.Updates = mUpdates;
            mAllFiles = null;
            mUpdates = null;
            Finished?.Invoke(this, args);
        }

        private void OnFinished(bool success, bool reboot)
        {
            if (!success)
                ErrorCount++;
            if (reboot)
                RebootRequired = true;

            mThread.Join();
            mThread = null;

            mCurrentTask++;
            NextUpdate();
        }

        public void RunInstall(object parameters)
        {
            List<string> Files = (List<string>)parameters;

            bool ok = true;
            bool reboot = false;

            foreach (string CurFile in Files)
            {
                if (Canceled)
                    break;

                string File = CurFile;

                AppLog.Line("Installing: {0}", File);

                try
                {
                    string ext = Path.GetExtension(File);

                    if (ext.Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string path = Path.GetDirectoryName(File) + @"\files";// + Path.GetFileNameWithoutExtension(File);

                        if (!Directory.Exists(path)) // is it already unpacked?
                            ZipFile.ExtractToDirectory(File, path);

                        string supportedExtensions = "*.msu,*.msi,*.cab,*.exe";
                        var foundFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(Path.GetExtension(s).ToLower()));
                        if (foundFiles.Count() == 0)
                            throw new System.IO.FileNotFoundException("Expected file not found in zip");

                        File = foundFiles.First();
                        ext = Path.GetExtension(File);
                    }

                    if (Canceled)
                        break;

                    int exitCode = 0;

                    if (ext.Equals(".exe", StringComparison.CurrentCultureIgnoreCase))
                        exitCode = InstallExe(File);
                    else if (ext.Equals(".msi", StringComparison.CurrentCultureIgnoreCase))
                        exitCode = InstallMsi(File);
                    else if (ext.Equals(".msu", StringComparison.CurrentCultureIgnoreCase))
                        exitCode = InstallMsu(File);
                    else if (ext.Equals(".cab", StringComparison.CurrentCultureIgnoreCase))
                        exitCode = InstallCab(File);
                    else
                        throw new System.IO.FileFormatException("Unknown Update format: " + ext);

                    if (exitCode == 3010 || exitCode == 3010)
                        reboot = true; // reboot requires
                    else if (exitCode == 1641)
                    {
                        AppLog.Line("Error, reboot got initiated: {0}", File);
                        reboot = true; // reboot in initiated, WTF !!!!
                        ok = false;
                    }
                    else if (exitCode != 1 && exitCode != 0)
                        ok = false; // some error
                }
                catch (Exception e)
                {
                    ok = false;
                    Console.WriteLine("Error installing update: {0}", e.Message);
                }
            }

            mDispatcher.BeginInvoke(new Action(() => {
                OnFinished(ok, reboot);
            }));
        }

        private int InstallExe(string fileName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = fileName;

            // ToDo: load from file
            string name = Path.GetFileNameWithoutExtension(fileName);
            if(name.IndexOf("ndp", StringComparison.CurrentCultureIgnoreCase) == 0 || 
               name.IndexOf("OFV", StringComparison.CurrentCultureIgnoreCase) == 0 ||
               name.IndexOf("2553065", StringComparison.CurrentCultureIgnoreCase) == 0)
                startInfo.Arguments = "/q /norestart";
            else
                startInfo.Arguments = "/q /z";

            return ExecTask(startInfo);
        }

        private int InstallMsi(string fileName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"%SystemRoot%\System32\msiexec.exe";
            startInfo.Arguments = "/i \"" + fileName  + "\" /qn /norestart";

            return ExecTask(startInfo);
        }

        private int InstallMsu(string fileName)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"%SystemRoot%\System32\wusa.exe";
            startInfo.Arguments = "\"" + fileName + "\" /quiet /norestart";

            return ExecTask(startInfo);
        }

        private bool CheckCab(string fileName)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\Dism.exe");
                proc.StartInfo.Arguments = "/Online /Get-PackageInfo /PackagePath:\"" + fileName + "\" /English";
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.Start();
                proc.WaitForExit();
                while (!proc.StandardOutput.EndOfStream)
                {
                    string[] line = proc.StandardOutput.ReadLine().Split(':');
                    if (line.Length != 2)
                        continue;
                    
                    if(!line[0].Trim().Equals("Applicable", StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    return line[1].Trim().Equals("Yes", StringComparison.CurrentCultureIgnoreCase);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Dism error: {0}", e.Message);
            }
            return false;
        }

        private int InstallCab(string fileName)
        {
            if (!CheckCab(fileName) || Canceled)
                return 0; // update not aplicable or user canceled

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"%SystemRoot%\System32\Dism.exe";
            startInfo.Arguments = "/Online /Quiet /NoRestart /Add-Package /PackagePath:\"" + fileName + "\" /IgnoreCheck";

            return ExecTask(startInfo);
        }

        private int ExecTask(ProcessStartInfo startInfo, bool silent = true)
        {
            startInfo.FileName = Environment.ExpandEnvironmentVariables(startInfo.FileName);

            if (silent)
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
            }

            Process proc = new Process();
            proc.StartInfo = startInfo;
            proc.EnableRaisingEvents = true;
            proc.Start();
            proc.WaitForExit();

            return proc.ExitCode;
        }

        public void RunUnInstall(object parameters)
        {
            string KB = (string)parameters;

            AppLog.Line("Uninstalling: {0}", KB);

            bool ok = true;
            bool reboot = false;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"%SystemRoot%\System32\wusa.exe";
                startInfo.Arguments = "/uninstall /kb:" + KB.Substring(2) + " /norestart"; // /quiet 

                int exitCode = ExecTask(startInfo);

                if (exitCode == 3010 || exitCode == 3010 || exitCode == 1641)
                {
                    reboot = true;
                }
                else if (exitCode != 1 && exitCode != 0)
                {
                    AppLog.Line("Error, exit coded: {0}", exitCode);
                    ok = false; // some error
                }
            }
            catch (Exception e)
            {
                ok = false;
                Console.WriteLine("Error removing update: {0}", e.Message);
            }
            
            mDispatcher.BeginInvoke(new Action(() => {
                OnFinished(ok, reboot);
            }));
        }

        public class FinishedEventArgs : EventArgs
        {
            public FinishedEventArgs(int ErrorCount, bool Reboot)
            {
                this.ErrorCount = ErrorCount;
                this.Reboot = Reboot;
            }
            public List<MsUpdate> Updates;
            public int ErrorCount = 0;
            public bool Reboot = false;
            //public MultiValueDictionary<string, string> AllFiles;
            public bool Success { get { return ErrorCount == 0; } }
        }
        public event EventHandler<FinishedEventArgs> Finished;

        public event EventHandler<WuAgent.ProgressArgs> Progress;
    }
}
