using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wumgr
{
    class GPO
    {
        static private string mWuGPO = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate";

        static public void ConfigAU(int option, int day = -1, int time = -1)
        {
            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", true);
            switch (option)
            {
                case 0: //Automatic(default)
                    subKey.DeleteValue("NoAutoUpdate", false);
                    subKey.DeleteValue("AUOptions", false);
                    break;
                case 1: //Disabled
                    subKey.SetValue("NoAutoUpdate", 1);
                    subKey.DeleteValue("AUOptions", false);
                    break;
                case 2: //Notification only
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 2);
                    break;
                case 3: //Download only
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 3);
                    break;
                case 4: //Scheduled Installation
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 4);
                    break;
                case 5: //Managed by Admin
                    subKey.SetValue("NoAutoUpdate", 0);
                    subKey.SetValue("AUOptions", 5);
                    break;
            }

            if (option == 4)
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

        static public int GetAU(out int day, out int time)
        {
            int option = 0;

            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", false);
            object value_no = subKey.GetValue("NoAutoUpdate");
            if (value_no == null || (int)value_no == 0)
            {
                object value_au = subKey.GetValue("AUOptions");
                switch (value_au == null ? 0 : (int)value_au)
                {
                    case 0: option = 0; break;
                    case 2: option = 2; break;
                    case 3: option = 3; break;
                    case 4: option = 4; break;
                    case 5: option = 5; break;
                }
            }
            else
            {
                option = 1;
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
            var subKey = Registry.LocalMachine.CreateSubKey(mWuGPO, true);
            object value_block = subKey.GetValue("DoNotConnectToWindowsUpdateInternetLocations");
            /*subKey.DeleteValue("WUServer", false);
            subKey.DeleteValue("WUStatusServer", false);
            subKey.DeleteValue("UpdateServiceUrlAlternate", false);*/

            var subKey2 = Registry.LocalMachine.CreateSubKey(mWuGPO + @"\AU", true);
            object value_wsus = subKey2.GetValue("UseWUServer");

            if ((value_block != null && (int)value_block == 1) && (value_wsus != null && (int)value_wsus == 1))
                return 1; // CheckState.Checked;
            else if ((value_block == null || (int)value_block == 0) && (value_wsus == null || (int)value_wsus == 0))
                return 0; // CheckState.Unchecked;
            else
                return 2; // CheckState.Indeterminate;
        }

        static public int IsRespected()
        {
            var subKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
            string version = subKey.GetValue("CurrentVersion", "0").ToString();
            float version_num = float.Parse(version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string edition = subKey.GetValue("EditionID", "").ToString();
            string type = subKey.GetValue("InstallationType", "").ToString();

            bool ok = (version_num < 6.3 || type.Equals("Server", StringComparison.CurrentCultureIgnoreCase) || edition.Contains("Education") || edition.Contains("Enterprise"));
            bool not_ok = (version_num >= 6.3 && type.Equals("Client", StringComparison.CurrentCultureIgnoreCase) && (edition.Contains("Pro") || edition.Contains("Home")));

            if (not_ok)
                return 0;
            else if (ok)
                return 1;
            return 2;
        }
    }
}
