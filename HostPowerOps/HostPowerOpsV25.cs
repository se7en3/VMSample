using System;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Net;

namespace HostPowerOps
{
   public class HostPowerOpsV25
    {
        static Vim25Api.VimService _service;
        static ServiceContent _sic;
       public static void powerDownHost(VimApi.ManagedObjectReference hmor, String[] args, ArrayList apiVersions, Cookie cookie)
       {
      ExtendedAppUtil ecb = null;                  
      ecb = ExtendedAppUtil.initialize("PowerDownHostToStandBy"
                                       , HostPowerOps.constructOptions()
                                       ,args);
      ecb.connect(cookie);

      _service = ecb.getServiceConnectionV25().Service;
      _sic = ecb.getServiceConnectionV25().ServiceContent;          
      // Convert the vim managed object to vim25 managed object
      ManagedObjectReference hmor1  = 
         VersionUtil.convertManagedObjectReference(hmor);
                
      ManagedObjectReference taskmor =  _service.PowerDownHostToStandBy_Task(hmor1,120,false,true);
      String result = ecb.getServiceUtilV25().WaitForTask(taskmor);
      if(result.Equals("sucess")) {
         Console.WriteLine("Operation powerDownHostToStandBy"
                            +" completed sucessfully");
      }
   }
    }
}
