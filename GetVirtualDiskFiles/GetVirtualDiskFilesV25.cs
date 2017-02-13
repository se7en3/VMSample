using System;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Net;

namespace GetVirtualDiskFiles
{
    class GetVirtualDiskFilesV25
    {
        static VimService _service;
        static ServiceContent _sic;
        private static AppUtil.AppUtil ecb = null;

        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[1];
            useroptions[0] = new OptionSpec("hostip", "String", 1
                                            , "IP of the host"
                                            , null);
            return useroptions;
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(String[] args)
        {
            GetVirtualDiskFilesV25 obj = new GetVirtualDiskFilesV25();
            ecb = AppUtil.AppUtil.initialize("GetVirtualDiskFilesV25"
                                            , GetVirtualDiskFilesV25.constructOptions()
                                            , args);

            ecb.connect();
            obj.GetVirtualDiskFilesForHost();
            ecb.disConnect();
            Console.WriteLine("Press any key to exit: ");
            Console.Read();
        }

        
        public static Object getObjectProperty(ManagedObjectReference moRef, String propertyName)
        {
            return getProperties(moRef, new String[] { propertyName })[0];           

        }

        /*
         * getProperties --
         * 
         * Retrieves the specified set of properties for the given managed object
         * reference into an array of result objects (returned in the same oder
         * as the property list).
         */
        public static Object[] getProperties(ManagedObjectReference moRef, String[] properties)
        {
            // PropertySpec specifies what properties to
            // retrieve and from type of Managed Object
            PropertySpec pSpec = new PropertySpec();
            pSpec.type = moRef.type;
            pSpec.pathSet = properties;

            // ObjectSpec specifies the starting object and
            // any TraversalSpecs used to specify other objects 
            // for consideration
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = moRef;

            // PropertyFilterSpec is used to hold the ObjectSpec and 
            // PropertySpec for the call
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = new PropertySpec[] { pSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };

            // retrieveProperties() returns the properties
            // selected from the PropertyFilterSpec


            ObjectContent[] ocs = new ObjectContent[20];
            ocs = ecb._svcUtil.retrievePropertiesEx(_sic.propertyCollector, new PropertyFilterSpec[] { pfSpec });

            // Return value, one object for each property specified
            Object[] ret = new Object[properties.Length];

            if (ocs != null)
            {
                for (int i = 0; i < ocs.Length; ++i)
                {
                    ObjectContent oc = ocs[i];
                    DynamicProperty[] dps = oc.propSet;
                    if (dps != null)
                    {
                        for (int j = 0; j < dps.Length; ++j)
                        {
                            DynamicProperty dp = dps[j];
                            // find property path index
                            for (int p = 0; p < ret.Length; ++p)
                            {
                                if (properties[p].Equals(dp.name))
                                {
                                    ret[p] = dp.val;
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }
        public void GetVirtualDiskFilesForHost()
        {
            try
            {
                _service = ecb.getConnection().Service;
                _sic = ecb.getConnection().ServiceContent;         
            
                ArrayList supportedVersions = VersionUtil.getSupportedVersions(ecb.get_option("url"));
                ManagedObjectReference hmor = _service.FindByIp(ecb.getConnection().ServiceContent.searchIndex, null, ecb.get_option("hostip"), false);
                if (hmor == null)
                {
                    Console.WriteLine("Unable to find host with IP : " + ecb.get_option("hostip") + " in Inventory");
                }
                else
                {
                    if (VersionUtil.isApiVersionSupported(supportedVersions, "2.5"))
                    {
                        Object[] datastores = getProperties(hmor, new String[] { "datastore" });
                        Console.WriteLine("Searching The Datastores");
                        ManagedObjectReference[] dstoreArr = datastores[0] as ManagedObjectReference[];
                        foreach (ManagedObjectReference dstore in dstoreArr)
                        {
                            ManagedObjectReference dsBrowser =
                                            ecb.getServiceUtil().GetMoRefProp(dstore, "browser");
                            ObjectContent[] objary = ecb.getServiceUtil().GetObjectProperties(_sic.propertyCollector, dstore, new String[] { "summary" });
                            DatastoreSummary ds = objary[0].propSet[0].val as DatastoreSummary;
                            String dsName = ds.name;
                            Console.WriteLine("");
                            Console.WriteLine("Searching The Datastore " + dsName);
                            VmDiskFileQueryFilter vdiskFilter = new VmDiskFileQueryFilter();
                            String[] type = { "VirtualIDEController" };
                            vdiskFilter.controllerType = type;
                            Boolean flag = VersionUtil.isApiVersionSupported(supportedVersions, "4.0");
                            if (flag)
                            {
                                vdiskFilter.thin = true;
                            }
                            VmDiskFileQuery fQuery = new VmDiskFileQuery();
                            fQuery.filter = vdiskFilter;

                            HostDatastoreBrowserSearchSpec searchSpec = new HostDatastoreBrowserSearchSpec();

                            FileQuery[] arr = { fQuery };
                            searchSpec.query = arr;
                            //searchSpec.setMatchPattern(matchPattern);

                            ManagedObjectReference taskmor = _service.SearchDatastoreSubFolders_Task(dsBrowser, "[" + dsName + "]", searchSpec);

                            object[] result = ecb.getServiceUtil().WaitForValues(taskmor, new string[] { "info.state", "info.result" },
                                  new string[] { "state" }, // info has a property - state for state of the task
                                  new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } }
                            );

                            // Wait till the task completes.
                            if (result[0].Equals(TaskInfoState.success))
                            {
                                ObjectContent[] objTaskInfo = ecb.getServiceUtil().GetObjectProperties(_sic.propertyCollector, taskmor, new String[] { "info" });
                                TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val; ;
                                HostDatastoreBrowserSearchResults[] searchResult = (HostDatastoreBrowserSearchResults[])tInfo.result;

                                int len = searchResult.Length;
                                for (int j = 0; j < len; j++)
                                {
                                    HostDatastoreBrowserSearchResults sres
                                        = searchResult[j];
                                    FileInfo[] fileArray = sres.file;
                                    if (fileArray != null)
                                    {
                                        for (int z = 0; z < fileArray.Length; z++)
                                        {
                                            Console.WriteLine("Virtual Disks Files " + fileArray[z].path);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("No Thin-provisioned Virtual Disks Files found");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("SearchDatastoreSubFolders Task couldn't be completed successfully");
                            }
                        }
                    }
                    else
                    {
                        Object[] datastores = getProperties(hmor, new String[] { "datastore" });
                        Console.WriteLine("Searching The Datastores");
                        ManagedObjectReference[] dstoreArr = datastores[0] as ManagedObjectReference[];
                        foreach (ManagedObjectReference dstore in dstoreArr)
                        {
                            ManagedObjectReference dsBrowser = (ManagedObjectReference)
                                                         ecb.getServiceUtil().GetMoRefProp(dstore, "browser");
                            ObjectContent[] objary = ecb.getServiceUtil().GetObjectProperties(_sic.propertyCollector, dstore, new String[] { "summary" });
                            DatastoreSummary ds = objary[0].propSet[0].val as DatastoreSummary;

                            String dsName = ds.name;
                            Console.WriteLine("");
                            Console.WriteLine("Searching The Datastore " + dsName);
                            HostDatastoreBrowserSearchSpec searchSpec = new HostDatastoreBrowserSearchSpec();
                            ManagedObjectReference taskmor = _service.SearchDatastoreSubFolders_Task(dsBrowser, "[" + dsName + "]", searchSpec);
                            object[] result = ecb.getServiceUtil().WaitForValues(taskmor, new string[] { "info.state", "info.result" },
                                     new string[] { "state" }, // info has a property - state for state of the task
                                     new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } }
                            );
                            // Wait till the task completes.
                            if (result[0].Equals(TaskInfoState.success))
                            {
                                ObjectContent[] objTaskInfo = ecb.getServiceUtil().GetObjectProperties(_sic.propertyCollector, taskmor, new String[] { "info" });
                                TaskInfo tInfo = (TaskInfo)objTaskInfo[0].propSet[0].val; ;
                                HostDatastoreBrowserSearchResults[] searchResult = (HostDatastoreBrowserSearchResults[])tInfo.result;
                                int len = searchResult.Length;
                                for (int j = 0; j < len; j++)
                                {
                                    HostDatastoreBrowserSearchResults sres
                                                     = searchResult[j];
                                    FileInfo[] fileArray = sres.file;
                                    for (int z = 0; z < fileArray.Length; z++)
                                    {
                                        Console.WriteLine("Virtual Disks Files " + fileArray[z].path);
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("SearchDatastoreSubFolders Task couldn't be completed successfully");
                            }
                        }
                    }
                }
            }          
            catch (Exception e)
            {
                ecb.log.LogLine("VirtualDiskFiles : Failed Connect");
                throw e;
            }
            finally
            {
                ecb.log.LogLine("Ended VirtualDiskFiles");
                ecb.log.Close();
            }
        }
    }
}
