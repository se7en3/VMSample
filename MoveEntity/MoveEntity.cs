using System;
using System.Collections.Generic;
using System.Text;
using AppUtil;
using Vim25Api;

namespace MoveEntity
{
    public class MoveEntity
    {
        private static AppUtil.AppUtil cb = null;

   private void runOperation() {
      doMove();
   }

   private void doMove()  {
      String entityname = cb.get_option("entityname");
      String foldername = cb.get_option("foldername");
      ManagedObjectReference memor
         = cb.getServiceUtil().GetDecendentMoRef(null, "ManagedEntity", entityname);
      if (memor == null) {
         Console.WriteLine("Unable to find a Managed Entity '" + entityname
                            + "' in the Inventory");
         return;
      }
      ManagedObjectReference foldermor
         = cb.getServiceUtil().GetDecendentMoRef(null, "Folder", foldername);
      if (foldermor == null) {
         Console.WriteLine("Unable to find folder '" + foldername
            + "' in the Inventory");
         return;
      }
      else {
         try {
            ManagedObjectReference taskmor
                  =  cb.getConnection()._service.MoveIntoFolder_Task(foldermor,
               new ManagedObjectReference[]{memor});
            String status = cb.getServiceUtil().WaitForTask(taskmor);
            if(status.Equals("failure")) {
                Console.WriteLine("Failure -: Managed Entity cannot be moved");
            }

            if(status.Equals("sucess")) {
               Console.WriteLine("ManagedEntity '" + entityname +
                                  "' moved to folder '" + foldername
                                + "' successfully.");
            }

         }
         catch(Exception e) {
            Console.WriteLine("Error: " + e.Message.ToString());
            e.StackTrace.ToString();
         }
      }
   }
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[2];
      useroptions[0] = new OptionSpec("entityname","String",1
                                     ,"Name of the virtual entity"
                                     ,null);
      useroptions[1] = new OptionSpec("foldername","String",1,
                                      "Name of the folder",
                                      null);
      return useroptions;
   }
   public static void Main(String[] args)  {
      MoveEntity obj = new MoveEntity();
      cb = AppUtil.AppUtil.initialize("MoveEntity",MoveEntity.constructOptions(),args);
      cb.validate();
      cb.connect();
      obj.runOperation();
      cb.disConnect();
      Console.WriteLine("Press any key to exit: ");
      Console.Read();

   }
    }
}
