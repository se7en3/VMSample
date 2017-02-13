using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using AppUtil;
using Vim25Api;
using System.Collections.Generic;

namespace ColdMigration
{
///<summary>
///This sample puts VM files in specified Datacenter and Datastore
///and register and reconfigure the particular VM.
///</summary>
///<param name="vmname">Name of the virtual machine</param>
///<param name="localpath">localpath to copy files</param>
///<param name="datacentername">Name of the datacenter</param>
///<param name="datastorename">Name of the datastore</param>
///<remarks>
///--url [webserviceurl] 
///--username [username]--password [password] --vmname [vmname] --localpath[localpath]
///--datacentername [datacentername] --datastorename [datastorename]
///</remarks>

    class ColdMigration
    {
        private static AppUtil.AppUtil cb = null;
        static VimService _service;
        static ServiceContent _sic;
        private ArrayList vdiskName = new ArrayList();

        /// <summary>
        /// Gets virtual machine name.
        /// </summary>
        /// <returns>Returns string type vmName</returns>
        private String getVmName() {
            return cb.get_option("vmname");
        }

        /// <summary>
        /// Gets local path.
        /// </summary>
        /// <returns>Returns string type localpath</returns>
        private String getLocalPath() {
            return cb.get_option("localpath");
        }

        /// <summary>
        /// Gets datacenter name.
        /// </summary>
        /// <returns>Returns string type datacentername.</returns>
        private String getDataCenter() {
            return cb.get_option("datacentername");
        }

        /// <summary>
        ///  Gets datastore name.
        /// </summary>
        /// <returns>Returns string type datastorename.</returns>
        private String getDataStore() {
            return cb.get_option("datastorename");
        }

        ///<summary>
        ///The function gets cookies of url.
        ///</summary>
        ///<returns>Returns the string type cookie string.</returns>
        private String getCookie() {
            Cookie cookie = cb._connection._service.CookieContainer.GetCookies(
                                      new Uri(cb.get_option("url")))[0];
            String cookieString = cookie.ToString();
            return cookieString;
        }

        /// <summary>
        /// Dump the data, register the virtual machine and do reconfig if specified.
        /// File greater than 500 MB is not supported.
        /// </summary>
        private void coldMigration() {
           _service = cb.getConnection()._service;
           _sic = cb.getConnection()._sic;
           Boolean validated = customValidation();
           if(validated) {
              String[] listOfDir = getSubDirectories(getLocalPath());

               if(listOfDir != null && listOfDir.Length != 0) {
                  Boolean bSize = false;
                  // Dumping All The Data
                  for(int i=0; i<listOfDir.Length; i++) {
                     bSize = copyDir(listOfDir[i]);
                  }
                  if (bSize.ToString() == "True") {
                    // Register The Virtual Machine
                    Boolean regFlag = registerVirtualMachine();
                    //Reconfig All The Stuff Back
                    if (regFlag) {
                        reconfigVirtualMachine();
                    }
                } else {
                  Console.WriteLine("Only copying files less than 500 MB is supported");
                }
             } else {
               Console.WriteLine("There are no VM Directories"
                                  +" available on Specified locations");
             }
          } else {
         // DO NOTHING
          }
       }

        /// <summary>
        /// Copying the directory.
        /// </summary>
        /// <param name="dirName"></param>
        /// <returns>Returns the boolean value true or false.</returns>
        private Boolean copyDir(String dirName) {
           Console.WriteLine("Copying The Virtual Machine To Host..........");

          // dirName = getLocalPath() + "/" + dirName;
           String [] listOfFiles = getDirFiles(dirName);
           long fileSize = 0;

           for(int i=0; i<listOfFiles.Length; i++) {
              System.IO.FileInfo info = new System.IO.FileInfo(listOfFiles[i].ToString());
              fileSize = info.Length;
              if (fileSize > 524288000)
              {
                 return false;
              }
              String remoteFilePath = "/"+getVmName()+"/"+listOfFiles[i].Substring(listOfFiles[i].LastIndexOf("\\")+1);
              String localFilePath = dirName + "\\" + listOfFiles[i].Substring(listOfFiles[i].LastIndexOf("\\") + 1);

              if(localFilePath.IndexOf("vdisk") != -1) {
                 String dataStoreName = dirName.Substring(dirName.LastIndexOf("#")+1);
                 remoteFilePath = "/" + getVmName() + "/" + dataStoreName + "/" + listOfFiles[i].Substring(listOfFiles[i].LastIndexOf("\\") + 1);
                 if(localFilePath.IndexOf("flat") == -1) {
                    vdiskName.Add(dataStoreName+"/"+listOfFiles[i]);
                 }
              } else {
                remoteFilePath = "/"+getVmName()+"/"+listOfFiles[i].Substring(listOfFiles[i].LastIndexOf("\\")+1);            
              }
              putVMFiles(remoteFilePath,localFilePath);
           }
           Console.WriteLine("Copying The Virtual Machine To Host..........Done");
           return true;
        }

