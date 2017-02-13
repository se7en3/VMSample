using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace RemoveVirtualNic
{

//#####################################################################################
//This sample is used to remove a Virtual Nic  
//
//To run this samples following parameters are used:
//
//portgroupname  [required]: Name of the port group from which the nic is to be removed
//host           [optional]: Name of the host
//datacenter     [optional]: Name of the datacenter
//
//<b>Parameters:</b>
//<b>Command Line:to remove a Host VirtualNic from a PortGroup</b>
// --url [webserviceurl] --username [username] --password  [password] 
//--datacenter [mydatacenter] --portgroupname [myportgroup]>  --host [hostname]
//
//<b>Command Line:to remove a Host VirtualNic from a PortGroup without specifying the host</b>
// --url [webserviceurl] --username [username] --password  [password] 
//--datacenter [mydatacenter] --portgroupname [myportgroup]
//
//<b>Command Line:to remove a Host VirtualNic from a PortGroup without specifying the datacenter</b>
// --url [webserviceurl] --username [username] --password  [password] 
//--portgroupname [myportgroup] --host [name of the host]
//#####################################################################################
   public class RemoveVirtualNic
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
   try
   {
       if (apiType.Equals("HostAgent"))
       {
           if (host != null)
           {
               Console.WriteLine("Host should not be specified"
                                + " when running via Host");
               throw new ArgumentHandlingException("Host Specified");
           }
           if (datacenter == null)
           {
               Console.WriteLine("Datacenter should be specified "
                                + " when running via Host");
               throw new ArgumentHandlingException("Host Specified");
           }
       }
       else if (apiType.Equals("VirtualCenter"))
       {
           if ((datacenter == null) && (host == null))
           {
               Console.WriteLine("Atleast one from datacenter "
                                 + " or host should be specified");
               throw new ArgumentHandlingException("Invalid Argument Specified");
           }
       }
   }
 
   catch (Exception e)
   {
       throw e;
   }   
   }

   private void doRemoveVirtualNic()  {
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
   
            Object obj 
               = cb.getServiceUtil().GetDynamicProperty(nwSystem, "networkInfo.vnic");
            HostVirtualNic[] hvns = (HostVirtualNic[])obj;
            Boolean found_one = false;
            if (hvns != null) {
               for (int i=0; i<hvns.Length; i++) {
                  HostVirtualNic nic = hvns[i];
                  String portGroup = nic.portgroup;
                  if (portGroup.Equals(portGroupName)) {
                     found_one = true;
                     cb.getConnection()._service.RemoveVirtualNic(nwSystem, 
                                                                      nic.device);
                  }
               }
            }
            if (found_one) {
               Console.WriteLine(cb.getAppName() 
                               + " : Successful removing : " +portGroupName );
            } else {
               Console.WriteLine(cb.getAppName() 
                               + " : PortGroupName not found failed removing : " 
                               + portGroupName );
            }
         }
         else {
            Console.WriteLine("Host not found");
         }     
      }
      catch (SoapException e)
      {
          if (e.Detail.FirstChild.LocalName.Equals("NotFoundFault"))
          {
              Console.WriteLine(cb.getAppName()
                         + " : Failed : virtual network adapter cannot be found. ");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("HostConfigFault"))
          {
              Console.WriteLine(cb.getAppName()
                            + " : Failed : Configuration falilures. ");
          }
      }
        catch (Exception e) {
            Console.WriteLine(cb.getAppName() + " : Failed removing nic: "
                          + portGroupName);
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
   
   public static void Main(String[] args)  {
      RemoveVirtualNic app = new RemoveVirtualNic();
      cb = AppUtil.AppUtil.initialize("RemoveVirtualNic",
                              RemoveVirtualNic.constructOptions(),
                              args);
      cb.connect();
      vmUtils = new VMUtils(cb);
      app.validate();
      app.doRemoveVirtualNic();
      cb.disConnect();
      Console.WriteLine("Please enter any key to exit: ");
      Console.Read();
   }
    }
}
