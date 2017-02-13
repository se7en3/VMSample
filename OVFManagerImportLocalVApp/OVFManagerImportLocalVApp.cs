using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppUtil;
using Vim25Api;
using System.IO;
using System.Net;

namespace OVFManagerImportLocalVApp
{
    ///<summary>
    ///This sample is used to import or deploy an OVF Appliance from the Local drive.
    ///</summary>
    ///<param name="host">Required: Name of host</param>
    /// <param name="vappname">Required: Name of the virtual machine</param>
    /// <param name="localpath">Required: local System ovf file path</param>
    /// <param name="datastore">Optional: local System Folder path</param>
    ///
    ///--url [webserviceurl]
    ///--username [username] --password [password] --host [hostname]
    /// --vappname [vappname] --localpath [localpath] --datastore[datastore]
    ///</remarks>
    public class OVFManagerImportLocalVApp
    {
        private static AppUtil.AppUtil cb = null;

        /// <summary>
        /// Method to import vapp from the local path
        /// </summary>
        private void ImportVApp()
        {

            string hostName = cb.get_option("host");
            string vappName = cb.get_option("vappname");
            string localPath = cb.get_option("localpath");
            string datastore = cb.get_option("datastore");
            ManagedObjectReference rpMor = null;
            ManagedObjectReference parentMor = null;
            ManagedObjectReference dsMor = null;
            long totalBytes;

            ManagedObjectReference hostRef = cb._svcUtil.getEntityByName("HostSystem", hostName);
            if (hostRef == null)
            {
                throw new Exception("Host Not Found");
            }
            else
            {
                ManagedObjectReference[] dsList = null;

                ObjectContent[] objContent = cb.getServiceUtil().GetObjectProperties(null, hostRef,
                      new string[] { "datastore", "parent" });
                if (objContent != null)
                {
                    foreach (ObjectContent oc in objContent)
                    {
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            foreach (DynamicProperty dp in dps)
                            {
                                if (dp.name.Equals("datastore"))
                                {
                                    dsList = (ManagedObjectReference[])dp.val;

                                }
                                else if (dp.name.Equals("parent"))
                                {
                                    parentMor = (ManagedObjectReference)dp.val;
                                }
                            }
                        }
                    }
                }
                if (dsList.Length == 0)
                {
                    throw new Exception("No Datastores accesible from host " + hostName);
                }
                if (datastore == null)
                {
                    dsMor = dsList.First();
                }
                else
                {
                    foreach (ManagedObjectReference ds in dsList)
                    {
                        if (datastore.Equals((string)cb._svcUtil.GetDynamicProperty(ds, "name")))
                        {
                            dsMor = ds;
                            break;
                        }
                    }
                }
                if (dsMor == null)
                {
                    if (datastore != null)
                    {
                        throw new Exception("No Datastore by name " + datastore
                                                        + " is accessible from host " + hostName);
                    }
                }

                rpMor = (ManagedObjectReference)cb._svcUtil.GetDynamicProperty(parentMor, "resourcePool");
                ManagedObjectReference dcMor = getDatacenterOfDatastore(dsMor);
                ManagedObjectReference vmFolder = (ManagedObjectReference)cb._svcUtil.GetDynamicProperty(dcMor,
                    "vmFolder");
                OvfCreateImportSpecParams importSpecParams = createImportSpecParams(hostRef, vappName);
                string ovfDescriptor = getOvfDescriptorFromLocal(localPath);
                if (ovfDescriptor == null)
                {
					throw new Exception("Could not load the OVF descriptor from the file " + localPath);
                }

                OvfCreateImportSpecResult ovfImportResult =
                cb._connection._service.CreateImportSpec(cb._connection._sic.ovfManager,
                 ovfDescriptor, rpMor, dsMor, importSpecParams);
                OvfFileItem[] fileItemAttr = ovfImportResult.fileItem;
                if (fileItemAttr != null)
                {
                    foreach (OvfFileItem fi in fileItemAttr)
                    {
                        printOvfFileItem(fi);
                        totalBytes = fi.size;
                        Console.WriteLine(totalBytes);
                    }
                    ManagedObjectReference httpNfcLease =
                    cb._connection._service.ImportVApp(rpMor, ovfImportResult.importSpec,
                           vmFolder, hostRef);
                    HttpNfcLeaseInfo httpNfcLeaseInfo = null;
                    Object[] result =
                    cb._svcUtil.WaitForValues(httpNfcLease, new String[] { "state" },
                          new String[] { "state" },
                          new Object[][] { new Object[] { "ready", "error" } });
                    if (result[0].Equals("ready"))
                    {
                        httpNfcLeaseInfo = cb._svcUtil.GetDynamicProperty(httpNfcLease, "info") as HttpNfcLeaseInfo;
                        printHttpNfcLeaseInfo(httpNfcLeaseInfo);
                        HttpNfcLeaseDeviceUrl[] deviceUrlArr = httpNfcLeaseInfo.deviceUrl;
                        if (deviceUrlArr != null)
                        {
                            List<OvfFile> ovfFiles = new List<OvfFile>();
                            int step = 100 / fileItemAttr.Length;
                            int progress = 0;
                            foreach (HttpNfcLeaseDeviceUrl deviceUrl in deviceUrlArr)
                            {
                                string deviceKey = deviceUrl.importKey;
                                foreach (OvfFileItem ovfFileItem in fileItemAttr)
                                {
                                    if (deviceKey.Equals(ovfFileItem.deviceId))
                                    {
                                        Console.WriteLine("Import key: " + deviceKey);
                                        Console.WriteLine("OvfFileItem device id: "
                                              + ovfFileItem.deviceId);
                                        Console.WriteLine("HTTP Post file: "
                                               + ovfFileItem.path);

                                        String absoluteFile =
                                              localPath.Substring(0, localPath.LastIndexOf("\\"));
                                        absoluteFile =
                                              absoluteFile + "/" + ovfFileItem.path;
                                        Console.WriteLine("Absolute path: " + absoluteFile);

                                        SendVMDKFile(ovfFileItem.create, absoluteFile,
                                              deviceUrl.url.Replace("*", hostName),
                                              ovfFileItem.size);
                                        Console.WriteLine("Completed uploading the VMDK file");
                                        progress += step;
                                        cb._connection._service.HttpNfcLeaseProgress(httpNfcLease, progress);
                                        break;
                                    }
                                }
                            }
                            cb._connection._service.HttpNfcLeaseProgress(httpNfcLease, 100);
                            cb._connection._service.HttpNfcLeaseComplete(httpNfcLease);
                        }
                    }
                    else
                    {
                        throw new Exception("Error happened while acquiring HttpNfcLease");
                    }
                }
            }
        }

