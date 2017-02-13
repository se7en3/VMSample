using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;
namespace SCSILunName
{
    class SCSILunName
    {
           private static AppUtil.AppUtil cb = null;
           private static VMUtils vmUtils = null;   
        private void displayScsiLuns() {
            String hostname;
            ManagedObjectReference hmor;
            ManagedObjectReference hostfoldermor;
            if (!validate()) {
                vmUtils = new VMUtils(cb);
                hostname = null;
                ManagedObjectReference dcmor = cb.getServiceUtil().GetDecendentMoRef
                          (null, "Datacenter", "ha-datacenter");
                hostfoldermor = vmUtils.getHostFolder(dcmor);
                hmor = vmUtils.getHost(hostfoldermor, hostname);
            }
            else {
                 hostname = cb.get_option("hostname");
                 hmor = cb.getServiceUtil().GetDecendentMoRef(null, "HostSystem", hostname);
            }
      if(hmor != null) {
         DynamicProperty[]  scsiArry = 
            getDynamicProarray(hmor,"config.storageDevice.scsiLun");
          
         ScsiLun[] scsiLun = ((ScsiLun[])(scsiArry[0]).val);
        
         try{
            if (scsiLun != null && scsiLun.Length > 0) {
               for (int j=0;j < scsiLun.Length;j++ ){
                  Console.WriteLine("\nSCSI LUN " + (j+1));
                  Console.WriteLine("--------------");
                  String canName = scsiLun[j].canonicalName;
                  String vendor = scsiLun[j].vendor;
                  String model = scsiLun[j].model;
                  ScsiLunDurableName scsiLunDurableName = scsiLun[j].durableName;
                  if (scsiLunDurableName != null)
                  {
                  sbyte[] data = scsiLunDurableName.data;
                  String scsinamespace = scsiLunDurableName.@namespace;
                  sbyte namespaceId = scsiLunDurableName.namespaceId;
                  Console.Write("\nData            : ");
                  for (int i = 0;i < data.Length ; i++ ){
                     Console.Write(data[i] + " ");
                  }
                  Console.WriteLine("Namespace       : " + scsinamespace);
                  Console.WriteLine("Namespace ID    : " + namespaceId);
                   }
                  Console.WriteLine("\nCanonical Name  : " + canName);
                  
                  Console.WriteLine("\nVMFS Affected ");
                  getVMFS(hmor,canName);
                  Console.WriteLine("Virtual Machines ");
                  getVMs(hmor,canName);
               }
            }
         }
         catch(Exception e) {
            Console.WriteLine("error" + e);
            e.StackTrace.ToString();
         }
      }
      else {
         Console.WriteLine("Host "+ cb.get_option("hostname")+" not found");
      }
   }
   
    /*
   * This subroutine prints the virtual machine file
   * system volumes affected by the given SCSI LUN.
   * @param  hmor      A HostSystem object of the given host.
   * @param canName    Canonical name of the SCSI logical unit
   */

   private void getVMFS(ManagedObjectReference hmor,String canName) {
      DynamicProperty[]  dsArr = getDynamicProarray(hmor,"datastore");
      ManagedObjectReference[] dataStoresMOR = 
               ((ManagedObjectReference[])(dsArr[0]).val);
      //ManagedObjectReference[] dataStoresMOR = ds.managedObjectReference;
      Boolean vmfsFlag = false;
      try {
         for (int j=0;j<dataStoresMOR.Length ; j++ ) {
            DynamicProperty[]  infoArr = getDynamicProarray(dataStoresMOR[j],"info");
            String infoClass = infoArr[0].val.GetType().ToString();
            if(infoClass.Equals("VmfsDatastoreInfo")){
               VmfsDatastoreInfo vds = (VmfsDatastoreInfo)(infoArr[0]).val;
               HostVmfsVolume hvms = vds.vmfs;
               String vmfsName  = vds.name;
               HostScsiDiskPartition[] hdp = hvms.extent;
               for (int k =0;k< hdp.Length ; k++ )  {
                  if(hdp[k].diskName.Equals(canName)){
                     vmfsFlag = true;
                     Console.WriteLine(" " + vmfsName + "\n");
                  }
               }
            }
         }
         if (!vmfsFlag) {
            Console.WriteLine(" None\n");
         }
      }
      catch(Exception e) {
         Console.WriteLine("error" + e);
         e.StackTrace.ToString();
      }
   }

