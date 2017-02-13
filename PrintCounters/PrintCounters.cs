using System;
using System.Collections.Generic;
using System.Text;
using AppUtil;
using Vim25Api;
using System.IO;
using System.Collections;


namespace PrintCounters
{
    class PrintCounters
    {
        private static AppUtil.AppUtil cb = null;    
    
   private void printCounters() {       
      String entityType = cb.get_option("entitytype");      
      
      if(entityType.Equals("HostSystem")) {
         printEntityCounters("HostSystem");
      }
      else if(entityType.Equals("VirtualMachine")) {
          ManagedObjectReference vmMor = getManagedObjectReference(entityType);
          if (vmMor != null)
          {
              VirtualMachineConfigInfo vmConfig = (VirtualMachineConfigInfo)cb.getServiceUtil().GetDynamicProperty(vmMor, "config");
              Boolean tmp = vmConfig.template;

              if (tmp)
              {
                  Console.WriteLine("Entity Argument passed is a template. ");
              }
              else
              {
                  printEntityCounters("VirtualMachine");
              }
          }
          else
          {
            Console.WriteLine("Entered virtual machine " + cb.get_option("entityname") + " doesn't exists ");
          }

      }
      else if(entityType.Equals("ResourcePool")) {
         printEntityCounters("ResourcePool");
      }
      else {
         Console.WriteLine("Entity Argument must be "
                          +"[HostSystem|VirtualMachine|ResourcePool]");
      }
   }

   private void printEntityCounters(String entityType){
      ManagedObjectReference mor = getManagedObjectReference(entityType);
      ManagedObjectReference pmRef 
         = cb.getConnection()._sic.perfManager;      
      PerfCounterInfo[] cInfo 
         = (PerfCounterInfo[])cb.getServiceUtil().GetDynamicProperty(pmRef, 
                                                              "perfCounter");
      if(mor!=null) {
         ArrayList ids = getPerfIdsAvailable(pmRef, mor);
        StreamWriter sw = new StreamWriter(cb.get_option("filename"));
         if(cInfo !=null) {
            sw.Write("<perf-counters>"); 
            for(int c=0; c<cInfo.Length; ++c) {
               PerfCounterInfo pci = cInfo[c];
               int id = pci.key;
               if(ids.Contains(id)) {
                  sw.Write("  <perf-counter key=\"");
                  sw.Write(id);
                  sw.Write("\" ");
           
                  sw.Write("rollupType=\"");
                  sw.Write(pci.rollupType.ToString());
                  sw.Write("\" ");
           
                  sw.Write("statsType=\"");
                  sw.Write(pci.statsType.ToString());
                  sw.Write("\">");
                  printElementDescription(sw, "groupInfo", pci.groupInfo);
                  printElementDescription(sw, "nameInfo", pci.nameInfo);
                  printElementDescription(sw, "unitInfo", pci.unitInfo);
           
                  sw.Write("    <entity type=\""+entityType+"\"/>");
                  int[] ac = pci.associatedCounterId;
                  if(ac != null) {
                     for(int a=0; a<ac.Length; ++a) {
                        sw.Write("    <associatedCounter>"+ac[a]
                                   +"</associatedCounter>");
                     }
                  }
                  sw.Write("  </perf-counter>");
               }
            }
            sw.Write("</perf-counters>");
            sw.Flush();
            sw.Close();
            
         }
         Console.WriteLine("Check " + cb.get_option("filename") 
                         + " for Print Counters");
      }
      else {
          Console.WriteLine(entityType + " " + cb.get_option("entityname") 
                                       + " not found.");
      }
   }    
    
   private void printElementDescription(StreamWriter sw, 
                                       String name, 
                                       ElementDescription ed) {
      sw.Write("   <"+ name + "-key>");
      sw.Write(ed.key);
      sw.Write("</" + name + "-key>");
     
      sw.Write("   <"+ name + "-label>");
      sw.Write(ed.label);
      sw.Write("</" + name + "-label>");
  
      sw.Write("   <"+ name + "-summary>");
      sw.Write(ed.summary);
      sw.Write("</" + name + "-summary>");
   }
  
   private ArrayList getPerfIdsAvailable(ManagedObjectReference perfMoRef,
                                  ManagedObjectReference entityMoRef) {
      ArrayList ret = new ArrayList();      
      if(entityMoRef != null) {
         PerfMetricId[] ids 
            = cb.getConnection()._service.QueryAvailablePerfMetric(perfMoRef, 
                                                                   entityMoRef, 
                                                                   DateTime.MinValue,
                                                                    false,
                                                                    DateTime.MinValue,
                                                                    false,
                                                                    300,true);
         if(ids != null){
            for(int i=0; i<ids.Length; ++i) {
               ret.Add(ids[i].counterId);
            }
         }
      }
      return ret;
   }    
   
   private ManagedObjectReference getManagedObjectReference(String entityType) 
        {
      ManagedObjectReference obj 
         = cb.getServiceUtil().GetDecendentMoRef(null,entityType,
                                                 cb.get_option("entityname"));
      return obj;
   }
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[3];
      useroptions[0] = new OptionSpec("entitytype","String",1
                                     ,"Type of the Entity "
                                     +"[HostSystem|VirtualMachine|ResourcePool]"
                                     ,null);
      useroptions[1] = new OptionSpec("entityname","String",1,
                                     "Name of the Managed Entity",
                                     null);
      useroptions[2] = new OptionSpec("filename","String",1,
                                     "Name of the file",
                                     null);
      return useroptions;
   }
        public static void Main(String[] args)
        {
      PrintCounters obj = new PrintCounters();
      cb = AppUtil.AppUtil.initialize("PrintCounters",
                              PrintCounters.constructOptions()
                              ,args);      
      cb.connect();
      obj.printCounters();
      cb.disConnect();
      Console.WriteLine("Press any key to exit");
      Console.Read();
   }
    }
}
