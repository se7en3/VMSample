using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Threading;


namespace RealTime
{
    ///<summary>
    ///This sample displays the performance measurements of selected cpu counters of specified
    ///Virtual machine (if available) from the current time at the console.
    ///</summary>
    ///<param name="vmname">Required: Name of virtual machine.</param>
    ///<remarks>
    ///--url [webserviceurl] --username [username] --password [password] 
    ///--vmname [myVMName]
    ///</remarks>

    public class RealTime {
        private static AppUtil.AppUtil cb = null;

        private void doRealTime() {
        ManagedObjectReference vmmor 
         = cb.getServiceUtil().GetDecendentMoRef(null,
                                                "VirtualMachine",
                                                cb.get_option("vmname"));
        if(vmmor!=null) {
             ManagedObjectReference pmRef 
                = cb.getConnection()._sic.perfManager;
             PerfCounterInfo[] cInfo 
                = (PerfCounterInfo[])cb.getServiceUtil().GetDynamicProperty(pmRef, 
                                                                            "perfCounter");
             ArrayList vmCpuCounters = new ArrayList();
             for(int i=0; i<cInfo.Length; ++i) {
                if("cpu".Equals(cInfo[i].groupInfo.key)) {
                   vmCpuCounters.Add(cInfo[i]);
                }
             }
             Hashtable counters = new Hashtable();
             while(true) {
                int index=0;
                for(IEnumerator it = vmCpuCounters.GetEnumerator(); it.MoveNext();) {
                   PerfCounterInfo pcInfo = (PerfCounterInfo)it.Current;
                   Console.WriteLine(++index +" - "+ pcInfo.nameInfo.summary);
                }
                index = cb.getUtil().getIntInput("Please select a counter from"
                                                 +" the above list"+ "\nEnter 0 to end: ", 1);
                if(index > vmCpuCounters.Count||index<=0) {
                   Console.WriteLine("*** Value out of range!");
                } else {
                   --index;
                if(index<0) return;
                   PerfCounterInfo pcInfo = (PerfCounterInfo)vmCpuCounters[index];
                   counters.Add((pcInfo.key), pcInfo);
                   break;
                }
             }
             PerfMetricId[] aMetrics 
                = cb.getConnection()._service.QueryAvailablePerfMetric(pmRef, 
                                                                        vmmor, 
                                                                        DateTime.MinValue, 
                                                                        false,
                                                                        DateTime.MaxValue, 
                                                                        false,
                                                                        20,true);
             ArrayList mMetrics = new ArrayList();
             if(aMetrics != null) {
                for(int index=0; index<aMetrics.Length; ++index) {
                   if(counters.ContainsKey(aMetrics[index].counterId)) {
                      mMetrics.Add(aMetrics[index]);
                   }
                }
             }
             if (mMetrics.Count > 0) {
                 monitorPerformance(pmRef, vmmor, mMetrics, counters);
             } else {
                 Console.WriteLine("Data for selected counter is not available");
             }
          } else {
            Console.WriteLine("Virtual Machine " + cb.get_option("vmname") + " not found");
          }
       }

       ///<summary>
       ///Monitors the performance.
       ///</summary>
       ///<param name="pmRef"></param>
       ///<param name="vmRef"></param>
       ///<param name="mMetrics"></param>
       ///<param name="counters"></param>
       void monitorPerformance(ManagedObjectReference pmRef,
                                 ManagedObjectReference vmRef, 
                                 ArrayList mMetrics,
                                 Hashtable counters) {
          PerfMetricId[] metricIds = (new PerfMetricId[] { (PerfMetricId)mMetrics[0] });
          PerfQuerySpec qSpec = new PerfQuerySpec();
          qSpec.entity=vmRef;
          qSpec.maxSample = 10;
          qSpec.maxSampleSpecified = true;
          qSpec.metricId= metricIds;
          qSpec.intervalId= 20;
          qSpec.intervalIdSpecified = true;

          PerfQuerySpec[] qSpecs = new PerfQuerySpec[] {qSpec};
          Boolean continueDataCol = true;
          while (continueDataCol) {
             PerfEntityMetricBase[] pValues 
                = cb.getConnection()._service.QueryPerf(pmRef,qSpecs);
             if(pValues != null) displayValues(pValues, counters);
                 Console.WriteLine("Do you want to continue: Y/N");
                 string con = Console.ReadLine();
                  if( con.Equals("Y")||con.Equals("y")){
                 Console.WriteLine("Sleeping 10 seconds...");
                 Thread.Sleep(10*1000);
              } else{
                  continueDataCol = false;
              }
          }
       }

       ///<summary>
       ///Displays the values of sample time range.
       ///</summary>
       ///<param name="values"></param>
       ///<param name="counters"></param>
       void displayValues(PerfEntityMetricBase[] values, Hashtable counters) {
          for(int i=0; i<values.Length; ++i) {
             PerfMetricSeries[] vals = ((PerfEntityMetric)values[i]).value;
             PerfSampleInfo[]  infos = ((PerfEntityMetric)values[i]).sampleInfo;
             Console.WriteLine("Sample time range: " +
                               infos[0].timestamp.TimeOfDay.ToString() + " - " +
                               infos[infos.Length-1].timestamp.TimeOfDay.ToString());
             for(int vi=0; vi<vals.Length; vi++) {
                PerfCounterInfo pci 
                   = (PerfCounterInfo)counters[vals[vi].id.counterId];
                if(pci != null)
                   Console.WriteLine(pci.nameInfo.summary);
                if(vals[vi].GetType().Name.Equals("PerfMetricIntSeries")) {
                   PerfMetricIntSeries val = (PerfMetricIntSeries)vals[vi];
                   long[] longs = val.value;
                   for(int k=0; k<longs.Length; ++k) {
                      Console.WriteLine(longs[k] + " ");
                   }
                   Console.WriteLine();
                }
             }
          }
       }

       /// <summary>
       /// This method is used to add application specific user options 
       /// </summary>
       ///<returns> Array of OptionSpec containing the details of application 
       /// specific user options 
       ///</returns>
       private static OptionSpec[] constructOptions() {
          OptionSpec [] useroptions = new OptionSpec[1];
          useroptions[0] = new OptionSpec("vmname","String",1
                                          ,"Name of the virtual machine"
                                          ,null);
          return useroptions;
       }

       /// <summary>
       ///  The main entry point for the application.
       /// </summary>
       public static void Main(String[] args) {
          RealTime obj = new RealTime();
          cb = AppUtil.AppUtil.initialize("RealTime"
                                  ,RealTime.constructOptions()
                                  ,args);
          cb.connect();
          obj.doRealTime();
          cb.disConnect();
          Console.WriteLine("Press any key to exit");
          Console.Read();
       }
    }
}