        /// <summary>
        /// Upload the VM files from local path.
        /// </summary>
        /// <param name="remoteFilePath"></param>
        /// <param name="localFilePath"></param>
        private void putVMFiles(String remoteFilePath, String localFilePath) {
           try {
              String serviceUrl = cb.getServiceUrl();
              serviceUrl = serviceUrl.Substring(0, serviceUrl.LastIndexOf("sdk") - 1);
              String httpUrl = serviceUrl + "/folder" + remoteFilePath + "?dcPath="
                                        + getDataCenter() + "&dsName=" + getDataStore();
              httpUrl = httpUrl.Replace("\\ ", "%20");
              Console.WriteLine("Putting VM File " + httpUrl);
              WebClient client = new WebClient();
              NetworkCredential nwCred = new NetworkCredential();
              nwCred.UserName = cb.get_option("username");
              nwCred.Password = cb.get_option("password");
              client.Credentials = nwCred;
              client.Headers.Add(HttpRequestHeader.Cookie, getCookie());
              client.UploadFile(httpUrl, "PUT", localFilePath);
           } catch (Exception e) {
             Console.WriteLine(e.Message.ToString());
            }
       }

        /// <summary>
        /// Gets datacenter for particular vm managed object reference.
        /// </summary>
        /// <param name="vmmor"></param>
        /// <returns>Returns string value dcName.</returns>
        private String getDataCenter(ManagedObjectReference vmmor) {
           ManagedObjectReference morParent = cb.getServiceUtil().GetMoRefProp(vmmor,"parent");       
           morParent = cb.getServiceUtil().GetMoRefProp(morParent, "parent");
           Object objdcName = cb.getServiceUtil().GetDynamicProperty(morParent, "name");
           String dcName = objdcName.ToString();
           return dcName;
        }

