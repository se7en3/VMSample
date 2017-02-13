using System;
using AppUtil;
using Vim25Api;

namespace NIOCForDVS
{

    ///<summary>
    ///This sample is used to perform operations like enable NIOC, add resource pool,
    ///modify resource pool, reconfigure Distributed Switch Port group, list resouce pool
    ///</summary>
    ///<param name="itemType">Required: Type of the Opeartion to be performed (enablenioc|addnrp|listnrp|modifynrp|reconfigurepg)</param>
    ///<param name="dvsname">Required: Name of the Distributed Virtual switch</param>
    ///<param name="enablenioc">Optional: True/False depending on whether to enable the NIOC or not </param>
    ///<param name="nrpsharelevel">Optional: The allocation level. It can be custom, high, low, normal</param>
    ///<param name="nrphostlimit">Optional: The maximum allowed usage for network clients</param>
    ///<param name="nrpprioritytag">Optional: Its value should be between 0-7</param>
    ///<param name="nrpname">Optional: The user defined name for the resource pool </param>
    ///<param name="nrpdesc">Optional: The user defined description for the resource pool</param>
    ///<param name="dvpgname">Optional: The name of the portgroup</param>
    ///<param name="nrpshares">Optional: The shares for nrp level.
    /// This value is only set if level is set to custom. Valid values are from 1-100.</param>
    ///<remarks>
    ///Enable Network I/O Control:
    /// --url [URLString] --username [User] --password [Password]
    ///--itemType enablenioc --dvsname [dvsname] --enablenioc [enablenioc]

    /// Add NetworkResourcePool:
    ///--url [URLString] --username [User] --password [Password]
    ///--itemType addnrp --dvsname [dvsname] --nrpsharelevel [nrpsharelevel] --nrphostlimit [nrphostlimit]
    ///--nrpprioritytag [nrpprioritytag] --nrpname [nrpname] --nrpdesc [nrpdesc] --nrpshares [nrpshares]

    ///List NetworkResourcePool:
    ///--url [URLString] --username [User] --password [Password]
    ///--itemType listnrp --dvsname [dvsname]

    /// Modify NetworkResourcePool:
    /// --url [URLString] --username [User] --password [Password]
    ///--itemType modifynrp --dvsname [dvsname] --nrpsharelevel [nrpsharelevel] --nrphostlimit [nrphostlimit]
    ///--nrpprioritytag [nrpprioritytag] --nrpname [nrpname] --nrpshares [nrpshares]

    ///Associate the NetworkResourcePool with DVSPortGroup on the DVS switch:
    ///--username [User] --password [Password]
    ///--itemType reconfigurepg --dvsname [dvsname] --nrpname [nrpname] --dvpgname [dvpgname]
    ///</remarks>
    public class NIOCForDVS
    {
        private static AppUtil.AppUtil cb = null;

        private String GetItemType()
        {
            return cb.get_option("itemType");
        }

