using System;
using System.Collections.Generic;
using System.Text;
using Vim25Api;
using AppUtil;
using System.Collections;

namespace History
{
    ///<summary>
    ///This sample displays the performance measurements of specified counter of specified
    ///Host System (if available) for duration of last 20 minutes(by default, if not specified)
    ///at the console.
    ///</summary>
    ///<param name="hostname">Required: Name of the host from which to obtain counter data.</param>
    ///<param name="interval">Required: Sampling interval.</param>
    ///<param name="starttime">Optional: Number of minutes to go back in time to start retrieving
    ///metrics. Default is 20.</param>
    ///<param name="duration">Optional: Duration for which to retrieve metrics. Default is 20. 
    ///Cannot be larger value than starttime.</param>
    ///<param name="groupname">Required: cpu, memory</param>
    ///<param name="countername">Required: Usage, Overhead.</param>
    ///<remarks>
    ///--url [webserviceurl]
    ///--username [username]  --password [password] --hostname [name of the history server] 
    ///--groupname cpu --countername usage
    ///To display historical values for the specified group and counter:
    ///run.bat com.vmware.samples.performance.History --url [webserviceurl]
    ///--username [username]  --password [password] --hostname []  --groupname cpu --countername usage
    ///</remarks>
    ///
    class History
    {
       private static AppUtil.AppUtil cb = null;
       Hashtable _pci = new Hashtable();

        /// <summary>
        ///Displays the performance measurements of specified counter of specified
        ///Host System.
        /// </summary>
       private void displayHistory() {
          ManagedObjectReference hostmor 
             = cb.getServiceUtil().GetDecendentMoRef(null,"HostSystem",
                                                     cb.get_option("hostname"));
          if(hostmor == null) {
             Console.WriteLine("Host " + cb.get_option("hostname") + " not found");
             return;
          }
          ManagedObjectReference pmRef 
             = cb.getConnection()._sic.perfManager;
          CounterInfo(pmRef);

          //Retrieves all configured historical archive sampling intervals.
          PerfInterval[] intervals 
             = (PerfInterval[])cb.getServiceUtil().GetDynamicProperty(pmRef, 
                                                                     "historicalInterval");
          int interval = (int.Parse(cb.get_option("interval")));
          Boolean valid = checkInterval(intervals,interval);
          if(!valid) {
             Console.WriteLine("Invalid inerval, Specify one from above");
             return;
          }

          PerfCounterInfo pci = getCounterInfo(cb.get_option("groupname"), 
                                               cb.get_option("countername"),
                                               PerfSummaryType.average);
          if(pci == null) {
             Console.WriteLine("Incorrect Group Name and Counters Specified");
             return;
          }

          //specifies the query parameters to be used while retrieving statistics.
          //e.g. entity, maxsample etc.
          PerfQuerySpec qSpec = new PerfQuerySpec();
          qSpec.entity = hostmor;
          qSpec.maxSample= 10;
          qSpec.maxSampleSpecified = true;
          PerfQuerySpec[] qSpecs = new PerfQuerySpec[] {qSpec};

          DateTime sTime;
          DateTime eTime = cb.getConnection()._service.CurrentTime(
                              cb.getConnection().ServiceRef);

          double duration = double.Parse(cb.get_option("duration"));
          double startTime = double.Parse(cb.get_option("starttime"));


          sTime = eTime.AddMinutes(-duration);

          Console.WriteLine("Start Time " + sTime.TimeOfDay.ToString());
          Console.WriteLine("End Time   " + eTime.TimeOfDay.ToString());

          Console.WriteLine();
          //Retrieves the query available of performance metric for a host.
          PerfMetricId[] aMetrics 
             = cb.getConnection()._service.QueryAvailablePerfMetric(pmRef, 
                                                                        hostmor, 
                                                                        sTime, 
                                                                        true,
                                                                        eTime, 
                                                                        true,
                                                                        interval,true);
          PerfMetricId ourCounter = null;

          for(int index=0; index<aMetrics.Length; ++index) {
             if(aMetrics[index].counterId == pci.key) {
                ourCounter = aMetrics[index];
                break;
             }
          }
          if(ourCounter == null) {
             Console.WriteLine("No data on Host to collect. "
                               +"Has it been running for at least " 
                               + cb.get_option("duration") + " minutes");
          } else {
             qSpec = new PerfQuerySpec();
             qSpec.entity= hostmor;
             qSpec.startTime =sTime;
             qSpec.endTime= eTime;
             qSpec.metricId = (new PerfMetricId[]{ourCounter});
             qSpec.intervalId =interval;
             qSpec.intervalIdSpecified = true;
             qSpec.startTimeSpecified = true;
             qSpec.endTimeSpecified = true;
             qSpecs = new PerfQuerySpec[] {qSpec};
             //
             PerfEntityMetricBase[] samples 
                = cb.getConnection()._service.QueryPerf(pmRef, qSpecs);
             if(samples != null) {
                displayValues(samples, pci, ourCounter, interval);
             }
             else {
                Console.WriteLine("No Smaples Found");
             }
          }
       }
        /// <summary>
        /// Validate the input interval.
        /// </summary>
        /// <param name="intervals"></param>
        /// <param name="interval"></param>
        /// <returns>returns a Boolean type value true or false</returns>
       private Boolean checkInterval(PerfInterval [] intervals, int interval) {
          Boolean flag = false;
          for(int i=0; i<intervals.Length; ++i) {
             PerfInterval pi = intervals[i];
             if(pi.samplingPeriod == interval){
                flag = true;
                break;
             }
          }
          if(!flag){
             Console.WriteLine("Available summary collection intervals");
             Console.WriteLine("Period\tLength\tName");
             for(int i=0; i<intervals.Length; ++i) {
                PerfInterval pi = intervals[i];
                Console.WriteLine(pi.samplingPeriod + "\t"
                                  +pi.length+"\t"+pi.name);
             }
             Console.WriteLine();
          }
          return flag;
       }