        /// <summary>
        /// Register the virtual machine.
        /// </summary>
        /// <returns>Returns the boolean value true or false.</returns>
        private Boolean registerVirtualMachine()
        {
            Boolean registered = false;
            Console.WriteLine("Registering The Virtual Machine ..........");

            // Get datacenter
            var serviceUtil = cb.getServiceUtil();
            var dataCenterMoref = serviceUtil.GetDecendentMoRef(_sic.rootFolder, "Datacenter", getDataCenter());
            var vmFolderMoref = (ManagedObjectReference)serviceUtil.GetDynamicProperty(dataCenterMoref, "vmFolder");

            // Get datastore
            var datastoreMorefs = (ManagedObjectReference[])serviceUtil.GetDynamicProperty(dataCenterMoref, "datastore");
            var dirSize = getDirSize(getLocalPath());
            ManagedObjectReference targetDatastoreMoref = null;
            foreach (var datastoreMoref in datastoreMorefs)
            {
                var datastoreSummary =
                    (DatastoreSummary)serviceUtil.GetDynamicProperty(datastoreMoref, "summary");
                if (datastoreSummary.name.Equals(getDataStore(), StringComparison.CurrentCultureIgnoreCase))
                {
                    var datastoreInfo
                        = (DatastoreInfo)serviceUtil.GetDynamicProperty(datastoreMoref, "info");
                    if (datastoreInfo.freeSpace > dirSize)
                    {
                        targetDatastoreMoref = datastoreMoref;
                        break;
                    }
                }
            }

            if (targetDatastoreMoref == null)
            {
                Console.WriteLine("Could not find user entered datastore.");
                return registered;
            }

            // Select the first accessible host attached to the datastore
            var datastoreHostMounts = 
                (DatastoreHostMount[])serviceUtil.GetDynamicProperty(targetDatastoreMoref, "host");
            ManagedObjectReference targetHostSystemMoref = null;
            foreach (var datastoreHostMount in datastoreHostMounts)
            {
                if (datastoreHostMount.mountInfo.accessible)
                    targetHostSystemMoref = datastoreHostMount.key;
            }

            if (targetHostSystemMoref == null)
            {
                Console.WriteLine(
                    "No accessible host found in datacenter that has the specified datastore and free space.");
                return registered;
            }
            else
            {
                // Get vmx path
                var vmxPath = "[" + getDataStore() + "] " + getVmName() + "/" + getVmName() + ".vmx";

                // The parent of a host system is either ComputeResource or ClusterComputeResource
                var computeResourceRef =
                    (ManagedObjectReference)serviceUtil.GetDynamicProperty(targetHostSystemMoref, "parent");

                // Get resource pool of the compute resource
                var resourcePoolMoref = 
                    (ManagedObjectReference)serviceUtil.GetMoRefProp(computeResourceRef, "resourcePool");

                // Registering the virtual machine
                var taskmor = _service.RegisterVM_Task(
                    vmFolderMoref, vmxPath, getVmName(), false, resourcePoolMoref, targetHostSystemMoref);

                string result = serviceUtil.WaitForTask(taskmor);
                if (result.Equals("sucess"))
                {
                    Console.WriteLine("Registering The Virtual Machine ..........Done");
                    registered = true;
                }
                else
                {
                    Console.WriteLine("Some Exception While Registering The VM");
                    registered = false;
                }
                return registered;
            }
        }

        /// <summary>
        /// Reconfig the virtual machine.
        /// </summary>
        private void reconfigVirtualMachine() {
           Console.WriteLine("ReConfigure The Virtual Machine ..........");
           VirtualMachineFileInfo vmFileInfo 
              = new VirtualMachineFileInfo();
           vmFileInfo.logDirectory = "["+getDataStore()+"]"+getVmName();
           vmFileInfo.snapshotDirectory= "["+getDataStore()+"]"+getVmName();
           vmFileInfo.suspendDirectory= "["+getDataStore()+"]"+getVmName();
           vmFileInfo.vmPathName= "["+getDataStore()
                                       +"]"+getVmName()+"/"+getVmName()+".vmx";

           VirtualMachineConfigSpec vmConfigSpec 
              = new VirtualMachineConfigSpec();
           vmConfigSpec.files =vmFileInfo;

           ManagedObjectReference taskmor 
              = _service.ReconfigVM_Task(
                   getVmMor(getVmName()),vmConfigSpec);

           Object[] result = cb.getServiceUtil().WaitForValues(
                 taskmor, new String[] { "info.state", "info.error" }, 
                 new String[] { "state" },
                 new Object[][] { new Object[] { TaskInfoState.success, TaskInfoState.error }}
           );

           if (result[0].Equals(TaskInfoState.success)) {
              Console.WriteLine("ReConfigure The Virtual Machine .......... Done");
           } else {
             Console.WriteLine("Some Exception While Reconfiguring The VM " + result[0]);
           }
         }

        /// <summary>
        /// Gets the managed object reference of particular virtual machine.
        /// </summary>
        /// <param name="vmName"></param>
        /// <returns>Returns the mor.</returns>
         private ManagedObjectReference getVmMor(String vmName) {
            ManagedObjectReference vmmor 
                 = cb.getServiceUtil().GetDecendentMoRef(null, "VirtualMachine", getVmName());
            return vmmor;
         }

        /// <summary>
        /// Gets the sub directory of any local directory.
        /// </summary>
        /// <param name="localDir"></param>
        /// <returns>Returns the string array.</returns>
         private String[] getSubDirectories(String localDir) {
            String[] listOfDirectories = Directory.GetDirectories(localDir);
            if (listOfDirectories != null) {
                return listOfDirectories;
            } else {
              Console.WriteLine("Local Path Doesn't Exist");
              return null;
            }
         }