        /// <summary>
        /// Places vmdk file content to  URL
        /// </summary>
        /// <param name="put">Boolean</param>
        /// <param name="fileName">string</param>
        /// <param name="url">string</param>
        /// <param name="diskCapacity">long</param>
        private void SendVMDKFile(Boolean put, string fileName, string url,
         long diskCapacity)
        {
            byte[] buffer = new byte[1024 * 1024];
			Console.WriteLine("Destination host URL: " + url);
			Uri uri = new Uri(url);
			WebRequest request = HttpWebRequest.Create(uri);
			if (put)
			{
				request.Method = "PUT";
			}
			else
			{
				request.Method = "POST";
			}
			//request.ContentLength = diskCapacity;
			request.ContentType = "application/x-vnd.vmware-streamVmdk";
			System.IO.FileStream fileStream = new FileStream(fileName, FileMode.Open);
			Stream dataStream = request.GetRequestStream();
			int len = 0;
			while ((len = fileStream.Read(buffer, 0, buffer.Length)) > 0)
			{
				dataStream.Write(buffer, 0, len);

			}
			dataStream.Flush();
			dataStream.Close();
			fileStream.Close();
        }

        /// <summary>
        /// Method to print Ovf file item
        /// </summary>
        /// <param name="fi">OvfFileItem</param>
        private void printOvfFileItem(OvfFileItem fi)
        {
            Console.WriteLine("##########################################################");
            Console.WriteLine("OvfFileItem");
            Console.WriteLine("chunkSize: " + fi.chunkSize);
            Console.WriteLine("create: " + fi.create);
            Console.WriteLine("deviceId: " + fi.deviceId);
            Console.WriteLine("path: " + fi.path);
            Console.WriteLine("size: " + fi.size);
            Console.WriteLine("##########################################################");
        }

