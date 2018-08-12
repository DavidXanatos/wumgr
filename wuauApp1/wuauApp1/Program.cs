using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WUApiLib;//this is required to use the Interfaces given by microsoft. 

namespace wuauApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            UpdatesAvailable();
            if (NeedsUpdate())
            {
                Console.WriteLine("There are updates for your computer available.");
                EnableUpdateServices();//enables everything windows need in order to make an update
                InstallUpdates(DownloadUpdates());
            }
            else
            {
                Console.WriteLine("There are no updates for your computer at this time.");
            }
            Console.WriteLine("Press any key to finalize the process");
            Console.Read();
        }
        //this is my first try.. I can see the need for abstract classes here...
        //but at least it gives most people a good starting point.
        public static void InstalledUpdates()
        {
            UpdateSession UpdateSession = new UpdateSession();
            IUpdateSearcher UpdateSearchResult = UpdateSession.CreateUpdateSearcher();
            UpdateSearchResult.Online = true;//checks for updates online
            ISearchResult SearchResults = UpdateSearchResult.Search("IsInstalled=1 AND IsHidden=0");
            //for the above search criteria refer to 
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
            //Check the remakrs section
            Console.WriteLine("The following updates are available");
            foreach (IUpdate x in SearchResults.Updates)
            {
                Console.WriteLine(x.Title);
            }
        }
        public static void UpdatesAvailable()
        {
            UpdateSession UpdateSession = new UpdateSession();
            IUpdateSearcher UpdateSearchResult = UpdateSession.CreateUpdateSearcher();
            UpdateSearchResult.Online = true;//checks for updates online
            ISearchResult SearchResults = UpdateSearchResult.Search("IsInstalled=0 AND IsPresent=0");
            //for the above search criteria refer to 
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
            //Check the remakrs section

            foreach (IUpdate x in SearchResults.Updates)
            {
                Console.WriteLine(x.Title);
            }
        }
        public static bool NeedsUpdate()
        {
            UpdateSession UpdateSession = new UpdateSession();
            IUpdateSearcher UpdateSearchResult = UpdateSession.CreateUpdateSearcher();
            UpdateSearchResult.Online = true;//checks for updates online
            ISearchResult SearchResults = UpdateSearchResult.Search("IsInstalled=0 AND IsPresent=0");
            //for the above search criteria refer to 
            //http://msdn.microsoft.com/en-us/library/windows/desktop/aa386526(v=VS.85).aspx
            //Check the remakrs section
            if (SearchResults.Updates.Count > 0)
                return true;
            else return false;
        }
        public static UpdateCollection DownloadUpdates()
        {
            UpdateSession UpdateSession = new UpdateSession();
            IUpdateSearcher SearchUpdates = UpdateSession.CreateUpdateSearcher();
            ISearchResult UpdateSearchResult = SearchUpdates.Search("IsInstalled=0 and IsPresent=0");
            UpdateCollection UpdateCollection = new UpdateCollection();
            //Accept Eula code for each update
            for (int i = 0; i < UpdateSearchResult.Updates.Count; i++)
            {
                IUpdate Updates = UpdateSearchResult.Updates[i];
                if (Updates.EulaAccepted == false)
                {
                    Updates.AcceptEula();
                }
                UpdateCollection.Add(Updates);
            }
            //Accept Eula ends here
            //if it is zero i am not sure if it will trow an exception -- I havent tested it.
            if (UpdateSearchResult.Updates.Count > 0)
            {
                UpdateCollection DownloadCollection = new UpdateCollection();
                UpdateDownloader Downloader = UpdateSession.CreateUpdateDownloader();

                for (int i = 0; i < UpdateCollection.Count; i++)
                {
                    DownloadCollection.Add(UpdateCollection[i]);
                }

                Downloader.Updates = DownloadCollection;
                Console.WriteLine("Downloading Updates... This may take several minutes.");


                IDownloadResult DownloadResult = Downloader.Download();
                UpdateCollection InstallCollection = new UpdateCollection();
                for (int i = 0; i < UpdateCollection.Count; i++)
                {
                    if (DownloadCollection[i].IsDownloaded)
                    {
                        InstallCollection.Add(DownloadCollection[i]);
                    }
                }
                Console.WriteLine("Download Finished");
                return InstallCollection;
            }
            else
                return UpdateCollection;
        }
        public static void InstallUpdates(UpdateCollection DownloadedUpdates)
        {
            Console.WriteLine("Installing updates now...");
            UpdateSession UpdateSession = new UpdateSession();
            UpdateInstaller InstallAgent = UpdateSession.CreateUpdateInstaller() as UpdateInstaller;
            InstallAgent.Updates = DownloadedUpdates;

            //Starts a synchronous installation of the updates.
            // http://msdn.microsoft.com/en-us/library/windows/desktop/aa386491(v=VS.85).aspx#methods
            if (DownloadedUpdates.Count > 0)
            {
                IInstallationResult InstallResult = InstallAgent.Install();
                if (InstallResult.ResultCode == OperationResultCode.orcSucceeded)
                {
                    Console.WriteLine("Updates installed succesfully");
                    if (InstallResult.RebootRequired == true)
                    {
                        Console.WriteLine("Reboot is required for one of more updates.");
                    }
                }
                else
                {
                    Console.WriteLine("Updates failed to install do it manually");
                }
            }
            else
            {
                Console.WriteLine("The computer that this script was executed is up to date");
            }

        }
        public static void EnableUpdateServices()
        {
            IAutomaticUpdates updates = new AutomaticUpdates();
            if (!updates.ServiceEnabled)
            {
                Console.WriteLine("Not all updates services where enabled. Enabling Now" + updates.ServiceEnabled);
                updates.EnableService();
                Console.WriteLine("Service enable success");
            }


        }
    }
}
