using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text;
using AppUtil;
using Vim25Api;

namespace SearchIndex
{
    public class SearchIndex
    {
         static VimService service;   // All Methods
    static ServiceContent content;
        private static AppUtil.AppUtil cb = null;   
    Log log = new Log();
    public SearchIndex() {
    }
    private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[5];
      useroptions[0] = new OptionSpec("dcName","String",1
                                     ,"Name of the Datacenter"
                                     ,null);
      useroptions[1] = new OptionSpec("vmDnsName","String",0,
                                      "Virtual machine dns name",
                                      null);
      useroptions[2] = new OptionSpec("hostDnsName","String",0
                                      ,"Host machine DNS name"
                                      ,null);
      useroptions[3] = new OptionSpec("vmPath","String",0,
                                      "VM path",
                                      null);
      useroptions[4] = new OptionSpec("vmIP","String",0
                                      ,"VM IP Address"
                                      ,null);
      return useroptions;
    } 
    /** Excercise the SearchIndex API
     * 
     * @param args Usage is:
     *  <url> <user> <password> <Datacenter Name> <A VM DNS Name>
     *  <A Host DNS Name> <A VM Inventory Path> 
     */
    public static void Main(String[] args) {
        
       try {
          SearchIndex app = new SearchIndex();
            cb = AppUtil.AppUtil.initialize("SearchIndex"
                                    ,SearchIndex.constructOptions()
                                    ,args);
            cb.connect();
            String dcName = cb.get_option("dcName");
            String vmDnsName = cb.get_option("vmDnsName");
            String hostDnsName =cb.get_option("hostDnsName");
            String vmPath = cb.get_option("vmPath");
            String vmIP = cb.get_option("vmIP");
            
            content = cb.getConnection()._sic;
            service = cb.getConnection()._service;
            // Find the Datacenter by using findChild()
            ManagedObjectReference dcMoRef = 
            cb.getServiceUtil().GetDecendentMoRef(null, "Datacenter", dcName);
            if (dcMoRef !=null){
                    Console.WriteLine("Found Datacenter with name: "
                                       +dcName+", MoRef: " + 
                    dcMoRef.Value);
            }else{
                   Console.WriteLine("Datacenter not Found with name: "+dcName);
            }
            if (vmDnsName != null){
                ManagedObjectReference vmMoRef = 
                service.FindByDnsName(content.searchIndex,
                                      dcMoRef,
                                      vmDnsName,
                                      true);
               if (vmMoRef !=null){
                 Console.WriteLine("Found VirtualMachine with DNS name: "+
                      vmDnsName+", MoRef: " + vmMoRef.Value);
               }
               else{
                 Console.WriteLine("VirtualMachine not Found with DNS name: "
                                  + vmDnsName);
               }
            }
            if (vmPath != null){
              ManagedObjectReference   vmMoRef = service.FindByInventoryPath(
                    content.searchIndex, vmPath);
               if (vmMoRef !=null) {
                  Console.WriteLine("Found VirtualMachine with Path: "+
                      vmPath+", MoRef: " + vmMoRef.Value);
            
               }
               else{
                      Console.WriteLine("VirtualMachine not found with vmPath "+
                      "address: "+ vmPath);
                }
             }
             if (vmIP != null){
                ManagedObjectReference vmMoRef =
                service.FindByIp(content.searchIndex,
                                 dcMoRef,
                                 vmIP,
                                 true);
              if (vmMoRef !=null){
                    Console.WriteLine("Found VirtualMachine with IP "+
                    "address "+ vmIP + ", MoRef: " + vmMoRef.Value);
              }else{
                   Console.WriteLine("VirtualMachine not found with IP "+
                   "address: "+vmIP);
               }
            }
            if (hostDnsName != null) {
               ManagedObjectReference hostMoRef =
                service.FindByDnsName(content.searchIndex,
                        null,
                        hostDnsName,
                        false);
               if (hostMoRef !=null) {
                  Console.WriteLine("Found HostSystem with DNS name "+
                  hostDnsName+", MoRef: " + hostMoRef.Value);
               }
               else{
                  Console.WriteLine("HostSystem not Found with DNS name:"+
                  hostDnsName);
               }
            }
          cb.disConnect();
          Console.WriteLine("Press enter to exit: ");
          Console.Read();
          
       } 
       catch(SoapException e) {

       }
    }
    }
}
