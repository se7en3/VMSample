using System;
using System.Collections.Generic;
using Vim25Api;
using AppUtil;

namespace DVSCreate
{
    ///<summary>
    ///This sample is used to create DVS or add the port group
    ///</summary>
    ///<param name="itemType">Required: Type of the Opeartion to be performed</param>
    ///<param name="dcname">Required: Datacenter name</param>
    ///<param name="dvsname">Required: Name of dvs switch to add </param>
    ///<param name="dvsdesc">Optional: Description of dvs switch to add</param>
    ///<param name="dvsversion">Optional: Distributed Virtual Switch version</param>
    /// either 4.0, 4.1.0, 5.0.0 or 5.1.0
    ///<param name="numports">Optional :Number of ports in the portgroup</param>
    ///<param name="portgroupname">Optional: Name of the port group </param>

    ///Create DVS 
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--dvsname [dvsname] --dcname [dcname] --dvsdesc [dvsdesc] --dvsversion [dvsversion]
    /// Add a port group
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--dvsname [dvsname]
    /// --numports[numports] --portgroupname[portgroupname]
    ///</remarks>
    public class DVSCreate
    {
        private static AppUtil.AppUtil cb = null;

        private String GetItemType()
        {
            return cb.get_option("itemType");
        }

        /// <summary>
        /// This method is used to create DVS or add port group according to user choice.
        /// </summary>
        private void DoCreate()
        {
            string dcname = cb.get_option("dcname");
            string dvsname = cb.get_option("dvsname");
            string dvsdesc = cb.get_option("dvsdesc");
            string dvsversion = cb.get_option("dvsversion");
            int numPorts = 0;
            if (cb.get_option("numports") != null)
            {
                numPorts = int.Parse(cb.get_option("numports"));
            }
            string portGroupName = cb.get_option("portgroupname");
            try
            {
                if (GetItemType().Equals("createdvs"))
                {
                    ManagedObjectReference dcmor = cb._svcUtil.getEntityByName("Datacenter", dcname);
                    if (dcmor != null)
                    {
                        ManagedObjectReference networkmor = cb.getServiceUtil().GetMoRefProp(dcmor, "networkFolder");
                        DVSCreateSpec dvscreatespec = new DVSCreateSpec();
                        DistributedVirtualSwitchProductSpec dvsProdSpec = GetDVSProductSpec(dvsversion);
                        dvscreatespec.productInfo = dvsProdSpec;
                        DistributedVirtualSwitchHostProductSpec[] dvsHostProdSpec =
                            cb.getConnection()._service.QueryDvsCompatibleHostSpec(cb.getConnection().
                            _sic.dvSwitchManager, dvsProdSpec);

                        DVSCapability dvsCapability = new DVSCapability();
                        dvsCapability.compatibleHostComponentProductInfo = dvsHostProdSpec;
                        dvscreatespec.capability = dvsCapability;
                        DVSConfigSpec configSpec = GetConfigSpec(dvsname, dvsdesc);
                        dvscreatespec.configSpec = configSpec;

                        ManagedObjectReference taskmor =
                               cb.getConnection()._service.CreateDVS_Task(networkmor, dvscreatespec);
                        if (taskmor != null)
                        {
                            String status = cb.getServiceUtil().WaitForTask(
                                  taskmor);
                            if (status.Equals("sucess"))
                            {
                                Console.WriteLine("Sucessfully created::"
                                      + dvsname);
                            }
                            else
                            {
                                Console.WriteLine("dvs switch" + dvsname
                                   + " not created::");
                                throw new Exception(status);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Datacenter" + dcname + "not found");
                    }
                }
                else if (GetItemType().Equals("addportgroup"))
                {
                    ManagedObjectReference dvsMor = cb._svcUtil.getEntityByName("VmwareDistributedVirtualSwitch", dvsname);
                    if (dvsMor != null)
                    {
                        DVPortgroupConfigSpec portGroupConfigSpec = new DVPortgroupConfigSpec();
                        portGroupConfigSpec.name = portGroupName;
                        portGroupConfigSpec.numPorts = numPorts;
                        portGroupConfigSpec.type = "earlyBinding";
                        List<DVPortgroupConfigSpec> lst = new List<DVPortgroupConfigSpec>();
                        lst.Add(portGroupConfigSpec);
                        ManagedObjectReference taskmor =
                        cb.getConnection()._service.AddDVPortgroup_Task(dvsMor, lst.ToArray());
                        if (taskmor != null)
                        {
                            String status = cb.getServiceUtil().WaitForTask(
                                  taskmor);
                            if (status.Equals("sucess"))
                            {
                                Console.WriteLine("Sucessfully added port group :"
                                      + portGroupName);
                            }
                            else
                            {
                                Console.WriteLine("port group" + portGroupName
                                   + " not added:");
                                throw new Exception(status);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("DvsSwitch  " + dvsname + " not found");
                    }
                }
                else
                {
                    Console.WriteLine("Unknown Type. Allowed types are:");
                    Console.WriteLine(" createdvs");
                    Console.WriteLine(" addportgroup");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        private static DVSConfigSpec GetConfigSpec(string dvsName, string dvsDesc)
        {
            DVSConfigSpec dvsConfigSpec = new DVSConfigSpec();
            dvsConfigSpec.name = dvsName;
            if (dvsDesc != null)
            {
                dvsConfigSpec.description = dvsDesc;
            }
            DVSPolicy dvsPolicy = new DVSPolicy();
            dvsPolicy.autoPreInstallAllowed = true;
            dvsPolicy.autoUpgradeAllowed = true;
            dvsPolicy.partialUpgradeAllowed = true;
            return dvsConfigSpec;
        }

        private static DistributedVirtualSwitchProductSpec GetDVSProductSpec(string version)
        {
            DistributedVirtualSwitchProductSpec[] dvsProdSpec = cb.getConnection()._service.
                QueryAvailableDvsSpec(cb.getConnection()._sic.dvSwitchManager);
            DistributedVirtualSwitchProductSpec dvsSpec = null;
            if (version != null)
            {
                for (int i = 0; i < dvsProdSpec.Length; i++)
                {
                    if (version.Equals(dvsProdSpec[i].version))
                    {
                        dvsSpec = dvsProdSpec[i];
                    }

                }
                if (dvsSpec == null)
                {
                    Console.WriteLine("Dvs version" + version + "not supported");
                }

            }
            else
            {
                dvsSpec = dvsProdSpec[dvsProdSpec.Length - 1];

            }
            return dvsSpec;
        }

        private Boolean customValidation()
        {
            Boolean flag = true;
            if (cb.option_is_set("dvsversion"))
            {
                String dvsVersion = cb.get_option("dvsversion");
                if (!dvsVersion.Equals("4.0")
                       && !dvsVersion.Equals("4.1.0")
                       && !dvsVersion.Equals("5.0.0")
                            && !dvsVersion.Equals("5.1.0"))
                {
                    Console.WriteLine("Must specify dvs version as 4.0 or 4.1.0\n"
                         + "5.0.0 or 5.1.0 ");
                    flag = false;
                }
            }
            return flag;
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[7];
            useroptions[0] = new OptionSpec("itemType", "String", 1
                                           , "createdvs|addportgroup"
                                           , null);
            useroptions[1] = new OptionSpec("dcname", "String", 1,
                                            "Datacenter name",
                                            null);
            useroptions[2] = new OptionSpec("dvsname", "String", 1,
                                            "Name of dvs switch to add ",
                                              null);
            useroptions[3] = new OptionSpec("dvsdesc", "String", 0,
                                            "Description of dvs switch to add",
                                            null);
            useroptions[4] = new OptionSpec("dvsversion", "String", 0,
                                            "Distributed Virtual Switch version"
                                            + "either 4.0, 4.1.0, 5.0.0 or 5.1.0",
                                              null);
            useroptions[5] = new OptionSpec("numports", "String", 0,
                                            "Required for addportgroup:Number of ports in the portgroup",
                                            null);
            useroptions[6] = new OptionSpec("portgroupname", "String", 0,
                                            "Required for addportgroup: Name of the port group",
                                              null);
            return useroptions;
        }

        public static void Main(String[] args)
        {
            DVSCreate app = new DVSCreate();
            cb = AppUtil.AppUtil.initialize("DVSCreate", DVSCreate.constructOptions(), args);
            Boolean valid = app.customValidation();
            if (valid)
            {
                try
                {
                    cb.connect();
                    app.DoCreate();
                    cb.disConnect();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.WriteLine("Please enter to exit: ");
            Console.Read();

        }
    }
}
