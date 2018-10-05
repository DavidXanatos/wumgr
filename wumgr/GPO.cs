using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace wumgr
{
    class GPO
    {
        static private string mWuGPO = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

        public enum AUOptions
        {
            Default = 0, // Automatic
            Disabled = 1,
            Notification = 2,
            Download = 3,
            Scheduled = 4,
            ManagedByAdmin = 5
        }

        static public void ConfigAU(AUOptions option, int day = -1, int time = -1)
        {
            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", true);
            switch (option)
            {
                case AUOptions.Default: //Automatic(default)
                    subKey.DeleteValue("NoAutoUpdate", false);
                    subKey.DeleteValue("AUOptions", false);
                    break;
                case AUOptions.Disabled: //Disabled
                    subKey.SetValue("NoAutoUpdate", 1);
                    subKey.DeleteValue("AUOptions", false);
                    break;
                case AUOptions.Notification: //Notification only
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 2);
                    break;
                case AUOptions.Download: //Download only
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 3);
                    break;
                case AUOptions.Scheduled: //Scheduled Installation
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 4);
                    break;
                case AUOptions.ManagedByAdmin: //Managed by Admin
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 5);
                    break;
            }

            if (option == AUOptions.Scheduled)
            {
                if(day != -1) subKey.SetValue("ScheduledInstallDay", day);
                if (time != -1) subKey.SetValue("ScheduledInstallTime", time);
            }
            else
            {
                subKey.DeleteValue("ScheduledInstallDay", false);
                subKey.DeleteValue("ScheduledInstallTime", false);
            }
        }

        static public AUOptions GetAU(out int day, out int time)
        {
            AUOptions option = AUOptions.Default;

            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", false);
            object value_no = subKey.GetValue("NoAutoUpdate");
            if (value_no == null || (int)value_no == 0)
            {
                object value_au = subKey.GetValue("AUOptions");
                switch (value_au == null ? 0 : (int)value_au)
                {
                    case 0: option = AUOptions.Default; break;
                    case 2: option = AUOptions.Notification; break;
                    case 3: option = AUOptions.Download; break;
                    case 4: option = AUOptions.Scheduled; break;
                    case 5: option = AUOptions.ManagedByAdmin; break;
                }
            }
            else
            {
                option = AUOptions.Disabled;
            }

            object value_day = subKey.GetValue("ScheduledInstallDay");
            day = value_day != null ? (int)value_day : 0;
            object value_time = subKey.GetValue("ScheduledInstallTime");
            time = value_time != null ? (int)value_time : 0;

            return option;
        }

        static public void ConfigDriverAU(int option)
        {
            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO, true);
            switch (option)
            {
                case 0: // CheckState.Unchecked:
                    subKey.SetValue("ExcludeWUDriversInQualityUpdate", 1);
                    break;
                case 2: // CheckState.Indeterminate:
                    subKey.DeleteValue("ExcludeWUDriversInQualityUpdate", false);
                    break;
                case 1: // CheckState.Checked:
                    subKey.SetValue("ExcludeWUDriversInQualityUpdate", 0);
                    break;
            }
        }

        static public int GetDriverAU()
        {
            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO, false);
            object value_drv = subKey.GetValue("ExcludeWUDriversInQualityUpdate");

            if (value_drv == null)
                return 2; // CheckState.Indeterminate;
            else if ((int)value_drv == 1)
                return 0; // CheckState.Unchecked;
            else //if ((int)value_drv == 0)
                return 1; // CheckState.Checked;
        }

        static public void HideUpdatePage(bool hide = true)
        {
            var subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
            if (hide)
                subKey.SetValue("SettingsPageVisibility", "hide:windowsupdate");
            else
                subKey.DeleteValue("SettingsPageVisibility", false);
        }

        static public bool IsUpdatePageHidden()
        {
            var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
            string value = subKey.GetValue("SettingsPageVisibility", "").ToString();
            return value.Contains("hide:windowsupdate");
        }

        static public void BlockMS(bool block = true)
        {
            if (block)
            {
                var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO, true);
                subKey.SetValue("DoNotConnectToWindowsUpdateInternetLocations", 1);
                subKey.SetValue("WUServer", "\" \"");
                subKey.SetValue("WUStatusServer", "\" \"");
                subKey.SetValue("UpdateServiceUrlAlternate", "\" \"");

                var subKey2 = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", true);
                subKey2.SetValue("UseWUServer", 1);
            }
            else
            {
                var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO, true);
                subKey.DeleteValue("DoNotConnectToWindowsUpdateInternetLocations", false);
                subKey.DeleteValue("WUServer", false);
                subKey.DeleteValue("WUStatusServer", false);
                subKey.DeleteValue("UpdateServiceUrlAlternate", false);

                var subKey2 = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", true);
                subKey2.DeleteValue("UseWUServer", false);
            }
        }

        static public int GetBlockMS()
        {
            var subKey = Registry.LocalMachine.OpenSubKey(mWuGPO);
            object value_block = subKey.GetValue("DoNotConnectToWindowsUpdateInternetLocations");
            /*subKey.DeleteValue("WUServer", false);
            subKey.DeleteValue("WUStatusServer", false);
            subKey.DeleteValue("UpdateServiceUrlAlternate", false);*/

            var subKey2 = Registry.LocalMachine.OpenSubKey(mWuGPO + @"\AU");
            object value_wsus = subKey2.GetValue("UseWUServer");

            if ((value_block != null && (int)value_block == 1) && (value_wsus != null && (int)value_wsus == 1))
                return 1; // CheckState.Checked;
            else if ((value_block == null || (int)value_block == 0) && (value_wsus == null || (int)value_wsus == 0))
                return 0; // CheckState.Unchecked;
            else
                return 2; // CheckState.Indeterminate;
        }

        static public void SetStoreAU(bool disable)
        {
            var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore");
            //var subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate", true);
            if (disable)
                subKey.SetValue("AutoDownload", 2);
            else
                subKey.DeleteValue("AutoDownload", false); // subKey.SetValue("AutoDownload", 4);
        }

        static public bool GetStoreAU()
        {
            var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\WindowsStore");
            //var subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate", true);
            object value_block = subKey.GetValue("AutoDownload");
            return (value_block != null && (int)value_block == 2);
        }

        static public void DisableAU(bool disable)
        {
            if (disable)
            {
                ConfigSvc("UsoSvc", ServiceStartMode.Disabled); // Update Orchestrator Service
                ConfigSvc("WaaSMedicSvc", ServiceStartMode.Disabled); // Windows Update Medic Service
            }
            else
            {
                ConfigSvc("UsoSvc", ServiceStartMode.Automatic); // Update Orchestrator Service
                ConfigSvc("WaaSMedicSvc", ServiceStartMode.Manual); // Windows Update Medic Service
            }
        }

        static public void ConfigSvc(string name, ServiceStartMode mode)
        {
            ServiceController svc = new ServiceController(name);
            try
            {
                if (mode == ServiceStartMode.Disabled && svc.Status == ServiceControllerStatus.Running)
                    svc.Stop();

                // Note: for UsoSvc and for WaaSMedicSvc this call fails with an access error so we have to set the registry
                //ServiceHelper.ChangeStartMode(svc, mode);
            }
            catch (Exception err)
            {
                AppLog.Line("Error: " + err.Message);
            }
            svc.Close();

            var subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.SetValue | RegistryRights.ChangePermissions | RegistryRights.TakeOwnership);
            subKey.SetValue("Start", (int)mode);

            var ac = subKey.GetAccessControl();
            AuthorizationRuleCollection rules = ac.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)); // get as SID not string
                                                                                                                                     // cleanup old roule
            foreach (RegistryAccessRule rule in rules)
            {
                if (rule.IdentityReference.Value.Equals(FileOps.SID_System))
                    ac.RemoveAccessRule(rule);
            }
            // Note: windows tryes to re enable this services so we need to remove system write access
            if (mode == ServiceStartMode.Disabled) // add new rule
                ac.AddAccessRule(new RegistryAccessRule(new SecurityIdentifier(FileOps.SID_System), RegistryRights.FullControl, AccessControlType.Deny));
            subKey.SetAccessControl(ac);
        }

        static public bool GetDisableAU()
        {
            return IsSvcDisabled("UsoSvc") && IsSvcDisabled("WaaSMedicSvc");
        }

        static public bool IsSvcDisabled(string name)
        {
            var subKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name);
            return subKey == null || (MiscFunc.parseInt(subKey.GetValue("Start", "-1").ToString()) == (int)ServiceStartMode.Disabled);
        }

        public enum Respect
        {
            Unknown = 0,
            Full, // Win 7, 8, 10 Ent/Edu/Svr
            Partial, // Win 10 Pro
            None // Win 10 Home
        }

        static public Respect GetRespect()
        {
            var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            //string edition = subKey.GetValue("EditionID", "").ToString();
            string name = subKey.GetValue("ProductName", "").ToString();
            string type = subKey.GetValue("InstallationType", "").ToString();

            if (GetWinVersion() < 10.0f || type.Equals("Server", StringComparison.CurrentCultureIgnoreCase) || name.Contains("Education") || name.Contains("Enterprise"))
                return Respect.Full;

            if (type.Equals("Client", StringComparison.CurrentCultureIgnoreCase))
            {
                if(name.Contains("Pro"))
                    return Respect.Partial;
                if (name.Contains("Home"))
                    return Respect.None;
            }
            return Respect.Unknown;
        }

        static public float GetWinVersion()
        {
            var subKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            //string Majorversion = subKey.GetValue("CurrentMajorVersionNumber", "0").ToString(); // this is 10 on 10 but not present on earlier editions
            string version = subKey.GetValue("CurrentVersion", "0").ToString();
            float version_num = float.Parse(version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            //string name = subKey.GetValue("ProductName", "").ToString();

            /*
                Operating system              Version number
                ----------------------------  --------------
                Windows 10                      6.3 WTF why not 10
                Windows Server 2016             6.3 WTF why not 10
                Windows 8.1                     6.3
                Windows Server 2012 R2          6.3
                Windows 8                       6.2
                Windows Server 2012             6.2
                Windows 7                       6.1
                Windows Server 2008 R2          6.1
                Windows Server 2008             6.0
                Windows Vista                   6.0
                Windows Server 2003 R2          5.2
                Windows Server 2003             5.2
                Windows XP 64-Bit Edition       5.2
                Windows XP                      5.1
                Windows 2000                    5.0
                Windows ME                      4.9
                Windows 98                      4.10
             */

            if (version_num >= 6.3)
            {
                //!name.Contains("8.1") && !name.Contains("2012 R2");
                int build = MiscFunc.parseInt(subKey.GetValue("CurrentBuildNumber", "0").ToString());
                if (build >= 10000) // 1507 RTM release
                    return 10.0f;
            }
            return version_num;
        }
    }
}