        /// <summary>
        /// method to print HttpNfcLease Info
        /// </summary>
        /// <param name="info">HttpNfcLeaseInfo</param>
        /// <param name="hostName">string</param>
        private void printHttpNfcLeaseInfo(HttpNfcLeaseInfo info)
        {
            Console.WriteLine("########################################################");
            Console.WriteLine("HttpNfcLeaseInfo");
            // System.out.println("cookie: " + info.getCookie());
            HttpNfcLeaseDeviceUrl[] deviceUrlArr = info.deviceUrl;
            foreach (HttpNfcLeaseDeviceUrl durl in deviceUrlArr)
            {
                Console.WriteLine("Device URL Import Key: " + durl.importKey);
                Console.WriteLine("Device URL Key: " + durl.key);
                Console.WriteLine("Device URL : " + durl.url);
            }
            Console.WriteLine("Lease Timeout: " + info.leaseTimeout);
            Console.WriteLine("Total Disk capacity: "
                  + info.totalDiskCapacityInKB);
            Console.WriteLine("########################################################");
        }

        /// <summary>
        /// Gets Descriptor of OVF FILE
        /// </summary>
        /// <param name="ovfDescriptorUrl">string</param>
        /// <returns>string</returns>
        private string getOvfDescriptorFromLocal(string ovfDescriptorUrl)
        {
            string strContent = "";
            StreamReader sr = new System.IO.StreamReader(ovfDescriptorUrl);
            strContent = sr.ReadToEnd();
            return strContent;
        }

        /// <summary>
        /// Create object of OvfCreateImportSpecParams
        /// </summary>
        /// <param name="host">ManagedObjectReference</param>
        /// <param name="newVmName">string</param>
        /// <returns>OvfCreateImportSpecParams</returns>
        private OvfCreateImportSpecParams createImportSpecParams(
         ManagedObjectReference host, string newVmName)
        {
            OvfCreateImportSpecParams importSpecParams =
                  new OvfCreateImportSpecParams();
            importSpecParams.hostSystem = host;
            importSpecParams.locale = "";
            importSpecParams.entityName = newVmName;
            importSpecParams.deploymentOption = "";
            return importSpecParams;
        }

        /// <summary>
        /// Get MOR of Datacenter
        /// </summary>
        /// <param name="dsMor">ManagedObjectReference</param>
        /// <returns>ManagedObjectReference</returns>
        private ManagedObjectReference getDatacenterOfDatastore(ManagedObjectReference dsMor)
        {
            ManagedObjectReference datacenter = null;

            // Create Property Spec
            PropertySpec propertySpec = new PropertySpec();
            propertySpec.all = false;
            propertySpec.type = "Datacenter";
            propertySpec.pathSet = new string[] { "name" };

            // Now create Object Spec
            ObjectSpec objectSpec = new ObjectSpec();
            objectSpec.obj = dsMor;
            objectSpec.skip = true;
            objectSpec.selectSet = buildTraversalSpecForDatastoreToDatacenter();

            // Create PropertyFilterSpec using the PropertySpec and ObjectPec
            // created above.
            PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
            propertyFilterSpec.propSet = new PropertySpec[] { propertySpec };
            propertyFilterSpec.objectSet = new ObjectSpec[] { objectSpec };

            PropertyFilterSpec[] propertyFilterSpecs =
                  new PropertyFilterSpec[] { propertyFilterSpec };

            ObjectContent[] oCont =
                    cb._svcUtil.retrievePropertiesEx(cb._connection._sic.propertyCollector,
                          propertyFilterSpecs);

            if (oCont != null)
            {
                foreach (ObjectContent oc in oCont)
                {
                    datacenter = oc.obj;
                    break;
                }
            }
            return datacenter;
        }




