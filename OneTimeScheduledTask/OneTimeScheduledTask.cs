using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Globalization;
using System.Text;
using AppUtil;
using Vim25Api;

namespace OneTimeScheduledTask
{
    class OneTimeScheduledTask
    {
        private static AppUtil.AppUtil cb = null;   
   private ManagedObjectReference _svcRef; // Service Instance Reference
   private VimService _service;           // All webservice methods
   
   
   // ServiceContent contains References to commonly used
   // Managed Objects like PropertyCollector, SearchIndex, EventManager, etc.
   private ServiceContent _sic;            
   private ManagedObjectReference _searchIndex;
   private ManagedObjectReference _scheduleManager;

   /**
    * Initialize the necessary Managed Object References needed here
    */
   private void initialize() {
      _sic = cb.getConnection()._sic;
      _service = cb.getConnection()._service;
      // Get the SearchIndex and ScheduleManager references from ServiceContent
      _searchIndex = _sic.searchIndex;
      _scheduleManager = _sic.scheduledTaskManager;
   }
   
   private ManagedObjectReference _virtualMachine;
   
 
    
   private void findVirtualMachine()  {
      _virtualMachine 
         = cb.getServiceUtil().GetDecendentMoRef(null, 
                                                 "VirtualMachine", 
                                                 cb.get_option("vmname"));
   }
   
   /**
    * Create method action to power off a vm
    * 
    * @return the action to run when the schedule runs
    */
   private Vim25Api.Action createTaskAction() {
      MethodAction action = new MethodAction();
      
      // Method Name is the WSDL name of the 
      // ManagedObject's method that is to be run, 
      // in this Case, the powerOff method of the VM
      action.name = "PowerOffVM_Task";
      
      // There are no arguments to this method
      // so we pass in an empty MethodActionArgument
      action.argument= new MethodActionArgument[] { };
      return action;
   }
   
   /**
    * Create a Once task scheduler to run 30 minutes from now
    * 
    * @return one time task scheduler
    */
   private TaskScheduler createTaskScheduler() {
      // Create a Calendar Object and add 30 minutes to allow 
      // the Action to be run 30 minutes from now
      DateTime currentTime = new DateTime();
      currentTime = DateTime.Now;
      DateTime runTime = currentTime.AddMinutes(30);
      
      // Create a OnceTaskScheduler and set the time to
      // run tha Task Action at in the Scheduler. 
      OnceTaskScheduler scheduler = new OnceTaskScheduler();
      scheduler.runAt= runTime;
      
      return scheduler;
   }
   
   /**
    * Create a Scheduled Task using the poweroff method action and 
    * the onetime scheduler, for the VM found. 
    * 
    * @param taskAction action to be performed when schedule executes
    * @param scheduler the scheduler used to execute the action
    * @
    */
    
    
   private void createScheduledTask(Vim25Api.Action taskAction, 
                                   TaskScheduler scheduler) 
       {
      try {
         // Create the Scheduled Task Spec and set a unique task name
         // and description, and enable the task as soon as it is created
         String taskName = cb.get_option("taskname");
         ScheduledTaskSpec scheduleSpec = new ScheduledTaskSpec();
         scheduleSpec.name=taskName;
         scheduleSpec.description="PowerOff VM in 30 minutes";
         scheduleSpec.enabled=true;
         
         // Set the PowerOff Method Task Action and the 
         // Once scheduler in the spec 
         scheduleSpec.action=taskAction;
         scheduleSpec.scheduler=scheduler;
         
         // Create ScheduledTask for the VirtualMachine we found earlier
         if(_virtualMachine != null) {         
            ManagedObjectReference task = 
               _service.CreateScheduledTask(
                        _scheduleManager, _virtualMachine, scheduleSpec);
            // printout the MoRef id of the Scheduled Task
            Console.WriteLine("Successfully created Once Task: "
                               + taskName);
         }
         else {
            Console.WriteLine("Virtual Machine " + cb.get_option("vmname") 
                               + " not found");
            return;
         }         
      }
      catch (SoapException e)
      {
          if (e.Detail.FirstChild.LocalName.Equals("InvalidRequestFault"))
          {
              Console.WriteLine(" InvalidRequest: VMname may be wrong");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("DuplicateNameFault"))
          {
              Console.WriteLine("Error :Task Name already Exists");
          }
      }
      
      catch(Exception e){
         Console.WriteLine("Error");
         e.StackTrace.ToString();
      }                    
   }

   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[2];
      useroptions[0] = new OptionSpec("vmname","String",1
                                      ,"Name of Virtual Machine"
                                      ,null);
      useroptions[1] = new OptionSpec("taskname","String",1,
                                      "Name of the task to be scheduled",
                                      null);
      return useroptions;
   }
   /**
    *  The main entry point for the application.
    *  @param args Arguments: <url> <user> <password> <VMname> 
    */
   public static void Main(String[] args) {
      try {
         OneTimeScheduledTask schedTask = new OneTimeScheduledTask();
         cb = AppUtil.AppUtil.initialize("OneTimeScheduledTask"
                                 ,OneTimeScheduledTask.constructOptions()
                                 ,args);         
                
         // Connect to the Service and initialize required ManagedObjectReferences
         cb.connect();
         schedTask.initialize();
         
         // find VM by inventory path to create a scheduled task for
         schedTask.findVirtualMachine();
         
         // create the power Off action to be scheduled
         Vim25Api.Action taskAction = schedTask.createTaskAction();
         
         // create a One time scheduler to run
         TaskScheduler taskScheduler = schedTask.createTaskScheduler();
         
         // Create Scheduled Task
         schedTask.createScheduledTask(taskAction, taskScheduler);
         
         // Disconnect from the WebService
         cb.disConnect();
         Console.WriteLine("Press enter to exit: ");
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
