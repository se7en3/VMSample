using System;
using System.Collections;
using System.Web.Services.Protocols;
using Vim25Api;

namespace SimpleClient {

    /// <summary>
    /// This is a simple standalone client whose purpose is to demonstrate the
    /// process for Logging into the vCenter or ESX, and get Container contents 
    /// starting at the root Folder available in the ServiceInstanceContent
    /// </summary>
    public class SimpleClient {

      protected Vim25Api.VimService _service;
      protected ServiceContent _sic;
      protected ManagedObjectReference _svcRef;
      protected ManagedObjectReference _propCol;
      protected ManagedObjectReference _rootFolder;
      private static AppUtil.AppUtil cb = null;

      /// <summary>
      /// Create the Managed Object References for the sample
      /// </summary>
      /// <param name="name">name of sample</param>
      public void CreateServiceRef(string name, string[] args) {
          var svcCon = cb.getConnection();
          _service = svcCon.Service;
          _propCol = svcCon.PropCol;
          _rootFolder = svcCon.Root;
      }

      /// <summary>
      /// Get Container contents for all childEntities accessible from rootFolder
      /// </summary>
      public void GetContainerContents() {
         // Create a Filter Spec to Retrieve Contents for...
        
          TraversalSpec rpToVm = new TraversalSpec();
          rpToVm.name = "rpToVm";
          rpToVm.type = "ResourcePool";
          rpToVm.path = "vm";
          rpToVm.skip = false;


          // Recurse through all ResourcePools

          TraversalSpec rpToRp = new TraversalSpec();
          rpToRp.name = "rpToRp";
          rpToRp.type = "ResourcePool";
          rpToRp.path = "resourcePool";
          rpToRp.skip = false;

          rpToRp.selectSet = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec() };
          rpToRp.selectSet[0].name = "rpToRp";
          rpToRp.selectSet[1].name = "rpToVm";


          // Traversal through ResourcePool branch
          TraversalSpec crToRp = new TraversalSpec();
          crToRp.name = "crToRp";
          crToRp.type = "ComputeResource";
          crToRp.path = "resourcePool";
          crToRp.skip = false;
          crToRp.selectSet = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec() };
          crToRp.selectSet[0].name = "rpToRp";
          crToRp.selectSet[1].name = "rpToVm";


          // Traversal through host branch
          TraversalSpec crToH = new TraversalSpec();
          crToH.name = "crToH";
          crToH.type = "ComputeResource";
          crToH.path = "host";
          crToH.skip = false;


          // Traversal through hostFolder branch
          TraversalSpec dcToHf = new TraversalSpec();
          dcToHf.name = "dcToHf";
          dcToHf.type = "Datacenter";
          dcToHf.path = "hostFolder";
          dcToHf.skip = false;
          dcToHf.selectSet = new SelectionSpec[] { new SelectionSpec() };
          dcToHf.selectSet[0].name = "visitFolders";


          // Traversal through vmFolder branch
          TraversalSpec dcToVmf = new TraversalSpec();
          dcToVmf.name = "dcToVmf";
          dcToVmf.type = "Datacenter";
          dcToVmf.path = "vmFolder";
          dcToVmf.skip = false;
          dcToVmf.selectSet = new SelectionSpec[] { new SelectionSpec() };
          dcToVmf.selectSet[0].name = "visitFolders";


          // Recurse through all Hosts
          TraversalSpec HToVm = new TraversalSpec();
          HToVm.name = "HToVm";
          HToVm.type = "HostSystem";
          HToVm.path = "vm";
          HToVm.skip = false;
          HToVm.selectSet = new SelectionSpec[] { new SelectionSpec() };
          HToVm.selectSet[0].name = "visitFolders";


          // Recurse thriugh the folders
          TraversalSpec visitFolders = new TraversalSpec();
          visitFolders.name = "visitFolders";
          visitFolders.type = "Folder";
          visitFolders.path = "childEntity";
          visitFolders.skip = false;
          visitFolders.selectSet = new SelectionSpec[] { new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec(), new SelectionSpec() };
          visitFolders.selectSet[0].name = "visitFolders";
          visitFolders.selectSet[1].name = "dcToHf";
          visitFolders.selectSet[2].name = "dcToVmf";
          visitFolders.selectSet[3].name = "crToH";
          visitFolders.selectSet[4].name = "crToRp";
          visitFolders.selectSet[5].name = "HToVm";
          visitFolders.selectSet[6].name = "rpToVm";
          SelectionSpec[] selectionSpecs = new SelectionSpec[] { visitFolders, dcToVmf, dcToHf, crToH, crToRp, rpToRp, HToVm, rpToVm };

