using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;
using System.IO;


class FileOps
{
    static public string FormatSize(decimal size)
    {
        if (size == 0)
            return "";
        if (size >= 1024 * 1024 * 1024)
            return (size / (1024 * 1024 * 1024)).ToString("F") + " GB";
        if (size >= 1024 * 1024)
            return (size / (1024 * 1024)).ToString("F") + " MB";
        if (size >= 1024)
            return (size / (1024)).ToString("F") + " KB";
        return ((Int64)size).ToString() + " B";
    }

    static public bool MoveFile(string from, string to, bool Overwrite = false)
    {
        try
        {
            if (File.Exists(to))
            {
                if (!Overwrite)
                    return false;
                File.Delete(to);
            }

            File.Move(from, to);

            if (File.Exists(from))
                return false;
        }
        catch (Exception e)
        {
            Console.WriteLine("The process failed: {0}", e.ToString());
            return false;
        }
        return true;
    }

    static public bool DeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static public int TestFileAdminSec(String filePath)
    {
        //get file info
        FileInfo fi = new FileInfo(filePath);
        if (!fi.Exists)
            return 2;
            
        //get security access
        FileSecurity fs = fi.GetAccessControl();

        //get any special user access
        AuthorizationRuleCollection rules = fs.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)); // get as SID not string


        //remove any special access
        foreach (FileSystemAccessRule rule in rules)
        {
            if (rule.AccessControlType != AccessControlType.Allow)
                continue;
            if (rule.IdentityReference.Value.Equals(SID_Admins) || rule.IdentityReference.Value.Equals(SID_System))
                continue;
            if ((rule.FileSystemRights & (FileSystemRights.Write | FileSystemRights.Delete)) != 0)
                return 0;
        }
        return 1;
    }

    static public void SetFileAdminSec(String filePath)
    {
        //get file info
        FileInfo fi = new FileInfo(filePath);
        if(!fi.Exists){
            FileStream f_out = fi.OpenWrite();
            f_out.Close();
        }

        //get security access
        FileSecurity fs = fi.GetAccessControl();

        //remove any inherited access
        fs.SetAccessRuleProtection(true, false);

        //get any special user access
        AuthorizationRuleCollection rules = fs.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)); // show as names

        //remove any special access
        foreach (FileSystemAccessRule rule in rules)
            fs.RemoveAccessRule(rule);

        fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SID_Admins), FileSystemRights.FullControl, AccessControlType.Allow));
        fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SID_System), FileSystemRights.FullControl, AccessControlType.Allow));
        fs.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(SID_Users), FileSystemRights.Read, AccessControlType.Allow));

        //add current user with full control.
        //fs.AddAccessRule(new FileSystemAccessRule(domainName + "\\" + userName, FileSystemRights.FullControl, AccessControlType.Allow));

        //add all other users delete only permissions.
        //SecurityIdentifier sid = new SecurityIdentifier("S-1-5-11"); // Authenticated Users
        //fs.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.Delete, AccessControlType.Allow));

        //flush security access.
        File.SetAccessControl(filePath, fs);
    }

    static public bool TestWrite(string filePath)
    {
        FileInfo fi = new FileInfo(filePath);
        try
        {
            FileStream f_out = fi.OpenWrite();
            f_out.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string SID_null = "S-1-0-0"; //	Null SID
    public static string SID_Worls = "S-1-1-0"; //	World
    public static string SID_Local = "S-1-2-0"; //	Local
    public static string SID_Console = "S-1-2-1"; //	Console Logon
    public static string SID_OwnerID = "S-1-3-0"; //	Creator Owner ID
    public static string SID_GroupeID = "S-1-3-1"; //	Creator Group ID
    public static string SID_OwnerSvr = "S-1-3-2"; //	Creator Owner Server
    public static string SID_CreatorSvr = "S-1-3-3"; //	Creator Group Server
    public static string SID_OwnerRights = "S-1-3-4"; //	Owner Rights
    public static string SID_NonUnique = "S-1-4"; //	Non-unique Authority
    public static string SID_NTAuth = "S-1-5"; //	NT Authority
    public static string SID_AllServices = "S-1-5-80-0"; //	All Services
    public static string SID_DialUp = "S-1-5-1"; //	Dialup
    public static string SID_LocalAcc = "S-1-5-113"; //	Local account
    public static string SID_LocalAccAdmin = "S-1-5-114"; //	Local account and member of Administrators group
    public static string SID_Net = "S-1-5-2"; //	Network
    public static string SID_Natch = "S-1-5-3"; //	Batch
    public static string SID_Interactive = "S-1-5-4"; //	Interactive
    //public static string SID_ = "S-1-5-5- *X*- *Y* Logon Session
    public static string SID_Service = "S-1-5-6"; //	Service
    public static string SID_AnonLogin = "S-1-5-7"; //	Anonymous Logon

    public static string SID_Proxy = "S-1-5-8"; //	Proxy
    public static string SID_EDC = "S-1-5-9"; //	Enterprise Domain Controllers
    public static string SID_Self = "S-1-5-10"; //	Self
    public static string SID_AuthenticetedUser = "S-1-5-11"; //	Authenticated Users

    public static string SID_Restricted = "S-1-5-12"; //	Restricted Code
    public static string SID_TermUser = "S-1-5-13"; //	Terminal Server User
    public static string SID_RemoteLogin = "S-1-5-14"; //	Remote Interactive Logon
    public static string SID_ThisORg = "S-1-5-15"; //	This Organization
    public static string SID_IIS = "S-1-5-17"; //	IIS_USRS
    public static string SID_System = "S-1-5-18"; //	System(or LocalSystem)

    public static string SID_NTAuthL = "S-1-5-19"; //	NT Authority(LocalService)
    public static string SID_NetServices = "S-1-5-20"; //	Network Service

    public static string SID_Admins = "S-1-5-32-544"; //	Administrators
    public static string SID_Users = "S-1-5-32-545"; //	Users
    public static string SID_Guests = "S-1-5-32-546"; //	Guests
    public static string SID_PowerUsers = "S-1-5-32-547"; //	Power Users
    public static string SID_AccOps = "S-1-5-32-548"; //	Account Operators
    public static string SID_ServerOps = "S-1-5-32-549"; //	Server Operators
    public static string SID_PrintOps = "S-1-5-32-550"; //	Print Operators
    public static string SID_BackupOps = "S-1-5-32-551"; //	Backup Operators
    public static string SID_Replicators = "S-1-5-32-552"; //	Replicators
    public static string SID_NTLM_Auth = "S-1-5-64-10"; //	NTLM Authentication
    public static string SID_SCh_Auth = "S-1-5-64-14"; //	SChannel Authentication
    public static string SID_DigestAuth = "S-1-5-64-21"; //	Digest Authentication
    public static string SID_NT_Service = "S-1-5-80"; //	NT Service
    public static string SID_All_Services = "S-1-5-80-0"; //	All Services
    public static string SID_VM = "S-1-5-83-0"; //	NT VIRTUAL MACHINE\Virtual Machines
    public static string SID_UntrustedLevel = "S-1-16-0"; //	Untrusted Mandatory Level
    public static string SID_LowLevel = "S-1-16-4096"; //	Low Mandatory Level
    public static string SID_MediumLevel = "S-1-16-8192"; //	Medium Mandatory Level
    public static string SID_MediumPLevel = "S-1-16-8448"; //	Medium Plus Mandatory Level
    public static string SID_HighLevel = "S-1-16-12288"; //	High Mandatory Level
    public static string SID_SysLevel = "S-1-16-16384"; //	System Mandatory Level
    public static string SID_PPLevel = "S-1-16-20480"; //	Protected Process Mandatory Level
    public static string SID_SPLevel = "S-1-16-28672"; //	Secure Process Mandatory Level

    internal static bool TakeOwn(string path)
    {
        bool ret = true;
        try
        {
            //TokenManipulator.AddPrivilege("SeRestorePrivilege");
            //TokenManipulator.AddPrivilege("SeBackupPrivilege");
            TokenManipulator.AddPrivilege("SeTakeOwnershipPrivilege");


            FileSecurity ac = File.GetAccessControl(path);
            ac.SetOwner(new SecurityIdentifier(FileOps.SID_Admins));
            File.SetAccessControl(path, ac);
        }
        catch (PrivilegeNotHeldException err)
        {
            AppLog.Line("Couldn't take Ownership {0}", err.ToString());
            ret = false;
        }
        finally
        {
            //TokenManipulator.RemovePrivilege("SeRestorePrivilege");
            //TokenManipulator.RemovePrivilege("SeBackupPrivilege");
            TokenManipulator.RemovePrivilege("SeTakeOwnershipPrivilege");
        }
        return ret;
    }
}