        ///<summary>
       ///Retrieves counter information.
       ///</summary>
       ///<param name="pmRef"></param>
       private void CounterInfo(ManagedObjectReference pmRef) {
          PerfCounterInfo[] cInfos 
             = (PerfCounterInfo[])cb.getServiceUtil().GetDynamicProperty(pmRef, 
                                                                        "perfCounter");
          for(int i=0; i<cInfos.Length; ++i) {
             PerfCounterInfo cInfo = cInfos[i]; 
             String group = cInfo.groupInfo.key;
             Hashtable nameMap = null;
             if(!_pci.ContainsKey(group)) {
                 nameMap = new Hashtable();
                _pci.Add(group, nameMap);
             } else {
                nameMap = (Hashtable)_pci[group];
             }
             String name = cInfo.nameInfo.key;
             ArrayList counters = null;
             if(!nameMap.ContainsKey(name)) {
                counters = new ArrayList();
                nameMap.Add(name, counters);
             } else {
                counters = (ArrayList)nameMap[name];
             }
             counters.Add(cInfo);
          }
       }

        ///<summary>
        ///Retrieves counter information for given groupname and countername.
        ///</summary>
        ///<param name="groupName"></param>
        ///<param name="counterName"></param>
        ///<returns>Returns the arraylist of counter name.</returns>
       private ArrayList getCounterInfos(String groupName, String counterName) {
          Hashtable nameMap = (Hashtable)_pci[groupName];
          if(nameMap != null) {
             ArrayList ret = (ArrayList)nameMap[counterName];
             if(ret != null) {
                return new ArrayList(ret);
             }
          }
          return null;
       }

