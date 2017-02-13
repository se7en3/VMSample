using System;
using System.Web.Services;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Security.Principal;
using System.Net;


namespace HostPowerOp
{   ///<summary>
    ///This sample is used to put the host in standByMode, shut down host and reboot the host.
    ///</summary>
    ///<param name="hostname">Required: Name of the host</param>
    ///<param name="operation">Required: Name of the operation[reboot|shutdown |powerdowntostandby </param>
    ///<remarks>
    /// Used to shut down , reboot the host and put host in standby. 
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --hostname[hostname]
    ///--operation [operation]
    ///</remarks>

    class HostPowerOp
    {
        static Vim25Api.VimService _service;
        static ServiceContent _sic;
        private static AppUtil.AppUtil cb = null;


        public void PowerDownHost(String[] args)
        {
            _service = cb.getConnection().Service;
            _sic = cb.getConnection().ServiceContent;
            String hostname = cb.get_option("hostname");
            ManagedObjectReference hmor =
                  cb.getServiceUtil().GetDecendentMoRef(null, "HostSystem", hostname);
            if (hmor != null)
            {
                if (cb.get_option("operation").Equals("reboot"))
                {
                    ManagedObjectReference taskmor
                       = _service.RebootHost_Task(hmor, true);
                    String result = cb.getServiceUtil().WaitForTask(taskmor);
                    if (result.Equals("sucess"))
                    {
                        Console.WriteLine("Operation reboot host"
                                           + " completed sucessfully");
                    }
                }
                else if (cb.get_option("operation").Equals("shutdown"))
                {
                    ManagedObjectReference taskmor
                       = _service.ShutdownHost_Task(hmor, true);
                    String result = cb.getServiceUtil().WaitForTask(taskmor);
                    if (result.Equals("sucess"))
                    {
                        Console.WriteLine("Operation shutdown host"
                                           + " completed sucessfully");
                    }
                }
                else if (cb.get_option("operation").Equals("powerdowntostandby"))
                {
                        ManagedObjectReference taskmor = _service.
                            PowerDownHostToStandBy_Task(hmor, 120, false, true);
                        String result = cb.getServiceUtil().WaitForTask(taskmor);
                        if (result.Equals("sucess"))
                        {
                            Console.WriteLine("Operation powerDownHostToStandBy"
                                     + " completed sucessfully");
                        }
                }
            }
            else
            {
                Console.WriteLine("Host " + cb.get_option("hostname") + " not found");
            }
        }


        private Boolean customValidation()
        {
            Boolean flag = true;
            String operation = cb.get_option("operation");
            if ((!operation.Equals("reboot")) && (!operation.Equals("shutdown"))
               && (!operation.Equals("powerdowntostandby")))
            {
                Console.WriteLine("Invalid operations ; [reboot | shutdown | powerdowntostandby]");
                flag = false;
            }
            return flag;
        }

        public static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("hostname", "String", 1
                                            , "Name of the host"
                                            , null);
            useroptions[1] = new OptionSpec("operation", "String", 1
                                            , "Name of the operation"
                                            , null);
            return useroptions;
        }

        public static void Main(String[] args)
        {
            HostPowerOp obj = new HostPowerOp();
            cb = AppUtil.AppUtil.initialize("PowerDownHostToStandBy"
                                       , HostPowerOp.constructOptions()
                                       , args);
            Boolean valid = obj.customValidation();
            if (valid)
            {
                cb.connect();
                obj.PowerDownHost(args);
                cb.disConnect();
                Console.WriteLine("Press any key to exit: ");
                Console.Read();
                Environment.Exit(1);
            }
        }
    }
}