        /// <summary>
        /// Create TraversalSpec for Datacenter
        /// </summary>
        /// <returns>SelectionSpec[]</returns>
        private SelectionSpec[] buildTraversalSpecForDatastoreToDatacenter()
        {
            // For Folder -> Folder recursion
            SelectionSpec sspecvfolders = new SelectionSpec();
            sspecvfolders.name = "VisitFolders";

            TraversalSpec visitFolders = new TraversalSpec();
            visitFolders.type = "Folder";
            visitFolders.path = "parent";
            visitFolders.skip = false;
            visitFolders.name = "VisitFolders";
            visitFolders.selectSet = new SelectionSpec[] { sspecvfolders };

            TraversalSpec datastoreToFolder = new TraversalSpec();
            datastoreToFolder.type = "Datastore";
            datastoreToFolder.path = "parent";
            datastoreToFolder.skip = false;
            datastoreToFolder.name = "DatastoreToFolder";
            datastoreToFolder.selectSet = new SelectionSpec[] { sspecvfolders };

            SelectionSpec[] speclist = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec() };
            speclist[0] = datastoreToFolder;
            speclist[1] = visitFolders;
            return speclist;
        }

        /// <summary>
        /// Return MOR 
        /// </summary>
        /// <param name="folder">ManagedObjectReference</param>
        /// <param name="type">string</param>
        /// <param name="name">string</param>
        /// <returns>ManagedObjectReference</returns>
        public ManagedObjectReference GetMOREFsInFolderByType(ManagedObjectReference folder, string type,
                                                             string name)
        {

            String propName = "name";
            string[] type1 = new string[2];
            type1[0] = type;
            ManagedObjectReference viewManager = cb._connection._sic.viewManager;
            ManagedObjectReference containerView =
                  cb._connection._service.CreateContainerView(viewManager, folder,
                        type1, true);

            PropertySpec propertySpec = new PropertySpec();
            propertySpec.all = false;
            propertySpec.type = type;
            propertySpec.pathSet = new string[] { propName };

            TraversalSpec ts = new TraversalSpec();
            ts.name = "view";
            ts.path = "view";
            ts.skip = false;
            ts.type = "ContainerView";

            // Now create Object Spec
            ObjectSpec objectSpec = new ObjectSpec();
            objectSpec.obj = containerView;
            objectSpec.skip = true;
            objectSpec.selectSet = new SelectionSpec[] { ts };

            // Create PropertyFilterSpec using the PropertySpec and ObjectPec
            // created above.
            PropertyFilterSpec propertyFilterSpec = new PropertyFilterSpec();
            propertyFilterSpec.propSet = new PropertySpec[] { propertySpec };
            propertyFilterSpec.objectSet = new ObjectSpec[] { objectSpec };

            PropertyFilterSpec[] filterspec = new PropertyFilterSpec[3];
            filterspec[0] = propertyFilterSpec;

            ObjectContent[] ocary =
            cb._svcUtil.retrievePropertiesEx(cb._connection._sic.propertyCollector,
                  filterspec);
            if (ocary == null || ocary.Length == 0)
            {
                return null;
            }

            ObjectContent oc = null;
            ManagedObjectReference mor = null;
            DynamicProperty[] propary = null;
            string propval = null;
            bool found = false;
            for (int oci = 0; oci < ocary.Length && !found; oci++)
            {
                oc = ocary[oci];
                mor = oc.obj;
                propary = oc.propSet;

                if ((type == null) || (type != null && cb._svcUtil.typeIsA(type, mor.type)))
                {
                    if (propary.Length > 0)
                    {
                        propval = (string)propary[0].val;
                    }

                    found = propval != null && name.Equals(propval);
                    propval = null;
                }
            }

            if (!found)
            {
                mor = null;
            }

            return mor;
        }

        private static OptionSpec[] ConstructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[4];
            useroptions[0] = new OptionSpec("host", "String", 1
                                           , "Name of the host"
                                           , null);
            useroptions[1] = new OptionSpec("vappname", "String", 1,
                                            "Name of the virtual appliance",
                                              null);
            useroptions[2] = new OptionSpec("localpath", "String", 1,
                                            "OVFFile local lpath",
                                             null);
            useroptions[3] = new OptionSpec("datastore", "String", 0,
                                           " Name of the datastore to be used ",
                                            null);
            return useroptions;
        }

        static void Main(string[] args)
        {

            try
            {
                OVFManagerImportLocalVApp app = new OVFManagerImportLocalVApp();
                cb = AppUtil.AppUtil.initialize("OVFManagerImportLocalVApp",
                                        OVFManagerImportLocalVApp.ConstructOptions(),
                                        args);
                cb.connect();
                app.ImportVApp();
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