        /// <summary>
        /// Method to perform operations like enable NIOC, add resource pool,
        /// modify resource pool, reconfigure Distributed Switch Port group, list resouce pool
        /// </summary>
        private void DoNIOC()
        {
            string dvsname = cb.get_option("dvsname");
            string nrpName = cb.get_option("nrpname");
            string nrpDesc = cb.get_option("nrpdesc");
            string dvPortGroupName = cb.get_option("dvpgname");
            int noOfShares = -1;
            if (cb.get_option("nrpshares") != null)
            {
                noOfShares = int.Parse(cb.get_option("nrpshares"));
            }
            string hostLimit = cb.get_option("nrphostlimit");
            string level = cb.get_option("nrpsharelevel");
            string prioritytag = cb.get_option("nrpprioritytag");

            if (GetItemType().Equals("listnrp"))
            {
                ListNetworkResourcePool(dvsname);
            }
            else if (GetItemType().Equals("enablenioc"))
            {
                string enableNioc = cb.get_option("enablenioc");
                if (enableNioc != null)
                {
                    EnableNIOC(dvsname, Boolean.Parse(enableNioc));
                }
                else
                {
                    Console.WriteLine("enablenioc cannot be null for enable NIOC method");
                }
            }
            else if (GetItemType().Equals("addnrp"))
            {
                AddNetworkResourcePool(dvsname, nrpName, nrpDesc, noOfShares, level, prioritytag, hostLimit);
            }

            else if (GetItemType().Equals("modifynrp"))
            {
                ModifyNetworkResourcePool(dvsname, nrpName, noOfShares, level, prioritytag, hostLimit);

            }
            else if (GetItemType().Equals("reconfigurepg"))
            {
                ReconfigureDVSPG(dvsname, nrpName, dvPortGroupName);
            }
            else
            {
                Console.WriteLine("Unknown Type. Allowed types are:");
                Console.WriteLine("enablenioc");
                Console.WriteLine("addnrp");
                Console.WriteLine("listnrp");
                Console.WriteLine("modifynrp");
                Console.WriteLine("reconfigurepg");
            }

        }

        /// <summary>
        /// Method to list all resource Pools under the specified Distributed Virtual Switch
        /// </summary>
        /// <param name="dvsname">string</param>
        private void ListNetworkResourcePool(string dvsname)
        {
            DVSNetworkResourcePool[] nrpList = null;
            ManagedObjectReference dvsMor = cb._svcUtil.getEntityByName("VmwareDistributedVirtualSwitch", dvsname);
            string[] type = new string[] { "networkResourcePool" };
            if (dvsMor != null)
            {
                ObjectContent[] objContent = cb.getServiceUtil().GetObjectProperties(null, dvsMor, type);
                if (objContent != null)
                {
                    nrpList = (DVSNetworkResourcePool[])objContent[0].propSet[0].val;
                }

                if (nrpList != null)
                {
                    Console.WriteLine("Existing DVSNetwork Resource Pool");
                    foreach (DVSNetworkResourcePool dvsNrp in nrpList)
                    {
                        String nrp = "System defined DVSNetworkResourcePool";
                        if (dvsNrp.key.StartsWith("NRP"))
                        {
                            nrp = "User defined DVSNetworkResourcePool";
                        }
                        Console.WriteLine(dvsNrp.name + ":networkResourcePool[\"" + dvsNrp.key +
                            "\"]: " + nrp);
                    }
                }
                else
                {
                    Console.WriteLine("No NetworkResourcePool found for DVS Switch" + dvsname);
                }
            }
            else
            {
                throw new Exception("DVS Switch " + dvsname + " not found");
            }
        }

        /// <summary>
        /// Method to enable network I/O control
        /// </summary>
        /// <param name="dvSwitchName">string</param>
        /// <param name="enableNIOC">Boolean</param>
        private void EnableNIOC(string dvSwitchName, Boolean enableNIOC)
        {

            ManagedObjectReference dvsMor = cb._svcUtil.getEntityByName("VmwareDistributedVirtualSwitch", dvSwitchName);
            if (dvsMor != null)
            {
                cb.getConnection().Service.EnableNetworkResourceManagement(dvsMor, enableNIOC);
                Console.WriteLine("Set network I/O control");
            }
            else
            {
                throw new Exception("DVS Switch " + dvSwitchName + " Not Found");
            }
        }

