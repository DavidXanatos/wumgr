using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WUApiLib;

namespace wumgr
{
    public class MsUpdate
    {
        public MsUpdate()
        {
        }

        public MsUpdate(IUpdate update, UpdateState state)
        {
            Entry = update;

            try
            {
                UUID = update.Identity.UpdateID;

                Title = update.Title;
                Category = GetCategory(update.Categories);
                Description = update.Description;
                Size = update.MaxDownloadSize;
                Date = update.LastDeploymentChangeTime;
                KB = GetKB(update);
                SupportUrl = update.SupportUrl;

                AddUpdates();

                State = state;

                Attributes |= update.IsBeta ? (int)UpdateAttr.Beta : 0;
                Attributes |= update.IsDownloaded ? (int)UpdateAttr.Downloaded : 0;
                Attributes |= update.IsHidden ? (int)UpdateAttr.Hidden : 0;
                Attributes |= update.IsInstalled ? (int)UpdateAttr.Installed : 0;
                Attributes |= update.IsMandatory ? (int)UpdateAttr.Mandatory : 0;
                Attributes |= update.IsUninstallable ? (int)UpdateAttr.Uninstallable : 0;
                Attributes |= update.AutoSelectOnWebSites ? (int)UpdateAttr.AutoSelect : 0;

                if (update.InstallationBehavior.Impact == InstallationImpact.iiRequiresExclusiveHandling)
                    Attributes |= (int)UpdateAttr.Exclusive;

                switch (update.InstallationBehavior.RebootBehavior)
                {
                    case InstallationRebootBehavior.irbAlwaysRequiresReboot:
                        Attributes |= (int)UpdateAttr.Reboot;
                        break;
                    case InstallationRebootBehavior.irbCanRequestReboot:
                    case InstallationRebootBehavior.irbNeverReboots:
                        break;
                }
            }
            catch { }
        }

        public MsUpdate(IUpdateHistoryEntry2 update)
        {
            try
            {
                UUID = update.UpdateIdentity.UpdateID;

                Title = update.Title;
                Category = GetCategory(update.Categories);
                Description = update.Description;
                Date = update.Date;
                SupportUrl = update.SupportUrl;
                ApplicationID = update.ClientApplicationID;

                State = UpdateState.History;

                ResultCode = (int)update.ResultCode;
                HResult = update.HResult;
            }
            catch { }
        }

        private void AddUpdates()
        {
            AddUpdates(Entry.DownloadContents);
            if (Downloads.Count == 0)
            {
                foreach (IUpdate5 bundle in Entry.BundledUpdates)
                    AddUpdates(bundle.DownloadContents);
            }
        }

        private void AddUpdates(IUpdateDownloadContentCollection content)
        {
            foreach (IUpdateDownloadContent2 udc in content)
            {
                if (udc.IsDeltaCompressedContent)
                    continue;
                if (String.IsNullOrEmpty(udc.DownloadUrl))
                    continue; // sanity check
                Downloads.Add(udc.DownloadUrl);
            }
        }

        static string GetKB(IUpdate update)
        {
            return update.KBArticleIDs.Count > 0 ? "KB" + update.KBArticleIDs[0] : "KBUnknown";
        }

        static public string GetCategory(ICategoryCollection cats)
        {
            string classification = "";
            string product = "";
            foreach (ICategory cat in cats)
            {
                if (cat.Type.Equals("UpdateClassification"))
                    classification = cat.Name;
                else if (cat.Type.Equals("Product"))
                    product = cat.Name;
                else
                    continue;

            }
            return product.Length == 0 ? classification  : (product + "; " + classification);
        }

        public void Invalidate() { Entry = null; }

        public IUpdate GetUpdate()
        {
            /*if (Entry == null)
            {
                WuAgent agen = WuAgent.GetInstance();
                if (agen.IsActive())
                    Entry = agen.FindUpdate(UUID);
            }*/
            return Entry;
        }

        private IUpdate Entry = null;

        public string UUID = "";
        public String Title = "";
        public String Description = "";
        public String Category = "";
        public String KB = "";
        public DateTime Date = DateTime.MinValue;
        public decimal Size = 0;
        public String SupportUrl = "";
        public String ApplicationID = "";
        public System.Collections.Specialized.StringCollection Downloads = new System.Collections.Specialized.StringCollection();

        public enum UpdateState
        {
            None = 0,
            Pending,
            Installed,
            Hidden,
            History
        }
        public UpdateState State = UpdateState.None;
        public enum UpdateAttr
        {
            None = 0x0000,
            Beta = 0x0001,
            Downloaded = 0x0002,
            Hidden = 0x0004,
            Installed = 0x0008,
            Mandatory = 0x0010,
            Uninstallable = 0x0020,
            Exclusive = 0x0040,
            Reboot = 0x0080,
            AutoSelect = 0x0100
        }
        public int Attributes = 0;
        public int ResultCode = 0;
        public int HResult = 0;
    }
}
