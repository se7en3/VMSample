using System;
using System.Linq;
using AppUtil;
using Vim25Api;
using System.Net;
using System.IO;

namespace OVFManagerExportVMDK
{
    ///<summary>
    ///This sample demonstrates OVFManager.Exports VMDK's of a VM to the localSystem.
    ///</summary>
    /// <param name="vmname">Required: Name of the virtual machine</param>
    /// <param name="localpath">Required: local System Folder path</param>
    ///
    ///--url [webserviceurl]
    ///--username [username] --password [password] --vmname [vmname] --localpath [localpath]
    ///</remarks>
    public class OVFManagerExportVMDK
    {
        private static AppUtil.AppUtil cb = null;
        private static string localPath = null;

        private void ExportVM()
        {
            string vmName = cb.get_option("vmname");
            localPath = cb.get_option("localpath");
            ManagedObjectReference vmMoRef = cb._svcUtil.getEntityByName("VirtualMachine", vmName);
            if (vmMoRef != null)
            {
                Console.WriteLine("Getting the HTTP NFCLEASE for the VM: " + vmName);
                ManagedObjectReference httpNfcLease = cb._connection._service.ExportVm(vmMoRef);
                HttpNfcLeaseInfo httpNfcLeaseInfo = null;
                Object[] result =
                        cb._svcUtil.WaitForValues(httpNfcLease, new String[] { "state" },
                                new String[] { "state" },
                                new Object[][] { new Object[] { "ready", "error" } });
                if (result[0].Equals("ready"))
                {
                    httpNfcLeaseInfo = cb._svcUtil.GetDynamicProperty(httpNfcLease, "info") as HttpNfcLeaseInfo;
                    httpNfcLeaseInfo.leaseTimeout = 300000000;
                    PrintHttpNfcLeaseInfo(httpNfcLeaseInfo);
                    long diskCapacity = (httpNfcLeaseInfo.totalDiskCapacityInKB) * 1024;
                    Console.WriteLine("************ " + diskCapacity);
                    HttpNfcLeaseDeviceUrl[] deviceUrlArr = httpNfcLeaseInfo.deviceUrl;
                    if (deviceUrlArr != null)
                    {
                        Console.WriteLine("Downloading Files:");
                        foreach (HttpNfcLeaseDeviceUrl urlArray in deviceUrlArr)
                        {
                            string deviceUrlStr = urlArray.url;
                            string absoluteFile = deviceUrlStr.Substring(deviceUrlStr.LastIndexOf("/") + 1);
                            Console.WriteLine("   Absolute File Name: " + absoluteFile);
                            Console.WriteLine("   VMDK URL: " + deviceUrlStr.Replace("*",cb.getHostName()));
                            WriteVmdkFile(absoluteFile, deviceUrlStr.Replace("*", cb.getHostName()), vmName);
                        }
                        Console.WriteLine("Completed Downloading the files");
                        cb._connection._service.HttpNfcLeaseComplete(httpNfcLease);
                    }
                    else
                    {
                        throw new Exception("No url found for the specified VM");
                    }
                }
                else
                {
                    throw new Exception("Error happened while acquiring HttpNfcLease");
                }
            }
            else
            {
                throw new Exception("Virtual machine  " + vmName + "not found");
            }
        }

        /// <summary>
        /// method to print HttpNfcLease Info
        /// </summary>
        /// <param name="info">HttpNfcLeaseInfo</param>
        private void PrintHttpNfcLeaseInfo(HttpNfcLeaseInfo info)
        {
            Console.WriteLine("########################################################");
            Console.WriteLine("HttpNfcLeaseInfo");
            Console.WriteLine("Lease Timeout: " + info.leaseTimeout);
            Console.WriteLine("Total Disk capacity: " + info.totalDiskCapacityInKB);
            HttpNfcLeaseDeviceUrl[] deviceUrlArr = info.deviceUrl;
            if (deviceUrlArr != null)
            {
                int deviceUrlCount = 1;
                foreach (HttpNfcLeaseDeviceUrl durl in deviceUrlArr)
                {
                    Console.WriteLine("HttpNfcLeaseDeviceUrl : " + deviceUrlCount++);
                    Console.WriteLine("   Device URL Import Key: "
                          + durl.importKey);
                    Console.WriteLine("   Device URL Key: " + durl.key);
                    Console.WriteLine("   Device URL : " + durl.url.Replace("*", cb.getHostName()));
                    Console.WriteLine("   SSL Thumbprint : " + durl.sslThumbprint);
                }
            }
            else
            {
                Console.WriteLine("No Device URLS Found");
                Console.WriteLine("########################################################");
            }
        }

        /// <summary>
        /// Method to write VMDK file to local path.
        /// 
        /// </summary>
        /// <param name="absolutePath">string</param>
        /// <param name="URL">string</param>
        /// <returns></returns>
        private void WriteVmdkFile(string absolutePath, string url, string vmName)
        {
            Uri uri = new Uri(url);
            WebRequest http = HttpWebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)http.GetResponse();
            Stream stream = response.GetResponseStream();
            string path = localPath + "/" + vmName + "-" + absolutePath;
            byte[] buffer = new byte[1024 * 1024];
            System.IO.FileStream fileStream = new FileStream(path, FileMode.Create);
            int len = 0;
            while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, len);

            }
            fileStream.Flush();
            fileStream.Close();
            response.Close();
        }

        private static OptionSpec[] ConstructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("vmname", "String", 1,
                                            "Name of the virtual machine",
                                              null);
            useroptions[1] = new OptionSpec("localpath", "String", 1,
                                            "local System path where files needs to be saved",
                                             null);
            return useroptions;
        }

        static void Main(string[] args)
        {
            try
            {
                OVFManagerExportVMDK app = new OVFManagerExportVMDK();
                cb = AppUtil.AppUtil.initialize("OVFManagerExportVMDK",
                                        OVFManagerExportVMDK.ConstructOptions(),
                                        args);
                cb.connect();
                app.ExportVM();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            cb.disConnect();
            Console.Read();
        }
    }
}
