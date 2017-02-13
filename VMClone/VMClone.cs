using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using AppUtil;
using Vim25Api;

namespace VMClone
{
    class VMClone
    {
        private static AppUtil.AppUtil cb = null;
        static VimService _service;
        static ServiceContent _sic;
        private void cloneVM() {    
             _service = cb.getConnection()._service;
             _sic = cb.getConnection()._sic;
      String cloneName = cb.get_option("CloneName");
      String vmPath = cb.get_option("vmPath");
      String datacenterName= cb.get_option("DatacenterName");
      
     
      // Find the Datacenter reference by using findByInventoryPath().
      ManagedObjectReference datacenterRef
         = _service.FindByInventoryPath(_sic.searchIndex, datacenterName);
      if (datacenterRef == null) {
         Console.WriteLine("The specified datacenter is not found");
         return;
      }
      // Find the virtual machine folder for this datacenter.
      ManagedObjectReference vmFolderRef
         = (ManagedObjectReference)cb.getServiceUtil().GetMoRefProp(datacenterRef, "vmFolder");
      if (vmFolderRef == null) {
         Console.WriteLine("The virtual machine is not found");
         return;
      }
      ManagedObjectReference vmRef
         = _service.FindByInventoryPath(_sic.searchIndex, vmPath);
      if (vmRef == null) {
         Console.WriteLine("The virtual machine is not found");
         return;
      }
      VirtualMachineCloneSpec cloneSpec = new VirtualMachineCloneSpec();
      VirtualMachineRelocateSpec relocSpec = new VirtualMachineRelocateSpec();
      cloneSpec.location=relocSpec;
      cloneSpec.powerOn=false;
      cloneSpec.template=false;
      
      String clonedName = cloneName;
      Console.WriteLine("Launching clone task to create a clone: " 
                         + clonedName);
      try {
         ManagedObjectReference cloneTask 
            = _service.CloneVM_Task(vmRef, vmFolderRef, clonedName, cloneSpec);
         String status = cb.getServiceUtil().WaitForTask(cloneTask);
         if(status.Equals("failure")) {
            Console.WriteLine("Failure -: Virtual Machine cannot be cloned");
         }
         if (status.Equals("sucess"))
         {
            Console.WriteLine("Virtual Machine Cloned  successfully.");
         }
         else{
             Console.WriteLine("Virtual Machine Cloned cannot be cloned");
      }
      }
      catch(Exception e) {
        
      }   
   }
        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[3];
            useroptions[0] = new OptionSpec("DatacenterName", "String", 1
                                     , "Name of the Datacenter"
                                     , null);
            useroptions[1] = new OptionSpec("vmPath", "String", 1,
                                            "A path to the VM inventory, example:Datacentername/vm/vmname",
                                            null);
            useroptions[2] = new OptionSpec("CloneName", "String", 1,
                                            "Name of the Clone",
                                            null);
            return useroptions;
        }
        public static void Main(String[] args)
        {
            VMClone obj = new VMClone();
            cb = AppUtil.AppUtil.initialize("VMClone"
                                    , VMClone.constructOptions()
                                   , args);
            cb.connect();
            obj.cloneVM();
            cb.disConnect();
            Console.WriteLine("Press any key to exit: ");
            Console.Read();
            Environment.Exit(1);
        }
    }
}
