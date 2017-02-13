using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace RemoveManagedObject
{
   public class RemoveManagedObject
    {
       private static  String STR_REMOVE = "remove";
       private static  String STR_UNREGISTER = "unregisterVM";

   private static AppUtil.AppUtil cb = null;
     
   private void validate(){
      String objType = cb.get_option("objtype");
      try{
         if(cb.option_is_set("operation")){
            String operation = cb.get_option("operation");   
            if(!(operation.Equals("remove")) 
                  && (!(operation.Equals("unregister"))) ){
               Console.WriteLine("Invalid Operation type");
               cb.displayUsage();
               throw new ArgumentHandlingException("Invalid Operation type");
            }   
         }
         if(objType.Equals("HostSystem") 
               ||objType.Equals("VirtualMachine") 
               || objType.Equals("Folder") 
               || objType.Equals("ResourcePool")
               || objType.Equals("Datacenter") ){                     
         }
         else{
            Console.WriteLine("Invalid Obj Type type " +objType);
            cb.displayUsage();
            throw new ArgumentHandlingException("Invalid obj type type");  
         }
      }    
      catch(Exception e){
         throw e;
      }   
   }
   
   private String getRemoveOp()   {
      String operation = cb.get_option("operation");
      String objType = cb.get_option("objtype");
      if ((operation == null || operation.Length == 0) 
            && (objType.Equals("VirtualMachine")) ) {
         operation = STR_UNREGISTER;
      }
      else if((operation == null || operation.Length == 0) 
             && !(objType.Equals("VirtualMachine"))) {
          operation = STR_REMOVE;
      }
      else {
         if (!(STR_REMOVE.Equals(operation)) 
               && !(STR_UNREGISTER.Equals(operation))) {
            operation = STR_UNREGISTER;
         }
      }
      return operation;
   }

   private void runOperation()  {
      doRemove();
   }

   private void doRemove()  {
     String objType = cb.get_option("objtype");
     String objName = cb.get_option("objname");
     String remOpStr = getRemoveOp();
      try {
         ManagedObjectReference objmor = 
            cb.getServiceUtil().GetDecendentMoRef(null, objType, objName);

         if (objmor != null) {
            if (STR_REMOVE.Equals(remOpStr)) {
               ManagedObjectReference taskmor 
                  = cb.getConnection()._service.Destroy_Task(objmor);
               String status = cb.getServiceUtil().WaitForTask(taskmor);
               if(status.Equals("failure")) {
                  Console.WriteLine("Failure -: Managed Entity Cannot Be Removed");
               }
               else if (status.Equals("The operation is not supported on the object.") && objType.Equals("HostSystem"))
               {
                   Console.WriteLine("Failure -: HostSystem Cannot Be Removed, this operation is supported to Remove Host From Cluster only");
               }
               else
               {
                   Console.WriteLine("Successful " + remOpStr + " of "
                             + objType + " : " + objName);
               }
            } 
            else if ("VirtualMachine".Equals(objType)) {
                try
                {
                    cb.getConnection()._service.UnregisterVM(objmor);
                    Console.WriteLine("Successful " + remOpStr + " of "
                             + objType + " : " + objName);
                }
                catch (SoapException e)
                {
                    if (e.Detail.FirstChild.LocalName.Equals("InvalidPowerState"))
                    {
                        Console.WriteLine("Invalid power state");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error");
                    e.StackTrace.ToString();
                    return;
                }
            } 
            else {
               throw new Exception("Invalid Operation specified.");
            }
           
         } else {
            Console.WriteLine("Unable to find object of type  " + objType 
                             + " with name  " + objName);
            Console.WriteLine(cb.getAppName() + " : Failed " + remOpStr 
                             + " of " + objType + " : " + objName);
         }
      } 
      catch (Exception e) {
         Console.WriteLine("Error");
         throw e;
      }
      
   }
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[3];
      useroptions[0] 
         = new OptionSpec("objtype","String",1
                         ,"type of object on which operation is to be performed " +
                        "e.g. HostSystem , VirtualMachine, Datacenter, ResourcePool, " +
                         "Folder"
                                     ,null);
      useroptions[1] = new OptionSpec("objname","String",1,
                                      "Name of the object",
                                      null);
      useroptions[2] = new OptionSpec("operation","String",0,
                                      "operation name remove/unregister",
                                      null);
      return useroptions;
   }
   public static void Main(String[] args)  {
      RemoveManagedObject remmor = new RemoveManagedObject();
      cb = AppUtil.AppUtil.initialize("RemoveManagedObject",
                              RemoveManagedObject.constructOptions(),
                              args);
      cb.connect();
      remmor.validate();
      remmor.runOperation();
      cb.disConnect();
      Console.WriteLine("Please enter to exit.");
      Console.Read();
   }
    }
}
