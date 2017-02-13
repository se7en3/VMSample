using System;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Net;

namespace DisplayNewPropertiesVM
{
    class DisplayNewPropertiesVMV25 {
        static Vim25Api.VimService _service;
        static ServiceContent _sic;
        private static AppUtil.AppUtil ecb = null;

        /// <summary>
        /// This method is used to add application specific user options 
        /// </summary>
        ///<returns> Array of OptionSpec containing the details of application 
        /// specific user options 
        ///</returns>
        ///
        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[1];
            useroptions[0] = new OptionSpec("vmname", "String", 1
                                            , "Name of the Virtual Machine"
                                            , null);
            return useroptions;
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        public static void Main(String[] args)
        {
            try
            {
                DisplayNewPropertiesVMV25 obj = new DisplayNewPropertiesVMV25();
                ecb = AppUtil.AppUtil.initialize("DisplayNewPropertiesVMV25"
                                                , DisplayNewPropertiesVMV25.constructOptions()
                                                , args);
                ecb.connect();
                obj.displayNewProperties();
                ecb.disConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failure : " + e.Message);
            }
            Console.WriteLine("Press enter to exit.");
            Console.Read();
        }

        public void displayNewProperties() {
            _service = ecb.getConnection().Service;
            _sic = ecb.getConnection().ServiceContent;
            String vmName = ecb.get_option("vmname");
            ManagedObjectReference vmmor = ecb.getServiceUtil().GetDecendentMoRef(null,
                                             "VirtualMachine", vmName);

            if (vmmor != null)
            {
                ArrayList supportedVersions = VersionUtil.getSupportedVersions(ecb.get_option("url"));

                Object[] vmProps = getProperties(vmmor, new String[] { "name" });
                String serverName = (String)vmProps[0];
                Console.WriteLine("Virtual Machine Name " + serverName);

                vmProps = getProperties(vmmor, new String[] { "config.uuid" });
                String uuid = (String)vmProps[0];
                Console.WriteLine("Config UUID " + uuid);

                vmProps = getProperties(vmmor, new String[] { "config.guestId" });
                String guestId = (String)vmProps[0];
                Console.WriteLine("Guest Id " + guestId);

                if (VersionUtil.isApiVersionSupported(supportedVersions, "2.5"))
                {
                    vmProps = getProperties(vmmor, new String[] { "name" });
                    
                    Boolean bootOptionsSupported = (Boolean)getObjectProperty(vmmor, "capability.bootOptionsSupported");
                    Console.WriteLine("Boot Options Supported " + bootOptionsSupported);

                    Boolean diskSharesSupported = (Boolean)getObjectProperty(vmmor, "capability.diskSharesSupported");
                    Console.WriteLine("Disk Shares Supported " + diskSharesSupported);

                    Boolean flag = VersionUtil.isApiVersionSupported(supportedVersions, "4.0");
                    Console.WriteLine("Is API Supported  " + flag);
                    if (flag)
                    {
                        Console.WriteLine("\nProperties added in vSphere API 4.0\n");
                        Boolean changeTrackingSupported = (Boolean)getObjectProperty(vmmor, "capability.changeTrackingSupported");
                        Console.WriteLine("Change Tracking Supported " + changeTrackingSupported);

                        Boolean recordReplaySupported = (Boolean)getObjectProperty(vmmor, "capability.recordReplaySupported");
                        Console.WriteLine("Record Replay Supported " + recordReplaySupported);

                        VirtualMachineFaultToleranceState faultToleranceState
                           = (VirtualMachineFaultToleranceState)getObjectProperty(vmmor, "runtime.faultToleranceState");
                        Console.WriteLine("Fault Tolerance State " + faultToleranceState);
                    }
                }
            }
            else
            {
                Console.WriteLine("Virtal Machine Not Found");
            }
        }

        public static Object getObjectProperty(ManagedObjectReference moRef, String propertyName) {
            return getProperties(moRef, new String[] { propertyName })[0];
        }

        ///<summary>
        ///Retrieves the specified set of properties for the given managed object
        ///reference into an array of result objects .
        ///</summary>
        ///<param name="moRef"></param>
        ///<param name="properties"></param>
        ///<returns>The function returns array of object.containg dynamic properties of host
        /// (returned in the same oder as the property list)
        ///</returns>
        ///
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
            ocs = _service.RetrieveProperties(_sic.propertyCollector, new PropertyFilterSpec[]{ pfSpec });

            // Return value, one object for each property specified
            Object[] ret = new Object[properties.Length];

            if (ocs != null) {
                for (int i = 0; i < ocs.Length; ++i) {
                    ObjectContent oc = ocs[i];
                    DynamicProperty[] dps = oc.propSet;
                    if (dps != null) {
                        for (int j = 0; j < dps.Length; ++j) {
                            DynamicProperty dp = dps[j];
                            // find property path index
                            for (int p = 0; p < ret.Length; ++p) {
                                if (properties[p].Equals(dp.name)) {
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
