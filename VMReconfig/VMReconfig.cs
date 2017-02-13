using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using AppUtil;
using Vim25Api;

namespace VMReconfig
{
  public class VMReconfig
    {
        private static VMUtils vmUtils = null;
        private static AppUtil.AppUtil cb = null;
        private ManagedObjectReference _virtualMachine = null;    
   
   private void getVmMor(String vmName)  {
      _virtualMachine 
         = cb.getServiceUtil().GetDecendentMoRef(null, "VirtualMachine", vmName);
   }   
   
         private void reConfig()  {
      String deviceType = cb.get_option("device");
      VirtualMachineConfigSpec vmConfigSpec = new VirtualMachineConfigSpec();
      
      if(deviceType.Equals("memory")) {
         Console.WriteLine("Reconfiguring The Virtual Machine For Memory Update " 
                           + cb.get_option("vmname"));
         try {
            vmConfigSpec.memoryAllocation = getShares();          
         }
         catch(Exception nfe) {
            Console.WriteLine("Value of Memory update must "
                              +"be either Custom or Integer");
            return;
         }
      }
      else if(deviceType.Equals("cpu")) {
         Console.WriteLine("Reconfiguring The Virtual Machine For CPU Update " 
                           + cb.get_option("vmname"));       
         try {
            vmConfigSpec.cpuAllocation=getShares();
         }
         catch(Exception nfe) {
            Console.WriteLine("Value of CPU update must "
                              +"be either Custom or Integer");
            return;
         }
      }
      else if(deviceType.Equals("disk")) {
         Console.WriteLine("Reconfiguring The Virtual Machine For Disk Update " 
                           + cb.get_option("vmname"));
         
         VirtualDeviceConfigSpec vdiskSpec = getDiskDeviceConfigSpec();
         if(vdiskSpec != null) {
             VirtualMachineConfigInfo vmConfigInfo
       = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(
           _virtualMachine, "config");
             int ckey = -1;
             VirtualDevice[] test = vmConfigInfo.hardware.device;
             for (int k = 0; k < test.Length; k++)
             {
                 if (test[k].deviceInfo.label.Equals(
                    "SCSI Controller 0"))
                 {
                     ckey = test[k].key;
                 }
             }
             
             if (ckey == -1)
             {
                 int diskCtlrKey = 1;
                 VirtualDeviceConfigSpec scsiCtrlSpec = new VirtualDeviceConfigSpec();
                 scsiCtrlSpec.operation = VirtualDeviceConfigSpecOperation.add;
                 scsiCtrlSpec.operationSpecified = true;
                 VirtualLsiLogicController scsiCtrl = new VirtualLsiLogicController();
                 scsiCtrl.busNumber = 0;
                 scsiCtrlSpec.device = scsiCtrl;
                 scsiCtrl.key = diskCtlrKey;
                 scsiCtrl.sharedBus = VirtualSCSISharing.physicalSharing;
                 String ctlrType = scsiCtrl.GetType().Name;
                 vdiskSpec.device.controllerKey = scsiCtrl.key;
                 VirtualDeviceConfigSpec[] vdiskSpecArray = { scsiCtrlSpec, vdiskSpec };
                 vmConfigSpec.deviceChange = vdiskSpecArray;
             }
             else
             {
                 vdiskSpec.device.controllerKey = ckey;
                 VirtualDeviceConfigSpec[] vdiskSpecArray = { vdiskSpec };
                 vmConfigSpec.deviceChange = vdiskSpecArray;
             }
                     
           
         }
         else {
            return;
         }
      }
      else if(deviceType.Equals("nic")) {
         Console.WriteLine("Reconfiguring The Virtual Machine For NIC Update " 
                           + cb.get_option("vmname"));                          
         VirtualDeviceConfigSpec nicSpec = getNICDeviceConfigSpec();
         if(nicSpec != null) {
            VirtualDeviceConfigSpec [] nicSpecArray = {nicSpec};                     
            vmConfigSpec.deviceChange=nicSpecArray;
         }
         else {
            return;
         }          
      }
      else if(deviceType.Equals("cd")) {
         Console.WriteLine("Reconfiguring The Virtual Machine For CD Update "  
                           + cb.get_option("vmname"));                          
         VirtualDeviceConfigSpec cdSpec = getCDDeviceConfigSpec();
         if(cdSpec != null) {
            VirtualDeviceConfigSpec [] cdSpecArray = {cdSpec};                     
            vmConfigSpec.deviceChange=cdSpecArray;
         }
         else {
            return;
         } 
      }
      else {
         Console.WriteLine("Invlaid device type [memory|cpu|disk|nic|cd]");
         return;
      }      
      
      ManagedObjectReference tmor 
         = cb.getConnection()._service.ReconfigVM_Task(
             _virtualMachine, vmConfigSpec);
      monitorTask(tmor);   
   }
   
