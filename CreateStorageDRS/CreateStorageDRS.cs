using System;
using AppUtil;
using Vim25Api;

namespace CreateStorageDRS
{

    ///<summary>
    ///This sample demonstrates how to create Storage DRSgroup
    ///</summary>
    ///<param name="dcname">Required: Datacenter name</param>
    ///<param name="sdrsname">Required: Name for the new storage pod</param>
    ///<param name="behavior">Optional: Storage DRS behavior, true if automated. It is manual by default.</param>
    ///<param name="iolatencythreshold">Optional: IO Latency threshold</param>
    ///<param name="spacethreshold">Optional :Space threshold</param>
    ///<param name="utilizationdiff">Optional: Storage DRS considers making storage migration
    /// recommendations if the difference in space utilization between the source and 
    /// destination datastores is higher than the specified threshold.</param>
    ///<param name="ioloadimbalancethreshold">Optional : IO Load Imbalance threshold</param>
    ///<param name="loadbalinterval">Optional :Interval that storage DRS run to load balance</param>
    ///<param name="datastore">Optional :Name of datastore</param>

    ///Create Storage DRS
    ///--url [webserviceurl]
    ///--username [username] --password [password] 
    ///--dcname [dcname] --sdrsname [sdrsname] --behavior [behavior] --iolatencythreshold [iolatencythreshold]
    /// --spacethreshold[spacethreshold] --utilizationdiff[utilizationdiff] 
    /// --ioloadimbalancethreshold[ioloadimbalancethreshold] 
    /// --loadbalinterval [loadbalinterval] --datastore[datastore]
    ///</remarks>
    public class CreateStorageDRS
    {
        private static AppUtil.AppUtil cb = null;
        static string ioLatencythreshold = null;
        static string spaceUtilizationThreshold = null;
        static string ioLoadImbalanceThreshold = null;
        static string minSpaceUtilizationDifference = null;

