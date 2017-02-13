using System;
using System.Collections;
using System.Text;
using System.Net;
using AppUtil;
using Vim25Api;

namespace QueryMemoryOverhead
{
   public class QueryMemoryOverheadV25
   {
       static Vim25Api.VimService _service;
       static ServiceContent _sic;
       private static AppUtil.AppUtil ecb = null;

       private Boolean customValidation()
       {
           Boolean flag = true;
           if (int.Parse(ecb.get_option("memorysize")) <= 0)
           {
               Console.WriteLine("Memory size must be greater than zero");
               flag = false;
           }
           if (int.Parse(ecb.get_option("cpucount")) <= 0)
           {
               Console.WriteLine("cpucount must be greater than zero");
               flag = false;
           }
           return flag;
       }

       public static OptionSpec[] constructOptions()
       {
           OptionSpec[] useroptions = new OptionSpec[3];
           useroptions[0] = new OptionSpec("hostname", "String", 1
                                           , "Name of the host"
                                           , null);
           useroptions[1] = new OptionSpec("memorysize", "Integer", 1,
                                           "Size of the memory",
                                           null);
           useroptions[2] = new OptionSpec("cpucount", "Integer", 1,
                                           "Number of the CPU count",
                                           null);
           return useroptions;
       }

       public static void Main(String[] args)
       {
           QueryMemoryOverheadV25 obj = new QueryMemoryOverheadV25();
           ecb = AppUtil.AppUtil.initialize("QueryMemoryOverheadV25"
                                                   , QueryMemoryOverheadV25.constructOptions()
                                                   , args);
           Boolean valid = obj.customValidation();
           if (valid)
           {
               ecb.connect();
               obj.queryMemoryOverhead();
               ecb.disConnect();
               Console.WriteLine("Press any key to exit: ");
               Console.Read();
           }
       }

       public void queryMemoryOverhead()
       {
           _service = ecb.getConnection().Service;
           _sic = ecb.getConnection().ServiceContent;

           ArrayList supportedVersions = VersionUtil.getSupportedVersions(ecb.get_option("url"));
           String hostname = ecb.get_option("hostname");
           ManagedObjectReference hmor =
              ecb.getServiceUtil().GetDecendentMoRef(null, "HostSystem", hostname);

           if (hmor != null)
           {
               if (VersionUtil.isApiVersionSupported(supportedVersions, "2.5"))
               {
                   VirtualMachineConfigInfo vmConfigInfo =
                           new VirtualMachineConfigInfo();
                   vmConfigInfo.changeVersion = "1";
                   DateTime dt = ecb.getConnection().Service.CurrentTime(ecb.getConnection().ServiceRef);
                   vmConfigInfo.modified = dt;
                
                   VirtualMachineDefaultPowerOpInfo defaultInfo 
                      = new VirtualMachineDefaultPowerOpInfo();
                   vmConfigInfo.defaultPowerOps=defaultInfo;
           
                   VirtualMachineFileInfo fileInfo 
                      = new VirtualMachineFileInfo();
                   vmConfigInfo.files=fileInfo;
            
                   VirtualMachineFlagInfo flagInfo 
                      = new VirtualMachineFlagInfo();
                   vmConfigInfo.flags=flagInfo;
            
                   vmConfigInfo.guestFullName="Full Name";
                   vmConfigInfo.guestId="Id";
            
                   VirtualHardware vhardware 
                      = new VirtualHardware();
                   vhardware.memoryMB=int.Parse(ecb.get_option("memorysize"));
                   vhardware.numCPU=int.Parse(ecb.get_option("cpucount"));
                   vmConfigInfo.hardware=vhardware;
            
                   // Not Required For Computing The Overhead
                   vmConfigInfo.name="OnlyFoeInfo";
                   vmConfigInfo.uuid="12345678-abcd-1234-cdef-123456789abc";
                   vmConfigInfo.version="First";
                   vmConfigInfo.template=false;
                   vmConfigInfo.alternateGuestName="Alternate";
            
                   long overhead 
                      = ecb._connection._service.QueryMemoryOverheadEx(
                                             hmor,vmConfigInfo);      
                   Console.WriteLine("Using queryMemoryOverheadEx API using vmReconfigInfo");
                   Console.WriteLine("Memory overhead necessary to "
                                     + "poweron a virtual machine with memory " 
                                     + ecb.get_option("memorysize") 
                                     + " MB and cpu count " 
                                     + ecb.get_option("cpucount") 
                                     + " -: " + overhead + " bytes");
               }
               else
               {
                   long overhead
                      = ecb._connection._service.QueryMemoryOverhead(hmor,
                           long.Parse(ecb.get_option("memorysize")), 0, false,
                           int.Parse(ecb.get_option("cpucount"))
                        );
                   Console.WriteLine("Using queryMemoryOverhead API "
                                     + "using CPU count and Memory Size");
                   Console.WriteLine("Memory overhead necessary to "
                                      + "poweron a virtual machine with memory "
                                      + ecb.get_option("memorysize")
                                      + " MB and cpu count "
                                      + ecb.get_option("cpucount")
                                      + " -: " + overhead + " bytes");
               }
           }
           else
           {
               Console.WriteLine("Host " + ecb.get_option("hostname") + " not found");
           }
       }
    }
}