   private void monitorTask(ManagedObjectReference tmor)  {
      if(tmor != null) {
         String result = cb.getServiceUtil().WaitForTask(tmor);
         if(result.Equals("sucess")) {
            Console.WriteLine("Task Completed Sucessfully");
         }
         else {
            Console.WriteLine("Failure " + result);
         }
      }
   }
    
   private VirtualDeviceConfigSpec getCDDeviceConfigSpec()  {
      String ops = cb.get_option("operation");
      VirtualDeviceConfigSpec cdSpec = new VirtualDeviceConfigSpec();      
      VirtualMachineConfigInfo vmConfigInfo 
         = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(
             _virtualMachine,"config");
      
      if(ops.Equals("add")) {                        
         cdSpec.operation = VirtualDeviceConfigSpecOperation.add;
         cdSpec.operationSpecified = true;
         VirtualCdrom cdrom =  new VirtualCdrom();
         
         VirtualCdromIsoBackingInfo cdDeviceBacking 
            = new  VirtualCdromIsoBackingInfo();
         DatastoreSummary dsum = getDataStoreSummary();        
         cdDeviceBacking.datastore= dsum.datastore;
         cdDeviceBacking.fileName="["+dsum.name+"] "+cb.get_option("value")+".iso";
         
         VirtualDevice vd = getIDEController();          
         cdrom.backing =cdDeviceBacking;                    
         cdrom.controllerKey = vd.key;
         cdrom.controllerKeySpecified = true;
         cdrom.unitNumber= -1;
         cdrom.unitNumberSpecified = true;
         cdrom.key= -100;          
         
         cdSpec.device=cdrom;
         
         return cdSpec;          
      }
      else {
         VirtualCdrom cdRemove = null;
         VirtualDevice [] test = vmConfigInfo.hardware.device;
         cdSpec.operation=VirtualDeviceConfigSpecOperation.remove;
         cdSpec.operationSpecified = true;
         for(int k=0;k<test.Length;k++){
            if(test[k].deviceInfo.label.Equals(
               cb.get_option("value"))){                             
               cdRemove = (VirtualCdrom)test[k];
            }
         }
         if(cdRemove != null) {
            cdSpec.device=cdRemove;
         }
         else {
            Console.WriteLine("No device available " + cb.get_option("value"));
            return null;
         }
      }
      return cdSpec;
   }
   
   private DatastoreSummary getDataStoreSummary()  {
      DatastoreSummary dsSum = null;
      VirtualMachineRuntimeInfo vmRuntimeInfo 
         = (VirtualMachineRuntimeInfo)cb.getServiceUtil().GetDynamicProperty(
             _virtualMachine,"runtime");       
      ManagedObjectReference envBrowser 
         = (ManagedObjectReference)cb.getServiceUtil().GetDynamicProperty(
             _virtualMachine,"environmentBrowser");       
      ManagedObjectReference hmor = vmRuntimeInfo.host;
      
      if(hmor != null) {       
         ConfigTarget configTarget 
            = cb.getConnection()._service.QueryConfigTarget(envBrowser, null);       
         if(configTarget.datastore != null) {
            for (int i = 0; i < configTarget.datastore.Length; i++) {
               VirtualMachineDatastoreInfo vdsInfo = configTarget.datastore[i];
               DatastoreSummary dsSummary = vdsInfo.datastore;
               if (dsSummary.accessible) {
                  dsSum = dsSummary;
                  break;
               }
            }
         }
         return dsSum;
      }
      else {
         Console.WriteLine("No Datastore found");
         return null;
      }
   }
   
