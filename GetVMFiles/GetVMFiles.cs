using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using AppUtil;
using Vim25Api;

namespace GetVMFiles
{
    class GetVMFiles
    {
        private static AppUtil.AppUtil cb = null;
        static VimService _service;
        static ServiceContent _sic;
        private void getVMFiles(String[] args)
        {
                _service = cb.getConnection()._service;
                _sic = cb.getConnection()._sic;
                String vmName = cb.get_option("vmname");
                String localpath = cb.get_option("localpath");
                Hashtable downloadedDir = new Hashtable();  
             try {               
                 
                 Cookie cookie = cb._connection._service.CookieContainer.GetCookies(
                                new Uri(cb.get_option("url")))[0];
                 CookieContainer cookieContainer = new CookieContainer();
                 cookieContainer.Add(cookie);
                 
                ManagedObjectReference vmmor = null;
                       
                vmmor = cb.getServiceUtil().GetDecendentMoRef(null, 
                                                              "VirtualMachine",
                                                              vmName);
                if (vmmor != null)
                {

                    String dataCenterName = getDataCenter(vmmor);
                    String[] vmDirectory = getVmDirectory(vmmor);

                    if (vmDirectory[0] != null)
                    {
                        Console.WriteLine("Downloading Virtual Machine Configuration Directory");
                        String dataStoreName = vmDirectory[0].Substring(vmDirectory[0].IndexOf("[")
                               + 1, vmDirectory[0].LastIndexOf("]") - 1);
                        int length = vmDirectory[0].LastIndexOf("/") - vmDirectory[0].IndexOf("]") - 2;
                        String configurationDir
                          = vmDirectory[0].Substring(vmDirectory[0].IndexOf("]") + 2, length);
                        String localDirPath = cb.get_option("localpath") + "/" + configurationDir + "#vm#" + dataStoreName;
                        Directory.CreateDirectory(localDirPath);
                        downloadDirectory(configurationDir, localDirPath, dataStoreName, dataCenterName);
                        downloadedDir.Add(configurationDir + "#vm#" + dataStoreName, "Directory");
                        Console.WriteLine("Downloading Virtual Machine"
                                          + " Configuration Directory Complete");
                    }
                   if(vmDirectory[1] != null) {
                    Console.WriteLine("Downloading Virtual Machine Snapshot / Suspend / Log Directory");
                    for(int i=1; i < vmDirectory.Length; i++) {
                       String dataStoreName 
                          = vmDirectory[i].Substring(vmDirectory[i].IndexOf("[")
                                                    +1,vmDirectory[i].LastIndexOf("]")-1);
                       String configurationDir = "";
                       
                       ServiceContent sc = cb.getConnection().ServiceContent;
                    
                       String apiType = sc.about.apiType; 
                       if(apiType.Equals("VirtualCenter")) {
                          configurationDir = vmDirectory[i].Substring(vmDirectory[i].IndexOf("]")+2);
                          configurationDir =  configurationDir.Substring(0,configurationDir.Length-1);
                       }
                       else {
                          configurationDir 
                             = vmDirectory[i].Substring(vmDirectory[i].IndexOf("]")+2);
                       }               
                       if(!downloadedDir.ContainsKey(configurationDir+"#vm#"+dataStoreName)) {
                           String localDirPath = cb.get_option("localpath") + "/" + configurationDir + "#vm#" + dataStoreName;
                           Directory.CreateDirectory(localDirPath);
                           downloadDirectory(configurationDir, localDirPath, dataStoreName, dataCenterName);
                           downloadedDir.Add(configurationDir + "#vm#" + dataStoreName, "Directory");
                                      
                       }
                       else {
                          Console.WriteLine("Already Downloaded");
                       }
                    }
                    Console.WriteLine("Downloading Virtual Machine Snapshot"
                                      +" / Suspend / Log Directory Complete");
                 }
                    String [] virtualDiskLocations = getVDiskLocations(vmmor);      
                     if(virtualDiskLocations != null) {
                        Console.WriteLine("Downloading Virtual Disks");            
                        for(int i=0; i<virtualDiskLocations.Length; i++) {
                           if(virtualDiskLocations[i]!=null) {                  
                              String dataStoreName 
                                 = virtualDiskLocations[i].Substring(
                                      virtualDiskLocations[i].IndexOf("[")
                                    +1,virtualDiskLocations[i].LastIndexOf("]")-1);
                              String configurationDir 
                                 = virtualDiskLocations[i].Substring(
                                       virtualDiskLocations[i].IndexOf("]")
                                    + 2, virtualDiskLocations[i].LastIndexOf("/") - virtualDiskLocations[i].IndexOf("]") - 2);
                              if(!downloadedDir.ContainsKey(configurationDir+"#vdisk#"+dataStoreName)) {
                                  String localDirPath = cb.get_option("localpath") + "/" + configurationDir + "#vdisk#" + dataStoreName;
                                  Directory.CreateDirectory(localDirPath);
                                  downloadDirectory(configurationDir, localDirPath, dataStoreName, dataCenterName);
                                  downloadedDir.Add(configurationDir + "#vdisk#" + dataStoreName, "Directory");
                              }
                              else {
                                 Console.WriteLine("Already Downloaded");
                              }
                           }
                           else {
                              // Do Nothing
                           }
                        }
                        Console.WriteLine("Downloading Virtual Disks Complete");
                     }
                     else {
                        // Do Nothing
                     }    
                }
                else
                {
                    Console.WriteLine("Virtual Machine " + cb.get_option("vmname") + " Not Found.");
                }
            }
            catch (Exception e)
            {
                cb.log.LogLine("GetVMFiles failed for VM : " + vmName);
                throw e;
            }        
                   
                          
        }
        private String getCookie() {
            Cookie cookie = cb._connection._service.CookieContainer.GetCookies(
                                      new Uri(cb.get_option("url")))[0];
            String cookieString = cookie.ToString();
           return cookieString;
        }
        private String [] getVmDirectory(ManagedObjectReference vmmor)  {
              String [] vmDir = new String [4];
              VirtualMachineConfigInfo vmConfigInfo
                 = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(vmmor,"config");
              if(vmConfigInfo != null) {        
                 vmDir[0] = vmConfigInfo.files.vmPathName;
                 vmDir[1] = vmConfigInfo.files.snapshotDirectory;
                 vmDir[2] = vmConfigInfo.files.suspendDirectory;
                 vmDir[3] = vmConfigInfo.files.logDirectory;
              }
              else {
                 Console.WriteLine("Cannot Restore VM. Not Able "+"To Find The Virtual Machine Config Info");
              }
              return vmDir;
           }

