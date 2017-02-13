using System;
using System.Collections.Generic;
using System.Text;
using AppUtil;
using Vim25Api;

namespace RenameEntity
{
    public class RenameEntity
    {
        private static AppUtil.AppUtil cb = null;

        public RenameEntity(string[] args)
        {
            cb = AppUtil.AppUtil.initialize("RenameEntity"
                                    , RenameEntity.ConstructOptions()
                                    , args);
        }

        public string GetCurrentName()
        {
            return cb.get_option("entityname");
        }

        public string GetNewName()
        {
            return cb.get_option("newname");
        }

        public bool RenameManagedEntity()
        {
            try
            {
                cb.connect();

                String entityname = this.GetCurrentName();
                String newname = this.GetNewName();

                ManagedObjectReference memor
                    = cb.getServiceUtil().GetDecendentMoRef(null, "ManagedEntity", entityname);

                if (memor != null)
                {
                    ManagedObjectReference taskmor
                       = cb.getConnection()._service.Rename_Task(memor, newname);

                    String status = cb.getServiceUtil().WaitForTask(taskmor);

                    if (status.Equals("sucess"))
                    {
                        Console.WriteLine("Managed entity '{0}' renamed to '{1}' successfully",
                            entityname, newname);

                        return true;
                    }
                    else
                    {
                        Console.WriteLine(
                            "Could not rename managed entity from '{0}' to '{1}'", 
                            entityname, newname);
                    }
                }
                else
                {
                    Console.WriteLine(
                        "Unable to find managed entity '{0}' in the Inventory", entityname);
                }

                return false;
            }
            finally
            {
                cb.disConnect();
            }
        }

        private static OptionSpec[] ConstructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("entityname", "String", 1
                                           , "Name of the virtual entity"
                                           , null);
            useroptions[1] = new OptionSpec("newname", "String", 1,
                                            "New name of the virtual entity",
                                            null);
            return useroptions;
        }

        public static void Main(String[] args)
        {
            try
            {
                var renameSample = new RenameEntity(args);
                renameSample.RenameManagedEntity();
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