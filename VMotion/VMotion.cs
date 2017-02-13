using System;
using AppUtil;
using Vim25Api;

namespace VMotion
{
    ///<summary>
    ///This sample is used to validate if VMotion is feasible between two hosts or not,
    ///It is also used to perform migrate/relocate task depending on the data given
    ///</summary>
    ///<param name="vmname">Required: Name of the virtual machine</param>
    ///<param name="targethost">Required: Name of the target host</param>
    ///<param name="sourcehost">Required: Name of the host containg the virtual machine </param>
    ///<param name="targetpool">Required: Name of the target resource pool</param>
    ///<param name="targetdatastore">Required: Name of the target datastore</param>
    ///<param name="priority">Optional: The priority of the migration task: defaultPriority,
    /// highPriority, lowPriority</param>
    ///<param name="state">Optional </param>
    ///<remarks>
    ///Relocate or migrate a VM
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --targetpool [tpool]
    ///--sourcehost [shost] --targethost [thost] --vmname [myVM] --targetdatastore [tDS]
    ///Validate the vmotion capability
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --targetpool [tpool]
    ///--sourcehost [shost] --targethost [thost] --vmname [myVM] --targetdatastore [tDS]
    ///--validate
    ///</remarks>

    public class VMotion
    {
        private static AppUtil.AppUtil cb = null;
        private static Vim25Api.ManagedObjectReference provisionChkr = null;

        private void getVersion(String[] args, VMotion vmotionObj)
        {
            String vmname = cb.get_option("vmname");
            String sourcehost = cb.get_option("sourcehost");
            String targethost = cb.get_option("targethost");
            String targetpool = cb.get_option("targetpool");
            String dataname = cb.get_option("targetdatastore");

            if (cb.option_is_set("validate"))
            {
                provisionChkr = cb.getConnection().ServiceContent.vmProvisioningChecker;
                Console.WriteLine("Investing the VMotion capability of VM in a Host");
                checkVMotionCompatibility(vmname, sourcehost, targethost, targetpool, dataname);
            }
            else
            {
                migrate_or_relocate_VM(vmname, sourcehost, targethost, targetpool, dataname);
            }
        }

        private void checkVMotionCompatibility(String vmname, String sourcehost, String targethost,
            String targetpool, String dataname)
        {
            ManagedObjectReference hostMOR = getMOR(sourcehost, "HostSystem", null);
            ManagedObjectReference vmMOR = getMOR(vmname, "VirtualMachine", hostMOR);
            ManagedObjectReference targethostMOR = getMOR(targethost, "HostSystem", null);
            ManagedObjectReference poolMOR = getMOR(targetpool, "ResourcePool", null);
            ManagedObjectReference[] dsTarget
                   = (ManagedObjectReference[])cb.getServiceUtil().GetDynamicProperty(targethostMOR, "datastore");
            ManagedObjectReference dsMOR = browseDSMOR(dsTarget, dataname);
            if (dsMOR == null)
            {
                Console.WriteLine("Datastore " + dataname + " not found");
            }
            if (vmMOR == null || hostMOR == null || targethostMOR == null || dsMOR == null || poolMOR == null)
            {
                return;
            }
            Boolean query = queryVMotionCompatibility(vmMOR, hostMOR, targethostMOR);
            Boolean migrate = checkMigrate(vmMOR, targethostMOR, poolMOR);
            Boolean relocation = checkRelocation(vmMOR, targethostMOR, poolMOR, dsMOR);

            if ((query) && (migrate) && (relocation))
            {
                Console.WriteLine("VMotion is feasible on VM " + vmname + " from host " + sourcehost + " to " + targethost);
            }
            else
            {
                Console.WriteLine("VMotion is not feasible on VM " + vmname + " from host " + sourcehost + " to " + targethost);
            }
        }

