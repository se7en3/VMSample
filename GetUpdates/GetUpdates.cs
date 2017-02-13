using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;


namespace GetUpdates
{
   public class GetUpdates
    {
       private static AppUtil.AppUtil cb = null;  
   
   private void getUpdates() {
      ManagedObjectReference vmRef 
         = cb.getServiceUtil().GetDecendentMoRef(null,"VirtualMachine",
                                                 cb.get_option("vmname"));
      if(vmRef == null) {
         Console.WriteLine("Virtual Machine " + cb.get_option("vmname") 
                          + " Not Found");
         return;
      }
      String[][] typeInfo = {
         new String[]{"VirtualMachine", "name","summary.quickStats","runtime"}
      };
      PropertySpec[] pSpecs = cb.getServiceUtil().BuildPropertySpecArray(typeInfo);
      ObjectSpec[] oSpecs = null;
      oSpecs = new ObjectSpec[]{new ObjectSpec()};            
      Boolean oneOnly = vmRef != null;
      oSpecs[0].obj = oneOnly ? vmRef : cb.getConnection().ServiceContent.rootFolder;
      oSpecs[0].skip = !oneOnly;
      if(!oneOnly) {
          SelectionSpec[] selectionSpecs = cb.getServiceUtil().buildFullTraversal();
          oSpecs[0].selectSet = selectionSpecs;
      }
      PropertyFilterSpec pSpec = new PropertyFilterSpec();
      pSpec.objectSet = new ObjectSpec[] { oSpecs[0] };
      pSpec.propSet = new PropertySpec[] { pSpecs[0] };  
      ManagedObjectReference propColl = cb.getConnection().PropCol; 
      
      ManagedObjectReference propFilter = cb.getConnection()._service.CreateFilter(propColl, pSpec, false);
         
      
      
      String version = "";
      do {
         UpdateSet update = cb.getConnection()._service.CheckForUpdates(propColl, 
                                                                            version);
         if(update != null && update.filterSet != null) {
            handleUpdate(update);
            version = update.version;
         } else {
            Console.WriteLine("No update is present!");
         }
         Console.WriteLine("");
         Console.WriteLine("Press <Enter> to check for updates");
         Console.WriteLine("Enter 'exit' <Enter> to exit the program");
         String line = Console.ReadLine();
         if(line.Trim().Equals("exit"))
            break;
      }while(true);        
      cb.getConnection()._service.DestroyPropertyFilter(propFilter);
   }
   
   void handleUpdate(UpdateSet update) {
      ArrayList vmUpdates = new ArrayList();
      ArrayList hostUpdates = new ArrayList();
      PropertyFilterUpdate[] pfus = update.filterSet; 
      for(int pfui=0; pfui<pfus.Length; ++ pfui) {
         ObjectUpdate[] ous = pfus[pfui].objectSet;
         for(int oui=0; oui<ous.Length; ++oui) {
            if(ous[oui].obj.type.Equals("VirtualMachine")) {
               vmUpdates.Add(ous[oui]);
            } else if(ous[oui].obj.type.Equals("HostSystem")) {
               hostUpdates.Add(ous[oui]);
            }
         }
      }      
      if(vmUpdates.Count > 0) {
         Console.WriteLine("Virtual Machine updates:");
         for (IEnumerator vmi = vmUpdates.GetEnumerator(); vmi.MoveNext(); )
         {
            handleObjectUpdate((ObjectUpdate)vmi.Current);
         }
      }      
      if(hostUpdates.Count > 0) {
         Console.WriteLine("Host updates:");
         for (IEnumerator vmi = hostUpdates.GetEnumerator(); vmi.MoveNext(); )
         {
             handleObjectUpdate((ObjectUpdate)vmi.Current);
         }
      }
   }
   
