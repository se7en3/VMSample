using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using AppUtil;
using Vim25Api;

namespace VMSnapshot
{
    ///<summary>
    ///This sample is used to list , create , remove and revert
    ///the VM Snapshots
    ///</summary>
    ///<param name="vmname">Required: Name of the virtual machine</param>
    ///<param name="operation">Required: Type of the operation [list|create|remove|removeall|revert]</param>
    ///<param name="snapshotname">Optional: Name of Snapshot</param>
    ///<param name="description">Optional: Description of snapshot</param>
    ///<param name="removechild">Optional: 1 if children needs to be removed and 0 if children needn't be removed</param>
    ///<remarks>
    ///List , create , remove and revert the VM Snapshots
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --vmname [vmname]
    ///--operation [operation] --snapshotname [snapshotname] --description [description] --removechild [removechild]
    ///</remarks>
    class VMSnapshot
    {
        private static AppUtil.AppUtil cb = null;

        private Boolean createSnapshot(ManagedObjectReference vmMor)
        {
            String snapshotName = cb.get_option("snapshotname");
            String desc = cb.get_option("description");
            ManagedObjectReference taskMor
               = cb.getConnection()._service.CreateSnapshot_Task(
                                             vmMor, snapshotName, desc, false, false);
            String res = cb.getServiceUtil().WaitForTask(taskMor);
            if (res.Equals("sucess"))
            {
                revertSnapshot(vmMor);
                removeSnapshot(vmMor);
                listSnapshot(vmMor);
                return true;
            }
            return false;
        }

        private Boolean listSnapshot(ManagedObjectReference vmMor)
        {
            ObjectContent[] snaps = cb.getServiceUtil().GetObjectProperties(
                                                        null, vmMor,
                                                        new String[] { "snapshot" });
            VirtualMachineSnapshotInfo snapInfo = null;
            if (snaps != null && snaps.Length > 0)
            {
                ObjectContent snapobj = snaps[0];
                DynamicProperty[] snapary = snapobj.propSet;
                if (snapary != null && snapary.Length > 0)
                {
                    snapInfo = ((VirtualMachineSnapshotInfo)(snapary[0]).val);
                    VirtualMachineSnapshotTree[] snapTree = snapInfo.rootSnapshotList;
                    traverseSnapshotInTree(snapTree, null, true);
                }
                else
                {
                    Console.WriteLine("No Snapshots found");
                    return false;
                }
            }
            return true;
        }