       /// <summary>
       /// Retrieves counter information for given groupname and countername and rolluptype.
       /// </summary>
       /// <param name="groupName"></param>
       /// <param name="counterName"></param>
       /// <param name="rollupType"></param>
       /// <returns>
       /// Returns the object type with the current information of performance counter
       /// </returns>
       private PerfCounterInfo getCounterInfo(String groupName, 
                                              String counterName,
                                              PerfSummaryType rollupType) {
          ArrayList counters = getCounterInfos(groupName, counterName);
          if(counters != null) {
             for(IEnumerator i=counters.GetEnumerator(); i.MoveNext();) {
                PerfCounterInfo pci = (PerfCounterInfo)i.Current;
                if(rollupType == null || rollupType.Equals(pci.rollupType)) {
                   return pci;
                }
             }
          }
          return null;
       }

        /// <summary>
        /// Displays the values timestamps, intervals, instances etc.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="pci"></param>
        /// <param name="pmid"></param>
        /// <param name="interval"></param>
       private void displayValues(PerfEntityMetricBase[] values, 
                                  PerfCounterInfo pci, 
                                  PerfMetricId pmid,
                                  int interval) {
          for(int i=0; i<values.Length; ++i) {
             PerfMetricSeries[] vals = ((PerfEntityMetric)values[i]).value;
             PerfSampleInfo[]  infos = ((PerfEntityMetric)values[i]).sampleInfo;
             if (infos == null || infos.Length == 0) {
                Console.WriteLine("No Samples available. Continuing.");
                continue;
             }
             Console.WriteLine("Sample time range: " 
                               + infos[0].timestamp.TimeOfDay.ToString() + " - " +
                                infos[infos.Length-1].timestamp.TimeOfDay.ToString() +
                                ", read every "+interval+" seconds");
             for(int vi=0; vi<vals.Length; ++vi) {
                if(pci != null) {
                   if(pci.key != vals[vi].id.counterId)
                   continue;
                   Console.WriteLine(pci.nameInfo.summary 
                                    + " - Instance: " + pmid.instance);
                }
                if(vals[vi].GetType().Name.Equals("PerfMetricIntSeries")){
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
        /// Validate if the start time is greater than duration.
        /// </summary>
        /// <returns>Returns the Boolean value true or false.</returns>
       private Boolean customValidation() {
          int duration = int.Parse(cb.get_option("duration"));
          int starttime = int.Parse(cb.get_option("starttime"));
          if(duration > starttime) {
             Console.WriteLine("Duration must be less than startime");
             return false;
          }
          else {
             return true;
          }
       }

       /// <summary>
       /// This method is used to add application specific user options 
       /// </summary>
       ///<returns> Array of OptionSpec containing the details of application 
       /// specific user options 
       ///</returns>
       ///
       private static OptionSpec[] constructOptions() {
          OptionSpec [] useroptions = new OptionSpec[6];
          useroptions[0] = new OptionSpec("hostname","String",1
                                          ,"Name of the host system"
                                          ,null);
          useroptions[1] = new OptionSpec("interval","String",1,
                                          "Sampling Interval",
                                          null);
          useroptions[2] = new OptionSpec("starttime","String",0
                                          ,"In minutes, to specfiy what's start time " +
                                          "from which samples needs to be collected"
                                          ,"20");
          useroptions[3] = new OptionSpec("duration","String",0,
                                          "Duration for which samples needs to be taken",
                                          "20");
          useroptions[4] = new OptionSpec("groupname","String",1
                                          ,"cpu, mem etc..."
                                          ,null);
          useroptions[5] = new OptionSpec("countername","String",1,
                                          "usage, overhead etc...",
                                          null);
          return useroptions;
       }

       /// <summary>
       ///  The main entry point for the application.
       /// </summary>
       public static void Main(String[] args)  {
          History obj = new History();
          cb = AppUtil.AppUtil.initialize("History",
                                  History.constructOptions()
                                 ,args);
          Boolean valid = obj.customValidation();
          if(valid) {
             cb.connect();
             obj.displayHistory();
             cb.disConnect();
             Console.WriteLine("Press any key to exit");
             Console.Read();
          }
       }
    }
}