        /// <summary>
        /// Method to add a resource Pool
        /// </summary>
        /// <param name="dvSwitchName">string</param>
        /// <param name="nrpName">string</param>
        /// <param name="nrpDesc">string</param>
        /// <param name="noOfShares">int</param>
        /// <param name="level">string</param>
        /// <param name="prioritytag">string</param>
        /// <param name="hostLimit">string</param>
        private void AddNetworkResourcePool(string dvSwitchName, string nrpName,
                                           string nrpDesc, int noOfShares, string level,
                                           string prioritytag, string hostLimit)
        {
            ManagedObjectReference dvsMor = cb._svcUtil.getEntityByName("VmwareDistributedVirtualSwitch", dvSwitchName);
            if (dvsMor != null)
            {
                DVSNetworkResourcePoolConfigSpec networkRPconfigSpec = new DVSNetworkResourcePoolConfigSpec();
                DVSNetworkResourcePoolAllocationInfo allocationInfo =
                                   new DVSNetworkResourcePoolAllocationInfo();
                if (level != null)
                {
                    SharesInfo shares = new SharesInfo();
                    if (noOfShares != -1)
                    {
                        shares.level = SharesLevel.custom;
                        shares.shares = noOfShares;
                    }
                    else
                    {
                        if (level.Equals(SharesLevel.high))
                        { shares.level = SharesLevel.high; }
                        else if (level.Equals(SharesLevel.low))
                        { shares.level = SharesLevel.low; }
                        else if (level.Equals(SharesLevel.normal))
                        { shares.level = SharesLevel.normal; }
                    }
                    allocationInfo.shares = shares;
                }

                if (hostLimit != null)
                {
                    allocationInfo.limit = long.Parse(hostLimit);
                }
                if (prioritytag != null)
                {
                    allocationInfo.priorityTag = int.Parse(prioritytag);
                }
                networkRPconfigSpec.configVersion = "0";
                if (nrpDesc != null)
                {
                    networkRPconfigSpec.description = nrpDesc;
                }
                else
                {
                    networkRPconfigSpec.description = nrpName;
                }
                networkRPconfigSpec.name = nrpName;
                networkRPconfigSpec.allocationInfo = allocationInfo;
                networkRPconfigSpec.key = "";
                DVSNetworkResourcePoolConfigSpec[] networkRPconfigSpecs = new DVSNetworkResourcePoolConfigSpec[] { networkRPconfigSpec };
                cb.getConnection().Service.AddNetworkResourcePool(dvsMor, networkRPconfigSpecs);
                Console.WriteLine("Susccessfully added Network Resource Pool :" + nrpName);
            }
            else
            {
                throw new Exception("DVS Switch " + dvSwitchName + " Not Found");
            }
        }

        /// <summary>
        /// Method to modify the NetworkResourcePool
        /// </summary>
        /// <param name="dvSwitchName"></param>
        /// <param name="nrpName"></param>
        /// <param name="noOfShares"></param>
        /// <param name="level"></param>
        /// <param name="prioritytag"></param>
        /// <param name="hostLimit"></param>
        private void ModifyNetworkResourcePool(string dvSwitchName, string nrpName,
                                             int noOfShares, string level, string prioritytag,
                                              string hostLimit)
        {
            ManagedObjectReference dvsMor = cb._svcUtil.getEntityByName("VmwareDistributedVirtualSwitch", dvSwitchName);
            DVSNetworkResourcePool[] nrpList = null;
            if (dvsMor != null)
            {
                string[] type = new string[] { "networkResourcePool" };
                ObjectContent[] objContent = cb.getServiceUtil().GetObjectProperties(null, dvsMor, type);
                if (objContent != null)
                {
                    nrpList = (DVSNetworkResourcePool[])objContent[0].propSet[0].val;
                }
                if (nrpList != null)
                {
                    String configVersion = null;
                    String nrpKey = null;
                    foreach (DVSNetworkResourcePool dvsNrp in nrpList)
                    {
                        if (dvsNrp.name.Equals(nrpName))
                        {
                            nrpKey = dvsNrp.key;
                            configVersion = dvsNrp.configVersion;
                            break;
                        }
                    }
                    if (nrpKey == null)
                    {
                        throw new Exception("NetworkResource Pool " + nrpName
                              + " Not Found");
                    }

                    DVSNetworkResourcePoolConfigSpec networkRPconfigSpec =
                        new DVSNetworkResourcePoolConfigSpec();
                    DVSNetworkResourcePoolAllocationInfo allocationInfo =
                                    new DVSNetworkResourcePoolAllocationInfo();
                    if (level != null)
                    {
                        SharesInfo shares = new SharesInfo();
                        if (noOfShares != -1)
                        {
                            shares.level = SharesLevel.custom;
                            shares.shares = noOfShares;
                        }
                        else
                        {
                            if (level.Equals(SharesLevel.high))
                            { shares.level = SharesLevel.high; }
                            else if (level.Equals(SharesLevel.low))
                            { shares.level = SharesLevel.low; }
                            else if (level.Equals(SharesLevel.normal))
                            { shares.level = SharesLevel.normal; }
                        }
                        allocationInfo.shares = shares;
                    }

                    if (hostLimit != null)
                    {
                        allocationInfo.limit = long.Parse(hostLimit);
                    }
                    if (prioritytag != null)
                    {
                        allocationInfo.priorityTag = int.Parse(prioritytag);
                    }
                    networkRPconfigSpec.configVersion = configVersion;
                    networkRPconfigSpec.key = nrpKey;
                    networkRPconfigSpec.allocationInfo = allocationInfo;
                    DVSNetworkResourcePoolConfigSpec[] configSpec = new
                        DVSNetworkResourcePoolConfigSpec[] { networkRPconfigSpec };
                    cb._connection._service.UpdateNetworkResourcePool(dvsMor, configSpec);
                    Console.WriteLine("Susccessfully modified Network Resource Pool :" + nrpName);
                }
                else
                {
                    Console.WriteLine("No NetworkResourcePool found for DVS Switch "
                          + dvSwitchName);
                    return;
                }
            }

            else
            {
                throw new Exception("DVS Switch " + dvSwitchName + " Not Found");
            }
        }

