using System;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Net;

namespace RecordSession
{
    class RecordSessionV25
    {
        private static AppUtil.AppUtil ecb = null;
        static VimService _service;
        static ServiceContent _sic;

        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[3];
            useroptions[0] = new OptionSpec("vmname", "String", 1
                                            , "Name of the virtual machine"
                                            , null);
            useroptions[1] = new OptionSpec("snapshotname", "String", 0
                                            , "Name of the snapshot name"
                                            , null);
            useroptions[2] = new OptionSpec("description", "String", 0
                                            , "Description"
                                            , null);
            return useroptions;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            RecordSessionV25 obj = new RecordSessionV25();
            ecb = AppUtil.AppUtil.initialize("RecordSessionV25"
                                    , RecordSessionV25.constructOptions()
                                   , args);

            ecb.connect();
            obj.RecordSessionOfVM();
            ecb.disConnect();
        }

        public void RecordSessionOfVM()
        {
            try
            {
                _service = ecb.getConnection().Service;
                _sic = ecb.getConnection().ServiceContent;
                ArrayList supportedVersions = VersionUtil.getSupportedVersions(ecb.get_option("url"));
                ManagedObjectReference vmmor = ecb.getServiceUtil().GetDecendentMoRef(null, "VirtualMachine", ecb.get_option("vmname"));
                if (vmmor == null)
                {
                    Console.WriteLine("Unable to find VirtualMachine named : " + ecb.get_option("vmname") + " in Inventory");
                }
                if (VersionUtil.isApiVersionSupported(supportedVersions, "2.5"))
                {
                    Boolean flag = VersionUtil.isApiVersionSupported(supportedVersions, "4.0");
                    if (flag)
                    {
                        if (ecb.get_option("snapshotname") == null || ecb.get_option("description") == null)
                        {
                            Console.WriteLine("snapshotname and description arguments are " +
                                              "mandatory for recording session feature");
                            return;
                        }
                        VirtualMachineFlagInfo flagInfo = new VirtualMachineFlagInfo();
                        flagInfo.recordReplayEnabled = true;
                        flagInfo.recordReplayEnabledSpecified = true;
                        VirtualMachineConfigSpec configSpec = new VirtualMachineConfigSpec();
                        configSpec.flags = flagInfo;
                        _service.ReconfigVM_TaskAsync(vmmor, configSpec);
                        _service.StartRecording_TaskAsync(vmmor, ecb.get_option("snapshotname"), ecb.get_option("description"));
                        _service.StopRecording_TaskAsync(vmmor);
                        Console.WriteLine("Session recorded successfully");
                    }
                    else
                    {
                        VirtualMachineSnapshotTree[] tree = (VirtualMachineSnapshotTree[])getObjectProperty(vmmor,
                                                             "snapshot.rootSnapshotList");
                        if (tree != null && tree.Length != 0)
                        {
                            ManagedObjectReference taskMor = _service.RemoveAllSnapshots_Task(vmmor, true, false);
                            object[] result = ecb.getServiceUtil().WaitForValues(taskMor, new string[] { "info.state", "info.result" },
                                              new string[] { "state" }, // info has a property - state for state of the task
                                              new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } }
                                              );
                            if (result[0].Equals(TaskInfoState.success))
                            {
                                Console.WriteLine("Removed all the snapshot successfully");
                            }
                        }
                        else
                        {
                            Console.WriteLine("No snapshot found for this virtual machine");
                        }
                    }
                }
                else
                {
                    VirtualMachineSnapshotTree[] tree = (VirtualMachineSnapshotTree[])getObjectProperty(vmmor,
                                                         "snapshot.rootSnapshotList");
                    if (tree != null && tree.Length != 0)
                    {
                        ManagedObjectReference taskMor = _service.RemoveAllSnapshots_Task(vmmor, true, false);
                        object[] result = ecb.getServiceUtil().WaitForValues(taskMor, new string[] { "info.state", "info.result" },
                                          new string[] { "state" }, // info has a property - state for state of the task
                                          new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } }
                                         );
                        if (result[0].Equals(TaskInfoState.success))
                        {
                            Console.WriteLine("Removed all the snapshot successfully");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No snapshot found for this virtual machine");
                    }
                }
            }
            catch (Exception e)
            {
                ecb.log.LogLine("RecordSession : Failed Connect");
                throw e;
            }
            finally
            {
                ecb.log.LogLine("Ended RecordSession");
                ecb.log.Close();
            }
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


            ObjectContent[] ocs = ecb._svcUtil.retrievePropertiesEx(_sic.propertyCollector, new PropertyFilterSpec[] { pfSpec });

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
    }
}
