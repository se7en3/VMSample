using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using System.Net;
using AppUtil;
using Vim25Api;

namespace RemoveVirtualSwitchPortGroup
{

//#######################################################################################
//This sample is used to remove a Virtual Switch PortGroup  
//
//To run this samples following parameters are used:
//
//<b>Parameters:</b>
//portgroupname  [required]: Name of the port group to be removed
//host           [optional]: Name of the host
//datacenter     [optional]: Name of the datacenter
//
//<b>Command Line: To remove a Virtual Switch Port Group</b>
// --url [webserviceurl] 
//--username [username] --password  <password> 
//--datacenter [mydatacenter] --portgroupname[<myportgroup] --host <hostname>
//
//<b>Command Line: To remove a Virtual Switch Port Group without specifying the host name</b>
// --url [webserviceurl] 
//--username [username] --password  <password> 
//--datacenter [mydatacenter] --portgroupname[<myportgroup]
//
//<b>Command Line: To remove a Virtual Switch Port Group without specifying the datacenter name</b>
// --url [webserviceurl] --username [username] --password  <password> 
//--portgroupname[<myportgroup] --host<hostname>

//#######################################################################################
    public class RemoveVirtualSwitchPortGroup
    {
        private static AppUtil.AppUtil cb = null;
        private static VMUtils vmUtils = null;
        String datacenter = null;
        String host = null;
        String portGroupName = null;

        private void validate(){
      ManagedObjectReference sic = cb.getConnection().ServiceRef;
      ServiceContent serCont = cb.getConnection()._sic;
      String apiType = serCont.about.apiType;
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      portGroupName = cb.get_option("portgroupname");     
      try{
         if(apiType.Equals("HostAgent")){
            if(host!=null){
               Console.WriteLine("Host should not be specified"
                                +" when running via Host");
               throw new ArgumentHandlingException("Host should not be Specified");
            }
            if(datacenter ==null){
               Console.WriteLine("Datacenter should be specified"
                                +" when running via Host");
               throw new ArgumentHandlingException("Datacenter not Specified");
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
   
   private void doRemoveVirtualSwitchPortGroup()  {
      ManagedObjectReference dcmor ;
      ManagedObjectReference hostfoldermor ;
      ManagedObjectReference hostmor = null; 
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      portGroupName = cb.get_option("portgroupname");
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
            HostNetworkInfo netInfo 
               = (HostNetworkInfo)cb.getServiceUtil().GetDynamicProperty(nwSystem,
                                                                        "networkInfo");
            cb.getConnection()._service.RemovePortGroup(nwSystem, portGroupName);
            Console.WriteLine(cb.getAppName() + " : Successful removing portgroup "
                             + portGroupName);
         }
         else {
            Console.WriteLine("Host not found");
         }      
      }
      catch (SoapException e)
      {
          if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
          {
             Console.WriteLine(cb.getAppName() + " : Failed removing  " + portGroupName);
             Console.WriteLine("PortGroup vlanId or network policy may be invalid .\n");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("NotFoundFault"))
          {
            Console.WriteLine(cb.getAppName() + " : Failed removing "+ portGroupName);
            Console.WriteLine(" Switch or portgroup not found.\n");
          }
      }

      catch (NullReferenceException e)
      {
          Console.WriteLine(cb.getAppName() + " : Failed removing "+ portGroupName);
          Console.WriteLine("Datacenter or Host may be invalid \n");
          throw e;
      }
      catch (Exception e)
      {
          Console.WriteLine(cb.getAppName() + " : Failed removing "+ portGroupName);
         throw e;
      }
   }
   
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[3];
      useroptions[0] = new OptionSpec("host","String",0
                                     ,"Name of the host"
                                     ,null);
      useroptions[1] = new OptionSpec("portgroupname","String",1,
                                      "Name of the portgroup",
                                      null);
      useroptions[2] = new OptionSpec("datacenter","String",0,
                                      "Name of the datacenter",
                                      null);
      return useroptions;
   }

        public static void Main(String[] args)
        {
            RemoveVirtualSwitchPortGroup app = new RemoveVirtualSwitchPortGroup();
            cb = AppUtil.AppUtil.initialize("RemoveVirtualSwitchPortGroup"
                                    , RemoveVirtualSwitchPortGroup.constructOptions()
                                   , args);
            cb.connect();
            vmUtils = new VMUtils(cb);
            app.validate();
            app.doRemoveVirtualSwitchPortGroup();
            cb.disConnect();
            Console.Read();
        }
    }
}
