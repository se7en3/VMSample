using System;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.IO;
using AppUtil;
using Vim25Api;

namespace VMCreate
{

    class VMCreate
    {
        private static AppUtil.AppUtil cb = null;
        private static VMUtils vmUtils = null;
        static VimService _service;
        static ServiceContent _sic;
        private void createVM()  {
          _service = cb.getConnection()._service;
          String dcName = cb.get_option("datacentername");
          ManagedObjectReference dcmor 
             = cb.getServiceUtil().GetDecendentMoRef(null,"Datacenter",dcName);
          
          if(dcmor == null) {
             Console.WriteLine("Datacenter " + dcName + " not found.");
             return;
          }
     try
     {
      ManagedObjectReference hfmor 
         = cb.getServiceUtil().GetMoRefProp(dcmor, "hostFolder");
      ArrayList crmors
         = cb.getServiceUtil().GetDecendentMoRefs(hfmor, "ComputeResource", null);    
     
      String hostName = cb.get_option("hostname");
      ManagedObjectReference hostmor;

      if (hostName != null) {
         hostmor = cb.getServiceUtil().GetDecendentMoRef(hfmor, "HostSystem", hostName);
         if(hostmor == null) {
            Console.WriteLine("Host " + hostName + " not found");
            return;
         }
      } else {
         hostmor = cb.getServiceUtil().GetFirstDecendentMoRef(dcmor, "HostSystem");
      }
      
      ManagedObjectReference crmor = null;      
      hostName = (String)cb.getServiceUtil().GetDynamicProperty(hostmor,"name");
      for(int i = 0; i < crmors.Count; i++) {
          
         ManagedObjectReference[] hrmors
            = (ManagedObjectReference[])cb.getServiceUtil().GetDynamicProperty((ManagedObjectReference)crmors[i], "host");
         if(hrmors != null && hrmors.Length > 0) {
            for(int j = 0; j < hrmors.Length; j++) {   
               String hname 
                  = (String)cb.getServiceUtil().GetDynamicProperty(hrmors[j],"name");
               if (hname.Equals(hostName))
               {
                   crmor = (ManagedObjectReference)crmors[i];
                   i = crmors.Count + 1;
                   j = hrmors.Length + 1;
               }
              
            }
         }
      }     
      
      if(crmor == null) {
         Console.WriteLine("No Compute Resource Found On Specified Host");
         return;
      }
      
      ManagedObjectReference resourcePool 
         = cb.getServiceUtil().GetMoRefProp(crmor, "resourcePool");
      ManagedObjectReference vmFolderMor 
         = cb.getServiceUtil().GetMoRefProp(dcmor, "vmFolder");
      
          VirtualMachineConfigSpec vmConfigSpec =
               vmUtils.createVmConfigSpec(cb.get_option("vmname"),
                                           cb.get_option("datastorename"),
                                           int.Parse(cb.get_option("disksize")),
                                           crmor, hostmor);
     
      
      vmConfigSpec.name=cb.get_option("vmname");
      vmConfigSpec.annotation="VirtualMachine Annotation";
      vmConfigSpec.memoryMB= (long)(int.Parse(cb.get_option("memorysize")));
      vmConfigSpec.memoryMBSpecified = true;
      vmConfigSpec.numCPUs = int.Parse(cb.get_option("cpucount"));
      vmConfigSpec.numCPUsSpecified = true;
      vmConfigSpec.guestId= (cb.get_option("guestosid"));

      ManagedObjectReference taskmor = _service.CreateVM_Task(
              vmFolderMor, vmConfigSpec, resourcePool, hostmor
      );
      String res = cb.getServiceUtil().WaitForTask(taskmor);       
      if(res.Equals("sucess")) {
          Console.WriteLine("Virtual Machine Created Sucessfully");
      }
      else {
          Console.WriteLine("Virtual Machine could not be created. ");
      }
  }
  catch (Exception e)
  {
      Console.WriteLine(e.Message.ToString());
  }
   }   
        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[8];
            useroptions[0] = new OptionSpec("vmname", "String", 1
                                            , "Name of the virtual machine"
                                            , null);
            useroptions[1] = new OptionSpec("datacentername", "String", 1,
                                            "Name of the datacenter",
                                            null);
            useroptions[2] = new OptionSpec("hostname", "String", 0
                                            , "Name of the host"
                                            , null);
            useroptions[3] = new OptionSpec("guestosid", "String", 0,
                                            "Type of Guest OS",
                                            "winXPProGuest");
            useroptions[4] = new OptionSpec("cpucount", "Integer", 0
                                            , "Total CPU Count"
                                            , "1");
            useroptions[5] = new OptionSpec("disksize", "Integer", 0,
                                            "Size of the Disk",
                                            "1024");
            useroptions[6] = new OptionSpec("memorysize", "Integer", 0
                                            , "Size of the Memory in the blocks of 1024 MB"
                                            , "1024");
            useroptions[7] = new OptionSpec("datastorename", "String", 0,
                                            "Name of the datastore",
                                            null);
            return useroptions;
        }   
        public static void Main(String[] args)
        {
            VMCreate obj = new VMCreate();
            cb = AppUtil.AppUtil.initialize("VMCreate"
                                    , VMCreate.constructOptions()
                                   , args);
            cb.connect();
            vmUtils = new VMUtils(cb);
            obj.createVM();
            cb.disConnect();
            Console.WriteLine("Press enter to exit: ");
            Console.Read();
           
        }
    }
}