   private VirtualDevice getIDEController()  {
      VirtualDevice ideCtlr = null;
      VirtualDevice [] defaultDevices = getDefaultDevices();
      for (int di = 0; di < defaultDevices.Length; di++) {
         if (defaultDevices[di].GetType().Name.Equals("VirtualIDEController")) {
            ideCtlr = defaultDevices[di];             
            break;
         }
      }
      return ideCtlr;
   }
   
   private VirtualDevice[] getDefaultDevices()  {
      VirtualMachineRuntimeInfo vmRuntimeInfo 
         = (VirtualMachineRuntimeInfo)cb.getServiceUtil().GetDynamicProperty(
               _virtualMachine,"runtime");       
      ManagedObjectReference envBrowser 
         = (ManagedObjectReference)cb.getServiceUtil().GetDynamicProperty(
              _virtualMachine,"environmentBrowser");       
      ManagedObjectReference hmor = vmRuntimeInfo.host;
      
      VirtualMachineConfigOption cfgOpt 
         = cb.getConnection()._service.QueryConfigOption(envBrowser, null, null);
      VirtualDevice[] defaultDevs = null;

      if (cfgOpt == null) {
         throw new Exception("No VirtualHardwareInfo found in ComputeResource");
      }
      else {
         defaultDevs = cfgOpt.defaultDevice;
         if (defaultDevs == null) {
            throw new Exception("No Datastore found in ComputeResource");
         }
      }
      return defaultDevs;
   }   
   
   
   private VirtualDeviceConfigSpec getNICDeviceConfigSpec()  {
      String ops = cb.get_option("operation");
      VirtualDeviceConfigSpec nicSpec = new VirtualDeviceConfigSpec();      
      VirtualMachineConfigInfo vmConfigInfo 
         = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(
              _virtualMachine,"config");
      
      if(ops.Equals("add")) {
         String networkName = getNetworkName(); 
         if(networkName != null) {
            nicSpec.operation=VirtualDeviceConfigSpecOperation.add;
            nicSpec.operationSpecified = true;
            VirtualEthernetCard nic =  new VirtualPCNet32();
            VirtualEthernetCardNetworkBackingInfo nicBacking 
               = new VirtualEthernetCardNetworkBackingInfo();
            nicBacking.deviceName = networkName;
            nic.addressType="generated";
            nic.backing= nicBacking;
            nic.key= 4;
            nicSpec.device=nic;
         }
         else {
            return null;
         }
      }
      else if(ops.Equals("remove")) {
         VirtualEthernetCard nic = null;
         VirtualDevice [] test = vmConfigInfo.hardware.device;
         nicSpec.operation=VirtualDeviceConfigSpecOperation.remove;
         nicSpec.operationSpecified = true;
         for(int k=0;k<test.Length;k++){
         if(test[k].deviceInfo.label.Equals(
               cb.get_option("value"))){                             
            nic = (VirtualEthernetCard)test[k];
            }
         }
         if(nic != null) {
            nicSpec.device=nic;
         }
         else {
            Console.WriteLine("No device available " + cb.get_option("value"));
            return null;
         }
      }
      return nicSpec;
   }
   
   private String getNetworkName()  {
      String networkName = null;
      VirtualMachineRuntimeInfo vmRuntimeInfo 
         = (VirtualMachineRuntimeInfo)cb.getServiceUtil().GetDynamicProperty(
             _virtualMachine,"runtime");       
      ManagedObjectReference envBrowser 
         = (ManagedObjectReference)cb.getServiceUtil().GetDynamicProperty(
             _virtualMachine,"environmentBrowser");       
      ManagedObjectReference hmor = vmRuntimeInfo.host;
      
      if(hmor != null) {       
         ConfigTarget configTarget 
            = cb.getConnection()._service.QueryConfigTarget(envBrowser, null);       
         if(configTarget.network != null) {
            for (int i = 0; i < configTarget.network.Length; i++) {
               VirtualMachineNetworkInfo netInfo = configTarget.network[i];
               NetworkSummary netSummary = netInfo.network;
               if (netSummary.accessible) {
                  if(netSummary.name.Equals(
                        cb.get_option("value"))) {
                     networkName = netSummary.name;
                     break;
                  }
               }
            }
            if(networkName == null) {
               Console.WriteLine("Specify the Correct Network Name");
               return null;
            }
         }
         Console.WriteLine("network Name " + networkName);
         return networkName;
      }
      else {
         Console.WriteLine("No Host is responsible to run this VM");
         return null;
      }
   }
   
