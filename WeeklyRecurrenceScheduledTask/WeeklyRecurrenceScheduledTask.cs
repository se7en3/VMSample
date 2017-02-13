using System;
using AppUtil;
using Vim25Api;
using System.Web.Services.Protocols;

namespace WeeklyRecurrenceScheduledTask
{
    public class WeeklyRecurrenceScheduledTask
    {
        private static AppUtil.AppUtil cb = null;

        private VimService _service;           // All webservice methods

        // ServiceContent contains References to commonly used
        // Managed Objects like PropertyCollector, SearchIndex, EventManager, etc.
        private ServiceContent _sic;

        private ManagedObjectReference _searchIndex;

        private ManagedObjectReference _scheduleManager;

        /**
         * Initialize the necessary Managed Object References needed here
         */
        private void initialize()
        {
            _sic = cb.getConnection()._sic;
            _service = cb.getConnection()._service;
            // Get the SearchIndex and ScheduleManager references from ServiceContent
            _searchIndex = _sic.searchIndex;
            _scheduleManager = _sic.scheduledTaskManager;
        }

        private ManagedObjectReference _virtualMachine;

        /**
         * Use the SearchIndex to find a VirtualMachine by Inventory Path
         * 
         * @
         */
        private void findVirtualMachine()
        {
            String vmPath = cb.get_option("vmpath");
            _virtualMachine = _service.FindByInventoryPath(_searchIndex, vmPath);
        }

        /**
         * Create method action to reboot the guest in a vm
         * 
         * @return the action to run when the schedule runs
         */
        private Vim25Api.Action createTaskAction()
        {
            MethodAction action = new MethodAction();

            // Method Name is the WSDL name of the 
            // ManagedObject's method to be run, in this Case, 
            // the rebootGuest method for the VM
            action.name = "RebootGuest";

            // There are no arguments to this method
            // so we pass in an empty MethodActionArgument
            action.argument = new MethodActionArgument[] { };

            return action;
        }

        /**
         * Create a Weekly task scheduler to run 
         * at 11:59 pm every Saturday
         * 
         * @return weekly task scheduler
         */
        private TaskScheduler createTaskScheduler()
        {
            WeeklyTaskScheduler scheduler = new WeeklyTaskScheduler();

            // Set the Day of the Week to be Saturday
            scheduler.saturday = true;

            // Set the Time to be 23:59 hours or 11:59 pm
            scheduler.hour = 23;
            scheduler.minute = 59;

            // set the interval to 1 to run the task only 
            // Once every Week at the specified time
            scheduler.interval = 1;

            return scheduler;
        }

        /**
         * Create a Scheduled Task using the reboot method action and 
         * the weekly scheduler, for the VM found. 
         * 
         * @param taskAction action to be performed when schedule executes
         * @param scheduler the scheduler used to execute the action
         * @
         */
        private void createScheduledTask(Vim25Api.Action taskAction,
                                        TaskScheduler scheduler)
        {
            try
            {
                // Create the Scheduled Task Spec and set a unique task name
                // and description, and enable the task as soon as it is created
                String taskName = cb.get_option("taskname");
                ScheduledTaskSpec scheduleSpec = new ScheduledTaskSpec();
                scheduleSpec.name = taskName;
                scheduleSpec.description = "Reboot VM's Guest at 11.59pm every Saturday";
                scheduleSpec.enabled = true;

                // Set the RebootGuest Method Task action and 
                // the Weekly scheduler in the spec 
                scheduleSpec.action = taskAction;
                scheduleSpec.scheduler = scheduler;

                // Create the ScheduledTask for the VirtualMachine we found earlier
                ManagedObjectReference task =
                   _service.CreateScheduledTask(
                            _scheduleManager, _virtualMachine, scheduleSpec);

                // printout the MoRef id of the Scheduled Task
                Console.WriteLine("Successfully created Weekly Task: " +
                                   taskName);
            }
            catch (SoapException e)
            {
                if (e.Detail.FirstChild.LocalName.Equals("InvalidRequestFault"))
                {
                    Console.WriteLine(" InvalidRequest: vmPath may be wrong");
                }
                else if (e.Detail.FirstChild.LocalName.Equals("DuplicateNameFault"))
                {
                    Console.WriteLine("Task Name already Exists");
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Error");
                e.StackTrace.ToString();
            }
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("vmpath", "String", 1
                                            , "VM Inventory Path"
                                            , null);
            useroptions[1] = new OptionSpec("taskname", "String", 1,
                                            "Name of the task to be scheduled",
                                            null);
            return useroptions;
        }

        /**
         *  The main entry point for the application.
          *  @param args Arguments: <url> <user> <password> <A VM Inventory Path> 
         */
        public static void Main(String[] args)
        {
            try
            {


                WeeklyRecurrenceScheduledTask schedTask = new WeeklyRecurrenceScheduledTask();
                cb = AppUtil.AppUtil.initialize("WeeklyRecurrenceScheduledTask"
                                        , WeeklyRecurrenceScheduledTask.constructOptions()
                                        , args);

                // Connect to the Service and initialize 
                // any required ManagedObjectReferences
                cb.connect();
                schedTask.initialize();

                // find the VM by dns name to create a scheduled task for
                schedTask.findVirtualMachine();

                // create the power Off action to be scheduled
                Vim25Api.Action taskAction = schedTask.createTaskAction();

                // create a One time scheduler to run
                TaskScheduler taskScheduler = schedTask.createTaskScheduler();

                // Create Scheduled Task
                schedTask.createScheduledTask(taskAction,
                                              taskScheduler);

                // Disconnect from the WebService
                cb.disConnect();
                Console.WriteLine("Press any key to exit: ");
                Console.Read();

            }
            catch (Exception e)
            {
                Console.WriteLine("Caught Exception : " +
                                   " Name : " + e.Data.ToString() +
                                 " Message : " + e.Message.ToString() +
                                 " Trace : ");
                e.StackTrace.ToString();
            }
        }
    }
}