   void handleObjectUpdate(ObjectUpdate oUpdate) {
      PropertyChange[] pc = oUpdate.changeSet; 
      if(oUpdate.kind==ObjectUpdateKind.enter) {
         Console.WriteLine(" New Data:");
         handleChanges(pc);
      } else if (oUpdate.kind==ObjectUpdateKind.leave) {
         Console.WriteLine(" Removed Data:");
         handleChanges(pc);
      } else if (oUpdate.kind==ObjectUpdateKind.modify) {
         Console.WriteLine(" Changed Data:");
         handleChanges(pc);
      }
      
   }   
   
   void handleChanges(PropertyChange[] changes) {
      for(int pci=0; pci<changes.Length; ++pci) {
         String name = changes[pci].name;
         Object value = changes[pci].val;
         PropertyChangeOp op = changes[pci].op;
         if(op!=PropertyChangeOp.remove) {
            Console.WriteLine("  Property Name: "+name);
            if("summary.quickStats".Equals(name)) {               
               if(value.GetType().Name.Equals("VirtualMachineQuickStats")) {
                  VirtualMachineQuickStats vmqs = (VirtualMachineQuickStats)value;
                  String cpu = vmqs.overallCpuUsage.ToString() ==null ? "unavailable" :vmqs.overallCpuUsage.ToString();
                  String memory = vmqs.hostMemoryUsage.ToString() ==null ? "unavailable" : vmqs.hostMemoryUsage.ToString();
                  Console.WriteLine("   Guest Status: " + 
                        vmqs.guestHeartbeatStatus.ToString());
                  Console.WriteLine("   CPU Load %: " + cpu);
                  Console.WriteLine("   Memory Load %: " + memory);
               } else if (value.GetType().Name.Equals("HostListSummaryQuickStats")) {
                  HostListSummaryQuickStats hsqs = (HostListSummaryQuickStats)value;
                  String cpu = hsqs.overallCpuUsage.ToString() ==null ? "unavailable" : hsqs.overallCpuUsage.ToString();
                  String memory = hsqs.overallMemoryUsage.ToString() == null ? "unavailable" : hsqs.overallMemoryUsage.ToString();
                  Console.WriteLine("   CPU Load %: " + cpu);
                  Console.WriteLine("   Memory Load %: " + memory);
               }
            } else if("runtime".Equals(name)) {
               if(value.GetType().Name.Equals("VirtualMachineRuntimeInfo")) {
                  VirtualMachineRuntimeInfo vmri = (VirtualMachineRuntimeInfo)value;
                  Console.WriteLine("   Power State: "
                                     + vmri.powerState.ToString());
                  Console.WriteLine("   Connection State: " 
                                     + vmri.connectionState.ToString());
                  DateTime bTime = vmri.bootTime;
                  if(bTime != null) {
                     Console.WriteLine("   Boot Time: " + bTime.TimeOfDay);
                  }
                  long mOverhead = vmri.memoryOverhead;
                  if(mOverhead.ToString() != null) {
                     Console.WriteLine("   Memory Overhead: "+mOverhead);
                  }
              }
              else if (value.GetType().Name.Equals("HostRuntimeInfo"))
              {
                  HostRuntimeInfo hri = (HostRuntimeInfo)value;
                  Console.WriteLine("   Connection State: " 
                                    + hri.connectionState.ToString());
                  DateTime bTime = hri.bootTime;
                  if(bTime != null) {
                     Console.WriteLine("   Boot Time: " + bTime.TimeOfDay);
                  }
               }
            } else if("name".Equals(name)) {
               Console.WriteLine("   "+value);
            } else {
               Console.WriteLine("   "+value.ToString());
            }
         } else {
            Console.WriteLine("Property Name: " +name+ " value removed.");
         }
      }
   }
   
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[1];
      useroptions[0] = new OptionSpec("vmname","String",1
                                     ,"Name of the virtual machine"
                                     ,null);
      return useroptions;
   }
   
   public static void Main(String [] args) {
      GetUpdates obj = new GetUpdates();
      cb = AppUtil.AppUtil.initialize("GetUpdates", GetUpdates.constructOptions(), args);            
      cb.connect();      
      obj.getUpdates();
      cb.disConnect();
   }
    }
}
