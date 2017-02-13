using System;
using System.Collections.Generic;
using System.Text;
using AppUtil;
using Vim25Api;
using System.Collections;

namespace Basics
{
    public class Basics
    {
        private static AppUtil.AppUtil cb = null;

        VimService service;
        ServiceContent sic;
        ManagedObjectReference perfMgr;

        private void displayBasics()
        {
            service = cb.getConnection()._service;
            sic = cb.getConnection()._sic;
            perfMgr = sic.perfManager;
            if (cb.get_option("info").Equals("interval"))
            {
                getIntervals(perfMgr, service);
            }
            else if (cb.get_option("info").Equals("counter"))
            {
                getCounters(perfMgr, service);
            }
            else if (cb.get_option("info").Equals("host"))
            {
                ManagedObjectReference hostmor
                   = cb.getServiceUtil().GetDecendentMoRef(null,
                                                          "HostSystem",
                                                           cb.get_option("hostname"));
                if (hostmor == null)
                {
                    Console.WriteLine("Host " + cb.get_option("hostname") + " not found");
                    return;
                }
                getQuerySummary(perfMgr, hostmor, service);
                getQueryAvailable(perfMgr, hostmor, service);
            }
            else
            {
                Console.WriteLine("Invalid info argument [host|counter|interval]");
            }
        }

        private void getIntervals(ManagedObjectReference perfMgr, VimService service)
        {
            Object property = getProperty(service, perfMgr, "historicalInterval");
            PerfInterval[] intervals = (PerfInterval[])property;
            // PerfInterval [] intervals = arrayInterval.perfInterval;
            Console.WriteLine("Performance intervals (" + intervals.Length + "):");
            Console.WriteLine("---------------------");
            for (int i = 0; i != intervals.Length; ++i)
            {
                PerfInterval interval = intervals[i];
                Console.WriteLine(i + ": " + interval.name);
                Console.WriteLine(" -- period = " + interval.samplingPeriod);
                Console.WriteLine(", length = " + interval.length);
            }
            Console.WriteLine();
        }
        private void getCounters(ManagedObjectReference perfMgr, VimService service)
        {
            Object property = getProperty(service, perfMgr, "perfCounter");
            PerfCounterInfo[] counters = (PerfCounterInfo[])property;
            //  PerfCounterInfo[] counters = arrayCounter.getPerfCounterInfo();
            Console.WriteLine("Performance counters (averages only):");
            Console.WriteLine("-------------------------------------");
            foreach (PerfCounterInfo counter in counters)
            {
                if (counter.rollupType == PerfSummaryType.average)
                {
                    ElementDescription desc = counter.nameInfo;
                    Console.WriteLine(desc.label + ": " + desc.summary);
                }
            }
            Console.WriteLine();
        }

        private void getQuerySummary(ManagedObjectReference perfMgr,
                                     ManagedObjectReference hostmor,
                                     VimService service)
        {
            PerfProviderSummary summary = service.QueryPerfProviderSummary(perfMgr, hostmor);
            Console.WriteLine("Host perf capabilities:");
            Console.WriteLine("----------------------");
            Console.WriteLine("  Summary supported: " + summary.summarySupported);
            Console.WriteLine("  Current supported: " + summary.currentSupported);
            if (summary.currentSupported)
            {
                Console.WriteLine("  Current refresh rate: " + summary.refreshRate);
            }
            Console.WriteLine();
        }

        private void getQueryAvailable(ManagedObjectReference perfMgr,
                                       ManagedObjectReference hostmor,
                                       VimService service)
        {
            DateTime end = DateTime.Now;
            DateTime start = end.AddHours(-12);

            PerfMetricId[] metricIds
               = service.QueryAvailablePerfMetric(perfMgr, hostmor, start, true, end, true, 20, true);
            int[] ids = new int[metricIds.Length];
            for (int i = 0; i != metricIds.Length; ++i)
            {
                ids[i] = metricIds[i].counterId;
            }
            PerfCounterInfo[] counters = service.QueryPerfCounter(perfMgr, ids);
            Console.WriteLine("Available metrics for host (" + metricIds.Length + "):");
            Console.WriteLine("--------------------------");
            for (int i = 0; i != metricIds.Length; ++i)
            {
                String label = counters[i].nameInfo.label;
                String instance = metricIds[i].instance;
                Console.WriteLine("   " + label);
                if (instance.Length != 0)
                {
                    Console.WriteLine(" [" + instance + "]");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private Object[] getProperties(VimService service,
                                  ManagedObjectReference moRef,
                                  String[] properties)
        {
            PropertySpec pSpec = new PropertySpec();
            pSpec.type = moRef.type;
            pSpec.pathSet = properties;
            ObjectSpec oSpec = new ObjectSpec();
            oSpec.obj = moRef;
            PropertyFilterSpec pfSpec = new PropertyFilterSpec();
            pfSpec.propSet = (new PropertySpec[] { pSpec });
            pfSpec.objectSet = (new ObjectSpec[] { oSpec });
            ObjectContent[] ocs
               = service.RetrieveProperties(sic.propertyCollector,
                                            new PropertyFilterSpec[] { pfSpec });
            Object[] ret = new Object[properties.Length];
            if (ocs != null)
            {
                for (int i = 0; i < ocs.Length; ++i)
                {
                    ObjectContent oc = ocs[i];
                    DynamicProperty[] dps = oc.propSet;
                    if (dps != null)
                    {
                        for (int j = 0; j < dps.Length; ++j)
                        {
                            DynamicProperty dp = dps[j];
                            for (int p = 0; p < ret.Length; ++p)
                            {
                                if (properties[p].Equals(dp.name))
                                {
                                    ret[p] = dp.val;
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        private Object getProperty(VimService service,
                              ManagedObjectReference moRef,
                              String prop)
        {
            Object[] props = getProperties(service, moRef, new String[] { prop });
            if (props.Length > 0)
            {
                return props[0];
            }
            else
            {
                return null;
            }
        }

        private Boolean customvalidation()
        {
            Boolean valid = true;
            if (cb.get_option("info").Equals("host"))
            {
                if ((!cb.option_is_set("hostname")))
                {
                    Console.WriteLine("Must specify the --hostname"
                                      + " parameter when --info is host");
                    valid = false;
                }
            }
            return valid;
        }
        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[2];
            useroptions[0] = new OptionSpec("info", "String", 1
                                            , "[interval|counter|host]"
                                            , null);
            useroptions[1] = new OptionSpec("hostname", "String", 0,
                                            "Name of the host system",
                                            null);
            return useroptions;
        }
        public static void Main(String[] args)
        {
            Basics obj = new Basics();
            cb = AppUtil.AppUtil.initialize("Basics", Basics.constructOptions(), args);
            Boolean valid = obj.customvalidation();
            if (valid)
            {
                cb.connect();
                obj.displayBasics();
                cb.disConnect();
            }
            Console.WriteLine("Press any key to exit");
            Console.Read();
        }


    }
}