        public String monitorTask(ManagedObjectReference taskmor)
        {
            Object[] result = cb.getServiceUtil().WaitForValues(
                              taskmor, new String[] { "info.state", "info.error" },
                              new String[] { "state" },
                              new Object[][] { new Object[] { TaskInfoState.success, TaskInfoState.error } });
            if (result[0].Equals(TaskInfoState.success))
            {
                return "sucess";
            }
            else
            {
                TaskInfo tinfo = (TaskInfo)cb.getServiceUtil().GetDynamicProperty(taskmor, "info");
                LocalizedMethodFault fault = tinfo.error;
                String error = "Error Occured";
                if (fault != null)
                {
                    error = fault.localizedMessage;
                }
                return error;
            }
        }

        private Boolean checkRelocation(ManagedObjectReference vmMOR, ManagedObjectReference hostMOR,
            ManagedObjectReference poolMOR, ManagedObjectReference dsMOR)
        {
            Boolean relocate = false;

            try
            {
                VirtualMachineRelocateSpec relSpec = new VirtualMachineRelocateSpec();
                relSpec.datastore = (dsMOR);
                relSpec.host = (hostMOR);
                relSpec.pool = (poolMOR);
                ManagedObjectReference taskMOR =
                 cb.getConnection().Service.CheckRelocate_Task(provisionChkr, vmMOR, relSpec, null);
                String res = monitorTask(taskMOR);
                if (res.Equals("sucess"))
                {
                    relocate = true;
                }
                else
                {
                    relocate = false;
                }
            }
            catch (Exception)
            {
                relocate = false;
            }
            return relocate;
        }

        private Boolean checkMigrate(ManagedObjectReference vmMOR,
           ManagedObjectReference hostMOR, ManagedObjectReference poolMOR)
        {
            Boolean migrate = false;

            try
            {
                ManagedObjectReference taskMOR
                 = cb.getConnection().Service.CheckMigrate_Task(provisionChkr,
                 vmMOR, hostMOR, poolMOR, VirtualMachinePowerState.poweredOff, false, null);
                String res = monitorTask(taskMOR);
                if (res.Equals("sucess"))
                {
                    migrate = true;
                }
                else
                {
                    migrate = false;
                }
            }
            catch (Exception)
            {
                migrate = false;
            }
            return migrate;
        }