        private String getDataCenter(ManagedObjectReference vmmor) 
         {
              ManagedObjectReference morParent = cb.getServiceUtil().GetMoRefProp(vmmor,"parent");
              morParent = cb.getServiceUtil().GetMoRefProp(morParent, "parent");
              if (!morParent.type.Equals("Datacenter"))
              {
                  morParent = cb.getServiceUtil().GetMoRefProp(morParent, "parent");
              }
              Object objdcName = cb.getServiceUtil().GetDynamicProperty(morParent, "name");
              String dcName = objdcName.ToString();
              return dcName;
         }
         private void downloadDirectory(String directoryName,String localDirectory, String dataStoreName, String dataCenter) 
         {
              String serviceUrl = cb.getServiceUrl();
              serviceUrl = serviceUrl.Substring(0,serviceUrl.LastIndexOf("sdk")-1);
              String httpUrl = serviceUrl+"/folder/"+directoryName+"?dcPath="
                               +dataCenter+"&dsName="+dataStoreName;
              httpUrl = httpUrl.Replace("\\ ","%20");                
              String [] linkMap = getListFiles(httpUrl);
              for(int i = 1; i < linkMap.Length; i++) {
                 Console.WriteLine("Downloading VM File " + linkMap[i]);
                 String urlString = serviceUrl + linkMap[i];
                 String fileName = localDirectory + linkMap[i].Substring(linkMap[i].LastIndexOf("/"), linkMap[i].LastIndexOf("?") - linkMap[i].LastIndexOf("/"));
                 urlString = urlString.Replace("\\ ","%20");         
                 getData(urlString, fileName);
              } 
           }
    
   
        private String [] getListFiles(String urlString)  {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlString);
            request.Headers.Add(HttpRequestHeader.Cookie, getCookie());
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
              String line = null;   
              String xmlString = "";
              using (StreamReader r = new StreamReader(response.GetResponseStream()))
               {
                   while ((line = r.ReadLine()) != null)
                   {
                     xmlString = xmlString + line;
                   }
               }
              xmlString =  xmlString.Replace("&amp;","&");           
              
