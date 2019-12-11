using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wumgr
{
    static public class Translate
    {
        static SortedDictionary<string, string> mStrings = new SortedDictionary<string, string>();

        static public void Load(string lang = "")
        {
            if (lang == "")
            {
                CultureInfo ci = CultureInfo.InstalledUICulture;

                /*Console.WriteLine("Default Language Info:");
                Console.WriteLine("* Name: {0}", ci.Name);
                Console.WriteLine("* Display Name: {0}", ci.DisplayName);
                Console.WriteLine("* English Name: {0}", ci.EnglishName);
                Console.WriteLine("* 2-letter ISO Name: {0}", ci.TwoLetterISOLanguageName);
                Console.WriteLine("* 3-letter ISO Name: {0}", ci.ThreeLetterISOLanguageName);
                Console.WriteLine("* 3-letter Win32 API Name: {0}", ci.ThreeLetterWindowsLanguageName);*/

                lang = ci.TwoLetterISOLanguageName;
            }
            

            mStrings.Add("msg_running", "Application is already running.");
            mStrings.Add("msg_admin_req", "The {0} requires Administrator privileges in order to install updates");
            mStrings.Add("msg_ro_wrk_dir", "Can't write to working directory: {0}");
            mStrings.Add("cap_chk_upd", "Please Check For Updates");
            mStrings.Add("msg_chk_upd", "{0} couldn't check for updates for {1} days, please check for updates manually and resolve possible issues");
            mStrings.Add("cap_new_upd", "New Updates found");
            mStrings.Add("msg_new_upd", "{0} has found {1} new updates, please review the updates and install them");
            mStrings.Add("lbl_fnd_upd", "Windows Update ({0})");
            mStrings.Add("lbl_inst_upd", "Installed Updates ({0})");
            mStrings.Add("lbl_block_upd", "Hidden Updates ({0})");
            mStrings.Add("lbl_old_upd", "Update History ({0})");
            mStrings.Add("msg_tool_err", "Failed to start tool");
            mStrings.Add("msg_admin_dl", "Administrator privileges are required in order to download updates using windows update services. Use 'Manual' download instead.");
            mStrings.Add("msg_admin_inst", "Administrator privileges are required in order to install updates.");
            mStrings.Add("msg_admin_rem", "Administrator privileges are required in order to remove updates.");
            mStrings.Add("msg_dl_done", "Updates downloaded to {0}, ready to be installed by the user.");
            mStrings.Add("msg_dl_err", "Updates downloaded to {0}, some updates failed to download.");
            mStrings.Add("msg_inst_done", "Updates successfully installed, however, a reboot is required.");
            mStrings.Add("msg_inst_err", "Installation of some Updates has failed, also a reboot is required.");
            mStrings.Add("err_admin", "Required privileges are not available");
            mStrings.Add("err_busy", "Another operation is already in progress");
            mStrings.Add("err_dl", "Download failed");
            mStrings.Add("err_inst", "Installation failed");
            mStrings.Add("err_no_sel", "No selected updates or no updates eligible for the operation");
            mStrings.Add("err_int", "Internal error");
            mStrings.Add("err_file", "Required file(s) could not be found");
            mStrings.Add("msg_err", "{0} failed: {1}.");
            mStrings.Add("msg_wuau", "Windows Update Service is not available, try to start it?");
            mStrings.Add("menu_tools", "&Tools");
            mStrings.Add("menu_about", "&About");
            mStrings.Add("menu_exit", "E&xit");
            mStrings.Add("stat_not_start", "Not Started");
            mStrings.Add("stat_in_prog", "In Progress");
            mStrings.Add("stat_success", "Succeeded");
            mStrings.Add("stat_success_2", "Succeeded with Errors");
            mStrings.Add("stat_failed", "Failed");
            mStrings.Add("stat_abbort", "Aborted");
            mStrings.Add("stat_beta", "Beta");
            mStrings.Add("stat_install", "Installed");
            mStrings.Add("stat_rem", "Removable");
            mStrings.Add("stat_block", "Hidden");
            mStrings.Add("stat_dl", "Downloaded");
            mStrings.Add("stat_pending", "Pending");
            mStrings.Add("stat_sel", "(!)");
            mStrings.Add("stat_mand", "Mandatory");
            mStrings.Add("stat_excl", "Exclusive");
            mStrings.Add("stat_reboot", "Needs Reboot");
            mStrings.Add("menu_wuau", "Windows Update Service");
            mStrings.Add("menu_refresh", "&Refresh");
            mStrings.Add("op_check", "Checking for Updates");
            mStrings.Add("op_prep", "Preparing Check");
            mStrings.Add("op_dl", "Downloading Updates");
            mStrings.Add("op_inst", "Installing Updates");
            mStrings.Add("op_rem", "Removing Updates");
            mStrings.Add("op_cancel", "Cancelling Operation");
            mStrings.Add("op_unk", "Unknown Operation");
            mStrings.Add("msg_gpo", "Your version of Windows does not respect the standard GPO's, to keep automatic Windows updates blocked, update facilitation services must be disabled.");
            mStrings.Add("col_title", "Title");
            mStrings.Add("col_cat", "Category");
            mStrings.Add("col_kb", "KB Article");
            mStrings.Add("col_app_id", "Application ID");
            mStrings.Add("col_date", "Date");
            mStrings.Add("col_site", "Size");
            mStrings.Add("col_stat", "State");
            mStrings.Add("lbl_support", "Support Url");
            mStrings.Add("lbl_search", "Search filter:");
            mStrings.Add("tip_search", "Search");
            mStrings.Add("tip_inst", "Install");
            mStrings.Add("tip_dl", "Download");
            mStrings.Add("tip_hide", "Hide");
            mStrings.Add("tip_lnk", "Get Links");
            mStrings.Add("tip_rem", "Uninstall");
            mStrings.Add("tip_cancel", "Cancel");
            mStrings.Add("lbl_opt", "Options");
            mStrings.Add("lbl_au", "Auto Update");
            mStrings.Add("lbl_off", "Offline Mode");
            mStrings.Add("lbl_dl", "Download wsusscn2.cab");
            mStrings.Add("lbl_man", "'Manual' Download/Install");
            mStrings.Add("lbl_old", "Include superseded");
            mStrings.Add("lbl_ms", "Register Microsoft Update");
            mStrings.Add("lbl_start", "Startup");
            mStrings.Add("lbl_auto", "Run in background");
            mStrings.Add("lbl_ac_no", "No auto search for updates");
            mStrings.Add("lbl_ac_day", "Search for updates every day");
            mStrings.Add("lbl_ac_week", "Search for updates once a week");
            mStrings.Add("lbl_ac_month", "Search for updates every month");
            mStrings.Add("lbl_uac", "Always run as Administrator");
            mStrings.Add("lbl_block_ms", "Block Access to WU Servers");
            mStrings.Add("lbl_au_off", "Disable Automatic Update");
            mStrings.Add("lbl_au_dissable", "Disable Update Facilitators");
            mStrings.Add("lbl_au_notify", "Notification Only");
            mStrings.Add("lbl_au_dl", "Download Only");
            mStrings.Add("lbl_au_time", "Scheduled & Installation");
            mStrings.Add("lbl_au_def", "Automatic Update (default)");
            mStrings.Add("lbl_hide", "Hide WU Settings Page");
            mStrings.Add("lbl_store", "Disable Store Auto Update");
            mStrings.Add("lbl_drv", "Include Drivers");
            mStrings.Add("msg_disable_au", "For the new configuration to fully take effect a reboot is required.");
            mStrings.Add("lbl_all", "Select All");
            mStrings.Add("lbl_group", "Group Updates");
            mStrings.Add("lbl_patreon", "Support WuMgr on Patreon");
            mStrings.Add("lbl_github", "Visit WuMgr on GitHub");

            string langINI = Program.appPath + @"\Translation.ini";

            if (!File.Exists(langINI))
            {
                foreach (string key in mStrings.Keys)
                    Program.IniWriteValue("en", key, mStrings[key], langINI);
                return;
            }

            if (lang != "en")
            {
                foreach (string key in mStrings.Keys.ToList())
                {
                    string str = Program.IniReadValue(lang, key, "", langINI);
                    if (str.Length == 0)
                        continue;

                    mStrings.Remove(key);
                    mStrings.Add(key, str);
                }
            }
        }

        static public string fmt(string id, params object[] args)
        {
            try
            {
                string str = id;
                mStrings.TryGetValue(id, out str);
                return string.Format(str, args);
            }
            catch
            {
                return "err on " + id;
            }
        }
    }
}
