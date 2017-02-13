using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text;
using AppUtil;
using Vim25Api;

namespace DeleteOneTimeScheduledTask
{
    public class DeleteOneTimeScheduledTask
    {
      private static AppUtil.AppUtil cb = null;   
   
      private VimService _service;           // All webservice methods
   
   // ServiceContent contains References to commonly used
   // Managed Objects like PropertyCollector, SearchIndex, EventManager, etc.
   private ServiceContent _sic;            
   private ManagedObjectReference _propCol;
   private ManagedObjectReference _scheduleManager;   

   /**
    * Initialize the necessary Managed Object References needed here
    */
   private void initialize() {
      _sic = cb.getConnection()._sic;
      _service = cb.getConnection()._service;
      // Get the PropertyCollector and ScheduleManager references from ServiceContent
      _propCol = _sic.propertyCollector;
      _scheduleManager = _sic.scheduledTaskManager;
   }
   
   /**
   * Create Property Collector Filter to get names of all 
   * ScheduledTasks the ScheduledTaskManager has.
   * 
   * @return PropertyFilterSpec to get properties
   */
   private PropertyFilterSpec createTaskPropertyFilterSpec() {
      // The traversal spec traverses the "scheduledTask" property of 
      // ScheduledTaskManager to get names of ScheduledTask ManagedEntities
      // A Traversal Spec allows traversal into a ManagedObjects 
      // using a single attribute of the managedObject
      TraversalSpec scheduledTaskTraversal =  new TraversalSpec(); 
      
      scheduledTaskTraversal.type=_scheduleManager.type;
      scheduledTaskTraversal.path= "scheduledTask";
      
      // We want to get values of the scheduleTask property
      // of the scheduledTaskManager, which are the ScheduledTasks
      // so we set skip = false. 
      scheduledTaskTraversal.skip= false;
       scheduledTaskTraversal.skipSpecified = true;
      
      // no further traversal needed once we get to scheduled task list
      scheduledTaskTraversal.selectSet=new SelectionSpec[] { };
      
      scheduledTaskTraversal.name="scheduleManagerToScheduledTasks";
      
      // Setup a PropertySpec to return names of Scheduled Tasks so 
      // we can find the named ScheduleTask ManagedEntity to delete
      // Name is an attribute of ScheduledTaskInfo so 
      // the path set will contain "info.name"
      PropertySpec propSpec = new PropertySpec(); 
      propSpec.all= false;
      propSpec.allSpecified= true;
      propSpec.pathSet= new String[] { "info.name" };
      propSpec.type="ScheduledTask";
      
      // PropertySpecs are wrapped in a PropertySpec array
      // since we only have a propertySpec for the ScheduledTask,
      // the only values we will get back are names of scheduledTasks
      PropertySpec[] propSpecArray = new PropertySpec[] { propSpec };
      
      // Create an Object Spec to specify the starting or root object
      // and the SelectionSpec to traverse to each ScheduledTask in the
      // array of scheduledTasks in the ScheduleManager
      ObjectSpec objSpec = new ObjectSpec();
      objSpec.obj=_scheduleManager;
      objSpec.selectSet= new SelectionSpec[] { scheduledTaskTraversal } ;
      
      // Set skip = true so properties of ScheduledTaskManager 
      // are not returned, and only values of info.name property of 
      // each ScheduledTask is returned
      objSpec.skip = true;
      objSpec.skipSpecified= true;

      // ObjectSpecs used in PropertyFilterSpec are wrapped in an array
      ObjectSpec[] objSpecArray = new ObjectSpec[] { objSpec };
      
      // Create the PropertyFilter spec with 
      // ScheduledTaskManager as "root" object
      PropertyFilterSpec spec = new PropertyFilterSpec();
      spec.propSet = propSpecArray;
      spec.objectSet= objSpecArray;
      return spec;
   }
   