              ArrayList list = getFileLinks(xmlString);;
              String [] linkMap = new String[list.Count];
              for(int i=0;i<list.Count;i++) {
                 linkMap[i]=(String)list[i];         
              }
              return linkMap;      
           }
        private ArrayList getFileLinks(String xmlString)  
        {
              ArrayList linkMap = new ArrayList();
                    
              Regex regex = new Regex("<a href=\".*?\">");
              MatchCollection regexMatcher = regex.Matches(xmlString);
              if (regexMatcher.Count > 0)
              {
                  foreach (Match m in regexMatcher)
                  {
                      String data = m.Value;
                      int ind = data.IndexOf("\"") + 1;
                      int lind = data.LastIndexOf("\"") - ind;
                      data = data.Substring(ind, lind);
                      linkMap.Add(data);
                  }
              }
              return linkMap;
        }

        private void getData(String urlString, String fileName)
        {
            WebClient client = new WebClient();
            NetworkCredential nwCred = new NetworkCredential();
            nwCred.UserName = cb.get_option("username");
            nwCred.Password = cb.get_option("password");
            client.Credentials = nwCred;
           
            try
            {
                client.DownloadFile(urlString, fileName);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.InnerException);
            }                       
        }

        private String replaceSpecialChar(String fileName)
        {
            fileName = fileName.Replace(':', '_');
            fileName = fileName.Replace('*', '_');
            fileName = fileName.Replace('<', '_');
            fileName = fileName.Replace('>', '_');
            fileName = fileName.Replace('|', '_');
            return fileName;
        }
        private String [] getVDiskLocations(ManagedObjectReference vmmor)  {
      VirtualMachineConfigInfo vmConfigInfo
         = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(vmmor, "config");
      if(vmConfigInfo != null) {
         VirtualDevice [] vDevice = vmConfigInfo.hardware.device;
         int count = 0;
         String [] virtualDisk = new String [vDevice.Length];
         
         for(int i=0; i<vDevice.Length; i++) {
             if (vDevice[i].GetType().FullName.Equals("VimApi.VirtualDisk"))
             {
               try {              
                  VirtualDeviceFileBackingInfo backingInfo 
                     = (VirtualDeviceFileBackingInfo)
                        vDevice[i].backing;
                  virtualDisk[count] = backingInfo.fileName;
                  count++;
               } catch(Exception e){
                  // DO NOTHING
               }                  
            }
         }
         
         return virtualDisk;
      }
      else {
         Console.WriteLine("Connot Restore VM. Not Able To"
            +" Find The Virtual Machine Config Info");
         return null;
      }
   }
        
        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("vmname", "String", 1
                                            , "Name of the Virtual Machine"
                                            , null);
            useroptions[1] = new OptionSpec("localpath", "String", 1
                                            , "localpath to copy files"
                                            , null);
            return useroptions;
        }
        public static void Main(String[] args)
        {
            GetVMFiles obj = new GetVMFiles();
            cb = AppUtil.AppUtil.initialize("GetVMFiles"
                                    , GetVMFiles.constructOptions()
                                   , args);            
            cb.connect();            
            obj.getVMFiles(args);
            cb.disConnect();
            Console.WriteLine("Please enter any key to exit: ");
            Console.Read();
        }
    }
}
