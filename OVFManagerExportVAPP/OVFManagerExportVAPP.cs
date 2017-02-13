using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using AppUtil;
using Vim25Api;
namespace OVFManagerExportVAPP
{
    ///<summary>
    ///This sample demonstrates OVFManager.
    /// Exports VMDK's and OVF Descriptor of all VM's in the vApp to the local disk.
    ///</summary>
    /// <param name="vapp">Required: Name of the vapp</param>
    /// <param name="localpath">Required: local System Folder path</param>
    ///
    ///--url [webserviceurl]
    ///--username [username] --password [password] --vapp [vapp] --localpath [localpath]
    ///</remarks>
    public class OVFManagerExportVAPP
    {
        private static AppUtil.AppUtil cb = null;
        private static string localPath;

        private void ExportVApp()
        {
            string vApp = cb.get_option("vapp");
            localPath = cb.get_option("localpath");
            ManagedObjectReference vAppMoRef = cb._svcUtil.getEntityByName("VirtualApp", vApp);
            if (vAppMoRef != null)
            {
                OvfCreateDescriptorParams ovfCreateDescriptorParams =
                          new OvfCreateDescriptorParams();
                Console.WriteLine("Getting the HTTP NFCLEASE for the vApp: " + vApp);
                ManagedObjectReference httpNfcLease = cb._connection._service.ExportVApp(vAppMoRef);
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
                        List<OvfFile> ovfFiles = new List<OvfFile>();
                        foreach (HttpNfcLeaseDeviceUrl urlArray in deviceUrlArr)
                        {
                            string deviceId = urlArray.key;
                            string deviceUrlStr = urlArray.url;
                            string absoluteFile = deviceUrlStr.Substring(deviceUrlStr.LastIndexOf("/") + 1);
                            Console.WriteLine("   Absolute File Name: " + absoluteFile);
                            Console.WriteLine("   VMDK URL: " + deviceUrlStr);
                            long writtenSize = WriteVMDKFile(absoluteFile, deviceUrlStr);
                            OvfFile ovfFile = new OvfFile();
                            ovfFile.path = absoluteFile;
                            ovfFile.deviceId = deviceId;
                            ovfFile.size = writtenSize;
                            ovfFiles.Add(ovfFile);
                        }
                        ovfCreateDescriptorParams.ovfFiles = ovfFiles.ToArray();
                        OvfCreateDescriptorResult ovfCreateDescriptorResult =
                        cb._connection._service.CreateDescriptor(
                          cb._connection._sic.ovfManager, vAppMoRef, ovfCreateDescriptorParams);
                        String outOvf = localPath + "/" + vApp + ".ovf";
                        File.WriteAllText(outOvf, ovfCreateDescriptorResult.ovfDescriptor);
                        Console.WriteLine("OVF Desriptor Written to file " + vApp + ".ovf");
                        Console.WriteLine("DONE");
                        if (ovfCreateDescriptorResult.error != null)
                        {
                            Console.WriteLine("Following Errors occured:");
                            foreach (LocalizedMethodFault lf in ovfCreateDescriptorResult.error)
                            {
                                Console.WriteLine(lf.localizedMessage);
                            }
                        }
                        if (ovfCreateDescriptorResult.warning != null)
                        {
                            Console.WriteLine("Following Warnings occured:");
                            foreach (LocalizedMethodFault lf in ovfCreateDescriptorResult.warning)
                            {
                                Console.WriteLine(lf.localizedMessage);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No url found");
                    }
                    Console.WriteLine("Completed Downloading the files");
                    cb._connection._service.HttpNfcLeaseProgress(httpNfcLease, 100);
                    cb._connection._service.HttpNfcLeaseComplete(httpNfcLease);

                }
                else
                {
                    throw new Exception("Error happened while acquiring HttpNfcLease");
                }
            }
            else
            {
                throw new Exception("vApp not found");
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
                    Console.WriteLine("   Device URL : " + durl.url);
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
        private long WriteVMDKFile(string absolutePath, string URL)
        {
            int size;
            Uri uri = new Uri(URL);
            WebRequest http = HttpWebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)http.GetResponse();
            Stream stream = response.GetResponseStream();
            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader readStream = new StreamReader(stream, encode);
            string strResponse = readStream.ReadToEnd();
            size = strResponse.Length;            
            string path = localPath + "/" + absolutePath;
            StreamWriter oSw = new StreamWriter(path);
            oSw.WriteLine(strResponse);
            oSw.Close();
            readStream.Close();
            response.Close();
            return size;
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("vapp", "String", 1,
                                            "Name of the vapp",
                                              null);
            useroptions[1] = new OptionSpec("localpath", "String", 1,
                                            "local System path where files needs to be saved",
                                             null);
            return useroptions;
        }

        public static void Main(string[] args)
        {
            try
            {
                OVFManagerExportVAPP app = new OVFManagerExportVAPP();
                cb = AppUtil.AppUtil.initialize("OVFManagerExportVAPP",
                                        OVFManagerExportVAPP.constructOptions(),
                                        args);
                cb.connect();
                app.ExportVApp();
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
