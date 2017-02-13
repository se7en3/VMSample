using System;
using System.Collections;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace Browser {
   /// <summary>
   /// Browse for contents in Vim - Hostd or Vpxd
   /// </summary>
   public class Browser {
      private static AppUtil.AppUtil cb = null;
      static VimService _service;
      static ServiceContent _sic;
      /// <summary>
      /// Array of typename + all its properties
      /// </summary>
      private string[][] typeInfo = new string[][] { 
              new string[] { "Folder", "name", "childEntity" }, };

      private void BuildTypeInfo() {
         string usertype = cb.get_option("typename");
         string property = cb.get_option("propertyname");         
         string[] typenprops = new string[2];
         typenprops[0] = usertype;
         typenprops[1] = property;
         typeInfo = 
            new string[][] { typenprops, };
      }
      /// <summary>
      /// Retrieve inventory from the given root 
      /// </summary>
      private void PrintInventory() {
          try
          {
              Console.WriteLine("Fetching Inventory");
              BuildTypeInfo();
              // Retrieve Contents recursively starting at the root folder 
              // and using the default property collector.            
              ObjectContent[] ocary =
                 cb.getServiceUtil().GetContentsRecursively(null, null, typeInfo, true);
              ObjectContent oc = null;
              ManagedObjectReference mor = null;
              DynamicProperty[] pcary = null;
              DynamicProperty pc = null;
              for (int oci = 0; oci < ocary.Length; oci++)
              {
                  oc = ocary[oci];
                  mor = oc.obj;
                  pcary = oc.propSet;
                  cb.log.LogLine("Object Type : " + mor.type);
                  cb.log.LogLine("Reference Value : " + mor.Value);
                  if (pcary != null)
                  {
                      for (int pci = 0; pci < pcary.Length; pci++)
                      {
                          pc = pcary[pci];
                          cb.log.LogLine("   Property Name : " + pc.name);
                          if (pc != null)
                          {
                              if (!pc.val.GetType().IsArray)
                              {
                                  cb.log.LogLine("   Property Value : " + pc.val);
                              }
                              else
                              {
                                  Array ipcary = (Array)pc.val;
                                  cb.log.LogLine("Val : " + pc.val);
                                  for (int ii = 0; ii < ipcary.Length; ii++)
                                  {
                                      object oval = ipcary.GetValue(ii);
                                      if (oval.GetType().Name.IndexOf("ManagedObjectReference") >= 0)
                                      {
                                          ManagedObjectReference imor = (ManagedObjectReference)oval;
                                          cb.log.LogLine("Inner Object Type : " + imor.type);
                                          cb.log.LogLine("Inner Reference Value : " + imor.Value);
                                      }
                                      else
                                      {
                                          cb.log.LogLine("Inner Property Value : " + oval);
                                      }
                                  }
                              }
                          }
                      }
                  }
              }
              cb.log.LogLine("Done Printing Inventory");
              cb.log.LogLine("Browser : Successful Getting Contents");
          }

          catch (SoapException e)
          {
              Console.WriteLine("Browser : Failed Getting Contents");
              Console.WriteLine("Encountered SoapException");
              throw e;
          }
          catch (Exception e)
          {
              cb.log.LogLine("Browser : Failed Getting Contents");              
              throw e;
          }
         
      }
      public static OptionSpec[] constructOptions() {
         OptionSpec[] useroptions = new OptionSpec[2];         
         useroptions[0] = new OptionSpec("typename", "String", 1
                                         , "Type of managed entity"
                                         , null);
         useroptions[1] = new OptionSpec("propertyname", "String", 1
                                         , "Name of the Property"
                                         , null);
         return useroptions;
      }
      public static void Main(String[] args) {
         Browser obj = new Browser();
         cb = AppUtil.AppUtil.initialize("Browser"
                                         , Browser.constructOptions()
                                         , args);
         cb.connect();          
         obj.PrintInventory();
         cb.disConnect();
         Console.WriteLine("Press enter to exit.");
         Console.Read();
      }
   }
}
