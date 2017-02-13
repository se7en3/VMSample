using System;
using System.Web.Services;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Security.Principal;
using System.Net;

namespace DisplayProperties
{
    ///<summary>
    ///This sample is used to Display the specified properties of the Host
    ///</summary>
    ///<param name="hostname">Required: Name of the host</param>
    ///<remarks>
    ///Display the properties of the host
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --hostname [hostname]
    ///</remarks>
    class DisplayProperties
    {

        private static AppUtil.AppUtil cb = null;
        static VimService _service;
        static ServiceContent _sic;

        private void displayProperties(String[] args)
        {

            _service = cb.getConnection().Service;
            _sic = cb.getConnection().ServiceContent;

            String hostName = cb.get_option("hostname");
            ManagedObjectReference hmor = _service.FindByDnsName(_sic.searchIndex, null, hostName, false);
            if (hmor != null)
            {
                ArrayList supportedVersions = VersionUtil.getSupportedVersions(cb.get_option("url"));
                if (VersionUtil.isApiVersionSupported(supportedVersions, "2.5"))
                {

                    _service = cb.getConnection().Service;
                    _sic = cb.getConnection().ServiceContent;
                    Object[] vmProps = getProperties(hmor, new String[] { "name" });
                    String serverName = (String)vmProps[0];
                    Console.WriteLine("Server Name " + serverName);

                    HostRuntimeInfo runtimeInfo = (HostRuntimeInfo)getObjectProperty(hmor, "runtime");
                    DateTime dt = runtimeInfo.bootTime;
                    Console.WriteLine("Boot Time " + dt.ToString());
                    String powerState = runtimeInfo.powerState.ToString();
                    Console.WriteLine("Power State " + powerState);
                    HostConfigInfo configInfo
                       = (HostConfigInfo)getObjectProperty(hmor, "config");
                    String timeZone = configInfo.dateTimeInfo.timeZone.key;
                    Console.WriteLine("Time Zone " + timeZone);

                    Boolean flag = VersionUtil.isApiVersionSupported(supportedVersions, "4.0");
                    if (flag)
                    {
                        Console.WriteLine("\nProperties added in 4.0 VI API\n");
                        Object objFlag = getObjectProperty(hmor, "capability.ipmiSupported");
                        if (objFlag != null)
                        {
                            Boolean infoFlag = (Boolean)objFlag;
                            Console.WriteLine("IPMI Supported  " + infoFlag);
                        }
                        else
                        {
                            Console.WriteLine("\nIPMI Flag not set");
                        }
                        objFlag = getObjectProperty(hmor, "capability.tpmSupported");
                        if (objFlag != null)
                        {
                            Boolean infoFlag = (Boolean)objFlag;
                            Console.WriteLine("TPM Supported  " + infoFlag);
                        }
                        else
                        {
                            Console.WriteLine("\nTPM Flag not set");
                        }
                    }
                }

                else
                {
                    Object[] vmProps = getProperties(hmor, new String[] { "name" });
                    String serverName = (String)vmProps[0];
                    Console.WriteLine("Server Name " + serverName);
                }
            }
            else
            {
                Console.WriteLine("Host Not Found");
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


            ObjectContent[] ocs = new ObjectContent[20];
            ocs = _service.RetrieveProperties(_sic.propertyCollector, new PropertyFilterSpec[] { pfSpec });

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


        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[1];
            useroptions[0] = new OptionSpec("hostname", "String", 1
                                            , "Name of the host"
                                            , null);
            return useroptions;
        }

        public static void Main(String[] args)
        {
            DisplayProperties obj = new DisplayProperties();
            cb = AppUtil.AppUtil.initialize("DisplayProperties"
                                    , DisplayProperties.constructOptions()
                                   , args);
            cb.connect();
            obj.displayProperties(args);
            cb.disConnect();

        }
    }


}
