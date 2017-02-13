using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace RemoveVirtualSwitch
{

//#######################################################################################
//This sample is used to remove a virtual switch  
//
//To run this samples following parameters are used:
//
//vsiwtchid   [required]: Name of the switch to be removed
//host        [optional]: Name of the host
//datacenter  [optional]: Name of the datacenter
//
//<b>Command Line: to remove the Virtual Switch from a virtual center</b>
// --url [webserviceurl] --username [username] --password  //[password]  --vsiwtchid [mySwitch] --datacenter [mydatacenter]  --host[hostname]
//
//<b>Command Line:to remove a Virtual Switch from a virtual center without specifying the host</b>
// --url [webserviceurl] --username [username] --password  //[password]  --vsiwtchid [mySwitch] --datacenter [mydatacenter] 
//
//<b> Command Line:to remove a Virtual Switch from a virtual center without specifying the datacenter</b>
// --url [webserviceurl] --username [username] --password  [password]  --vsiwtchid [mySwitch] --host [host]
//
//#######################################################################################
    public class RemoveVirtualSwitch
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
      try{
         if(apiType.Equals("HostAgent")){
            if(host!=null){
               Console.WriteLine("Host should not be specified"
                                 +" when running via Host");
               throw new ArgumentHandlingException("Host Specified");
            }
            if(datacenter ==null){
               Console.WriteLine("Datacenter should be specified"
                                 +" when running via Host");
               throw new ArgumentHandlingException("Host Specified");
            }
         }
         else if(apiType.Equals("VirtualCenter")){
            if((datacenter == null) && (host ==null)){
               Console.WriteLine("Atleast one from datacenter"
                                 +" or host should be specified");
               throw new ArgumentHandlingException("Invalid Argument Specified");
            }
         }
      }
      catch(Exception e){
         throw e;
      }   
   }
  
   private void doRemoveVirtualSwitch()  {
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
            cb.getConnection()._service.RemoveVirtualSwitch(nwSystem, vswitchId);
            Console.WriteLine(cb.getAppName() + " : Successful removing : " 
                             + vswitchId);
         }
         else {
            Console.WriteLine("Host not found");
         }     
      }
      catch (SoapException e)
      {
          if (e.Detail.FirstChild.LocalName.Equals("ResourceInUseFault"))
          {
               Console.WriteLine(cb.getAppName() + " : Failed removing switch "+ vswitchId);
               Console.WriteLine("There are virtual network adapters "
                           +"associated with the virtual switch.");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("NotFoundFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed : virtual switch cannot be found. ");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("HostConfigFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed : Configuration failures. ");
          }
          else
          {
              throw e;
          }
      }
     catch(Exception e){
         throw e;
     }
   }

       private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[3];
      useroptions[0] = new OptionSpec("vswitchid","String",1
                                     ,"Name of the virtual switch"
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
      RemoveVirtualSwitch app = new RemoveVirtualSwitch();
      cb = AppUtil.AppUtil.initialize("RemoveVirtualSwitch"
                              ,RemoveVirtualSwitch.constructOptions()
                              ,args);
      cb.connect();
      vmUtils = new VMUtils(cb);
      app.validate();
      app.doRemoveVirtualSwitch();
      cb.disConnect();
      Console.Read();
   }
    }
}
