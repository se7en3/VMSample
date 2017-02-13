using System;
using System.Collections;
using AppUtil;
using Vim25Api;

namespace VmPowerOps {

    /// <summary>
    /// Virtual Machine Power Ops implementation.
    /// </summary>
    
    public class VmPowerOps {
      private static AppUtil.AppUtil cb = null;
      static VimService _service;
      static ServiceContent _sic;

      public void DoPowerOps() {
         ArrayList morlist = null;
         String powerOnHostName = cb.get_option("hostname");
         String vmName = cb.get_option("vmname");
         String powerOperation = cb.get_option("operation");
         try {
            ManagedObjectReference vmmor = null;
            String errmsg = "";            
            vmmor = cb.getServiceUtil().GetDecendentMoRef(null, 
                                                          "VirtualMachine",
                                                          vmName);
            if (vmmor == null) {
               errmsg = "Unable to find VirtualMachine named : " 
                        + cb.get_option("vmname") 
                        + " in Inventory";
            }
            

            //TODO: find required host.            
            //ManagedObjectReference hostmor = cb.getServiceUtil().GetDecendentMoRef(null, "HostSystem", powerOnHostName);
            ManagedObjectReference hostmor = cb.getServiceUtil().GetFirstDecendentMoRef(null, "HostSystem");
            bool nonTaskOp = false;            
            ManagedObjectReference taskmor = null;
            if (powerOperation.Equals("on")) {
               taskmor = cb.getConnection().Service.PowerOnVM_Task(vmmor, hostmor);
           }
           else if (powerOperation.Equals("off"))
           {
               taskmor = cb.getConnection().Service.PowerOffVM_Task(vmmor);
           }
           else if (powerOperation.Equals("suspend"))
           {
               taskmor = cb.getConnection().Service.SuspendVM_Task(vmmor);
           }
           else if (powerOperation.Equals("reset"))
           {
               taskmor = cb.getConnection().Service.ResetVM_Task(vmmor);
           }
           else if (powerOperation.Equals("rebootGuest"))
           {
               cb.getConnection().Service.RebootGuest(vmmor);
               nonTaskOp = true;
           }
           else if (powerOperation.Equals("shutdownGuest"))
           {
               cb.getConnection().Service.ShutdownGuest(vmmor);
               nonTaskOp = true;
           }
           else if (powerOperation.Equals("standbyGuest"))
           {
               cb.getConnection().Service.StandbyGuest(vmmor);
               nonTaskOp = true;
           }
           else
           {
               throw new Exception("Invaild power operation : " + powerOperation);

           }

            // If we get a valid task reference, monitor the task for success or failure
            // and report task completion or failure.
            if (taskmor != null) {
               cb.log.LogLine("Got Valid Task Reference");

               object[] result = 
                  cb.getServiceUtil().WaitForValues(
                     taskmor, new string[] { "info.state", "info.error" }, 
                     new string[] { "state" }, // info has a property - state for state of the task
                     new object[][] { new object[] { TaskInfoState.success, TaskInfoState.error } }
                  );

               // Wait till the task completes.
               if (result[0].Equals(TaskInfoState.success)) {
                   cb.log.LogLine("VmPowerOps : : Successful " + powerOperation 
                                  + " for VM : " 
                                  + vmName);
               } else {
                   cb.log.LogLine("VmPowerOps : Failed " + powerOperation 
                                  + " for VM : " 
                                  + vmName);
               }
            } else if (nonTaskOp) {
                cb.log.LogLine("VmPowerOps : Successful " + powerOperation 
                           + " for VM : "  
                           + vmName);
            }
         } catch (Exception e) {
             cb.log.LogLine("VmPowerOps : Failed " + powerOperation + " for VM : " + vmName);
             throw e;
         }
      }

      public static OptionSpec[] constructOptions()
      {
          OptionSpec[] useroptions = new OptionSpec[3];
          useroptions[0] = new OptionSpec("vmname", "String", 1
                                          , "Name of Virtual Machine"
                                          , null);
          useroptions[1] = new OptionSpec("operation", "String", 1
                                          , "Operation [on|off|suspend|reset|rebootGuest]"
                                          , null);
          useroptions[2] = new OptionSpec("hostname", "String", 1
                                          , "Name of the host system"
                                          , null);
          return useroptions;
      }
      /// <summary>
      /// The main entry point for the application.
      /// </summary>      
      public static void Main(String[] args)
      {
          VmPowerOps obj = new VmPowerOps();
          cb = AppUtil.AppUtil.initialize("VmPowerOps"
                                          , VmPowerOps.constructOptions()
                                          , args);
          cb.connect();
          obj.DoPowerOps();
          cb.disConnect();
      }
  }
}