   private VirtualDeviceConfigSpec getDiskDeviceConfigSpec() {
      String ops = cb.get_option("operation");
      VirtualDeviceConfigSpec diskSpec = new VirtualDeviceConfigSpec();      
      VirtualMachineConfigInfo vmConfigInfo 
         = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(
             _virtualMachine,"config");                           
      
      if(ops.Equals("add")) {                             
         VirtualDisk disk =  new VirtualDisk();
         VirtualDiskFlatVer2BackingInfo diskfileBacking 
            = new VirtualDiskFlatVer2BackingInfo();    
         String dsName 
            = getDataStoreName(int.Parse(cb.get_option("disksize")));         
         
         int ckey = -1;
         int unitNumber = 0;
     
         VirtualDevice [] test = vmConfigInfo.hardware.device;
         for(int k=0;k<test.Length;k++){
            if(test[k].deviceInfo.label.Equals(
               "SCSI Controller 0")){
               ckey = test[k].key;                                
            }
         }
         
             
         

         unitNumber = test.Length + 1;                
         String fileName = "["+dsName+"] "+ cb.get_option("vmname") 
                         + "/"+cb.get_option("value")+".vmdk";
         
         diskfileBacking.fileName=fileName;
         diskfileBacking.diskMode=cb.get_option("diskmode");

         disk.controllerKey = ckey;
         disk.unitNumber=unitNumber;
         disk.controllerKeySpecified = true;
         disk.unitNumberSpecified = true;
         disk.backing= diskfileBacking;
         int size = 1024 * (int.Parse(cb.get_option("disksize")));
         disk.capacityInKB= size;
         disk.key= 0;
         
         diskSpec.operation=VirtualDeviceConfigSpecOperation.add;           
         diskSpec.fileOperation=VirtualDeviceConfigSpecFileOperation.create;
         diskSpec.fileOperationSpecified = true;
         diskSpec.operationSpecified = true;
         diskSpec.device=disk;                 
      }
      else if(ops.Equals("remove")) {                             
         VirtualDisk disk =  null;
         VirtualDiskFlatVer2BackingInfo diskfileBacking 
            = new VirtualDiskFlatVer2BackingInfo();

         VirtualDevice [] test = vmConfigInfo.hardware.device;
         for(int k=0;k<test.Length;k++){
            if(test[k].deviceInfo.label.Equals(
                    cb.get_option("value"))){                             
               disk = (VirtualDisk)test[k];
            }
         }             
         if(disk != null) {
            diskSpec.operation=VirtualDeviceConfigSpecOperation.remove;
            diskSpec.operationSpecified = true;
            diskSpec.fileOperation=VirtualDeviceConfigSpecFileOperation.destroy;
            diskSpec.fileOperationSpecified = true;
            diskSpec.device=disk;                 
         }
         else {
            Console.WriteLine("No device found " + cb.get_option("value"));
            return null;
         }
      }
      return diskSpec;
   }
   
   private String getDataStoreName(int size) {
      String dsName = null;
      ManagedObjectReference [] datastores 
         = (ManagedObjectReference [])cb.getServiceUtil().GetDynamicProperty(
               _virtualMachine,"datastore");
      for(int i=0; i<datastores.Length; i++) {
         DatastoreSummary ds 
            = (DatastoreSummary)cb.getServiceUtil().GetDynamicProperty(datastores[i],
                                                                      "summary");
         if(ds.freeSpace > size) {
            dsName = ds.name;
            i = datastores.Length + 1;           
         }
      }
      return dsName;
   }
   