        private Boolean revertSnapshot(ManagedObjectReference vmMor)
        {
            String snapshotName = cb.get_option("snapshotname");
            bool suppressPowerOn = false;
            ManagedObjectReference snapmor
               = getSnapshotReference(vmMor, cb.get_option("vmname"),
                                                          cb.get_option("snapshotname"));
            if (snapmor != null)
            {
                ManagedObjectReference taskMor
                  = cb.getConnection()._service.RevertToSnapshot_Task(snapmor, null, suppressPowerOn, false);
                String res = cb.getServiceUtil().WaitForTask(taskMor);
                if (res.Equals("sucess"))
                {
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Snapshot not found");
            }
            return false;
        }

        private Boolean removeAllSnapshot(ManagedObjectReference vmMor)
        {
            Boolean isSnap = listSnapshot(vmMor);
            bool consolidate = true;
            if (isSnap)
            {
                ManagedObjectReference taskMor
                 = cb.getConnection()._service.RemoveAllSnapshots_Task(vmMor, consolidate, false);
                String res = cb.getServiceUtil().WaitForTask(taskMor);
                if (res.Equals("sucess"))
                {
                    return true;
                }
            }
            return false;
        }

        private Boolean removeSnapshot(ManagedObjectReference vmMor)
        {
            String snapshotName = cb.get_option("snapshotname");

            int rem = int.Parse(cb.get_option("removechild"));

            Boolean flag = true;
            bool consolidate = true;
            if (rem == 0) flag = false;
            ManagedObjectReference snapmor = getSnapshotReference(
                                             vmMor, cb.get_option("vmname"),
                                             cb.get_option("snapshotname"));
            if (snapmor != null)
            {
                ManagedObjectReference taskMor
                   = cb.getConnection()._service.RemoveSnapshot_Task(snapmor, flag, consolidate, false);
                String res = cb.getServiceUtil().WaitForTask(taskMor);
                if (res.Equals("sucess"))
                {
                    return true;
                }
            }
            else
            {
                Console.WriteLine("Snapshot not found");
            }
            return false;

        }

        private ManagedObjectReference traverseSnapshotInTree(
                                           VirtualMachineSnapshotTree[] snapTree,
                                           String findName,
                                           Boolean print)
        {
            ManagedObjectReference snapmor = null;
            if (snapTree == null)
            {
                return snapmor;
            }
            for (int i = 0; i < snapTree.Length && snapmor == null; i++)
            {
                VirtualMachineSnapshotTree node = snapTree[i];
                if (print)
                {
                    Console.WriteLine("Snapshot Name : " + node.name);
                }

                if (findName != null && node.name.Equals(findName))
                {
                    snapmor = node.snapshot;
                }
                else
                {
                    VirtualMachineSnapshotTree[] childTree = node.childSnapshotList;
                    snapmor = traverseSnapshotInTree(childTree, findName, print);
                }
            }

            return snapmor;
        }

        private ManagedObjectReference getSnapshotReference(ManagedObjectReference vmmor,
                                                            String vmName,
                                                            String snapName)
        {
            VirtualMachineSnapshotInfo snapInfo = getSnapshotInfo(vmmor, vmName);
            ManagedObjectReference snapmor = null;
            if (snapInfo != null)
            {
                VirtualMachineSnapshotTree[] snapTree = snapInfo.rootSnapshotList;
                snapmor = traverseSnapshotInTree(snapTree, snapName, false);
            }
            else
            {
                Console.WriteLine("No Snapshot named : " + snapName
                                 + " found for VirtualMachine : " + vmName);
            }
            return snapmor;
        }

        private VirtualMachineSnapshotInfo getSnapshotInfo
               (ManagedObjectReference vmmor, String vmName)
        {
            ObjectContent[] snaps = cb.getServiceUtil().GetObjectProperties(
               null, vmmor, new String[] { "snapshot" }
            );

            VirtualMachineSnapshotInfo snapInfo = null;
            if (snaps != null && snaps.Length > 0)
            {
                ObjectContent snapobj = snaps[0];
                DynamicProperty[] snapary = snapobj.propSet;
                if (snapary != null && snapary.Length > 0)
                {
                    snapInfo = ((VirtualMachineSnapshotInfo)(snapary[0]).val);
                }
            }
            else
            {
                Console.WriteLine("No Snapshots found for VirtualMachine : "
                                  + vmName);
            }
            return snapInfo;
        }

        private Boolean customValidation()
        {
            Boolean flag = true;
            String op = cb.get_option("operation");
            if (op.Equals("create"))
            {
                if ((!cb.option_is_set("snapshotname"))
                       || (!cb.option_is_set("description")))
                {
                    Console.WriteLine("For Create operation SnapshotName"
                                     + " and Description are the Mandatory options");
                    flag = false;
                }
            }
            if (op.Equals("remove"))
            {
                if ((!cb.option_is_set("snapshotname"))
                       || (!cb.option_is_set("removechild")))
                {
                    Console.WriteLine("For Remove operation Snapshotname"
                                      + " and removechild are the Mandatory option");
                    flag = false;
                }
                else
                {
                    try
                    {
                        int child = (Convert.ToInt32(cb.get_option("removechild")));

                        if (child != 0 && child != 1)
                        {
                            Console.WriteLine("Value of removechild parameter"
                                              + " must be either 0 or 1");
                            flag = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Value of removechild parameter must be either 0 or 1");
                        flag = false;
                    }
                }
            }
            if (op.Equals("revert"))
            {
                if ((!cb.option_is_set("snapshotname")))
                {
                    Console.WriteLine("For Revert operation SnapshotName"
                                      + " is the Mandatory option");
                    flag = false;
                }
            }
            return flag;
        }
        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[5];
            useroptions[0] = new OptionSpec("vmname", "String", 1
                                            , "Name of the virtual machine"
                                            , null);
            useroptions[1] = new OptionSpec("operation", "String", 1,
                                            "Type of the operation [list|"
                                           + "create|remove|removeall|revert]",
                                            null);
            useroptions[2] = new OptionSpec("snapshotname", "String", 0
                                            , "Name of Snapshot"
                                            , "Test VM Snapshot");
            useroptions[3] = new OptionSpec("description", "String", 0,
                                            "Description of snapshot",
                                            "Test Snapshot");
            useroptions[4] = new OptionSpec("removechild", "String", 0
                                            , "1 if children needs to be removed"
                                            + " and 0 if children needn't be removed"
                                            , "0");
            return useroptions;
        }
        public static void Main(String[] args)
        {
            VMSnapshot obj = new VMSnapshot();

            cb = AppUtil.AppUtil.initialize("VMSnapshot"
                                    , VMSnapshot.constructOptions()
                                   , args);

            Boolean valid = obj.customValidation();
            if (valid)
            {
                cb.connect();
                String vmName = cb.get_option("vmname");
                ManagedObjectReference vmMor
                   = cb.getServiceUtil().GetDecendentMoRef(null, "VirtualMachine", vmName);

                if (vmMor != null)
                {
                    String op = cb.get_option("operation");
                    Boolean res = false;
                    if (op.Equals("create"))
                    {
                        res = obj.createSnapshot(vmMor);
                    }
                    else if (op.Equals("list"))
                    {
                        res = obj.listSnapshot(vmMor);
                    }
                    else if (op.Equals("revert"))
                    {
                        res = obj.revertSnapshot(vmMor);
                    }
                    else if (op.Equals("removeall"))
                    {
                        res = obj.removeAllSnapshot(vmMor);
                        if (!res)
                        {
                            Console.WriteLine("Operation " + op + " cannot be performed.");
                        }
                    }
                    else if (op.Equals("remove"))
                    {
                        res = obj.removeSnapshot(vmMor);
                    }
                    else
                    {
                        Console.WriteLine("Invalid operation" + op.ToString() + ". Valid Operations are [create|list|revert|remoeveall|remove]");
                        cb.disConnect();
                        Console.WriteLine("Press any key to exit: ");
                        Console.Read();
                        Environment.Exit(1);
                    }
                    if (res)
                    {
                        Console.WriteLine("Operation " + op + " completed sucessfully");
                    }
                }
                else
                {
                    Console.WriteLine("No VM " + vmName + " found");
                }
                cb.disConnect();
            }
            else
            {
                Console.WriteLine("Operation can not be performed");

            }
            Console.WriteLine("Press any key to exit: ");
            Console.Read();
            Environment.Exit(1);

        }
    }
}
