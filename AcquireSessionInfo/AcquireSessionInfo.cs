using System;
using System.Text;
using AppUtil;
using Vim25Api;
using System.IO;

namespace AcquireSessionInfo
{
    ///<summary>
    ///This sample will acquire a session with VC or ESX and print a cim service ticket and related
    /// session information to a file
    ///</summary>
    ///<param name="host">Required: Name of host</param>
    /// <param name="info">Optional: Type of info required only [cimticket] for now</param>
    /// <param name="file">Optional: Full path of the file to save data to</param>
    ///
    ///List storage recommnedation
    ///--url [webserviceurl]
    ///--username [username] --password [password] --host [hostname]
    /// --info [cimticket] --file [path_to_file]
    ///</remarks>
    public class AcquireSessionInfo
    {
        private static AppUtil.AppUtil cb = null;

        private void AcquireInfo()
        {
            string hostName = cb.get_option("host");
            string info = cb.get_option("info");
            string fileName = cb.get_option("file");
            ManagedObjectReference hostmor = cb._svcUtil.getEntityByName("HostSystem", hostName);
            if (hostmor == null)
            {
                String msg = "Failure: Host [" + hostName + "] not found";
                throw new Exception(msg);
            }
            if ((info == null) || (info.Equals("cimticket")))
            {
                HostServiceTicket serviceTicket =
                      cb._connection._service.AcquireCimServicesTicket(hostmor);
                if (serviceTicket != null)
                {
                    String datatoWrite = StringToWrite(serviceTicket);
                    WriteToFile(datatoWrite, fileName);
                }
            }
            else
            {
                Console.WriteLine("Support for info " + info + " not implemented.");
            }
        }

        private void WriteToFile(string datatoWrite, string fileName)
        {            
            File.WriteAllText(fileName, datatoWrite);
            Console.WriteLine("saved in a file " + fileName);

        }

        private string StringToWrite(HostServiceTicket serviceTicket)
        {
            StringBuilder datatowrite = new StringBuilder("");
            datatowrite.Append("CIM Host Service Ticket Information");
            datatowrite.Append(System.Environment.NewLine);
            datatowrite.Append("Service        : ");
            datatowrite.Append(serviceTicket.service);
            datatowrite.Append(System.Environment.NewLine);
            datatowrite.Append("Service Version: ");
            datatowrite.Append(serviceTicket.serviceVersion);
            datatowrite.Append(System.Environment.NewLine);
            datatowrite.Append("Session Id     : ");
            datatowrite.Append(serviceTicket.sessionId);
            datatowrite.Append(System.Environment.NewLine);
            datatowrite.Append("SSL Thumbprint : ");
            datatowrite.Append(serviceTicket.sslThumbprint);
            datatowrite.Append(System.Environment.NewLine);
            datatowrite.Append("Host           : ");
            datatowrite.Append(serviceTicket.host);
            datatowrite.Append(System.Environment.NewLine);
            datatowrite.Append("Port           : ");
            datatowrite.Append(serviceTicket.port != 0 ? serviceTicket.port.ToString() : "");
            datatowrite.Append(System.Environment.NewLine);
            Console.WriteLine(datatowrite.ToString());
            return datatowrite.ToString();
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[3];
            useroptions[0] = new OptionSpec("host", "String", 1
                                           , "Name of the host"
                                           , null);
            useroptions[1] = new OptionSpec("info", "String", 0,
                                            "Type of info required,only [cimticket] for now",
                                            "cimticket");
            useroptions[2] = new OptionSpec("file", "String", 0,
                                            "Full path of the file to save data to"
                                            , "cimTicketInfo.txt");
            return useroptions;
        }
        public static void Main(String[] args)
        {
            AcquireSessionInfo app = new AcquireSessionInfo();
            cb = AppUtil.AppUtil.initialize("AcquireSessionInfo",
                                    AcquireSessionInfo.constructOptions(),
                                    args);
            try
            {
                cb.connect();
                app.AcquireInfo();
                cb.disConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.WriteLine("Please enter any key to exit: ");
            Console.Read();
        }
    }
}