         PropertySpec[] propspecary = new PropertySpec[] { new PropertySpec() };
         propspecary[0].all = false;
         propspecary[0].allSpecified = true;
         propspecary[0].pathSet = new string[] { "name" };
         propspecary[0].type = "ManagedEntity";

         PropertyFilterSpec spec = new PropertyFilterSpec();
         spec.propSet = propspecary;
         spec.objectSet = new ObjectSpec[] { new ObjectSpec() };
         spec.objectSet[0].obj = _rootFolder;
         spec.objectSet[0].skip = false;
         spec.objectSet[0].selectSet =  selectionSpecs;

         // Recursively get all ManagedEntity ManagedObjectReferences 
         // and the "name" property for all ManagedEntities retrieved
         ObjectContent[] ocary = 
            _service.RetrieveProperties(
               _propCol, new PropertyFilterSpec[] { spec }
            );

         // If we get contents back. print them out.
         if (ocary != null) {
            ObjectContent oc = null;
            ManagedObjectReference mor = null;
            DynamicProperty[] pcary = null;
            DynamicProperty pc = null;
            for (int oci = 0; oci < ocary.Length; oci++) {
               oc = ocary[oci];
               mor = oc.obj;
               pcary = oc.propSet;

               Console.WriteLine("Object Type : " + mor.type);
               Console.WriteLine("Reference Value : " + mor.Value);

               if (pcary != null) {
                  for (int pci = 0; pci < pcary.Length; pci++) {
                     pc = pcary[pci];
                     Console.WriteLine("   Property Name : " + pc.name);
                     if (pc != null) {
                        if (!pc.val.GetType().IsArray) {
                           Console.WriteLine("   Property Value : " + pc.val);
                        } 
                        else {
                           Array ipcary = (Array)pc.val;
                           Console.WriteLine("Val : " + pc.val);
                           for (int ii = 0; ii < ipcary.Length; ii++) {
                              object oval = ipcary.GetValue(ii);
                              if (oval.GetType().Name.IndexOf("ManagedObjectReference") >= 0) {
                                 ManagedObjectReference imor = (ManagedObjectReference)oval;

                                 Console.WriteLine("Inner Object Type : " + imor.type);
                                 Console.WriteLine("Inner Reference Value : " + imor.Value);
                              } 
                              else {
                                 Console.WriteLine("Inner Property Value : " + oval);
                              }
                           }
                        }
                     }
                  }
               }
            }
         } 
         else {
            Console.WriteLine("No Managed Entities retrieved!");
         }
      }

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      //[STAThread]
      public static void Main(string[] args){
         if (args == null || args.Length < 2) 
         {
            Console.WriteLine(
               "Usage : SimpleClient <webserviceurl> <username> <password>"
               );
         }

         cb = AppUtil.AppUtil.initialize("SimpleClient", args);
         SimpleClient sc = new SimpleClient();
         
         try {
            // Connect to the Service
            cb.connect();
            // Create the Service Managed Object Reference
            sc.CreateServiceRef("SimpleClient", args);
            // Retrieve Container contents for all Managed Entities and their names
            sc.GetContainerContents();
            // Disconnect from the WebServcice
            cb.disConnect();
            Console.WriteLine("Press enter to exit ");
            Console.Read();

         } catch (SoapException se) {
            Console.WriteLine("Caught SoapException - " + 
                              " Actor : " + se.Actor + 
                              " Code : " + se.Code + 
                              " Detail XML : " + se.Detail.OuterXml);
            Console.Read();
         } catch (Exception e) {
            Console.WriteLine("Caught Exception : " + 
                              " Name : " + e.GetType().Name +
                              " Message : " + e.Message +
                              " Trace : " + e.StackTrace);
            Console.Read();
         }
      }
   }
}
