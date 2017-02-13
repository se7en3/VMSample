using System;
using System.Collections;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace PropertyCollector
{
    ///<summary>
    /// This sample excercise the PropertyCollector API of all the managed entity.
    ///</summary>
    ///<param name="dcName">Required: Name of the datacenter.</param>
    ///<param name="vmDnsName">Required: Dns name of a virtual machine.</param>
    ///<remarks>
    ///--url [webserviceurl]
    ///--username [username] --password [password] --dcName [datacenterName]
    ///--vmDnsName [vmDnsName]
    ///</remarks>
    public class PropertyCollector
    {
        static VimService _service;
        static ServiceContent _sic;
        private static AppUtil.AppUtil cb = null;
        Log log = new Log();

        /// <summary>
        /// This method is used to add application specific user options
        /// </summary>
        ///<returns> Array of OptionSpec containing the details of
        /// application specific user options
        ///</returns>
        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("dcName", "String", 1
                                        , "Name of the Datacenter"
                                        , null);
            useroptions[1] = new OptionSpec("vmDnsName", "String", 1,
                                         "Virtual machine dns name",
                                         null);
            return useroptions;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main(String[] args)
        {
            try
            {
                PropertyCollector app = new PropertyCollector();
                cb = AppUtil.AppUtil.initialize("PropertyCollector",
                                        PropertyCollector.constructOptions(),
                                        args);
                cb.connect();
                ManagedObjectReference sic = cb.getConnection().ServiceRef;
                _sic = cb.getConnection()._sic;
                _service = cb.getConnection()._service;
                String dcName = cb.get_option("dcName");
                String vmDnsName = cb.get_option("vmDnsName");
                ObjectContent[] ocs = null;
                ManagedObjectReference dcMoRef
                   = cb.getServiceUtil().GetDecendentMoRef(null, "Datacenter", dcName);
                if (dcMoRef == null)
                {
                    Console.WriteLine("Datacenter not found");
                }
                else
                {
                    ManagedObjectReference vmMoRef =
                       _service.FindByDnsName(_sic.searchIndex,
                                             dcMoRef, vmDnsName, true);
                    if (vmMoRef == null)
                    {
                        Console.WriteLine("The virtual machine with DNS '" + vmDnsName
                           + "' not found ");
                    }
                    else
                    {
                        // Retrieve name and powerState from a Virtual Machine
                        Object[] properties
                            = getProperties(vmMoRef, new String[] { "name", "runtime.powerState" });
                        String vmName = (String)properties[0];
                        VirtualMachinePowerState vmState
                              = (VirtualMachinePowerState)properties[1];
                        if (vmName != null && vmState != null)
                        {
                            Console.WriteLine("The VM with DNS name \'" + vmDnsName
                                              + "\' is named \'" + vmName +
                                              "\' and is " + vmState.ToString());
                        }
                        ocs = getDatacenters();
                        printObjectContent(ocs, "All Datacenters");
                        // Find all the VMs in the Datacenter
                        ocs = getVMs(dcMoRef);
                        printObjectContent(ocs, "All VMs in the Datacenter: " + dcName);
                        //Find all the Hosts in the Datacenter
                        ocs = getHosts(dcMoRef);
                        printObjectContent(ocs, "All Hosts in the Datacenter: " + dcName);
                        // Display summary information about a VM
                        ocs = getVMInfo(vmMoRef);
                        printVmInfo(ocs);
                        // Display all of inventory
                        ocs = getInventory();
                        printInventoryTree(ocs);
                        ocs = getNetworkInfo(dcMoRef);
                        printNetworkInfo(ocs);
                        cb.disConnect();
                        Console.WriteLine("Press any key to exit:");
                        Console.Read();
                    }
                }
            }
            catch (SoapException e)
            {
                if (e.Detail.FirstChild.LocalName.Equals("DuplicateNameFault"))
                {
                    Console.WriteLine("Managed Entity with the name already exists");
                }
                else if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
                {
                    Console.WriteLine("Specification is invalid");
                }
                else if (e.Detail.FirstChild.LocalName.Equals("InvalidNameFault"))
                {
                    Console.WriteLine("Managed Entity Name is empty or too long");
                }
                else if (e.Detail.FirstChild.LocalName.Equals("RuntimeFault"))
                {
                    Console.WriteLine(e.Message.ToString() + "Either parent name or item name is invalid");
                }
                else if (e.Detail.FirstChild.LocalName.Equals("RuntimeFault"))
                {
                    Console.WriteLine(e.Message.ToString() + " "
                                        + "The Operation is not supported on this object");
                }
                else
                {
                    Console.WriteLine(e.Message.ToString() + " "
                                     + "The Operation is not supported on this object");
                }
            }
            Console.Read();
        }

        ///<summary>
        ///Retrieve properties from a single MoRef.
        ///</summary>
        private static Object[] getProperties(ManagedObjectReference moRef, String[] properties)
        {
            PropertySpec pSpec = new PropertySpec();
            pSpec.type = moRef.type;
            pSpec.pathSet = properties;
            ObjectSpec oSpec = new ObjectSpec();
            // Set the starting object
            oSpec.obj = moRef;
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = new PropertySpec[] { pSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };
            ObjectContent[] ocs = _service.RetrieveProperties(
                  _sic.propertyCollector,
                  new PropertyFilterSpec[] { pfSpec });
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

        ///<summary>
        /// Print out the ObjectContent[]
        /// returned from RetrieveProperties()
        ///</summary>
        private static void printObjectContent(ObjectContent[] ocs,
           String title)
        {
            // Print out the title to label the output
            Console.WriteLine(title);
            if (ocs != null)
            {
                for (int i = 0; i < ocs.Length; ++i)
                {
                    ObjectContent oc = ocs[i];
                    // Print out the managed object type
                    Console.WriteLine(oc.obj.type);
                    Console.WriteLine("  Property Name:Value");
                    DynamicProperty[] dps = oc.propSet;
                    if (dps != null)
                    {
                        for (int j = 0; j < dps.Length; ++j)
                        {
                            DynamicProperty dp = dps[j];
                            // Print out the property name and value
                            Console.WriteLine("  " + dp.name + ": "
                                    + dp.val);
                        }
                    }
                }
            }
        }

        ///<summary>
        ///Specifications to find all the Datacenters and
        ///retrieve their name, vmFolder and hostFolder values.
        ///</summary>
        private static ObjectContent[] getDatacenters()
        {
            // The PropertySpec object specifies what properties
            // to retrieve from what type of Managed Object
            PropertySpec pSpec = new PropertySpec();
            pSpec.type = "Datacenter";
            pSpec.pathSet = new String[] {
              "name", "vmFolder", "hostFolder" };
            // The following TraversalSpec and SelectionSpec
            // objects create the following relationship:
            //
            // a. Folder -> childEntity
            //   b. recurse to a.
            //
            // This specifies that starting with a Folder
            // managed object, traverse through its childEntity
            // property. For each element in the childEntity
            // property, process by going back to the 'parent'
            // TraversalSpec.
            // SelectionSpec to cause Folder recursion
            SelectionSpec recurseFolders = new SelectionSpec();
            // The name of a SelectionSpec must refer to a
            // TraversalSpec with the same name value.
            recurseFolders.name = "folder2childEntity";
            // Traverse from a Folder through the 'childEntity' property
            TraversalSpec folder2childEntity = new TraversalSpec();
            // Select the Folder type managed object
            folder2childEntity.type = "Folder";
            // Traverse through the childEntity property of the Folder
            folder2childEntity.path = "childEntity";
            // Name this TraversalSpec so the SelectionSpec above
            // can refer to it
            folder2childEntity.name = recurseFolders.name;
            // Add the SelectionSpec above to this traversal so that
            // we will recurse through the tree via the childEntity
            // property
            folder2childEntity.selectSet = new SelectionSpec[] {
          recurseFolders };
            // The ObjectSpec object specifies the starting object and
            // any TraversalSpecs used to specify other objects
            // for consideration
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = _sic.rootFolder;
            // We set skip to true because we are not interested
            // in retrieving properties from the root Folder
            oSpec.skip = true;
            // Specify the TraversalSpec. This is what causes
            // other objects besides the starting object to
            // be considered part of the collection process
            oSpec.selectSet = new SelectionSpec[] { folder2childEntity };
            // The PropertyFilterSpec object is used to hold the
            // ObjectSpec and PropertySpec objects for the call
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = new PropertySpec[] { pSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };
            // RetrieveProperties() returns the properties
            // selected from the PropertyFilterSpec
            return _service.RetrieveProperties(
                  _sic.propertyCollector,
                  new PropertyFilterSpec[] { pfSpec });
        }

        ///<summary>
        /// Specifications to find all the VMs in a Datacenter and
        /// retrieve their name and runtime.powerState values.
        ///</summary>
        private static ObjectContent[] getVMs(ManagedObjectReference dcMoRef)
        {
            // The PropertySpec object specifies what properties
            // retrieve from what type of Managed Object
            PropertySpec pSpec = new PropertySpec();
            pSpec.type = "VirtualMachine";
            pSpec.pathSet = new String[] { "name", "runtime.powerState" };
            SelectionSpec recurseFolders = new SelectionSpec();
            recurseFolders.name = "folder2childEntity";
            TraversalSpec folder2childEntity = new TraversalSpec();
            folder2childEntity.type = "Folder";
            folder2childEntity.path = "childEntity";
            folder2childEntity.name = recurseFolders.name;
            folder2childEntity.selectSet =
               new SelectionSpec[] { recurseFolders };
            // Traverse from a Datacenter through the 'vmFolder' property
            TraversalSpec dc2vmFolder = new TraversalSpec();
            dc2vmFolder.type = "Datacenter";
            dc2vmFolder.path = "vmFolder";
            dc2vmFolder.selectSet =
               new SelectionSpec[] { folder2childEntity };
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = dcMoRef;
            oSpec.skip = true;
            oSpec.selectSet = new SelectionSpec[] { dc2vmFolder };
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = new PropertySpec[] { pSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };
            return _service.RetrieveProperties(_sic.propertyCollector,
                  new PropertyFilterSpec[] { pfSpec });
        }

        private static ObjectContent[] getHosts(ManagedObjectReference dcMoRef)
        {
            // PropertySpec specifies what properties to
            // retrieve from what type of Managed Object
            PropertySpec pSpec = new PropertySpec();
            pSpec.type = "HostSystem";
            pSpec.pathSet = new String[] { "name", "runtime.connectionState" };
            SelectionSpec recurseFolders = new SelectionSpec();
            recurseFolders.name = "folder2childEntity";
            TraversalSpec computeResource2host = new TraversalSpec();
            computeResource2host.type = "ComputeResource";
            computeResource2host.path = "host";
            TraversalSpec folder2childEntity = new TraversalSpec();
            folder2childEntity.type = "Folder";
            folder2childEntity.path = "childEntity";
            folder2childEntity.name = recurseFolders.name;
            // Add BOTH of the specifications to this specification
            folder2childEntity.selectSet = new SelectionSpec[] { recurseFolders };
            // Traverse from a Datacenter through
            // the 'hostFolder' property
            TraversalSpec dc2hostFolder = new TraversalSpec();
            dc2hostFolder.type = "Datacenter";
            dc2hostFolder.path = "hostFolder";
            dc2hostFolder.selectSet = new SelectionSpec[] { folder2childEntity };
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = dcMoRef;
            oSpec.skip = true;
            oSpec.selectSet = new SelectionSpec[] { dc2hostFolder };
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = new PropertySpec[] { pSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };
            return _service.RetrieveProperties(
                  _sic.propertyCollector,
                     new PropertyFilterSpec[] { pfSpec });
        }

        private static ObjectContent[] getVMInfo(ManagedObjectReference vmMoRef)
        {
            // This spec selects VirtualMachine information
            PropertySpec vmPropSpec = new PropertySpec();
            vmPropSpec.type = "VirtualMachine";
            vmPropSpec.pathSet = new String[] {
              "name",
              "config.guestFullName",
              "config.hardware.memoryMB",
              "config.hardware.numCPU",
              "guest.toolsStatus",
              "guestHeartbeatStatus",
              "guest.ipAddress",
              "guest.hostName",
              "runtime.powerState",
              "summary.quickStats.overallCpuUsage",
              "summary.quickStats.hostMemoryUsage",
              "summary.quickStats.guestMemoryUsage", };
            PropertySpec hostPropSpec = new PropertySpec();
            hostPropSpec.type = "HostSystem";
            hostPropSpec.pathSet = new String[] { "name" };
            PropertySpec taskPropSpec = new PropertySpec();
            taskPropSpec.type = "Task";
            taskPropSpec.pathSet = new String[] { "info.name", "info.completeTime" };
            PropertySpec datastorePropSpec = new PropertySpec();
            datastorePropSpec.type = "Datastore";
            datastorePropSpec.pathSet = new String[] { "info" };
            PropertySpec networkPropSpec = new PropertySpec();
            networkPropSpec.type = "Network";
            networkPropSpec.pathSet = new String[] { "name" };
            TraversalSpec hostTraversalSpec = new TraversalSpec();
            hostTraversalSpec.type = "VirtualMachine";
            hostTraversalSpec.path = "runtime.host";
            TraversalSpec taskTravesalSpec = new TraversalSpec();
            taskTravesalSpec.type = "VirtualMachine";
            taskTravesalSpec.path = "recentTask";
            TraversalSpec datastoreTraversalSpec = new TraversalSpec();
            datastoreTraversalSpec.type = "VirtualMachine";
            datastoreTraversalSpec.path = "datastore";
            TraversalSpec networkTraversalSpec = new TraversalSpec();
            networkTraversalSpec.type = "VirtualMachine";
            networkTraversalSpec.path = "network";
            // ObjectSpec specifies the starting object and
            // any TraversalSpecs used to specify other objects
            // for consideration
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = vmMoRef;
            // Add the TraversalSpec objects to the ObjectSpec
            // This specifies what additional object we want to
            // consider during the process.
            oSpec.selectSet = new SelectionSpec[] {
            hostTraversalSpec,
            taskTravesalSpec,
            datastoreTraversalSpec,
            networkTraversalSpec };
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            // Add the PropertySpec objects to the PropertyFilterSpec
            // This specifies what data we want to collect while
            // processing the found objects from the ObjectSpec
            pfSpec.propSet = new PropertySpec[] {
            vmPropSpec,
            hostPropSpec,
            taskPropSpec,
            datastorePropSpec,
            networkPropSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };
            return _service.RetrieveProperties(
               _sic.propertyCollector,
               new PropertyFilterSpec[] { pfSpec });
        }

        ///<summary>
        ///Take the ObjectContent[] from RetrieveProperties()
        ///and print it out.
        ///</summary>
        private static void printVmInfo(ObjectContent[] ocs)
        {
            if (ocs != null)
            {
                //Each instance of ObjectContent contains the properties
                // retrieved for one instance of a managed object
                for (int oci = 0; oci < ocs.Length; ++oci)
                {
                    // Properties for one managed object
                    ObjectContent oc = ocs[oci];
                    // Get the type of managed object
                    String type = oc.obj.type;
                    Console.WriteLine("VM Information");
                    // Handle data from VirtualMachine managed objects
                    if ("VirtualMachine".Equals(type))
                    {
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            // Each instance of DynamicProperty contains a
                            // single property from the managed object
                            // This data comes back as name-value pairs
                            // The code below is checking each name and
                            // assigning the proper field in the data
                            // object
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                DynamicProperty dp = dps[j];
                                if ("name".Equals(dp.name))
                                {
                                    Console.WriteLine("  Name               : " + (String)dp.val);
                                }
                                else if ("config.guestFullName".Equals(dp.name))
                                {
                                    Console.WriteLine("  Guest OS Name      : " + (String)dp.val);
                                }
                                else if ("config.hardware.memoryMB".Equals(dp.name))
                                {
                                    Console.WriteLine("  Memory             : " + (int)dp.val);
                                }
                                else if ("config.hardware.numCPU".Equals(dp.name))
                                {
                                    Console.WriteLine("  Num vCPU           : " + (int)dp.val);
                                }
                                else if ("guest.toolsStatus".Equals(dp.name))
                                {
                                    Console.WriteLine("  VMware Tools       : " + (VirtualMachineToolsStatus)dp.val);
                                }
                                else if ("guestHeartbeatStatus".Equals(dp.name))
                                {
                                    Console.WriteLine("  Guest Heartbeat    : " + (ManagedEntityStatus)dp.val);
                                }
                                else if ("guest.ipAddress".Equals(dp.name))
                                {
                                    Console.WriteLine("  Guest IP Address   : " + (String)dp.val);
                                }
                                else if ("guest.hostName".Equals(dp.name))
                                {
                                    Console.WriteLine("  Guest DNS Name     : " + (String)dp.val);
                                }
                                else if ("runtime.powerState".Equals(dp.name))
                                {
                                    Console.WriteLine("  State              : " + (VirtualMachinePowerState)dp.val);
                                }
                                else if (
                                  "summary.quickStats.overallCpuUsage".Equals(dp.name))
                                {
                                    Console.WriteLine("  CPU Usage          : " + (int)dp.val + " MHz");
                                }
                                else if (
                                  "summary.quickStats.hostMemoryUsage".Equals(dp.name))
                                {
                                    Console.WriteLine("  Host Memory Usage  : " + (int)dp.val + " MB");
                                }
                                else if (
                                  "summary.quickStats.guestMemoryUsage".Equals(dp.name))
                                {
                                    Console.WriteLine("  Guest Memory Usage : " + (int)dp.val + " MB");
                                }
                            }
                        }
                        // Handle data from HostSystem managed objects
                    }
                    else if ("HostSystem".Equals(type))
                    {
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                DynamicProperty dp = dps[j];
                                if ("name".Equals(dp.name))
                                {
                                    Console.WriteLine("  Host               : " + (String)dp.val);
                                }
                            }
                        }
                        // Handle data from Task managed objects
                    }
                    else if ("Task".Equals(type))
                    {
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            Boolean taskCompleted = false;
                            String taskName = "";
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                DynamicProperty dp = dps[j];
                                if ("info.name".Equals(dp.name))
                                {
                                    Console.WriteLine("  Active Tasks       : " + (String)dp.val);
                                }
                                else if ("info.completeTime".Equals(dp
                                        .name))
                                {
                                    taskCompleted = true;
                                }
                            }
                            if (!taskCompleted)
                            {
                                Console.WriteLine("  Tasks Status       : " + taskCompleted);
                            }
                        }
                        // Handle data from Datastore managed objects
                    }
                    else if ("Datastore".Equals(type))
                    {
                        Console.WriteLine("  Datastores           :");
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                DynamicProperty dp = dps[j];
                                if ("info".Equals(dp.name))
                                {
                                    DatastoreInfo dInfo = (DatastoreInfo)dp.val;
                                    int cap = 0;
                                    if (dInfo.GetType().Name.Equals("VmfsDatastoreInfo"))
                                    {
                                        VmfsDatastoreInfo vmfsInfo =
                                            (VmfsDatastoreInfo)dInfo;
                                        if (vmfsInfo.vmfs != null)
                                        {
                                            cap = (int)(vmfsInfo.vmfs.capacity / 1024 / 1024 / 1024);
                                        }
                                    }
                                    else if (dInfo.GetType().Name.Equals("NasDatastoreInfo"))
                                    {
                                        NasDatastoreInfo nasInfo =
                                            (NasDatastoreInfo)dInfo;
                                        if (nasInfo.nas != null)
                                        {
                                            cap = (int)(nasInfo.nas.capacity / 1024 / 1024 / 1024);
                                        }
                                    }
                                    Console.WriteLine("    Name             : " + dInfo.name);
                                    Console.WriteLine("    Free Space       : " + (dInfo.freeSpace) / 1024 / 1024 / 1024 + "GB");
                                    Console.WriteLine("    Capacity         : " + cap + "GB");
                                }
                            }
                        }
                        // Handle data from Network managed objects
                    }
                    else if ("Network".Equals(type))
                    {
                        Console.WriteLine("  Networks           :");
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                DynamicProperty dp = dps[j];
                                if ("name".Equals(dp.name))
                                {
                                    Console.WriteLine("    Name       : " + (String)dp.val);
                                }
                            }
                        }
                    }
                }
                // Print out the results
                //Console.WriteLine(vmInfo.toString());
            }
        }

        ///<summary>
        /// Specifications to find all items in inventory and
        /// retrieve their name and parent values.
        ///</summary>
        private static ObjectContent[] getInventory()
        {
            string[][] typeInfo = new string[][] {
            new string[] { "ManagedEntity", "parent", "name" }, };
            ObjectContent[] ocary =
                   cb.getServiceUtil().GetContentsRecursively(null, null, typeInfo, true);
            return ocary;
        }

        private class MeNode
        {
            private ManagedObjectReference parent;
            private ManagedObjectReference node;
            private String name;
            private ArrayList children = new ArrayList();

            public MeNode(ManagedObjectReference parent,
                    ManagedObjectReference node, String name)
            {
                this.setParent(parent);
                this.setNode(node);
                this.setName(name);
            }

            public void setParent(ManagedObjectReference parent)
            {
                this.parent = parent;
            }

            public ManagedObjectReference getParent()
            {
                return parent;
            }

            public void setNode(ManagedObjectReference node)
            {
                this.node = node;
            }

            public ManagedObjectReference getNode()
            {
                return node;
            }

            public void setName(String name)
            {
                this.name = name;
            }

            public String getName()
            {
                return name;
            }

            public void setChildren(ArrayList children)
            {
                this.children = children;
            }

            public ArrayList getChildren()
            {
                return children;
            }
        }

        ///<summary>
        /// Recursive method to print an inventory tree node
        ///</summary>
        private static void printNode(MeNode node, int indent)
        {
            // Make it pretty
            for (int i = 0; i < indent; ++i)
            {
                Console.Write(' ');
            }
            Console.WriteLine(node.getName() +
                    " (" + node.getNode().type + ")");
            if (node.getChildren().Count != 0)
            {
                for (int c = 0; c < node.getChildren().Count; ++c)
                {
                    printNode((MeNode)
                            node.getChildren()[c], indent + 2);
                }
            }
        }

        ///<summary>
        /// Print the inventory tree retrieved from
        /// the PropertyCollector
        ///</summary>
        private static void printInventoryTree(ObjectContent[] ocs)
        {
            // Hashtable MoRef.Value -> MeNode
            Hashtable nodes = new Hashtable();
            // The root folder node
            MeNode root = null;
            for (int oci = 0; oci < ocs.Length; ++oci)
            {
                ObjectContent oc = ocs[oci];
                ManagedObjectReference mor = oc.obj;
                DynamicProperty[] dps = oc.propSet;
                if (dps != null)
                {
                    ManagedObjectReference parent = null;
                    String name = null;
                    for (int dpi = 0; dpi < dps.Length; ++dpi)
                    {
                        DynamicProperty dp = dps[dpi];
                        if (dp != null)
                        {
                            if ("name".Equals(dp.name))
                            {
                                name = (String)dp.val;
                            }
                            if ("parent".Equals(dp.name))
                            {
                                parent = (ManagedObjectReference)dp
                                .val;
                            }
                        }
                    }
                    // Create a MeNode to hold the data
                    MeNode node = new MeNode(parent, mor, name);
                    // The root folder has no parent
                    if (parent == null)
                    {
                        root = node;
                    }
                    // Add the node
                    nodes.Add(node.getNode().Value, node);
                }
            }
            // Build the nodes into a tree
            foreach (String key in nodes.Keys)
            {
                MeNode meNode = nodes[key] as MeNode;
                if (meNode.getParent() != null)
                {
                    MeNode parent = (MeNode)nodes[meNode.getParent().Value];
                    parent.getChildren().Add(meNode);
                }
            }
            Console.WriteLine("Inventory Tree");
            printNode(root, 0);
        }

        ///<summary>
        /// Specifications to find all Networks in a Datacenter,
        /// list all VMs on each Network,
        /// list all Hosts on each Network
        ///</summary>
        private static ObjectContent[] getNetworkInfo(
            ManagedObjectReference dcMoRef)
        {
            // PropertySpec specifies what properties to
            // retrieve from what type of Managed Object
            // This spec selects the Network name
            PropertySpec networkPropSpec = new PropertySpec();
            networkPropSpec.type = "Network";
            networkPropSpec.pathSet = new String[] { "name" };
            // This spec selects HostSystem information
            PropertySpec hostPropSpec = new PropertySpec();
            hostPropSpec.type = "HostSystem";
            hostPropSpec.pathSet = new String[] { "network", "name",
                "summary.hardware", "runtime.connectionState",
                "summary.overallStatus", "summary.quickStats" };
            // This spec selects VirtualMachine information
            PropertySpec vmPropSpec = new PropertySpec();
            vmPropSpec.type = "VirtualMachine";
            vmPropSpec.pathSet = new String[] { "network", "name",
                "runtime.host", "runtime.powerState",
                "summary.overallStatus", "summary.quickStats" };
            // The following TraversalSpec and SelectionSpec
            // objects create the following relationship:
            //
            // a. Datacenter -> network
            //   b. Network -> host
            //   c. Network -> vm
            // b. Traverse from a Network through the 'host' property
            TraversalSpec network2host = new TraversalSpec();
            network2host.type = "Network";
            network2host.path = "host";
            // c. Traverse from a Network through the 'vm' property
            TraversalSpec network2vm = new TraversalSpec();
            network2vm.type = "Network";
            network2vm.path = "vm";
            // a. Traverse from a Datacenter through
            // the 'network' property
            TraversalSpec dc2network = new TraversalSpec();
            dc2network.type = "Datacenter";
            dc2network.path = "network";
            dc2network.selectSet = new SelectionSpec[] {
                // Add b. traversal
                network2host,
                // Add c. traversal
                network2vm };
            // ObjectSpec specifies the starting object and
            // any TraversalSpecs used to specify other objects
            // for consideration
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = dcMoRef;
            oSpec.skip = true;
            oSpec.selectSet = new SelectionSpec[] { dc2network };
            // PropertyFilterSpec is used to hold the ObjectSpec and
            // PropertySpec for the call
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = new PropertySpec[] { networkPropSpec,
                hostPropSpec, vmPropSpec };
            pfSpec.objectSet = new ObjectSpec[] { oSpec };
            // RetrieveProperties() returns the properties
            // selected from the PropertyFilterSpec
            return _service.RetrieveProperties(
                    _sic.propertyCollector,
                    new PropertyFilterSpec[] { pfSpec });
        }

        private class Host
        {
            private ManagedObjectReference moRef;
            private String name;
            private HostHardwareSummary hardware;
            private HostSystemConnectionState connectionState;
            private ManagedEntityStatus overallStatus;
            private HostListSummaryQuickStats quickStats;

            public Host(ManagedObjectReference _this)
            {
                this.setMoRef(_this);
            }

            public Host(ManagedObjectReference _this,
                    String name,
                    HostHardwareSummary hardware,
                    HostSystemConnectionState connectionState,
                    ManagedEntityStatus overallStatus,
                    HostListSummaryQuickStats quickStats)
            {
                this.setMoRef(_this);
                this.setName(name);
                this.setHardware(hardware);
                this.setConnectionState(connectionState);
                this.setOverallStatus(overallStatus);
                this.setQuickStats(quickStats);
            }

            public void setMoRef(ManagedObjectReference moRef)
            {
                this.moRef = moRef;
            }

            public ManagedObjectReference getMoRef()
            {
                return moRef;
            }

            public void setName(String name)
            {
                this.name = name;
            }

            public String getName()
            {
                return name;
            }

            public void setHardware(HostHardwareSummary hardware)
            {
                this.hardware = hardware;
            }

            public HostHardwareSummary getHardware()
            {
                return hardware;
            }

            public void setConnectionState(
                    HostSystemConnectionState connectionState)
            {
                this.connectionState = connectionState;
            }

            public HostSystemConnectionState getConnectionState()
            {
                return connectionState;
            }

            public void setOverallStatus(
                    ManagedEntityStatus overallStatus)
            {
                this.overallStatus = overallStatus;
            }

            public ManagedEntityStatus getOverallStatus()
            {
                return overallStatus;
            }

            public void setQuickStats(
                    HostListSummaryQuickStats quickStats)
            {
                this.quickStats = quickStats;
            }

            public HostListSummaryQuickStats getQuickStats()
            {
                return quickStats;
            }
        }

        private class VirtualMachine
        {
            private ManagedObjectReference moRef;
            private String name;
            private ManagedObjectReference host;
            private VirtualMachinePowerState powerState;
            private ManagedEntityStatus overallStatus;
            private VirtualMachineQuickStats quickStats;

            public VirtualMachine(ManagedObjectReference _this)
            {
                this.setMoRef(_this);
            }

            public VirtualMachine(ManagedObjectReference _this,
                    String name,
                    ManagedObjectReference host,
                    VirtualMachinePowerState powerState,
                    ManagedEntityStatus overallStatus,
                    VirtualMachineQuickStats quickStats)
            {
                this.setMoRef(_this);
                this.setName(name);
                this.setHost(host);
                this.setPowerState(powerState);
                this.setOverallStatus(overallStatus);
                this.setQuickStats(quickStats);
            }

            public void setMoRef(ManagedObjectReference moRef)
            {
                this.moRef = moRef;
            }

            public ManagedObjectReference getMoRef()
            {
                return moRef;
            }

            public void setName(String name)
            {
                this.name = name;
            }

            public String getName()
            {
                return name;
            }

            public void setHost(ManagedObjectReference host)
            {
                this.host = host;
            }

            public ManagedObjectReference getHost()
            {
                return host;
            }

            public void setPowerState(
                    VirtualMachinePowerState powerState)
            {
                this.powerState = powerState;
            }

            public VirtualMachinePowerState getPowerState()
            {
                return powerState;
            }

            public void setOverallStatus(
                    ManagedEntityStatus overallStatus)
            {
                this.overallStatus = overallStatus;
            }

            public ManagedEntityStatus getOverallStatus()
            {
                return overallStatus;
            }

            public void setQuickStats(
                    VirtualMachineQuickStats quickStats)
            {
                this.quickStats = quickStats;
            }

            public VirtualMachineQuickStats getQuickStats()
            {
                return quickStats;
            }
        }

        private class Network
        {
            private ManagedObjectReference moRef;
            private String name;

            public Network(ManagedObjectReference _this)
            {
                this.setMoRef(_this);
            }

            public void setMoRef(ManagedObjectReference moRef)
            {
                this.moRef = moRef;
            }

            public ManagedObjectReference getMoRef()
            {
                return moRef;
            }

            public void setName(String name)
            {
                this.name = name;
            }

            public String getName()
            {
                return name;
            }
        }

        ///<summary>
        ///Take the ObjectContent[] from RetrieveProperties()
        ///and print it out.
        ///ObjectContent[] should have Network information
        ///</summary>
        private static void printNetworkInfo(ObjectContent[] ocs)
        {
            // Network MoRef -> Network
            Hashtable networksByNetwork = new Hashtable();
            // Network MoRef -> Host
            Hashtable hostsByNetwork = new Hashtable();
            // Network MoRef -> VirtualMachine
            Hashtable vmsByNetwork = new Hashtable();
            // HostSystem MoRef -> Host
            Hashtable hostByHost = new Hashtable();
            if (ocs != null)
            {
                for (int i = 0; i < ocs.Length; ++i)
                {
                    ObjectContent oc = ocs[i];
                    String type = oc.obj.type;
                    // Create our Network objects
                    if ("Network".Equals(type))
                    {
                        Network network = new Network(oc.obj);
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                DynamicProperty dp = dps[j];
                                if ("name".Equals(dp.name))
                                {
                                    network.setName((String)dp.val);
                                }
                            }
                        }
                        // Put them in the Map
                        networksByNetwork.Add(oc.obj.Value,
                                network);
                        // Create our Host objects
                    }
                    else if ("HostSystem".Equals(type))
                    {
                        Host cHost = new Host(oc.obj);
                        ManagedObjectReference[] network = null;
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                String pName = dps[j].name;
                                Object pVal = dps[j].val;
                                if ("name".Equals(pName))
                                {
                                    cHost.setName((String)pVal);
                                }
                                else if ("network".Equals(pName))
                                {
                                    network =
                                        (ManagedObjectReference[])pVal;
                                }
                                else if (
                                  "summary.hardware".Equals(pName))
                                {
                                    cHost.setHardware(
                                            (HostHardwareSummary)pVal);
                                }
                                else if ("runtime.connectionState"
                                      .Equals(pName))
                                {
                                    cHost.setConnectionState(
                                        (HostSystemConnectionState)pVal);
                                }
                                else if ("summary.overallStatus"
                                      .Equals(pName))
                                {
                                    cHost.setOverallStatus(
                                            (ManagedEntityStatus)pVal);
                                }
                                else if ("summary.quickStats"
                                      .Equals(pName))
                                {
                                    cHost.setQuickStats(
                                        (HostListSummaryQuickStats)pVal);
                                }
                            }
                        }
                        Host host = new Host(
                                cHost.getMoRef(),
                                cHost.getName(),
                                cHost.getHardware(),
                                cHost.getConnectionState(),
                                cHost.getOverallStatus(),
                                cHost.getQuickStats());
                        hostByHost.Add(
                                host.getMoRef().Value, host);
                        for (int n = 0; n < network.Length; ++n)
                        {
                            ArrayList hl = (ArrayList)hostsByNetwork[network[n].Value];
                            if (hl == null)
                            {
                                hl = new ArrayList();
                                hostsByNetwork.Add(network[n].Value,
                                        hl);
                            }
                            hl.Add(host);
                        }
                        // Create our VirtualMachine objects
                    }
                    else if ("VirtualMachine".Equals(type))
                    {
                        VirtualMachine cVm =
                            new VirtualMachine(oc.obj);
                        ManagedObjectReference[] network = null;
                        DynamicProperty[] dps = oc.propSet;
                        if (dps != null)
                        {
                            for (int j = 0; j < dps.Length; ++j)
                            {
                                String pName = dps[j].name;
                                Object pVal = dps[j].val;
                                if ("name".Equals(pName))
                                {
                                    cVm.setName((String)pVal);
                                }
                                else if ("network".Equals(pName))
                                {
                                    network =
                                        (ManagedObjectReference[])pVal;
                                }
                                else if ("runtime.host".Equals(pName))
                                {
                                    cVm.setHost(
                                        (ManagedObjectReference)pVal);
                                }
                                else if ("runtime.powerState"
                                      .Equals(pName))
                                {
                                    cVm.setPowerState(
                                        (VirtualMachinePowerState)pVal);
                                }
                                else if ("summary.overallStatus"
                                      .Equals(pName))
                                {
                                    cVm.setOverallStatus(
                                            (ManagedEntityStatus)pVal);
                                }
                                else if ("summary.quickStats"
                                      .Equals(pName))
                                {
                                    cVm.setQuickStats(
                                        (VirtualMachineQuickStats)pVal);
                                }
                            }
                        }
                        VirtualMachine vm = new VirtualMachine(
                                cVm.getMoRef(),
                                cVm.getName(),
                                cVm.getHost(),
                                cVm.getPowerState(),
                                cVm.getOverallStatus(),
                                cVm.getQuickStats());
                        for (int n = 0; n < network.Length; ++n)
                        {
                            ArrayList vml = (ArrayList)vmsByNetwork[network[n].Value];
                            if (vml == null)
                            {
                                vml = new ArrayList();
                                vmsByNetwork.Add(network[n].Value,
                                        vml);
                            }
                            vml.Add(vm);
                        }
                    }
                }
            }
            // Now the Hashtables have all the information
            // Now populate our Network object with the Hosts
            // and VMs connected and print out the 'tables'
            for (IEnumerator nit = networksByNetwork.GetEnumerator();
                nit.MoveNext(); )
            {
                foreach (String key in networksByNetwork.Keys)
                {
                    Network network = networksByNetwork[key] as Network;
                    if (network != null)
                    {
                        ArrayList vms = (ArrayList)
                        vmsByNetwork[network.getMoRef().Value];
                        ArrayList hosts = (ArrayList)hostsByNetwork[network.getMoRef().Value];
                        Console.WriteLine("Network: " + network.getName());
                        Console.WriteLine("  Virtual Machines:");
                        if (vms != null)
                        {
                            for (IEnumerator vmIt = vms.GetEnumerator(); vmIt.MoveNext(); )
                            {
                                VirtualMachine vm = (VirtualMachine)vmIt.Current;
                                Host host =
                                    (Host)hostByHost[vm.getHost().Value];
                                int cpuUsage =
                                    vm.getQuickStats().overallCpuUsage;
                                int memUsage =
                                    vm.getQuickStats().hostMemoryUsage;
                                StringBuilder sb = new StringBuilder();
                                sb
                                .Append("    Name          :")
                                .Append(vm.getName())
                                .Append("\n")
                                .Append("    State         :")
                                .Append(vm.getPowerState())
                                .Append("\n")
                                .Append("    Status        :")
                                .Append(vm.getOverallStatus())
                                .Append("\n")
                                .Append("    Host Name     :")
                                .Append(host != null ? host.getName() : "")
                                .Append("\n")
                                .Append("    Host CPU MHZ  :")
                                .Append(cpuUsage != null ? cpuUsage
                                                : 0)
                                .Append("\n")
                                .Append("    Host Mem = MB :")
                                .Append(memUsage != null ?
                                        memUsage / 1024 / 1024
                                        : 0)
                                .Append("\n");
                                Console.WriteLine(sb.ToString());
                            }
                        }
                        Console.WriteLine("  Hosts:");
                        if (hosts != null)
                        {
                            for (IEnumerator hostIt = hosts.GetEnumerator();
                                hostIt.MoveNext(); )
                            {
                                Host host = (Host)hostIt.Current;
                                int cpuUsage =
                                    host.getQuickStats().overallCpuUsage;
                                int memUsage = host.getQuickStats().overallMemoryUsage;
                                StringBuilder sb = new StringBuilder();
                                sb
                                .Append("    Name    :")
                                .Append(host.getName())
                                .Append("\n")
                                .Append("    State   :")
                                .Append(host.getConnectionState())
                                .Append("\n")
                                .Append("    Status  :")
                                .Append(host.getOverallStatus())
                                .Append("\n")
                                .Append("    CPU %   :")
                                .Append(cpuUsage != null ? cpuUsage : 0)
                                .Append("\n")
                                .Append("    Mem MB  :")
                                .Append(memUsage != null ? memUsage
                                        : 0).Append("\n");
                                Console.WriteLine(sb.ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}