        /// <summary>
        /// Gets the directory files.
        /// </summary>
        /// <param name="localDir"></param>
        /// <returns>Returns the string array.</returns>
         private String [] getDirFiles(String localDir) {
            String[] listOfFiles = Directory.GetFiles(localDir);
            if(listOfFiles != null) {
               return listOfFiles;
            } else {
              Console.WriteLine("Local Path Doesn't Exist");
              return null;
            }
         }

        /// <summary>
        /// Gets the directory size.
        /// </summary>
        /// <param name="localDir"></param>
        /// <returns>Returns size.</returns>
         private long getDirSize(String localDir)  {
            String[] fileList = Directory.GetFiles(localDir);
            long size = 0;
            if(fileList.Length != 0) {
               for(int i=0; i<fileList.Length; i++) {
                  System.IO.FileInfo temp = new System.IO.FileInfo(localDir + "/" + fileList[i]);
                  temp.Create();
                     if(temp.Exists) {
                        size = size + getDirSize(temp.Directory.FullName);
                     } else {
                       size = size + temp.Length;
                     }
                  }
               } else {
                 // DO NOTHING
               }
               return size;
         }

        /// <summary>
        /// Does the custom validation.
        /// </summary>
        /// <returns>Returns the boolean type true or false.</returns>
         private Boolean customValidation() {
            Boolean validate = false;
            String datacenterName = getDataCenter();
            String datastoreName = getDataStore();
            if(datacenterName.Length != 0 && datacenterName != null
                  && datastoreName.Length != 0 && datastoreName != null) {
              ManagedObjectReference dcmor
                = cb.getServiceUtil().GetDecendentMoRef(null, "Datacenter", datacenterName);
            if(dcmor != null) {
                ManagedObjectReference [] datastores 
                   = (ManagedObjectReference [])
                        cb.getServiceUtil().GetDynamicProperty(dcmor,"datastore");
               if(datastores.Length != 0) {
                  for(int i=0; i<datastores.Length; i++) {
                      DatastoreSummary dsSummary 
                         = (DatastoreSummary)
                              cb.getServiceUtil().GetDynamicProperty(datastores[i],
                                                                    "summary");
                      if(dsSummary.name.Equals(datastoreName)) {
                         i = datastores.Length + 1;
                         validate = true;
                      }
                   }
                   if(!validate) {
                      Console.WriteLine("Specified Datastore is not"
                                        +" found in specified Datacenter");
                   }
                   return validate;
                } else {
                  Console.WriteLine("No Datastore found in specified Datacenter");
                   return validate;
                }
             } else {
                Console.WriteLine("Specified Datacenter Not Found");
                return validate;
             }
           }
           return validate;
         }

         ///<summary>
         ///This method is used to add application specific user options 
         ///</summary>
         ///<returns> Array of OptionSpec containing the details of application specific user options 
         ///</returns>
         public static OptionSpec[] constructOptions() {
           OptionSpec[] useroptions = new OptionSpec[4];
           useroptions[0] = new OptionSpec("vmname", "String", 1
                                     , "Name of the virtual machine"
                                     , null);
           useroptions[1] = new OptionSpec("localpath", "String", 1,
                                            "Localpath to copy files",
                                            null);
           useroptions[2] = new OptionSpec("datacentername", "String", 1,
                                            "Name of the datacenter",
                                            null);
           useroptions[3] = new OptionSpec("datastorename", "String", 1,
                                            "Name of the datastore",
                                            null);
           return useroptions;
         }

         /// <summary>
         /// The main entry point for the application.
         /// </summary>
         public static void Main(String[] args) {
            ColdMigration obj = new ColdMigration();
            cb = AppUtil.AppUtil.initialize("ColdMigration"
                                    , ColdMigration.constructOptions()
                                   , args);
            cb.connect();
            obj.coldMigration();
            cb.disConnect();
            Console.WriteLine("Please enter any key to exit: ");
            Console.Read();
         }
   }
}
