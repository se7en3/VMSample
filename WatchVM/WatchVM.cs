using System;
using System.Collections;
using AppUtil;
using Vim25Api;

namespace WatchVM
{
   public class WatchVM
   {
      private static AppUtil.AppUtil cb = null;
      static VimService _service;
      static ServiceContent _sic;  
      public static OptionSpec[] constructOptions()
      {
          OptionSpec[] useroptions = new OptionSpec[1];
          useroptions[0] = new OptionSpec("vmpath", "String", 1
                                          , "Path of Virtual Machine"
                                          , null);
          return useroptions;
      }
      public static void Main(String[] args)
      {
          WatchVM obj = new WatchVM();
          cb = AppUtil.AppUtil.initialize("WatchVM"
                                          , WatchVM.constructOptions()
                                          , args);
          cb.connect();
          obj.Watch();
          cb.disConnect();
      }

      void Watch() {
          PropertyManager PM = new PropertyManager(cb.getConnection(), cb.getConnection().ServiceContent);
          PM.ListenerException += new ListenerExceptionHandler(PM_ListenerException);
          PM.StartListening();
          Watch(PM, cb.get_option("vmpath"));
      }

      void Watch(PropertyManager PM, String path)
      {
         ManagedObjectReference vm = 
            cb.getConnection().Service.FindByInventoryPath(
            cb.getConnection().ServiceContent.searchIndex, path);
         if (vm == null)
         {
            System.Console.WriteLine("Virtual Machine located at path: " + path + " not found.");
            return;
         }

         // Create a FilterSpec
         PropertySpec pSpec = new PropertySpec();
         pSpec.type = vm.type;
         pSpec.pathSet = new String[] { "guest", "summary.quickStats", "summary.runtime.powerState" };
         ObjectSpec oSpec = new ObjectSpec();
         oSpec.obj = vm;
         oSpec.skip = false; oSpec.skipSpecified = true;
         PropertyFilterSpec pfSpec = new PropertyFilterSpec();
         pfSpec.propSet = new PropertySpec[] { pSpec };
         pfSpec.objectSet = new ObjectSpec[] { oSpec };

         Console.WriteLine("Updates being displayed...Press Ctrl-Break to exit");

         PM.Register(pfSpec, false, new PropertyFilterUpdateHandler(DisplayUpdates));

         while (true)
         {
            System.Threading.Thread.Sleep(100);
         }
      }
      
      void DisplayUpdates(Object sender, PropertyFilterUpdateEventArgs eArgs)
      {
         foreach(PropertyChange change in eArgs.FilterUpdate.objectSet[0].changeSet)
         {
            if(change.op == PropertyChangeOp.add || change.op == PropertyChangeOp.assign)
            {
               if("guest" == change.name) 
               {
                  GuestInfo gi = (GuestInfo)change.val;
                  System.Console.WriteLine("GuestInfo.state->{0}\tGuestInfo.toolsStatus->{1}",
                     gi.guestState, gi.toolsStatusSpecified?gi.toolsStatus.ToString():"not-present");
               }
               else if("summary.quickStats" == change.name)
               {
                  VirtualMachineQuickStats qs = (VirtualMachineQuickStats)change.val;
                  System.Console.WriteLine("QuickStats.guestHeartbeatStatus->{0}\tQuickStats.overallCpuUsage->{1}",
                     qs.guestHeartbeatStatus, qs.overallCpuUsage);
               }
               else if("summary.runtime.powerState" == change.name)
               {
                  System.Console.WriteLine("PowerState->{0}", change.val);
               }
            }
         }
      }

      void PM_ListenerException(object sender, ListenerExceptionEventArgs eventArgs)
      {
         //System.Console.WriteLine("Exception: " + eventArgs.Exception.Message);
      }
   }
}
