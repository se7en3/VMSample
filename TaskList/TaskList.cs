using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text;
using AppUtil;
using Vim25Api;

namespace TaskList
{
    public class TaskList
    {
       private static AppUtil.AppUtil cb = null;    
   private PropertyFilterSpec[] createPFSForRecentTasks(
      ManagedObjectReference taskManagerRef) {      
      PropertySpec pSpec = new PropertySpec();
      pSpec.all= false;
      pSpec.type="Task";
      pSpec.pathSet=
            new String[]
           {"info.entity",
            "info.entityName",
            "info.name",
            "info.state",
            "info.cancelled",
            "info.error"};
      
      ObjectSpec oSpec = new ObjectSpec();
      oSpec.obj = taskManagerRef;
      oSpec.skip= false;
      oSpec.skipSpecified = true;
      
      TraversalSpec tSpec = new TraversalSpec();
      tSpec.type="TaskManager";
      tSpec.path="recentTask";
      tSpec.skip= false;
            
      
      oSpec.selectSet=new SelectionSpec[]{tSpec};      
      
      PropertyFilterSpec pfSpec = new PropertyFilterSpec();      
      pfSpec.propSet=new PropertySpec[]{pSpec};      
      pfSpec.objectSet=new ObjectSpec[]{oSpec};
      
      return new PropertyFilterSpec[]{pfSpec};
   }
   
   private void displayTasks(ObjectContent[] oContents) {      
      for(int oci=0; oci<oContents.Length; ++oci) {
         Console.WriteLine("Task");
         DynamicProperty[] dps = oContents[oci].propSet;
         if(dps!=null) {
            String op="", name="", type="", state="", error="";
            for(int dpi=0; dpi<dps.Length; ++dpi) {               
               DynamicProperty dp = dps[dpi];
               if("info.entity".Equals(dp.name)) {
                  type = ((ManagedObjectReference)dp.val).GetType().ToString();
               } else if ("info.entityName".Equals(dp.name)) {
                  name = ((String)dp.val);
               } else if ("info.name".Equals(dp.name)) {
                  op = ((String)dp.val);
               } else if ("info.state".Equals(dp.name)) {
                  TaskInfoState tis = (TaskInfoState)dp.val;
                  if(TaskInfoState.error.Equals(tis)) {
                     state = "-Error";
                  } else if(TaskInfoState.queued.Equals(tis)) {
                     state = "-Queued";
                  } else if(TaskInfoState.running.Equals(tis)) {
                     state = "-Running";
                  } else if(TaskInfoState.success.Equals(tis)) {
                     state = "-Success";
                  }
               } else if ("info.cancelled".Equals(dp.name)) {
                  Boolean b = (Boolean)dp.val;
                   //I need to chk
                  if(b != null ) {
                     state += "-Cancelled";
                  }
               } else if ("info.error".Equals(dp.name)) {
                  LocalizedMethodFault mf = (LocalizedMethodFault)dp.val;
                  if(mf != null) {
                     error = mf.localizedMessage;
                  }
               } else {
                  op = "Got unexpected property: "+dp.name
                      +" Value: "+dp.val.ToString();
               }
            }
            Console.WriteLine("Operation " + op);
            Console.WriteLine("Name " + name);
            Console.WriteLine("Type " + type);
            Console.WriteLine("State " + state);
            Console.WriteLine("Error " + error);
            Console.WriteLine("======================");
         }
      }
      if(oContents.Length == 0) {
         Console.WriteLine("Currently no task running");
      }
   }
   public static void Main(String [] args) {
      TaskList obj = new TaskList();
      cb = AppUtil.AppUtil.initialize("TaskList", args);      
      cb.connect();      
      
      PropertyFilterSpec [] pfs;
      ManagedObjectReference taskManagerRef 
         = cb.getConnection()._sic.taskManager; 
      pfs = obj.createPFSForRecentTasks(taskManagerRef);

      ManagedObjectReference propColl = cb.getConnection().PropCol;      
      ObjectContent[] oContents
         = cb._svcUtil.retrievePropertiesEx(propColl, pfs);
      if(oContents != null) {
         obj.displayTasks(oContents);         
      }
      else {
         Console.WriteLine("Currently no task running");
      }      
      cb.disConnect();
       Console.WriteLine("Press enter to exit: ");
       Console.Read();
      
   }  
    }
}