        /// <summary>
        /// Method to create Storage DRSgroup
        /// </summary>
        private void CreateSDRS()
        {
            string dcName = cb.get_option("dcname");
            string drsname = cb.get_option("sdrsname");
            string behavior = cb.get_option("behavior");
            string loadBalanceInterval = cb.get_option("loadbalinterval");
            string dsname = cb.get_option("datastore");
            ManagedObjectReference storagePod = new ManagedObjectReference();
            ManagedObjectReference storageResourceManager =
                  cb._connection._sic.storageResourceManager;
            ManagedObjectReference dcmor = cb._svcUtil.getEntityByName("Datacenter",dcName);
            if (dcmor != null)
            {
                ManagedObjectReference datastoreFolder = cb.getServiceUtil().GetMoRefProp
                    (dcmor, "datastoreFolder");
                storagePod = cb.getConnection()._service.CreateStoragePod(datastoreFolder, drsname);

                Console.WriteLine("Success: Creating storagePod.");
                StorageDrsConfigSpec sdrsConfigSpec = GetStorageDrsConfigSpec(behavior, loadBalanceInterval);
                ManagedObjectReference taskmor =
                cb.getConnection()._service.ConfigureStorageDrsForPod_Task(storageResourceManager,
                     storagePod, sdrsConfigSpec, true);
                if (taskmor != null)
                {
                    String status = cb.getServiceUtil().WaitForTask(
                          taskmor);
                    if (status.Equals("sucess"))
                    {
                        Console.WriteLine("Sucessfully configured storage pod"
                              + drsname);
                    }
                    else
                    {
                        Console.WriteLine("Failure: Configuring storagePod");
                        throw new Exception(status);
                    }
                }
                if (dsname != null)
                {
                    ManagedObjectReference dsMoref = cb._svcUtil.getEntityByName("Datastore", dsname);
                    if (dsMoref != null)
                    {
                        ManagedObjectReference[] dslist = new ManagedObjectReference[] { dsMoref };
                        ManagedObjectReference task = cb.getConnection()._service.
                            MoveIntoFolder_Task(storagePod, dslist);
                        if (task != null)
                        {
                            String status = cb.getServiceUtil().WaitForTask(
                             taskmor);
                            if (status.Equals("sucess"))
                            {
                                Console.WriteLine("\nSuccess: Adding datastore to storagePod.");
                            }
                            else
                            {
                                Console.WriteLine("Failure: Adding datastore to storagePod.");
                                throw new Exception(status);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Datastore" + dsname + "not found");
                    }
                }
            }
            else
            {
                throw new Exception("datacenter" + dcName + "not found");
            }
        }

        /// <summary>
        /// Create Object of StorageDrsConfigSpec
        /// </summary>
        /// <param name="behavior">string</param>
        /// <param name="loadBalanceInterval">string</param>
        /// <returns></returns>
        private StorageDrsConfigSpec GetStorageDrsConfigSpec(string behavior, string loadBalanceInterval)
        {

            StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
            StorageDrsPodConfigSpec podConfigSpec = new StorageDrsPodConfigSpec();
            if (behavior.Equals("true"))
            {
                podConfigSpec.defaultVmBehavior = "automated";
            }
            else
            {
                podConfigSpec.defaultVmBehavior = "manual";
            }
            podConfigSpec.defaultIntraVmAffinity = true;
            podConfigSpec.defaultIntraVmAffinitySpecified = true;
            podConfigSpec.enabled = true;
            podConfigSpec.enabledSpecified = true;
            StorageDrsIoLoadBalanceConfig sdrsIoLoadBalanceConfig =
            new StorageDrsIoLoadBalanceConfig();
            if (ioLatencythreshold != null)
            {
                sdrsIoLoadBalanceConfig.ioLatencyThreshold = int.Parse(ioLatencythreshold);
                sdrsIoLoadBalanceConfig.ioLatencyThresholdSpecified = true;
            }
            if (ioLoadImbalanceThreshold != null)
            {
                sdrsIoLoadBalanceConfig.ioLoadImbalanceThreshold = int.Parse(ioLoadImbalanceThreshold);
                sdrsIoLoadBalanceConfig.ioLoadImbalanceThresholdSpecified = true;
            }
            podConfigSpec.ioLoadBalanceConfig = sdrsIoLoadBalanceConfig;
            podConfigSpec.ioLoadBalanceEnabled = true;
            podConfigSpec.ioLoadBalanceEnabledSpecified = true;
            if (loadBalanceInterval != null)
            {
                podConfigSpec.loadBalanceInterval = int.Parse(loadBalanceInterval);
                podConfigSpec.loadBalanceIntervalSpecified = true;
            }
            StorageDrsSpaceLoadBalanceConfig sdrsSpaceLoadBalanceConfig =
                             new StorageDrsSpaceLoadBalanceConfig();
            if (spaceUtilizationThreshold != null)
            {
                sdrsSpaceLoadBalanceConfig.spaceUtilizationThreshold = int.Parse(spaceUtilizationThreshold);
                sdrsSpaceLoadBalanceConfig.spaceUtilizationThresholdSpecified = true;
            }
            if (minSpaceUtilizationDifference != null)
            {
                sdrsSpaceLoadBalanceConfig.minSpaceUtilizationDifference = int.Parse(minSpaceUtilizationDifference);
                sdrsSpaceLoadBalanceConfig.minSpaceUtilizationDifferenceSpecified = true;
            }
            podConfigSpec.spaceLoadBalanceConfig = sdrsSpaceLoadBalanceConfig;
            sdrsConfigSpec.podConfigSpec = podConfigSpec;
            return sdrsConfigSpec;

        }

        private Boolean customValidation()
        {
            Boolean flag = true;
            if (cb.option_is_set("behavior"))
            {
                String state = cb.get_option("behavior");
                if (!state.Equals("true")
                       && !state.Equals("false"))
                {
                    Console.WriteLine("Must specify 'true'or 'false' as the enablenioc"
                    + "value option\n");
                    flag = false;
                }
            }
            if (cb.option_is_set("iolatencythreshold"))
            {
                ioLatencythreshold = cb.get_option("iolatencythreshold");
                if (int.Parse(ioLatencythreshold) < 5 || int.Parse(ioLatencythreshold) > 50)
                {
                    Console.WriteLine("Expected valid --iolatencythreshold argument. Range is 5-50 ms.");
                    flag = false;
                }
            }
            if (cb.option_is_set("spacethreshold"))
            {
                spaceUtilizationThreshold = cb.get_option("spacethreshold");
                if (int.Parse(spaceUtilizationThreshold) < 50 || int.Parse(spaceUtilizationThreshold) > 100)
                {
                    Console.WriteLine("Expected valid --spacethreshold argument. Range is 50-100%.");
                    flag = false;
                }
            }
            if (cb.option_is_set("utilizationdiff"))
            {
                minSpaceUtilizationDifference = cb.get_option("utilizationdiff");
                if (int.Parse(minSpaceUtilizationDifference) < 1 || int.Parse(minSpaceUtilizationDifference) > 50)
                {
                    Console.WriteLine("Expected valid --utilizationdiff argument. Range is 1-50%.");
                    flag = false;
                }
            }
            if (cb.option_is_set("ioloadimbalancethreshold"))
            {
                ioLoadImbalanceThreshold = cb.get_option("ioloadimbalancethreshold");
                if (int.Parse(ioLoadImbalanceThreshold) < 1 || int.Parse(ioLoadImbalanceThreshold) > 100)
                {
                    Console.WriteLine("Expected valid --ioloadimbalancethreshold argument. Range is 1-100.");
                    flag = false;
                }
            }
            return flag;
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[9];
            useroptions[0] = new OptionSpec("dcname", "String", 1
                                           , "Datacenter name"
                                           , null);
            useroptions[1] = new OptionSpec("sdrsname", "String", 1,
                                            "Name for the new storage pod",
                                              null);
            useroptions[2] = new OptionSpec("behavior", "String", 0,
                                            "Storage DRS behavior, true if automated. It is manual by default.",
                                             null);
            useroptions[3] = new OptionSpec("iolatencythreshold", "String", 0,
                                            "Storage DRS makes storage migration"
                                            + "recommendations if I/O latency on one (or more)"
                                            + "of the datastores is higher than the specified"
                                            + "threshold. Range is 5-50 ms, default is 15ms",
                                              null);
            useroptions[4] = new OptionSpec("spacethreshold", "String", 0,
                                            "Storage DRS makes storage migration"
                                            + "recommendations if space utilization on one"
                                            + "(or more) of the datastores is higher than the"
                                            + " specified threshold. Range 50-100%, default is 80%,",
                                            null);
            useroptions[5] = new OptionSpec("utilizationdiff", "String", 0,
                                            "Storage DRS considers making storage migration"
                                           + "recommendations if the difference in space"
                                           + "utilization between the source and  destination"
                                           + "datastores is higher than the specified threshold."
                                           + "Range 1-50%, default is 5%",
                                              null);
            useroptions[6] = new OptionSpec("ioloadimbalancethreshold", "String", 0,
                                            " Storage DRS makes storage migration"
                                            + "recommendations if I/O load imbalance"
                                            + "level is higher than the specified threshold."
                                            + "Range is 1-100, default is 5",
                                             null);
            useroptions[7] = new OptionSpec("loadbalinterval", "String", 0,
                                           " Specify the interval that storage DRS runs to"
                                           + "load balance among datastores within a storage"
                                           + "pod. it is 480 by default.",
                                             null);
            useroptions[8] = new OptionSpec("datastore", "String", 0,
                                            "Name of the datastore to be added in StoragePod.",
                                            null);
            return useroptions;
        }

        public static void Main(string[] args)
        {
            CreateStorageDRS app = new CreateStorageDRS();
            cb = AppUtil.AppUtil.initialize("CreateStorageDRS", CreateStorageDRS.constructOptions(), args);
            Boolean valid = app.customValidation();
            if (valid)
            {
                try
                {
                    cb.connect();
                    app.CreateSDRS();
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