   private void getVMs(ManagedObjectReference hmor,String canName)  {
      DynamicProperty[]  dsArr = getDynamicProarray(hmor,"datastore");
      ManagedObjectReference[] dataStoresMOR = 
               ((ManagedObjectReference[])(dsArr[0]).val); 
      //ManagedObjectReference[] dataStoresMOR = ds.managedObjectReference;   
      Boolean vmfsFlag = false;
      try{
         for (int j=0;j<dataStoresMOR.Length ; j++ ) {
            DynamicProperty[]  infoArr = getDynamicProarray(dataStoresMOR[j],"info");
            String infoClass = infoArr[0].val.GetType().ToString();
            if (infoClass.Equals("VimApi.VmfsDatastoreInfo"))
            {
               VmfsDatastoreInfo vds = (VmfsDatastoreInfo)(infoArr[0]).val; 
               HostVmfsVolume hvms = vds.vmfs;
               String vmfsName  = vds.name;
               HostScsiDiskPartition[] hdp = hvms.extent;
               for (int k =0;k< hdp.Length ; k++ ) {
                  if(hdp[k].diskName.Equals(canName)){
                     DynamicProperty[]  vmArr 
                        = getDynamicProarray(dataStoresMOR[j],"vm");
                     ManagedObjectReference[] vmsMOR =
                              ((ManagedObjectReference[])(vmArr[0]).val);
                  //   ManagedObjectReference[] vmsMOR = vms.managedObjectReference;
                    
                     if(vmsMOR != null){
                        for (int l=0;l<vmsMOR.Length ; l++ ) {
                           vmfsFlag = true;
                           DynamicProperty[]  nameArr = 
                              getDynamicProarray(vmsMOR[l],"name");
                           String vmname = nameArr[0].val.ToString();
                           Console.WriteLine(" "+vmname);
                        }
                     }
                  }
               }
            }
         }
         if (!vmfsFlag) {
            Console.WriteLine(" None\n");
         }
      }
      catch(Exception e){
         Console.WriteLine("error" + e);
         e.StackTrace.ToString();
      }           
   }
   
   private  DynamicProperty[] getDynamicProarray
                    (ManagedObjectReference MOR,String pName ){

      ObjectContent[] objContent = 
           cb.getServiceUtil().GetObjectProperties(null, MOR,
              new String[] { pName });
      ObjectContent contentObj = objContent[0];
      DynamicProperty[] objArr = contentObj.propSet;
      return objArr;
   }   

   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[1];
      useroptions[0] = new OptionSpec("hostname","String",0
                                      ,"Name of the host"
                                      ,null);
      return useroptions;
   }
private Boolean validate()
{
    Boolean valid = false;
    String apitype = cb._connection._sic.about.apiType;
    if (apitype == "VirtualCenter") {
        String hostname = cb.get_option("hostname");
        if (hostname == null) {
            valid = false;
            throw new ArgumentHandlingException("Host  name need to be specified");
        }
        else {
            valid = true;
        }
    }
   
    return valid;
}
       public static void Main(string[] args)
        {
            SCSILunName obj = new SCSILunName();
            cb = AppUtil.AppUtil.initialize("SCSILunName"
                                   , SCSILunName.constructOptions()
                                   , args);
            cb.connect();            
            obj.displayScsiLuns();            
            cb.disConnect();
            Console.WriteLine("Press any key to exit: ");
            Console.Read();
        }
    }
}