   /**
    * Find the Scheduled Task to be deleted
    * 
    * @return ManagedObjectReference of the OneTimeScheduled Task
    * @ an reported exceptions
    */
   private ManagedObjectReference findOneTimeScheduledTask(
            PropertyFilterSpec scheduledTaskSpec ) 
       {
      String findTaskName =  cb.get_option("taskname");
      Boolean found = false;
      ManagedObjectReference oneTimeTask = null;
      
      // Use PropertyCollector to get all scheduled tasks the 
      // ScheduleManager has
      ObjectContent[] scheduledTasks =
         cb._svcUtil.retrievePropertiesEx(_propCol, 
                                     new PropertyFilterSpec[] { scheduledTaskSpec });

      // Find the task name we're looking for and return the 
      // ManagedObjectReference for the ScheduledTask with the 
      // name that matched the name of the OneTimeTask created earlier
      if(scheduledTasks != null) {
         for (int i = 0; i < scheduledTasks.Length && !found; i++) {
            ObjectContent taskContent = scheduledTasks[i];
            DynamicProperty[] props = taskContent.propSet;
            for (int p = 0; p < props.Length && !found; p++) {
               DynamicProperty prop = props[p];
               String taskName = (String)prop.val;
               if (taskName.Equals(findTaskName)) {
                  oneTimeTask = taskContent.obj;
                  found = true;
               }
            }
         }
      }
      if(!found) {
         Console.WriteLine("Scheduled task '" + findTaskName 
                            + "' not found");
      }
      return oneTimeTask;
   }

   /**
   * Delete a Scheduled Task
   * 
   * @param oneTimeTask the ManagedObjectReference of task to delete
   * @
   */
   private void deleteScheduledTask(ManagedObjectReference oneTimeTask) 
       {
      try {
         
         // Remove the One Time Scheduled Task 
         
         _service.RemoveScheduledTask(oneTimeTask);
         Console.WriteLine("Successfully Deleted ScheduledTask: " +
                            cb.get_option("taskname"));
      }
      catch(SoapException e){
         Console.WriteLine(" InvalidRequest: Task Name may be wrong");
      }
      catch(Exception e){
         Console.WriteLine("Error");
         e.StackTrace.ToString();
      }     
   }
   
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[1];
      useroptions[0] = new OptionSpec("taskname","String",1
                                      ,"Name of the task to be scheduled"
                                      ,null);
      return useroptions;
   }
   
   /**
   *  The main entry point for the application.
   *  @param args Arguments: <url> <user> <password> 
   */   
   public static void Main(String[] args) {
   try {
      DeleteOneTimeScheduledTask schedTask = 
         new DeleteOneTimeScheduledTask();
      cb = AppUtil.AppUtil.initialize("DeleteOneTimeScheduledTask"
                               ,DeleteOneTimeScheduledTask.constructOptions()
                               ,args);
      
      // Connect to the Service and initialize any required ManagedObjectReferences
      cb.connect();
      schedTask.initialize();
      
      // create a Property Filter Spec to get names 
      // of all scheduled tasks
      PropertyFilterSpec taskFilterSpec = 
         schedTask.createTaskPropertyFilterSpec();
      
      // Retrieve names of all ScheduledTasks and find 
      // the named one time Scheduled Task
      ManagedObjectReference oneTimeTask = 
         schedTask.findOneTimeScheduledTask(taskFilterSpec);
      
      // Delete the one time scheduled task 
      if(oneTimeTask != null) {
         schedTask.deleteScheduledTask(oneTimeTask);
      }     
      
      // Disconnect from the WebService
      cb.disConnect();
       Console.WriteLine("Press any key to exit: ");
       Console.Read();
      } 
      catch (Exception e) {
         Console.WriteLine("Caught Exception : " +
                             " Name : " + e.Data.ToString() +
                            " Message : " + e.Message.ToString() +
                            " Trace : ");
         e.StackTrace.ToString();
         
      }
   }
    }
}
