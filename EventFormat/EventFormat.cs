using System;
using System.Collections;
using AppUtil;
using Vim25Api;

namespace EventFormat
{
   /// <summary>
   /// Retrieve and format the last event - ESX Server or VC Server
   /// </summary>
   public class EventFormat {
      private static AppUtil.AppUtil cb = null;
      static VimService _service;
      static ServiceContent _sic;
      enum FormatType {Full,Vm,Host,ComputeResource,Datacenter}

      /// <summary>
      /// Format the latest event message
      /// </summary>
      private void FormatLatestEvent() {
         try {
            // Get the static EventDescriptionEventDetail[] (format strings etc.)
            ObjectContent[] oc = cb.getServiceUtil().GetObjectProperties(               
                                 cb.getConnection().PropCol,
                                 cb.getConnection().ServiceContent.eventManager,
                                 new String[] {"description.eventInfo"});
            EventDescriptionEventDetail[] eventDetails =  
               oc[0].propSet[0].val as EventDescriptionEventDetail[];
            // 'Map' Between event type and details
            Hashtable eventDetail = new Hashtable();
            foreach (EventDescriptionEventDetail detail in eventDetails) {
               eventDetail[detail.key] = detail;
            }
            // Get an Event
            oc = cb.getServiceUtil().GetObjectProperties(
                 cb.getConnection().PropCol,
                 cb.getConnection().ServiceContent.eventManager,
                 new String[] {"latestEvent"});
            
            Event anEvent = oc[0].propSet[0].val as Event;
            Console.WriteLine("The latestEvent was:" + anEvent.GetType());
            Console.WriteLine(FormatEvent(FormatType.Vm, eventDetail, anEvent));
            Console.WriteLine(FormatEvent(FormatType.Host, eventDetail, anEvent));
            Console.WriteLine(FormatEvent(FormatType.ComputeResource, eventDetail, anEvent));
            Console.WriteLine(FormatEvent(FormatType.Datacenter, eventDetail, anEvent));
            Console.WriteLine(FormatEvent(FormatType.Full, eventDetail, anEvent));
         } 
         catch (Exception e) {            
            cb.log.LogLine("EventFormat : Failed Formatting the event.");
            throw e;
         }
         cb.log.LogLine("EventFormat : Successful Formatting the event.");
      }

      /// <summary>
      /// This function formats the event message using the format strings
      /// in the EventDescriptionEventDetail for the event passed in
      /// using the passed in format requested
      /// </summary>
      /// <param name="fType">The format type you wish to display</param>
      /// <param name="eventDetail">Map of Event typename to EventDescriptionEventDetail objects</param>
      /// <param name="theEvent">The Event to format the message for</param>
      /// <returns></returns>
      String FormatEvent(FormatType fType, Hashtable eventDetail, Event theEvent)
      {
         // EventDescriptionEventDetail contains format strings and category for the event
         // There are 5 format strings to use depending on which context:
         // formatOnComputeResource - Used for the ComputeResource (Usually a cluster) context
         // formatOnDatacenter      - Used for the Datacenter context
         // formatOnHost            - Used for the HostSystem context
         // formatOnVm              - Used for the VirtualMachine context
         // fullFormat              - Used for a fully qualified context
          
         // The place holder used for string replacement has the following format:
         // 
         //    {<property-path>}
         //
         // Where <property-path> is the path to the data that should be used to
         // replace the place holder. These are relative to the event in question

         // For example, the messages for the Event type 'VmPoweredOnEvent' are:
         //   formatOnComputeResource - "{vm.name} on  {host.name} is powered on"
         //   formatOnDatacenter      - "{vm.name} on  {host.name} is powered on"
         //   formatOnHost            - "{vm.name} is powered on"
         //   formatOnVm              - "Virtual machine on {host.name} is powered on"
         //   fullFormat              - "{vm.name} on  {host.name} in {datacenter.name} is powered on"

         // The messages for the Event type 'VmRenamedEvent' are:
         //   formatOnComputeResource - "Renamed {vm.name} from {oldName} to {newName}"
         //   formatOnDatacenter      - "Renamed {vm.name} from {oldName} to {newName}"
         //   formatOnHost            - "Renamed {vm.name} from {oldName} to {newName}"
         //   formatOnVm              - "Renamed from {oldName} to {newName}"
         //   fullFormat              - "Renamed {vm.name} from {oldName} to {newName} in {datacenter.name}"

         // To handle event messages in a general way you would need to handle each type
         // in a specific way.

         String typeName = theEvent.GetType().Name;
         EventDescriptionEventDetail detail = eventDetail[typeName] as EventDescriptionEventDetail;

         // Determine format string
         String format = detail.fullFormat;
         switch (fType)
         {
               case FormatType.ComputeResource:
               format = detail.formatOnComputeResource;
                  break;
               case FormatType.Datacenter:
               format = detail.formatOnDatacenter;
                  break;
               case FormatType.Host:
               format = detail.formatOnHost;
                  break;
               case FormatType.Vm:
               format = detail.formatOnVm;
                  break;
         }

         switch (typeName)
         {
               case "VmPoweredOnEvent":
                  return ReplaceText(format, theEvent as VmPoweredOnEvent);
               case "VmRenamedEvent":
                  return ReplaceText(format, theEvent as VmRenamedEvent);
               case "UserLoginSessionEvent":
                  return ReplaceText(format, theEvent as UserLoginSessionEvent);
               default:
               // Try generic, if all values are replaced by base type
               // return that, else return fullFormattedMessage;
               String ret = ReplaceText(format, theEvent);
               if(ret.Length==0 || ret.IndexOf("{") != -1)
                  ret = theEvent.fullFormattedMessage;
               return ret;
         }
      }

      String ReplaceText(String format, UserLoginSessionEvent theEvent)
      {
         // Do base first
         format = ReplaceText(format, theEvent as Event);
         // Then specific values
         format = format.Replace("{ipAddress}", theEvent.ipAddress);         
         return format;
      }

      String ReplaceText(String format, VmPoweredOnEvent theEvent)
      {
         // Same as base type
         return ReplaceText(format, theEvent as Event);
      }

      String ReplaceText(String format, VmRenamedEvent theEvent)
      {
         // Do base first
         format = ReplaceText(format, theEvent as Event);
         // Then specific values
         format = format.Replace("{oldName}", theEvent.oldName);         
         format = format.Replace("{newName}", theEvent.newName);
         return format;
      }

      String ReplaceText(String format, Event theEvent)
      {
         format = format.Replace("{userName}", theEvent.userName);
         if(theEvent.computeResource != null)
            format = format.Replace("{computeResource.name}", theEvent.computeResource.name);         
         if(theEvent.datacenter != null)
            format = format.Replace("{datacenter.name}", theEvent.datacenter.name);         
         if(theEvent.host != null)
            format = format.Replace("{host.name}", theEvent.host.name);         
         if(theEvent.vm != null)
            format = format.Replace("{vm.name}", theEvent.vm.name);         
         return format;
      }

      /// <summary>
      /// The main entry point for the application.
      /// </summary>      
       public static void Main(String[] args)
       {
           EventFormat obj = new EventFormat();
           cb = AppUtil.AppUtil.initialize("EventFormat"
                                           , args);
           cb.connect();
           obj.FormatLatestEvent();
           cb.disConnect();
           Console.WriteLine("Press any key to exit: ");
           Console.Read();
       }
   }
}
