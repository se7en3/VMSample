using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace Delete
{
    public class Delete
    {
        private AppUtil.AppUtil cb = null;

        Log log = new Log();

        public Delete(string[] args)
        {
            cb = AppUtil.AppUtil.initialize("Delete", this.ConstructOptions(), args);
        }

        public String GetMeName()
        {
            return cb.get_option("meName");
        }

        private OptionSpec[] ConstructOptions()
        {
            OptionSpec[] useroptions = 
            {
                new OptionSpec("meName", "String", 1, 
                    "Virtual Machine|ClusterComputeResource|folder", null)
            };

            return useroptions;
        }

        public bool DeleteManagedEntity()
        {
            cb.connect();

            bool deleteResult = false;

            try
            {
                ManagedObjectReference memor =
                    cb.getServiceUtil().GetDecendentMoRef(null, "ManagedEntity", this.GetMeName());

                if (memor == null)
                {
                    var errorMessage = string.Format(
                        "Unable to find a managed entity named '{0}' in Inventory", this.GetMeName());

                    Console.WriteLine(errorMessage);
                    log.LogLine(errorMessage);

                    return false;
                }

                ManagedObjectReference taskmor
                   = cb.getConnection()._service.Destroy_Task(memor);

                // If we get a valid task reference, monitor the task for success or failure
                // and report task completion or failure.
                if (taskmor != null)
                {
                    Object[] result =
                    cb.getServiceUtil().WaitForValues(
                       taskmor, new String[] { "info.state", "info.error" },
                       new String[] { "state" }, // info has a property - 
                        //state for state of the task
                       new Object[][] { new Object[] { 
                     TaskInfoState.success, TaskInfoState.error } 
                  }
                    );

                    // Wait till the task completes.
                    if (result[0].Equals(TaskInfoState.success))
                    {
                        log.LogLine(cb.getAppName() + " : Successful delete of Managed Entity : "
                                  + this.GetMeName());

                        deleteResult = true;
                    }
                    else
                    {
                        log.LogLine(cb.getAppName() + " : Failed delete of Managed Entity : "
                                  + this.GetMeName());
                        if (result.Length == 2 && result[1] != null)
                        {
                            if (result[1].GetType().Equals("MethodFault"))
                            {
                                cb.getUtil().LogException((Exception)result[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                cb.getUtil().LogException(e);
                log.LogLine(cb.getAppName() + " : Failed delete of Managed Entity : "
                          + this.GetMeName());
                throw e;
            }
            finally
            {
                cb.disConnect();
            }

            return deleteResult;
        }

        public static void Main(String[] args)
        {
            try
            {
                var deleteSample = new Delete(args);
                var status = deleteSample.DeleteManagedEntity();

                if (status)
                {
                    Console.WriteLine("Successfully deleted object {0}",
                        deleteSample.GetMeName());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Press <Enter> to exit...");
            Console.Read();
        }
    }
}
