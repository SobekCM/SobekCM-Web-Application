#region Using directives

using Microsoft.Win32;
using System;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;

#endregion

namespace SobekCM.Tools
{
    /// <summary> Object used to determine and ensure security.    It allows
    /// for reading from the registry and checking local users and computer information. <br /><br />
    /// </summary>
    /// <remarks> This class allows for the following actions: <ul>
    /// <li type="circle" /> Getting the current username. [ <see cref="SecurityInfo.UserName"/> ]
    /// <li type="circle" /> Getting username and security level information from a security database.
    /// </ul> <br /> <br />
    /// Object created by Mark V Sullivan (2003) for University of Florida's Digital Library Center. </remarks>
    public class SecurityInfo
    {
        ///// <summary> Gets the MAC    address of the network adapter for the current computer </summary>
        ///// <remarks> The MAC address is returned as a string in the form 00:##:##:##:##:##. </remarks>
        //public string MAC_Address
        //{
        //    get
        //    {
        //        ManagementClass oNetworkAdapter = new ManagementClass ("Win32_NetworkAdapter");
        //        ManagementObjectCollection moc = oNetworkAdapter.GetInstances();
        //        foreach(ManagementObject mo in moc)
        //        {
        //            try
        //            {
        //                if ( ( mo["MACAddress"] != null ) && ( mo["MACAddress"].ToString().Substring(0,2) == "00" ) )
        //                {
        //                    return (string)mo["MACAddress"];
        //                }
        //            }
        //            catch
        //            {
        //                return "";
        //            }
        //        }
        //        return "";
        //    }
        //}

        ///// <summary> Returns the hard drive serial number for the hard drive indicated by drive letter </summary>
        ///// <param name="driveLetter"> Drive letter for the drive in question </param>
        ///// <returns> Hard drive serial number </returns>
        //public string HardDriveSerial( char driveLetter )
        //{
        //    ManagementObject disk = new ManagementObject("Win32_Logicaldisk=" + "\"" + driveLetter + ":\"");
        //    string SerialNumber = disk.Properties["Volumeserialnumber"].Value.ToString();
        //    return SerialNumber;
        //}

        /// <summary> Gets the complete current users name as a string. </summary>
        /// <remarks> This name is returned in the form 'DOMAIN\username'. </remarks>
        public string UserName
        {
            get
            {
                if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) return "Only works on windows";

                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                WindowsPrincipal principal = (WindowsPrincipal)Thread.CurrentPrincipal;
                WindowsIdentity identity = (WindowsIdentity)principal.Identity;
                return identity.Name;
            }
        }

        /// <summary> Gets the complete current users name as a string. </summary>
        /// <remarks> This name is returned in the form 'DOMAIN\username'. </remarks>
        public static string Current_UserName
        {
            get
            {
                if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) return "Only works on windows";
                
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                WindowsPrincipal principal = (WindowsPrincipal)Thread.CurrentPrincipal;
                WindowsIdentity identity = (WindowsIdentity)principal.Identity;
                return identity.Name;
            }
        }


        /// <summary> Returns a string value from the registry under HKEY_LOCAL_MACHINE. </summary>
        /// <param name="KeyLocation"> Location of the key (i.e. "Control Panel\Desktop") </param>
        /// <param name="ValueName"> Name of the value to retrieve </param>
        /// <returns> String value from the registry, or "-1" if an error occurs </returns>
        public string LocalMachineKey(string KeyLocation, string ValueName)
        {
            try
            {
                if(!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) return "Only works on windows";

                RegistryKey fetcher = Registry.LocalMachine;
                fetcher = fetcher.OpenSubKey(KeyLocation);
                if (fetcher != null)
                {
                    string fetchedValue = (string)fetcher.GetValue(ValueName);
                    fetcher.Close();
                    return fetchedValue ?? "-1";
                }
            }
            catch
            {
                return "-1";
            }
            return "-1";
        }

        /// <summary> Returns a string value from the registry under HKEY_CURRENT_USER. </summary>
        /// <param name="KeyLocation"> Location of the key (i.e. "Control Panel\Desktop") </param>
        /// <param name="ValueName"> Name of the value to retrieve </param>
        /// <returns> String value from the registry, or "-1" if an error occurs </returns>
		public string CurrentUserKey(string KeyLocation, string ValueName)
        {
            try
            {
                if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) return "Only works on windows";

                RegistryKey fetcher = Registry.CurrentUser;
                fetcher = fetcher.OpenSubKey(KeyLocation);
                if (fetcher != null)
                {
                    string fetchedValue = (string)fetcher.GetValue(ValueName);
                    fetcher.Close();
                    return fetchedValue ?? "-1";
                }
            }
            catch
            {
                return "-1";
            }
            return "-1";
        }

        /// <summary> Encrypt a string, given the string.  </summary>
        /// <param name="Source"> String to encrypt </param>
        /// <returns> The encrypted string </returns>
        /// <remarks> KNOWN ISSUE (SCS0006): weak by modern standards (CWE-916), kept only so
        /// <see cref="PasswordHasher.VerifyPassword"/> can still validate "Sobek" accounts whose password
        /// hash hasn't yet been migrated to PBKDF2 - see PasswordHasher remarks for the migration path.
        /// Do not use this for new password hashes; call <see cref="PasswordHasher.HashPassword"/> instead. </remarks>
        public static string SHA1_EncryptString(string Source)
        {
            byte[] bytIn = Encoding.ASCII.GetBytes(Source);

            // set the private key
            SHA1 sha = SHA1.Create();
            byte[] bytOut = sha.ComputeHash(bytIn);

            // convert into Base64 so that the result can be used in xml
            return Convert.ToBase64String(bytOut, 0, bytOut.Length);
        }

    }
}
