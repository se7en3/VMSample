using System;
using AppUtil;
using Vim25Api;
using System.Collections.Generic;
namespace SDRSRules
{
    ///<summary>
    ///This sample demonstrates how to Add/List/Modify/Delete the rules for an existing SDRS cluster.
    ///</summary>
    ///<param name="itemType">Required: Type of operation to be performed</param>
    ///<param name="podname">Required: StoragePod name</param>
    ///<param name="rulename">Optional: Rule name. </param>
    ///<param name="vmlist">Optional: Comma separated, list of VM names. It is required while
    ///  adding VmAntiAffinity Rule.</param>
    ///<param name="newrulename">Optional: New name for rule while modifying</param>
    ///<param name="enable">Optional: Flag to indicate whether or not the rule is enabled.</param>
    ///<param name="vmname">Optional: virtual machine name.</param>
    ///<remarks>
    ///Adding VmAntiAffinity rule
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--podname [podname] --rulename [rulename] --vmlist [vmlist] --enable[true enables this rule | any other value is false]
    ///Adding VmdkAntiAffinity rule
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--podname [podname] --rulename [rulename] --vmname[vmname] --enable [true enables this rule | any other value is false]
    ///Modifying VmAntiAffinity rule
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--podname [podname] --rulename [rulename] --vmlist [vmlist] --enable[true enables this rule | any other value is false]
    /// --newrulename [newrulename]
    ///Modifying VmdkAntiAffinity rule
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--podname [podname] --rulename [rulename] --vmname[vmname] --enable [true enables this rule | any other value is false]
    /// --newrulename [newrulename]
    /// delete VmAntiAffinity rule
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--podname [podname] --rulename [rulename] 
    /// Delete VmdkAntiAffinity rule
    /// delete VmAntiAffinity rule
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///--podname [podname] --rulename [rulename] 
    /// List rules 
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --itemType [itemType]
    ///</remarks>
    public class SDRSRules
    {
        private static AppUtil.AppUtil cb = null;

        private String GetItemType()
        {
            return cb.get_option("itemType");
        }

        /// <summary>
        /// Method to Add/List/Modify/Delete the rules for an existing SDRS cluster
        /// </summary>
        private void CheckRules()
        {
            string podName = cb.get_option("podname");
            string ruleName = cb.get_option("rulename");
            string enabled = cb.get_option("enable") ?? "false";
            string vmname = cb.get_option("vmname");
            string vmliststring = cb.get_option("vmlist");
            string newRuleName = cb.get_option("newrulename");
            String str = ",";
            string[] vmList = new string[5];
            char[] separator = str.ToCharArray();
            if (vmliststring != null)
            {
                vmList = vmliststring.Split(separator);
            }
            if (GetItemType().Equals("addVmAntiAffinity"))
            {
                AddVmAntiAffinityRule(podName, ruleName, enabled, vmList);

            }
            else if (GetItemType().Equals("addVmdkAntiAffinity"))
            {
                AddVmdkAntiAffinityRule(podName, ruleName, enabled,
                                           vmname);
            }
            else if (GetItemType().Equals("list"))
            {
                ListRules(podName);
            }
            else if (GetItemType().Equals("modifyVmAntiAffinity"))
            {
                ModifyVmAntiAffinityRule(podName, ruleName, newRuleName, enabled, vmList);
            }
            else if (GetItemType().Equals("modifyVmdkAntiAffinity"))
            {
                ModifyVmdkAntiAffinityRule(podName, ruleName, newRuleName, enabled);

            }
            else if (GetItemType().Equals("deleteVmAntiAffinity"))
            {
                DeleteVmAntiAffinityRule(podName, ruleName);
            }
            else if (GetItemType().Equals("deleteVmdkAntiAffinity"))
            {
                DeleteVmdkAntiAffinityRule(podName, ruleName);
            }
            else
            {
                Console.WriteLine("Unknown Type. Allowed types are:");
                Console.WriteLine("addVmAntiAffinity");
                Console.WriteLine("addVmdkAntiAffinity");
                Console.WriteLine("list");
                Console.WriteLine("modifyVmAntiAffinity");
                Console.WriteLine("modifyVmdkAntiAffinity");
                Console.WriteLine("deleteVmAntiAffinity");
                Console.WriteLine("deleteVmdkAntiAffinity");
            }

        }