        /// <summary>
        /// Associates a given NetworkResourcePool to the given DVSPortGroup on the given DVS switch
        /// </summary>
        /// <param name="dvSwitchName"></param>
        /// <param name="nrpName"></param>
        /// <param name="dvPortGroupName"></param>
        private void ReconfigureDVSPG(string dvSwitchName, string nrpName,
                                             string dvPortGroupName)
        {
            DVSNetworkResourcePool[] nrpList = null;
            ManagedObjectReference dvsMor = cb._svcUtil.getEntityByName("VmwareDistributedVirtualSwitch", dvSwitchName);
            if (dvsMor != null)
            {
                string[] type = new string[] { "networkResourcePool" };
                ObjectContent[] objContent = cb.getServiceUtil().GetObjectProperties(null, dvsMor, type);
                if (objContent != null)
                {
                    nrpList = (DVSNetworkResourcePool[])objContent[0].propSet[0].val;
                }

                string nrpKey = null;
                if (nrpList != null)
                {
                    foreach (DVSNetworkResourcePool dvsNrp in nrpList)
                    {
                        if (dvsNrp.name.Equals(nrpName))
                        {
                            nrpKey = dvsNrp.key;
                            break;
                        }
                    }

                    if (nrpKey == null)
                    {
                        throw new Exception("NetworkResource Pool " + nrpName + " Not Found");
                    }
                }
                else
                {
                    throw new Exception("No NetworkResourcePool found for DVS Switch " + dvSwitchName);
                }
                ManagedObjectReference dvspgMor = cb._svcUtil.getEntityByName("DistributedVirtualPortgroup",
                                                    dvPortGroupName);

                if (dvspgMor != null)
                {

                    DVPortgroupConfigInfo configInfo = (DVPortgroupConfigInfo)cb.getServiceUtil().GetDynamicProperty(dvspgMor, "config");
                    DVPortgroupConfigSpec dvPortGConfigSpec = new DVPortgroupConfigSpec();
                    dvPortGConfigSpec.configVersion = configInfo.configVersion;
                    DVPortSetting portSetting = new DVPortSetting();
                    StringPolicy networkResourcePoolKey = new StringPolicy();
                    networkResourcePoolKey.value = nrpKey;
                    networkResourcePoolKey.inherited = false;
                    portSetting.networkResourcePoolKey = networkResourcePoolKey;
                    dvPortGConfigSpec.name = dvPortGroupName;
                    dvPortGConfigSpec.defaultPortConfig = portSetting;
                    ManagedObjectReference taskmor = cb._connection._service.ReconfigureDVPortgroup_Task(dvspgMor, dvPortGConfigSpec);
                    if (taskmor != null)
                    {
                        String status = cb.getServiceUtil().WaitForTask(
                              taskmor);
                        if (status.Equals("sucess"))
                        {
                            Console.WriteLine("Sucessfully Associated Port Group::" + dvPortGroupName + " with the Network Resource Pool::"
                                + nrpName + " on the DVS::" + dvSwitchName);
                        }
                        else
                        {
                            Console.WriteLine("Failure while reconfiguring the port group");
                            throw new Exception(status);
                        }
                    }
                }
                else
                {
                    throw new Exception("DVS port group " + dvPortGroupName + " Not Found");
                }
            }
            else
            {
                throw new Exception("DVS Switch " + dvSwitchName + " Not Found");
            }
        }

