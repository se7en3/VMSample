using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace AddVirtualSwitch
{
    class AddVirtualSwitch
    {
      private static AppUtil.AppUtil cb = null;
   private static VMUtils vmUtils = null;   
   String datacenter = null;
   String host = null;
   String vswitchId = null;
   
   private void validate(){
       ManagedObjectReference sic = cb.getConnection().ServiceRef;
      ServiceContent serCont = cb.getConnection()._sic;
      String apiType = serCont.about.apiType;
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      vswitchId = cb.get_option("vswitchid");
      try {
         if(apiType.Equals("HostAgent")){
            if(host!=null){
               Console.WriteLine("Host should not be specified when running via Host");
               throw new ArgumentHandlingException("Host should not be Specified");
            }
            if(datacenter ==null){
               Console.WriteLine("Datacenter should be specified when running via Host");
               throw new ArgumentHandlingException("Host Specified");
            }
         }
         else if(apiType.Equals("VirtualCenter")){
            if((datacenter == null) && (host ==null)){
               Console.WriteLine("Atleast one from datacenter " 
                                + "or host should be specified");
               throw new ArgumentHandlingException("Invalid Argument Specified");
            }
         }
      }
      catch(Exception e){
         throw e;
      }   
   }

   private void doAddVirtualSwitch()  {
      ManagedObjectReference dcmor ;
      ManagedObjectReference hostfoldermor ;
      ManagedObjectReference hostmor = null; 
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      vswitchId = cb.get_option("vswitchid");
      try {
         if(((datacenter !=null) && (host !=null)) 
               ||((datacenter !=null) && (host ==null))) {
            dcmor 
               = cb.getServiceUtil().GetDecendentMoRef(null, "Datacenter", datacenter);
            if(dcmor == null) {
                Console.WriteLine("Datacenter not found");
                return;
            }
            hostfoldermor = vmUtils.getHostFolder(dcmor);
            hostmor = vmUtils.getHost(hostfoldermor, host);
         }
         else if ((datacenter ==null) && (host !=null)) {
            hostmor = vmUtils.getHost(null, host); 
         }
         if(hostmor != null) {
            Object cmobj 
               = cb.getServiceUtil().GetDynamicProperty(hostmor, "configManager");
            HostConfigManager configMgr = (HostConfigManager)cmobj;
            ManagedObjectReference nwSystem = configMgr.networkSystem;
            HostVirtualSwitchSpec spec = new HostVirtualSwitchSpec();
            spec.numPorts=8;
            cb.getConnection()._service.AddVirtualSwitch(nwSystem, vswitchId, spec);
            Console.WriteLine(cb.getAppName() + " : Successful creating : " 
                             + vswitchId);
         }
         else{
            Console.WriteLine("Host not found");
         }  
      }
      catch (SoapException e)
      {
          if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
          {
              Console.WriteLine(cb.getAppName() + "vswitchName exceeds the maximum "
                               + "allowed length, or the number of ports "
                               + "specified falls out of valid range, or the network "
                               + "policy is invalid, or beacon configuration is invalid. ");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("AlreadyExistsFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed : Switch already exists ");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("HostConfigFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed : Configuration failures. ");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("NotFoundFault"))
          {
              Console.WriteLine(e.Message.ToString());
          }
          else
          {
              throw e;
          }
      }
     
      catch (Exception e) {
         Console.WriteLine(cb.getAppName() + " : Failed adding switch: "+ vswitchId);
         throw e;
      }
   }
        private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[3];
      useroptions[0] = new OptionSpec("vswitchid","String",1
                                     ,"Name of the switch"
                                     ,null);
      useroptions[1] = new OptionSpec("host","String",0,
                                      "Name of the host",
                                      null);
      useroptions[2] = new OptionSpec("datacenter","String",0,
                                      "Name of the datacenter",
                                      null);
      return useroptions;
   } 
   public static void Main(String[] args)  {
      AddVirtualSwitch app = new AddVirtualSwitch();
      cb = AppUtil.AppUtil.initialize("AddVirtualSwitch",
                              AddVirtualSwitch.constructOptions(),
                              args);
      cb.connect();
      vmUtils = new VMUtils(cb);
      app.validate();
      app.doAddVirtualSwitch();
      cb.disConnect();
      Console.Read();
   }
    }
}
