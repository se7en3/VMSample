using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace LicenseManager
{
    public class LicenseManager
    {
        private static AppUtil.AppUtil cb = null;
   private static ManagedObjectReference licMgr = null;
   
   private void useLicenseManager()  {
      String action = cb.get_option("action");
      if(action.Equals("browse")) {
         Console.WriteLine("Display the license usage. " 
                             + "The license usage is a list of supported features"
                             + "and the"                                        
                             + " number of licenses that have been reserved.");
         displayLicenseUsage();
      }
      else if(action.Equals("setserver")) {
         Console.WriteLine("Set the License server.");
         setLicenseServer();
      }      
      else if(action.Equals("setedition")) {
         Console.WriteLine("Set the License Edition.");
         setEdition();
      }
      else if(action.Equals("featureinfo")) {         
         displayFeatureInfo();
      }      
      else {
         Console.WriteLine("Invalid Action ");
         Console.WriteLine("Valid Actions [browse|setserver|setedition|featureinfo]");
      }
   }
   
   private void displayLicenseUsage()  {
      ObjectContent[] licContent = 
         cb.getServiceUtil().GetObjectProperties(null,
                                                 licMgr,new String[] {"source", 
                                                                      "sourceAvailable",
                                                                      "featureInfo" });
      LicenseUsageInfo licUsage =
                  cb.getConnection()._service.QueryLicenseUsage(licMgr, null);
      LicenseAvailabilityInfo[] avail =
                  cb.getConnection()._service.QueryLicenseSourceAvailability(licMgr,
                  null);
      print(licUsage);
      print(avail);
   }
   
   private void setLicenseServer()  {
      LicenseServerSource source = new LicenseServerSource();
      source.licenseServer = cb.get_option("serverurl");      
      try{
         cb.getConnection()._service.ConfigureLicenseSource(licMgr, null, source);
      }
      catch(SoapException  e) {
          
          if (e.Detail.FirstChild.LocalName.Equals("InvalidLicenseFault"))
          {
               Console.WriteLine("License file is not valid");	  
          }
         else if (e.Detail.FirstChild.LocalName.Equals("NotEnoughLicensesFault"))
          {
               Console.WriteLine("New license source does not have " +
                                     "enough licenses.");
          }
          else if (e.Detail.FirstChild.LocalName.Equals("LicenseServerUnavailableFault"))
          {
             Console.WriteLine("License server is unreachable.");
          }
            
             
         else {
            throw e;
         }
      }
   }  
 
   private void setEdition()  {
      Boolean valid = validate(cb.get_option("edition"));
      if(valid) {
         try{      
            cb.getConnection()._service.SetLicenseEdition(licMgr,
                                          null, cb.get_option("edition"));
         }
         catch(SoapException e) {
              if (e.Detail.FirstChild.LocalName.Equals("InvalidStateFault"))
          {
               Console.WriteLine("Feature cannot be supported on the platform");	  
          }
         else if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
          {
                Console.WriteLine("Feature key is not an edition feature key.");
          }
          else 
          {
             Console.WriteLine(e.Message.ToString());
          }
            }
      }
   }
   
   private void displayFeatureInfo()  {
      Boolean valid = validate(cb.get_option("feature"));
      LicenseFeatureInfo [] feature = 
         (LicenseFeatureInfo [])cb.getServiceUtil().GetDynamicProperty(licMgr,"featureInfo");
      Boolean flag = false;
      for(int i=0 ;i < feature.Length; i++) {
         if(feature[i].key.Equals(cb.get_option("feature"))) {
            Console.WriteLine("Name       " + feature[i].featureName);
            Console.WriteLine("Unique Key " + feature[i].key);
            Console.WriteLine("State      " + feature[i].state);
            Console.WriteLine("Cost Unit  " + feature[i].costUnit);
            i = feature.Length + 1;
            flag = true;
         }
      }
      Console.WriteLine("Feature Not Available");
   }     
   
   private Boolean validate(String feature){
      String [] features = {"backup","das","drs","esxExpress","esxFull"
                           ,"esxHost","esxVmtn","gsxHost","iscsi","nas",
                            "san","vc","vmotion","vsmp"};
      Boolean flag = false;
      for(int i=0; i<features.Length; i++) {
         if(features[i].Equals(feature)){
            flag = true;
            i = features.Length + 1;            
         }
      }
      if(!flag) {
         Console.WriteLine("Invalid Edition / Feature : " +
                            "Specify the edition/feature from below list");
         Console.WriteLine("backup     "+"Enable ESX Server consolidated" + 
                            "backup feature. This is a per CPU package license.");
         Console.WriteLine("das        "+"Enable VirtualCenter HA. This is a per" + 
                            "ESX server CPU package license.");
         Console.WriteLine("drs        "+"Enable VirtualCenter Distributed Resource" + 
                            "Scheduler. This is a per ESX server CPU package license.");
         Console.WriteLine("esxExpress "+
                            "The edition license for the ESX server, Starter edition." + 
                            "This is a per CPU package license.");
         Console.WriteLine("esxFull    "+
                            "The edition license for the ESX Server, Standard edition." + 
                            "This is a per CPU package license.");
         Console.WriteLine("esxHost    "+
                            "Enable VirtualCenter ESX Server host management" + 
                            "functionality." + 
                            "This is a per ESX server CPU package license. ");
         Console.WriteLine("esxVmtn    "+
                            "The edition license for the ESX server, VMTN edition." + 
                            "This is a per CPU package license. ");
         Console.WriteLine("gsxHost    "+
                            "Enable VirtualCenter GSX Server host management" + 
                            "functionality. This is a per GSX server CPU " + 
                            "package license. ");
         Console.WriteLine("iscsi      "+"Enable use of iSCSI. This is a per " +
                            "CPU package license.");
         Console.WriteLine("nas        "+
                            "Enable use of NAS. This is a per CPU package license.");
         Console.WriteLine("san        "+
                            "Enable use of SAN. This is a per CPU package license.");
         Console.WriteLine("vc"+
                            "The edition license for a VirtualCenter server, full" + 
                            "edition. This license is independent of the number of" + 
                            "CPU packages for the VirtualCenter host.");
         Console.WriteLine("vmotion "+"Enable VMotion. This is a per ESX server" + 
                            "CPU package license.");
         Console.WriteLine("vsmp       "+
                            "Enable up to 4-way VSMP feature." + 
                            "This is a per CPU package license.");
      }
      return flag;
   }
   
   private void print(LicenseUsageInfo usage) {
      if(usage != null && usage.featureInfo != null) {
         
         for (int i = 0; i < usage.featureInfo.Length; ++i)
         {
             print(usage.featureInfo[i]);
         }
         if(usage.reservationInfo != null) {
             for (int i = 0; i < usage.reservationInfo.Length; ++i)
             {
                 print(usage.reservationInfo[i]);
            }
         }
      } else {
         Console.WriteLine(" : No usage returned.");
      }
   }
   
   private void print(LicenseFeatureInfo feature) {
      LicenseFeatureInfoState state = feature.state;
      String fState;
      if (LicenseFeatureInfoState.optional == state)
      {
          fState = "Optional";
      } else if(LicenseFeatureInfoState.enabled == state) {
         fState = "Included";
      } else if(LicenseFeatureInfoState.disabled == state) {
         fState = "Disabled";
      } else {
          fState = "Edition";
      }
      Console.WriteLine(" : F: " + feature.key +
            ", Cost: " + feature.costUnit +
            ", State: " + fState);
   }

   private void print(LicenseReservationInfo reservation) {
       Console.WriteLine(" : R: " + reservation.key +
            ", Required: " + reservation.required +
            ", State: " + reservation.state);
   }
   
   private void print(LicenseAvailabilityInfo[] avail) {
      Console.WriteLine(" : License Available Info:");
      if(avail != null) {
         for(int i=0; i<avail.Length; ++i) {
            LicenseAvailabilityInfo info = avail[i];
            print(info.feature);
            Console.WriteLine(" : Total: " + info.total+
                  ", Avaliable: " + info.available);
         }
      } else {
         Console.WriteLine(" : No usage returned.");
      }
   }
   private Boolean customValidation() {
       Boolean flag = true;
      String action = cb.get_option("action");
      if(action.Equals("setserver")) {
         if(!cb.option_is_set("serverurl")) {
            Console.WriteLine("For action setserver --serverurl" + 
                               "is mandatory argument");
            flag = false;
         }
      }
      else if(action.Equals("setedition")) {
         if(!cb.option_is_set("edition")) {
            Console.WriteLine("For action setedition --edition is "+
                               "mandatory argument");
            flag = false;
         }
      }
      else if(action.Equals("featureinfo")) {
         if(!cb.option_is_set("feature")) {
           Console.WriteLine("For action featureinfo --feature is " + 
                              "mandatory argument");
           flag = false;
         }
      }
      return flag;
   }
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[4];
      useroptions[0] = new OptionSpec("action","String",1,
                                      "[browse|setserver|setedition|featureinfo]"
                                      ,null);
      useroptions[1] = new OptionSpec("serverurl","String",0,
                                      "License Server URL",
                                      null);
      useroptions[2] = new OptionSpec("edition","String",0,
                                      "License Edition",
                                      null);
      useroptions[3] = new OptionSpec("feature","String",0,
                                      "Name of the feature",
                                      null);
      return useroptions;
   }   
   public static void Main(String[] args)  {
      LicenseManager app = new LicenseManager();
      cb = AppUtil.AppUtil.initialize("LicenseManager", LicenseManager.constructOptions(), args);
      Boolean valid = app.customValidation();
      if(valid) {
         cb.connect();
         licMgr = cb.getConnection()._sic.licenseManager;
         app.useLicenseManager();
         cb.disConnect();
      }
      Console.WriteLine("Press enter to exit: ");
      Console.Read();
   }
    }
}
