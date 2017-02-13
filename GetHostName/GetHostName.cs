using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppUtil;
using Vim25Api;
using System.Collections;

namespace GetHostName
{
    public class GetHostName
    {
        private static AppUtil.AppUtil cb = null;
        private static List<string> hostSystemAttributesArr = new List<string>();


        private static void SetHostSystemAttributesList()
        {
            hostSystemAttributesArr.Add("name");
            hostSystemAttributesArr.Add("config.product.productLineId");
            hostSystemAttributesArr.Add("summary.hardware.cpuMhz");
            hostSystemAttributesArr.Add("summary.hardware.numCpuCores");
            hostSystemAttributesArr.Add("summary.hardware.cpuModel");
            hostSystemAttributesArr.Add("summary.hardware.uuid");
            hostSystemAttributesArr.Add("summary.hardware.vendor");
            hostSystemAttributesArr.Add("summary.hardware.model");
            hostSystemAttributesArr.Add("summary.hardware.memorySize");
            hostSystemAttributesArr.Add("summary.hardware.numNics");
            hostSystemAttributesArr.Add("summary.config.name");
            hostSystemAttributesArr.Add("summary.config.product.osType");
            hostSystemAttributesArr.Add("summary.config.vmotionEnabled");
            hostSystemAttributesArr.Add("summary.quickStats.overallCpuUsage");
            hostSystemAttributesArr.Add("summary.quickStats.overallMemoryUsage");
        }

        private void PrintHostProductDetails()
        {
            SetHostSystemAttributesList();
            string prop = null;
            Dictionary<ManagedObjectReference, Dictionary<string, object>> hosts = 
                cb._svcUtil.getEntitiesByType("HostSystem", hostSystemAttributesArr.ToArray());
            foreach (KeyValuePair<ManagedObjectReference, Dictionary<string, object>> host in hosts)
            {
                foreach (KeyValuePair<string, object> hostProps in host.Value)
                {
                    prop = hostProps.Key;
                    Console.WriteLine(prop + " : " + hostProps.Value);
                }
                Console.WriteLine("***************************************************************");
            }
        }

        public static void Main(string[] args)
        {
            try
            {
                GetHostName app = new GetHostName();
                cb = AppUtil.AppUtil.initialize("GetHostName",
                                        null,
                                        args);
                cb.connect();
                app.PrintHostProductDetails();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            cb.disConnect();
            Console.Read();
        }
    }
}