        /// <summary>
        /// Method to modify VM Anti Affinity Rule
        /// </summary>
        /// <param name="podName">string</param>
        /// <param name="ruleName">string</param>
        /// <param name="newRuleName">string</param>
        /// <param name="enabled">string</param>
        /// <param name="vmList">string array</param>
        private void ModifyVmAntiAffinityRule(string podName, string ruleName,
                                              string newRuleName, string enabled,
                                              string[] vmList)
        {
            ManagedObjectReference srmRef = cb._connection._sic.storageResourceManager;
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                PodStorageDrsEntry podStorageDrsEntry = (PodStorageDrsEntry)cb._svcUtil.GetDynamicProperty(sdrsMor, "podStorageDrsEntry");
                ClusterRuleInfo[] clusterRuleInfo = podStorageDrsEntry.storageDrsConfig.podConfig.rule;
                ClusterRuleSpec ruleSpec = new ClusterRuleSpec();
                List<ManagedObjectReference> vmMorefList = new List<ManagedObjectReference>();
                ManagedObjectReference vmMoref = null;
                StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
                StorageDrsPodConfigSpec podConfigSpec = new StorageDrsPodConfigSpec();
                ClusterAntiAffinityRuleSpec vmAntiAffinityRuleSpec = null;
                foreach (ClusterRuleInfo vmRule in clusterRuleInfo)
                {
                    if (vmRule.name.Equals(ruleName))
                    {
                        vmAntiAffinityRuleSpec = (ClusterAntiAffinityRuleSpec)vmRule;
                    }
                }

                if (vmAntiAffinityRuleSpec != null)
                {
                    if (newRuleName != null)
                    {
                        vmAntiAffinityRuleSpec.name = newRuleName;
                    }

                    if (enabled.Equals("true"))
                    {
                        vmAntiAffinityRuleSpec.enabled = true;
                        vmAntiAffinityRuleSpec.enabledSpecified = true;
                    }
                    else
                    {
                        vmAntiAffinityRuleSpec.enabled = false;
                        vmAntiAffinityRuleSpec.enabledSpecified = false;
                    }

                    foreach (string vmname in vmList)
                    {
                        vmMoref = cb._svcUtil.getEntityByName("VirtualMachine", vmname);
                        if (vmMoref != null)
                        {
                            vmMorefList.Add(vmMoref);
                        }
                        else
                        {
                            string message = "Failure: " + vmname + "VM not found";
                            throw new Exception(message);
                        }
                    }
                    vmAntiAffinityRuleSpec.vm = vmMorefList.ToArray();

                    vmAntiAffinityRuleSpec.userCreated = true;
                    vmAntiAffinityRuleSpec.userCreatedSpecified = true;
                    ruleSpec.info = vmAntiAffinityRuleSpec;
                    ruleSpec.operation = ArrayUpdateOperation.edit;
                    podConfigSpec.rule = new ClusterRuleSpec[] { ruleSpec };
                    sdrsConfigSpec.podConfigSpec = podConfigSpec;
                    ManagedObjectReference taskmor =
                   cb._connection._service.ConfigureStorageDrsForPod_Task(srmRef,
                         sdrsMor, sdrsConfigSpec, true);

                    if (taskmor != null)
                    {
                        String status = cb.getServiceUtil().WaitForTask(
                              taskmor);
                        if (status.Equals("sucess"))
                        {
                            Console.WriteLine("Success: Modifying VmAntiAffinity Rule.");
                        }
                        else
                        {
                            Console.WriteLine("Failure: Modifying VmAntiAffinity Rule.");
                            throw new Exception(status);
                        }
                    }
                }
                else
                {
                    string msg = "\nFailure: Rule " + ruleName + " not found.";
                    throw new Exception(msg);
                }
            }
            else
            {
                throw new Exception("Storage Pod " + podName + "not found");
            }
        }