        private Boolean customValidation()
        {
            Boolean flag = true;
            if (cb.option_is_set("enablenioc"))
            {
                String state = cb.get_option("enablenioc");
                if (!state.Equals("true")
                       && !state.Equals("false"))
                {
                    Console.WriteLine("Must specify 'true'or 'false' as the enablenioc"
                    + "value option\n");
                    flag = false;
                }
            }
            if (cb.option_is_set("nrpsharelevel"))
            {
                String level = cb.get_option("nrpsharelevel");
                if (!level.Equals("custom")
                       && !level.Equals("high")
                        && !level.Equals("normal")
                            && !level.Equals("low"))
                {
                    Console.WriteLine("Must specify 'custom', 'high', " +
                                 " 'normal' or 'low' for sharelevel option\n");
                    flag = false;
                }
            }
            return flag;
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[10];
            useroptions[0] = new OptionSpec("itemType", "String", 1
                                           , "enablenioc|addnrp|listnrp|modifynrp|reconfigurepg"
                                           , null);
            useroptions[1] = new OptionSpec("dvsname", "String", 1,
                                            "Distributed Virtual Switch name ",
                                              null);
            useroptions[2] = new OptionSpec("enablenioc", "String", 0,
                                            "If true, enables I/O control. If false, disables network I/O control",
                                            null);
            useroptions[3] = new OptionSpec("nrpsharelevel", "String", 0,
                                            "The allocation level.",
                                              null);
            useroptions[4] = new OptionSpec("nrphostlimit", "String", 0,
                                            "Numeric value: The maximum allowed usage for network clients"
                                            + "belonging to this resource pool per host.",
                                            null);
            useroptions[5] = new OptionSpec("nrpprioritytag", "String", 0,
                                            "Numeric value: The 802.1p tag to be used for this resource pool."
                                            + "Its value should be between 0-7",
                                              null);
            useroptions[6] = new OptionSpec("nrpname", "String", 0,
                                            " The user defined name for the resource pool.",
                                             null);
            useroptions[7] = new OptionSpec("nrpdesc", "String", 0,
                                           " The user defined description for the resource pool",
                                             null);
            useroptions[8] = new OptionSpec("dvpgname", "String", 0,
                                          " The name of the portgroup.",
                                            null);
            useroptions[9] = new OptionSpec("nrpshares", "String", 0,
                                        " The shares for nrp level",
                                          null);
            return useroptions;
        }

        public static void Main(string[] args)
        {
            NIOCForDVS app = new NIOCForDVS();
            cb = AppUtil.AppUtil.initialize("NIOCForDVS", NIOCForDVS.constructOptions(), args);
            Boolean valid = app.customValidation();
            if (valid)
            {
                try
                {
                    cb.connect();
                    app.DoNIOC();
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
