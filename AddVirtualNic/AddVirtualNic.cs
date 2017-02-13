using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace AddVirtualNic
{
    public class AddVirtualNic
    {
        private static AppUtil.AppUtil cb = null;
   private static VMUtils vmUtils = null;   
   String datacenter = null;
   String host = null;
   String vswitchId = null;
   String portGroup = null;
   String ipAddr= null;

   private void validate()  {
       ManagedObjectReference sic = cb.getConnection().ServiceRef;
      ServiceContent serCont = cb.getConnection()._sic;
      String apiType = serCont.about.apiType;
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      portGroup = cb.get_option("portgroupname");
      vswitchId = cb.get_option("vswitchid");
      try {
         if(apiType.Equals("HostAgent")){
            if(host!=null){
               Console.WriteLine("Host should not be specified when running via Host");
               throw new ArgumentHandlingException("Host Specified");
            }
            if(datacenter ==null){
               Console.WriteLine("Datacenter should be specified when running via Host");
               throw new ArgumentHandlingException("Host Specified");
            }
         }
         else if(apiType.Equals("VirtualCenter")){
            if((datacenter == null) && (host ==null)){
               Console.WriteLine("Atleast one from datacenter ");
               Console.WriteLine("or host should be specified");
               throw new ArgumentHandlingException("Invalid Argument Specified");
            }
         }
      }
      catch(Exception e){
         throw e;
      }   
   }

   private HostVirtualNicSpec createVNicSpecification() {
      HostVirtualNicSpec vNicSpec = new HostVirtualNicSpec();
      HostIpConfig ipConfig = new HostIpConfig();
      ipConfig.dhcp=false;
      ipAddr = cb.get_option("ipaddress");
      ipConfig.ipAddress=ipAddr;
      ipConfig.subnetMask="255.255.255.0";      
      vNicSpec.ip=ipConfig;
      return vNicSpec;
   }

   private void doAddVirtualNic()  {
      ManagedObjectReference dcmor ;
      ManagedObjectReference hostfoldermor ;
      ManagedObjectReference hostmor = null; 
      datacenter = cb.get_option("datacenter");
      host = cb.get_option("host");
      vswitchId = cb.get_option("vswitchid");
      portGroup = cb.get_option("portgroupname");
      ipAddr = cb.get_option("ipaddress");
     
      try {
          if(((datacenter !=null) && (host !=null)) 
               || ((datacenter !=null) && (host ==null))) {
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
            portgrp.name=portGroup;
   
            HostVirtualNicSpec vNicSpec = createVNicSpecification();
            cb.getConnection()._service.AddVirtualNic(nwSystem, portGroup, vNicSpec);
            
            Console.WriteLine(cb.getAppName() + " : Successful creating nic on portgroup : " 
                               + portGroup);
         }
         else {
            Console.WriteLine("Host not found");
         }      
      }
      
      catch (SoapException e) {
          if (e.Detail.FirstChild.LocalName.Equals("ResourceInUseFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed to add nic "
                               + ipAddr);
          }
          else if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed to add nic " + ipAddr);
              Console.WriteLine("PortGroup vlanId or network policy or ipaddress may be invalid .\n");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("NotFoundFault"))
          {
              Console.WriteLine(cb.getAppName() + " : Failed to add nic " + ipAddr);
              Console.WriteLine(" Switch or portgroup not found.\n");
          }
      }
       
      catch (NullReferenceException e) {
          Console.WriteLine(cb.getAppName() + " : Failed to add nic " + ipAddr);
         Console.WriteLine("Datacenter or Host may be invalid \n");
         throw e;
      }   
      catch (Exception e) {
          Console.WriteLine(cb.getAppName() + " : Failed to add nic " + ipAddr);
         throw e;
      } 
   }
   
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[5];
      useroptions[0] = new OptionSpec("vswitchid","String",1
                                     ,"Name of the switch"
                                     ,null);
      useroptions[1] = new OptionSpec("host","String",0,
                                      "Name of the host",
                                      null);
      useroptions[2] = new OptionSpec("portgroupname","String",1
                                      ,"Name of the portgroup"
                                      ,null);
      useroptions[3] = new OptionSpec("ipaddress","String",1,
                                      "Ipaddress of the nic",
                                      null);
      useroptions[4] = new OptionSpec("datacenter","String",0,
                                      "Name of the datacenter",
                                      null);
      return useroptions;
   }

   public static void Main(String[] args) {
      AddVirtualNic app = new AddVirtualNic();
      cb = AppUtil.AppUtil.initialize("AddVirtualNic",
                              AddVirtualNic.constructOptions(),
                              args);
      cb.connect();
      vmUtils = new VMUtils(cb);
      app.validate();
      app.doAddVirtualNic();
      cb.disConnect();
      Console.WriteLine("Please enter any key to exit: ");
      Console.Read();
      Environment.Exit(1);
   }
    }
}
