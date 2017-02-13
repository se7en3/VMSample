using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text;
using AppUtil;
using Vim25Api;

namespace AddVirtualSwitchPortGroup
{
//#######################################################################################
//
// AddVirtualSwitchPortGroup.cs
// This sample is used to add a Virtual Switch PortGroup
//
//<b>To run this samples following parameters are used:></b>
//
//vsiwtchid      [required]: Name of the switch in which portgroup is to be added
//portgroupname  [required]: Name of the port group
//host           [optional]: Name of the host
//datacenter     [optional]: Name of the datacenter
//
//<b>Command Line:to add a Virtual switch Port Group as: -</b>
//--url [webserviceurl] --username [username] --password  [password] 
//--vsiwtchid [mySwitch] --datacenter [mydatacenter] --portgroupname [myportgroup] --host[hostname]
//
//<b>Command Line:to add a Virtual switch Port Group without specifying the host: </b>
//--url [webserviceurl] --username [username] --password  [password] 
//--vsiwtchid [mySwitch] --datacenter [mydatacenter] --portgroupname [myportgroup]
//
//
//<b>Command Line:to add a Virtual switch Port Group without specifying the datacenter -</b>
//--url [webserviceurl] --username [username] --password  [password] 
//--vsiwtchid [mySwitch]  --portgroupname [myportgroup]

//#######################################################################################
    public class AddVirtualSwitchPortGroup
    {
        private static AppUtil.AppUtil cb = null;
        private static VMUtils vmUtils = null;

        String datacenter = null;
   String host = null;
   String vswitchId = null;
   String portGroupName = null;

   
   private void validate(){
    ManagedObjectReference sic = cb.getConnection().ServiceRef;
      ServiceContent serCont = cb.getConnection()._sic;
      String apiType = serCont.about.apiType;
   datacenter = cb.get_option("datacenter");
   host = cb.get_option("host");
   portGroupName = cb.get_option("portgroupname");
   vswitchId = cb.get_option("vswitchid");
      try{
         if(apiType.Equals("HostAgent")){
            if(host!=null){
               Console.WriteLine("Host should not be specified when running via Host");
               throw new ArgumentHandlingException("Host should not be Specified");
            }
            if(datacenter ==null){
               Console.WriteLine("Datacenter should be "
                                 +"specified when running via Host");
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

   private void doAddVirtualSwitchPortGroup()  {
      ManagedObjectReference dcmor ;
      ManagedObjectReference hostfoldermor ;
      ManagedObjectReference hostmor = null; 
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      portGroupName = cb.get_option("portgroupname");
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
   
            HostPortGroupSpec portgrp = new HostPortGroupSpec();
            portgrp.name=portGroupName;
            portgrp.vswitchName=vswitchId;
            portgrp.policy=new HostNetworkPolicy();
   
            cb.getConnection()._service.AddPortGroup(nwSystem, portgrp);
   
            Console.WriteLine(cb.getAppName() + " : Successful creating : " 
                             + vswitchId +"/"+ portGroupName);
         }
         else {
            Console.WriteLine("Host not found");
         }      
      }
     catch ( SoapException e) {
         if (e.Detail.FirstChild.LocalName.Equals("AlreadyExistsFault"))
         {
             Console.WriteLine(cb.getAppName() + " : Failed creating : "
                              + vswitchId + "/" + portGroupName);
             Console.WriteLine("Portgroup name already exists");
         }
         if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
         {
             Console.WriteLine(cb.getAppName() + " : Failed creating : "
                          + vswitchId + "/" + portGroupName);
             Console.WriteLine("PortGroup vlanId or network policy may be invalid.");
         }
         else if (e.Detail.FirstChild.LocalName.Equals("NotFoundFault"))
         {
             Console.WriteLine(cb.getAppName() + " : Failed creating : "
                          + vswitchId + "/" + portGroupName);
             Console.WriteLine("Switch Not found.");
         }
      }
          
      catch (NullReferenceException e) {
         Console.WriteLine(cb.getAppName() + " : Failed creating : " 
                          + vswitchId +"/"+ portGroupName);
         Console.WriteLine("Datacenter or Host may be invalid");
         throw e;
      }   
      catch (Exception e) {
         Console.WriteLine(cb.getAppName() + " : Failed creating : " 
                          + vswitchId +"/"+ portGroupName);
         throw e;
      }
   }
         private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[4];
      useroptions[0] = new OptionSpec("vswitchid","String",1
                                     ,"Name of the switch on which portgroup is to be added"
                                     ,null);
      useroptions[1] = new OptionSpec("host","String",0,
                                      "Name of the host",
                                      null);
      useroptions[2] = new OptionSpec("portgroupname","String",1
                                     ,"Name of the portgroup"
                                     ,null);
      useroptions[3] = new OptionSpec("datacenter","String",0,
                                      "Name of the datacenter",
                                      null);
      return useroptions;
   }
   public static void Main(String[] args)  {
      AddVirtualSwitchPortGroup app 
         = new AddVirtualSwitchPortGroup();
      cb = AppUtil.AppUtil.initialize("AddVirtualSwitchPortGroup",
                              AddVirtualSwitchPortGroup.constructOptions(),
                              args);
      cb.connect();
      vmUtils = new VMUtils(cb);
      app.validate();
      app.doAddVirtualSwitchPortGroup();
      cb.disConnect();
      Console.Read();
   }
    }
}