        private Boolean queryVMotionCompatibility(ManagedObjectReference vmMOR,
             ManagedObjectReference hostMOR, ManagedObjectReference targethostMOR)
        {
            Boolean result = false;

            try
            {
                ManagedObjectReference[] vmMORs = new ManagedObjectReference[] { vmMOR };
                ManagedObjectReference[] hostMORs = new ManagedObjectReference[] { hostMOR, targethostMOR };
                ManagedObjectReference taskMOR
                   = cb.getConnection().Service.QueryVMotionCompatibilityEx_Task(provisionChkr, vmMORs, hostMORs);
                String res = monitorTask(taskMOR);
                if (res.Equals("sucess"))
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }

        private ManagedObjectReference getMOR(String name, String type, ManagedObjectReference root)
        {
            ManagedObjectReference nameMOR
            = (ManagedObjectReference)cb.getServiceUtil().GetDecendentMoRef(root, type, name);
            if (nameMOR == null)
            {
                Console.WriteLine("Error:: " + name + " not found");
                return null;
            }
            else
            {
                return nameMOR;
            }
        }

        public void migrate_or_relocate_VM(String vmname, String sourceHost, String targetHost,
            String targetPool,
            String targetDS)
        {
            // first we need to check if the VM should be migrated of relocated
            // If target host and source host both contains
            //the datastore, virtual machine needs to be migrated
            // If only target host contains the datastore, machine needs to be relocated
            String operationName = check_operation_type(targetHost, sourceHost, targetDS);

            if (operationName.Equals("migrate"))
            {
                migrateVM(vmname, targetPool, targetHost, sourceHost);
            }
            else if (operationName.Equals("relocate"))
            {
                relocateVM(vmname, targetPool, targetHost, targetDS, sourceHost);
            }
        }

        public void migrateVM(String vmname, String pool, String tHost, String srcHost)
        {
            String state;
            VirtualMachinePowerState st = VirtualMachinePowerState.poweredOff;
            VirtualMachineMovePriority pri = VirtualMachineMovePriority.defaultPriority;
            if (cb.option_is_set("state"))
            {
                state = cb.get_option("state");
                if (cb.get_option("state").Equals("suspended"))
                {
                    st = VirtualMachinePowerState.suspended;
                }
                else if (cb.get_option("state").Equals("poweredOn"))
                {
                    st = VirtualMachinePowerState.poweredOn;
                }
                else if (cb.get_option("state").Equals("poweredOff"))
                {
                    st = VirtualMachinePowerState.poweredOff;
                }
            }
            pri = getPriority();

            try
            {
                ManagedObjectReference srcMOR = getMOR(srcHost, "HostSystem", null);
                ManagedObjectReference vmMOR = getMOR(vmname, "VirtualMachine", srcMOR);
                ManagedObjectReference poolMOR = getMOR(pool, "ResourcePool", null);
                ManagedObjectReference hMOR = getMOR(tHost, "HostSystem", null);
                if (vmMOR == null || srcMOR == null || poolMOR == null || hMOR == null)
                {
                    return;
                }

                Console.WriteLine("Migrating the Virtual Machine " + vmname);
                ManagedObjectReference taskMOR
                   = cb._connection._service.MigrateVM_Task(vmMOR, poolMOR, hMOR, pri, st, true);
                String res = cb.getServiceUtil().WaitForTask(taskMOR);
                if (res.Equals("sucess"))
                {
                    Console.WriteLine("Migration of Virtual Machine " + vmname + " done successfully to " + tHost);
                }
                else
                {
                    Console.WriteLine("Error::  Migration failed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void relocateVM(String vmname, String pool, String tHost, String tDS, String srcHost)
        {
            VirtualMachineMovePriority pri = getPriority();
            try
            {
                ManagedObjectReference srcMOR = getMOR(srcHost, "HostSystem", null);
                ManagedObjectReference vmMOR = getMOR(vmname, "VirtualMachine", srcMOR);
                ManagedObjectReference poolMOR = getMOR(pool, "ResourcePool", null);
                ManagedObjectReference hMOR = getMOR(tHost, "HostSystem", null);
                ManagedObjectReference[] dsTarget
                = (ManagedObjectReference[])cb.getServiceUtil().GetDynamicProperty(hMOR, "datastore");
                ManagedObjectReference dsMOR = browseDSMOR(dsTarget, tDS);
                if (dsMOR == null)
                {
                    Console.WriteLine("Datastore " + tDS + " not found");
                }
                if (vmMOR == null || srcMOR == null || poolMOR == null || hMOR == null || dsMOR == null)
                {
                    return;
                }
                VirtualMachineRelocateSpec relSpec = new VirtualMachineRelocateSpec();
                relSpec.datastore = (dsMOR);
                relSpec.host = (hMOR);
                relSpec.pool = (poolMOR);
                Console.WriteLine("Relocating the Virtual Machine " + vmname);
                ManagedObjectReference taskMOR =
               cb._connection._service.RelocateVM_Task(vmMOR, relSpec, pri, true);
                String res = cb.getServiceUtil().WaitForTask(taskMOR);
                if (res.Equals("sucess"))
                {
                    Console.WriteLine("Relocation done successfully of " + vmname + " to host " + tHost);
                }
                else
                {
                    Console.WriteLine("Error::  Relocation failed");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public VirtualMachineMovePriority getPriority()
        {
            VirtualMachineMovePriority prior = VirtualMachineMovePriority.defaultPriority;
            if (!cb.option_is_set("priority"))
            {
                prior = VirtualMachineMovePriority.defaultPriority;
            }
            else
            {
                if (cb.get_option("priority").Equals("lowPriority"))
                {
                    prior = VirtualMachineMovePriority.lowPriority;
                }
                else if (cb.get_option("priority").Equals("highPriority"))
                {
                    prior = VirtualMachineMovePriority.highPriority;
                }
                else if (cb.get_option("priority").Equals("defaultPriority"))
                {
                    prior = VirtualMachineMovePriority.defaultPriority;
                }
            }
            return prior;
        }

        private String check_operation_type(String targetHost, String sourceHost, String targetDS)
        {
            String operation = "";
            try
            {
                ManagedObjectReference targetHostMOR = getMOR(targetHost, "HostSystem", null);
                ManagedObjectReference sourceHostMOR = getMOR(sourceHost, "HostSystem", null);
                if ((targetHostMOR == null) || (sourceHostMOR == null))
                {
                    return "";
                }
                ManagedObjectReference[] dsTarget
                  = (ManagedObjectReference[])cb.getServiceUtil().GetDynamicProperty(targetHostMOR, "datastore");
                ManagedObjectReference tarHostDS = browseDSMOR(dsTarget, targetDS);
                ManagedObjectReference[] dsSource
                  = (ManagedObjectReference[])cb.getServiceUtil().GetDynamicProperty(sourceHostMOR, "datastore");
                ManagedObjectReference srcHostDS = browseDSMOR(dsSource, targetDS);
                if ((tarHostDS != null) && (srcHostDS != null))
                {
                    // we have a shared datastore we can do migration
                    operation = "migrate";
                }
                else
                {
                    operation = "relocate";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return operation;
        }

        private ManagedObjectReference browseDSMOR(ManagedObjectReference[] dsMOR, String dsName)
        {
            ManagedObjectReference dataMOR = null;
            try
            {
                if (dsMOR != null && dsMOR.Length > 0)
                {
                    for (int i = 0; i < dsMOR.Length; i++)
                    {
                        String dsname = (String)cb.getServiceUtil().GetDynamicProperty(dsMOR[i], "summary.name");
                        if (dsname.Equals(dsName))
                        {
                            dataMOR = dsMOR[i];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dataMOR;
        }

        private Boolean customValidation()
        {
            Boolean flag = true;
            if (cb.option_is_set("state"))
            {
                String state = cb.get_option("state");
                if (!state.Equals("poweredOn")
                       && !state.Equals("poweredOff")
                            && !state.Equals("suspended"))
                {
                    Console.WriteLine("Must specify 'poweredOn', 'poweredOff' or" +
                                 " 'suspended' for 'state' option\n");
                    flag = false;
                }
            }
            if (cb.option_is_set("priority"))
            {
                String prior = cb.get_option("priority");
                if (!prior.Equals("defaultPriority")
                       && !prior.Equals("highPriority")
                            && !prior.Equals("lowPriority"))
                {
                    Console.WriteLine("Must specify 'defaultPriority', 'highPriority " +
                                 " 'or 'lowPriority' for 'priority' option\n");
                    flag = false;
                }
            }
            return flag;
        }

        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[8];
            useroptions[0] = new OptionSpec("vmname", "String", 1,
                                            "Name of the virtual machine"
                                            , null);
            useroptions[1] = new OptionSpec("targethost", "String", 1,
                                            "Target host on which VM is to be migrated",
                                            null);
            useroptions[2] = new OptionSpec("targetpool", "String", 1,
                                            "Name of the target resource pool",
                                            null);
            useroptions[3] = new OptionSpec("priority", "String", 0,
                                            "The priority of the migration task: defaultPriority, highPriority, lowPriority",
                                            null);
            useroptions[4] = new OptionSpec("validate", "String", 0,
                                            "Check whether the vmotion feature is legal for 4.0 servers",
                                            null);
            useroptions[5] = new OptionSpec("sourcehost", "String", 1,
                                            "Name of the host containg the virtual machine.",
                                            null);
            useroptions[6] = new OptionSpec("targetdatastore", "String", 1,
                                            "Name of the target datastore",
                                            null);
            useroptions[7] = new OptionSpec("state", "String", 0,
                                            "State of the VM poweredOn,poweredOff, suspended",
                                            null);
            return useroptions;
        }

        public static void Main(String[] args)
        {
            VMotion app = new VMotion();
            cb = AppUtil.AppUtil.initialize("VMotion", VMotion.constructOptions(), args);
            Boolean valid = app.customValidation();
            if (valid)
            {
                try
                {
                    cb.connect();
                    app.getVersion(args, app);
                    cb.disConnect();
                }
                catch (Exception e)
                {
                }
                Console.WriteLine("Press any key to exit: ");
                Console.Read();
            }
        }
    }
}