        /// <summary>
        /// Method to delete VM Anti AffinityRule
        /// </summary>
        /// <param name="podName">string</param>
        /// <param name="ruleName">string</param>
        private void DeleteVmAntiAffinityRule(string podName, string ruleName)
        {
            ManagedObjectReference srmRef = cb._connection._sic.storageResourceManager;
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                PodStorageDrsEntry podStorageDrsEntry = (PodStorageDrsEntry)cb._svcUtil.GetDynamicProperty(sdrsMor, "podStorageDrsEntry");
                ClusterRuleSpec ruleSpec = new ClusterRuleSpec();
                ClusterRuleInfo[] clusterRuleInfo = podStorageDrsEntry.storageDrsConfig.podConfig.rule;
                StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
                StorageDrsPodConfigSpec podConfigSpec = new StorageDrsPodConfigSpec();
                ClusterAntiAffinityRuleSpec vmAntiAffinityRuleSpec = null;
                if (clusterRuleInfo != null)
                {
                    foreach (ClusterRuleInfo vmRule in clusterRuleInfo)
                    {
                        if (vmRule.name.Equals(ruleName))
                        {
                            vmAntiAffinityRuleSpec = (ClusterAntiAffinityRuleSpec)vmRule;
                        }
                    }
                    if (vmAntiAffinityRuleSpec != null)
                    {
                        ruleSpec.operation = ArrayUpdateOperation.remove;
                        ruleSpec.info = vmAntiAffinityRuleSpec;
                        ruleSpec.removeKey = vmAntiAffinityRuleSpec.key;
                        podConfigSpec.rule = new ClusterRuleSpec[] { ruleSpec };
                        sdrsConfigSpec.podConfigSpec = podConfigSpec;
                        ManagedObjectReference taskmor =
                               cb._connection._service.ConfigureStorageDrsForPod_Task(srmRef,
                         sdrsMor, sdrsConfigSpec, true);
                        if (taskmor != null)
                        {
                            String status = cb.getServiceUtil().WaitForTask(taskmor);
                            if (status.Equals("sucess"))
                            {
                                Console.WriteLine("Success: Delete VmAntiAffinity Rule.");
                            }
                            else
                            {
                                Console.WriteLine("Failure: Delete VmAntiAffinity Rule.");
                                throw new Exception(status);
                            }
                        }
                    }
                    else
                    {
                        string msg = "\nFailure: Rule " + ruleName + " not found.";
                        throw new Exception(msg);
                    }
                }
            }
            else
            {
                throw new Exception("Storage Pod " + podName + "not found");
            }
        }

        /// <summary>
        /// Delete VMdk AntiAffinity Rule Spec
        /// </summary>
        /// <param name="podName">string</param>
        /// <param name="ruleName">string</param>
        private void DeleteVmdkAntiAffinityRule(string podName, string ruleName)
        {
            ManagedObjectReference srmRef = cb._connection._sic.storageResourceManager;
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                PodStorageDrsEntry podStorageDrsEntry = (PodStorageDrsEntry)cb._svcUtil.GetDynamicProperty(sdrsMor, "podStorageDrsEntry");
                StorageDrsVmConfigInfo drsVmConfigInfo = null;
                StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
                StorageDrsVmConfigSpec drsVmConfigSpec = new StorageDrsVmConfigSpec();
                StorageDrsVmConfigInfo[] sdrsVmConfig = podStorageDrsEntry.storageDrsConfig.vmConfig;

                foreach (StorageDrsVmConfigInfo vmConfig in sdrsVmConfig)
                {
                    if (vmConfig.intraVmAntiAffinity != null)
                    {
                        if (vmConfig.intraVmAntiAffinity.name.Equals(ruleName))
                        {
                            drsVmConfigInfo = vmConfig;
                        }
                    }
                }

                if (drsVmConfigInfo != null)
                {
                    drsVmConfigInfo.intraVmAntiAffinity = null;
                    drsVmConfigSpec.info = drsVmConfigInfo;
                    drsVmConfigSpec.operation = ArrayUpdateOperation.edit;
                    sdrsConfigSpec.vmConfigSpec = new StorageDrsVmConfigSpec[] { drsVmConfigSpec };
                }
                else
                {
                    string msg = "\nFailure: Rule " + ruleName + " not found.";
                    throw new Exception(msg);
                }

                ManagedObjectReference taskmor =
                            cb._connection._service.ConfigureStorageDrsForPod_Task(srmRef,
                                  sdrsMor, sdrsConfigSpec, true);
                if (taskmor != null)
                {
                    String status = cb.getServiceUtil().WaitForTask(
                          taskmor);
                    if (status.Equals("sucess"))
                    {
                        Console.WriteLine("Success: Delete VmdkAntiAffinity Rule.");
                    }
                    else
                    {
                        Console.WriteLine("Failure: Delete VmdkAntiAffinity Rule.");
                        throw new Exception(status);
                    }
                }
            }
            else
            {
                throw new Exception("Storage Pod " + podName + "not found");
            }
        }

        /// <summary>
        /// Method to Modify VmdkAntiAffinityRule
        /// </summary>
        /// <param name="podName">string</param>
        /// <param name="ruleName">string</param>
        /// <param name="newRuleName">string</param>
        /// <param name="enabled">string</param>
        private void ModifyVmdkAntiAffinityRule(string podName, string ruleName,
                                                string newRuleName, string enabled)
        {
            ManagedObjectReference srmRef = cb._connection._sic.storageResourceManager;
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                PodStorageDrsEntry podStorageDrsEntry = (PodStorageDrsEntry)cb._svcUtil.GetDynamicProperty(sdrsMor, "podStorageDrsEntry");
                StorageDrsVmConfigInfo drsVmConfigInfo = null;
                StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
                StorageDrsVmConfigSpec drsVmConfigSpec = new StorageDrsVmConfigSpec();
                StorageDrsVmConfigInfo[] sdrsVmConfig = podStorageDrsEntry.storageDrsConfig.vmConfig;
                foreach (StorageDrsVmConfigInfo vmConfig in sdrsVmConfig)
                {
                    if (vmConfig.intraVmAntiAffinity != null)
                    {
                        if (vmConfig.intraVmAntiAffinity.name.Equals(ruleName))
                        {
                            drsVmConfigInfo = vmConfig;
                        }
                    }
                }
                if (drsVmConfigInfo != null)
                {
                    if (newRuleName != null)
                    {
                        drsVmConfigInfo.intraVmAntiAffinity.name = newRuleName;
                    }
                    if (enabled != null)
                    {
                        if (enabled.Equals("true"))
                        {
                            drsVmConfigInfo.intraVmAntiAffinity.enabled = true;
                            drsVmConfigInfo.intraVmAntiAffinity.enabledSpecified = true;
                        }
                        else
                        {
                            drsVmConfigInfo.intraVmAntiAffinity.enabled = false;
                            drsVmConfigInfo.intraVmAntiAffinity.enabledSpecified = false;
                        }
                    }
                    drsVmConfigSpec.info = drsVmConfigInfo;
                    drsVmConfigSpec.operation = ArrayUpdateOperation.edit;
                    sdrsConfigSpec.vmConfigSpec = new StorageDrsVmConfigSpec[] { drsVmConfigSpec };
                }
                else
                {
                    string msg = "\nFailure: Rule " + ruleName + " not found.";
                    throw new Exception(msg);
                }
                ManagedObjectReference taskmor =
               cb._connection._service.ConfigureStorageDrsForPod_Task(srmRef,
                     sdrsMor, sdrsConfigSpec, true);
                if (taskmor != null)
                {
                    String status = cb.getServiceUtil().WaitForTask(
                          taskmor);
                    if (status.Equals("sucess"))
                    {
                        Console.WriteLine("Success: Modify VmdkAntiAffinity Rule.");
                    }
                    else
                    {
                        Console.WriteLine("Failure: Modify VmdkAntiAffinity Rule.");
                        throw new Exception(status);
                    }
                }
            }
            else
            {
                throw new Exception("Storage Pod " + podName + "not found");
            }
        }

        /// <summary>
        /// Method to add VmdkAntiAffinityRule
        /// </summary>
        /// <param name="podName">string</param>
        /// <param name="ruleName">string</param>
        /// <param name="enabled">string</param>
        /// <param name="vm">string</param>
        private void AddVmdkAntiAffinityRule(string podName, string ruleName, string enabled,
                                             string vm)
        {
            ManagedObjectReference srmRef = cb._connection._sic.storageResourceManager;
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                ManagedObjectReference vmMoref = null;
                StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
                StorageDrsVmConfigSpec drsVmConfigSpec = new StorageDrsVmConfigSpec();
                StorageDrsVmConfigInfo drsVmConfigInfo = new StorageDrsVmConfigInfo();
                VirtualDiskAntiAffinityRuleSpec vmdkAntiAffinityRuleSpec =
                      new VirtualDiskAntiAffinityRuleSpec();
                vmdkAntiAffinityRuleSpec.name = ruleName;
                if (enabled.Equals("true"))
                {
                    vmdkAntiAffinityRuleSpec.enabled = true;
                    vmdkAntiAffinityRuleSpec.enabledSpecified = true;
                }
                else
                {
                    vmdkAntiAffinityRuleSpec.enabled = false;
                    vmdkAntiAffinityRuleSpec.enabledSpecified = false;
                }
                vmMoref = cb._svcUtil.getEntityByName("VirtualMachine", vm);
                if (vmMoref != null)
                {
                    VirtualMachineConfigInfo vmConfigInfo = (VirtualMachineConfigInfo)cb._svcUtil.GetDynamicProperty(vmMoref, "config");
                    VirtualDevice[] vDevice = vmConfigInfo.hardware.device;
                    List<VirtualDevice> vDisk = new List<VirtualDevice>();
                    VirtualDevice[] virtualDisk = null;
                    List<int> diskIdList = new List<int>();
                    foreach (VirtualDevice device in vDevice)
                    {
                        if (device.GetType().Name.Equals("VirtualDisk"))
                        {
                            vDisk.Add(device);
                            diskIdList.Add(device.key);
                        }
                    }
                    virtualDisk = vDisk.ToArray();
                    vmdkAntiAffinityRuleSpec.diskId = diskIdList.ToArray();
                    if (virtualDisk.Length < 2)
                    {
                        throw new Exception(
                              "VM should have minimum of 2 virtual disks"
                                    + " while adding VMDK AntiAffinity Rule.");
                    }
                    Console.WriteLine("Adding below list of virtual disk to rule "
                 + ruleName + " :");
                    foreach (VirtualDevice device in virtualDisk)
                    {
                        Console.WriteLine("Virtual Disk : "
                             + device.deviceInfo.label + ", Key : "
                             + device.key);
                    }

                    vmdkAntiAffinityRuleSpec.userCreated = true;
                    vmdkAntiAffinityRuleSpec.userCreatedSpecified = true;
                    drsVmConfigInfo.intraVmAntiAffinity = vmdkAntiAffinityRuleSpec;
                    drsVmConfigInfo.intraVmAffinitySpecified = true;
                    drsVmConfigInfo.vm = vmMoref;
                }
                else
                {
                    string message = "Failure: " + vm + "VM not found";
                    throw new Exception(message);
                }
                drsVmConfigSpec.info = drsVmConfigInfo;
                drsVmConfigSpec.operation = ArrayUpdateOperation.add;
                sdrsConfigSpec.vmConfigSpec = new StorageDrsVmConfigSpec[] { drsVmConfigSpec };
                ManagedObjectReference taskmor =
               cb._connection._service.ConfigureStorageDrsForPod_Task(srmRef,
                     sdrsMor, sdrsConfigSpec, true);
                if (taskmor != null)
                {
                    String status = cb.getServiceUtil().WaitForTask(
                          taskmor);
                    if (status.Equals("sucess"))
                    {
                        Console.WriteLine("Success: Adding VmdkAntiAffinity Rule.");
                    }
                    else
                    {
                        Console.WriteLine("Failure: Adding VmdkAntiAffinity Rule.");
                        throw new Exception(status);
                    }
                }
            }
            else
            {
                throw new Exception("Storage Pod " + podName + "not found");
            }
        }

        /// <summary>
        /// Method to add VmAntiAffinityRule
        /// </summary>
        /// <param name="podName">string</param>
        /// <param name="ruleName">string</param>
        /// <param name="enabled">string</param>
        /// <param name="vm">string array</param>
        private void AddVmAntiAffinityRule(string podName,
                                           string ruleName, string enabled,
                                           string[] vm)
        {
            ManagedObjectReference srmRef = cb._connection._sic.storageResourceManager;
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                ManagedObjectReference vmMoref = null;
                StorageDrsConfigSpec sdrsConfigSpec = new StorageDrsConfigSpec();
                StorageDrsPodConfigSpec podConfigSpec = new StorageDrsPodConfigSpec();
                ClusterAntiAffinityRuleSpec vmAntiAffinityRuleSpec =
               new ClusterAntiAffinityRuleSpec();
                ClusterRuleSpec ruleSpec = new ClusterRuleSpec();
                vmAntiAffinityRuleSpec.name = ruleName;
                List<ManagedObjectReference> mor = new List<ManagedObjectReference>();
                if (enabled.Equals("true"))
                {
                    vmAntiAffinityRuleSpec.enabled = true;
                    vmAntiAffinityRuleSpec.enabledSpecified = true;
                }
                else
                {
                    vmAntiAffinityRuleSpec.enabled = false;
                    vmAntiAffinityRuleSpec.enabledSpecified = false;
                }
                foreach (string vmname in vm)
                {
                    vmMoref = cb._svcUtil.getEntityByName("VirtualMachine", vmname);
                    if (vmMoref != null)
                    {
                        mor.Add(vmMoref);
                    }
                    else
                    {
                        string message = "Failure: " + vmname + "VM not found";
                        throw new Exception(message);
                    }
                }
                vmAntiAffinityRuleSpec.vm = mor.ToArray();
                vmAntiAffinityRuleSpec.userCreated = true;
                vmAntiAffinityRuleSpec.userCreatedSpecified = true;
                ruleSpec.info = vmAntiAffinityRuleSpec;
                ruleSpec.operation = ArrayUpdateOperation.add;
                podConfigSpec.rule = new ClusterRuleSpec[] { ruleSpec };
                sdrsConfigSpec.podConfigSpec = podConfigSpec;
                ManagedObjectReference taskmor =
                       cb._connection._service.ConfigureStorageDrsForPod_Task(srmRef,
                       sdrsMor, sdrsConfigSpec, true);
                if (taskmor != null)
                {
                    String status = cb.getServiceUtil().WaitForTask(
                          taskmor);
                    if (status.Equals("sucess"))
                    {
                        Console.WriteLine("Success: Adding VmAntiAffinity Rule.");
                    }
                    else
                    {
                        Console.WriteLine("Failure: Adding VmAntiAffinity Rule.");
                        throw new Exception(status);
                    }
                }

            }
            else
            {
                throw new Exception("Storage Pod " + podName + "not found");
            }
        }

        /// <summary>
        /// Method to list all rules
        /// </summary>
        /// <param name="podName">string</param>
        private void ListRules(string podName)
        {
            ManagedObjectReference sdrsMor = cb._svcUtil.getEntityByName("StoragePod", podName);
            if (sdrsMor != null)
            {
                PodStorageDrsEntry podStorageDrsEntry = (PodStorageDrsEntry)cb._svcUtil.GetDynamicProperty(sdrsMor, "podStorageDrsEntry");
                Console.WriteLine("List of VM anti-affinity rules: ");
                ClusterRuleInfo[] clusterRuleInfo = podStorageDrsEntry.storageDrsConfig.podConfig.rule;
                if (clusterRuleInfo != null)
                {
                    foreach (ClusterRuleInfo vmRule in clusterRuleInfo)
                    {
                        Console.WriteLine(vmRule.name);
                    }
                }
                else
                {
                    Console.WriteLine("No rule set for VM anti-affinity");
                }
                Console.WriteLine("List of VMDK anti-affinity rules:");
                StorageDrsVmConfigInfo[] vmConfig = podStorageDrsEntry.storageDrsConfig.vmConfig;
                if (vmConfig != null)
                {
                    foreach (StorageDrsVmConfigInfo sdrsVmConfig in vmConfig)
                    {
                        if (sdrsVmConfig.intraVmAntiAffinity != null)
                        {
                            Console.WriteLine(sdrsVmConfig.intraVmAntiAffinity.name);
                        }

                    }
                }
                else
                {
                    Console.WriteLine("No rule set for VMDK anti-affinity");
                }

            }
            else
            {
                throw new Exception("Failure: StoragePod" + podName + "not found");
            }
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[7];
            useroptions[0] = new OptionSpec("itemType", "String", 1
                                           , "addVmAntiAffinity|addVmdkAntiAffinity|list|"
                                           + "modifyVmAntiAffinity|modifyVmdkAntiAffinity"
                                           + "deleteVmAntiAffinity|deleteVmdkAntiAffinity",
                                           null);
            useroptions[1] = new OptionSpec("podname", "String", 1,
                                            "StoragePod name",
                                              null);
            useroptions[2] = new OptionSpec("rulename", "String", 0,
                                            "Rule name",
                                            null);
            useroptions[3] = new OptionSpec("vmlist", "String", 0,
                                            "Comma separated, list of VM names. It is required while"
                                            + " adding VmAntiAffinity Rule.",
                                              null);
            useroptions[4] = new OptionSpec("newrulename", "String", 0,
                                            "New name for rule while modifying",
                                            null);
            useroptions[5] = new OptionSpec("enable", "String", 0,
                                            "Set to true to enable this rule. Any other value is false",
                                              "false");
            useroptions[6] = new OptionSpec("vmname", "String", 0,
                                            " virtual machine name.",
                                             null);
            return useroptions;
        }

        public static void Main(string[] args)
        {
            SDRSRules app = new SDRSRules();
            cb = AppUtil.AppUtil.initialize("SDRSRules", SDRSRules.constructOptions(), args);

            try
            {
                cb.connect();
                app.CheckRules();
                cb.disConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Please enter to exit: ");
            Console.Read();
        }

    }
}