   private ResourceAllocationInfo getShares()  {
      ResourceAllocationInfo raInfo = new ResourceAllocationInfo();
      SharesInfo sharesInfo = new SharesInfo();
      
      String val = cb.get_option("value");       
      if(val.Equals(SharesLevel.high.ToString())) {       
         sharesInfo.level=SharesLevel.high;          
      }
      else if(val.Equals(SharesLevel.normal.ToString())) {
         sharesInfo.level=SharesLevel.normal;
      }
      else if(val.Equals(SharesLevel.low.ToString())) {
         sharesInfo.level=SharesLevel.low;
      }
      else {
         sharesInfo.level=SharesLevel.custom;          
         sharesInfo.shares=int.Parse(val);          
      }    
      raInfo.shares=sharesInfo;
      return raInfo;
   }

        
        private Boolean customValidation() {
      Boolean flag = true;
      String device = cb.get_option("device");
      if(device.Equals("disk")) {
          if ((!cb.option_is_set("operation")) || (!cb.option_is_set("disksize"))
                || (!cb.option_is_set("diskmode")))
          {
              Console.WriteLine("For update disk operation, disksize "
                 + "and diskmode are the Mandatory options");
              flag = false;
          }
          else if (int.Parse(cb.get_option("disksize")) <= 0)
          {
                  Console.WriteLine("Disksize must be a greater than zero");
                  flag = false;
          }
      }
      if(device.Equals("nic")) {
         if((!cb.option_is_set("operation")) ) {
            Console.WriteLine("For update nic operation is the Mandatory options");
            flag = false;
         }
      }
      if(device.Equals("cd")) {
         if((!cb.option_is_set("operation"))) {
            Console.WriteLine("For update cd operation is the Mandatory options");
            flag = false;
         }
      }
      if(device.Equals("cpu") || device.Equals("memory")) {
          int val;
          Boolean b = int.TryParse(cb.get_option("value"), out val);
          if (!b)
          {
          }          
          else if(int.Parse(cb.get_option("value")) <= 0 ) {
            Console.WriteLine("CPU and Memory shares must be a greater than zero");
            flag = false;
            
         }
      }
      if(cb.option_is_set("operation")) {
         if(cb.get_option("operation").Equals("add") 
            || cb.get_option("operation").Equals("remove")) {}
         else {
            Console.WriteLine("Operation must be either add or remove");
            flag = false;
         }
      }
      return flag;             
   }
        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[6];
            useroptions[0] = new OptionSpec("vmname", "String", 1
                                            , "Name of the virtual machine"
                                            , null);
            useroptions[1] = new OptionSpec("device", "String", 1,
                                            "Type of device {cpu|memory|disk|cd|nic}",
                                            null);
            useroptions[2] = new OptionSpec("operation", "String", 0
                                            , "{add|remove}"
                                            , null);
            useroptions[3] = new OptionSpec("value", "String", 1,
                                            "{numeric(For Memory and CPU) (high|"
                                           + "low|normal|numeric value|deviceId}",
                                            null);
            useroptions[4] = new OptionSpec("disksize", "Integer", 0
                                            , "Size of virtual disk"
                                            , null);
            useroptions[5] = new OptionSpec("diskmode", "String", 0,
                                            "{persistent|independent_persistent,"
                                            + "independent_nonpersistent}",
                                            null);
            return useroptions;
        }  
        public static void Main(String[] args)
        {
            VMReconfig obj = new VMReconfig();
            cb = AppUtil.AppUtil.initialize("VMReconfig"
                                    , VMReconfig.constructOptions()
                                   , args);
           Boolean valid = obj.customValidation();
      if(valid) {
         cb.connect();      
         obj.getVmMor(cb.get_option("vmname"));
         if(obj._virtualMachine != null) {
            vmUtils = new VMUtils(cb);
             obj.reConfig();
         }
         else {
            Console.WriteLine("Virtual Machine " + cb.get_option("vmname") 
                            + " Not Found");
         }
         cb.disConnect();
        
      }
      Console.WriteLine("Press enter to exit.");
      Console.Read();
        }
    }
}
