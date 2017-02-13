using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text;
using AppUtil;
using Vim25Api;

namespace EventHistoryCollectorMonitor
{
    public class EventHistoryCollectorMonitor
    {
   private static AppUtil.AppUtil cb = null;
   private VimService _service;           // All webservice methods
   private ServiceContent _sic;            
   private ManagedObjectReference _propCol; // PropertyCollector Reference

   // EventManager and EventHistoryCollector References
   private ManagedObjectReference _eventManager;
   private ManagedObjectReference _eventHistoryCollector;  
   
   private void initialize() {
      _sic = cb.getConnection()._sic;
      _service = cb.getConnection()._service;
      // The PropertyCollector and EventManager References are present
      // in the ServiceInstanceContent
      _propCol = _sic.propertyCollector;
      _eventManager = _sic.eventManager;
   }
  
   private void createEventHistoryCollector()  {
      EventFilterSpec eventFilter = new EventFilterSpec();
      _eventHistoryCollector = 
      _service.CreateCollectorForEvents(_eventManager, eventFilter);
   }
  
   private PropertyFilterSpec createEventFilterSpec() {
      // Set up a PropertySpec to use the latestPage attribute 
      // of the EventHistoryCollector
      PropertySpec propSpec = new PropertySpec();
      propSpec.all=false;
      propSpec.allSpecified = true;
      propSpec.pathSet =new String[] { "latestPage" };
      propSpec.type = _eventHistoryCollector.type;
      
      // PropertySpecs are wrapped in a PropertySpec array
      PropertySpec[] propSpecAry = new PropertySpec[] { propSpec };
      
      // Set up an ObjectSpec with the above PropertySpec for the
      // EventHistoryCollector we just created
      // as the Root or Starting Object to get Attributes for.
      ObjectSpec objSpec = new ObjectSpec();
      objSpec.obj =_eventHistoryCollector;
      objSpec.skip = false;
       objSpec.skipSpecified = true;
      
      // Get Event objects in "latestPage" from "EventHistoryCollector"
      // and no "traversl" further, so, no SelectionSpec is specified 
      objSpec.selectSet=new SelectionSpec[] { };
      
      // ObjectSpecs are wrapped in an ObjectSpec array
      ObjectSpec[] objSpecAry = new ObjectSpec[] { objSpec };
      PropertyFilterSpec spec = new PropertyFilterSpec();
      spec.propSet=propSpecAry;
      spec.objectSet=objSpecAry;
      return spec;
   }
   
   private void monitorEvents(PropertyFilterSpec spec)  {
      // Get all Events returned from the EventHistoryCollector
      // This will result in a large number of events, depending on the
      // page size of the latestPage.
      ObjectContent[] objectContents =
         cb._svcUtil.retrievePropertiesEx(_propCol, new PropertyFilterSpec[] { spec });
      // Print out class names of the Events we got back 
      if (objectContents != null) {
         
         Event[] events = (Event[])objectContents[0].propSet[0].val;

         Console.WriteLine("Events In the latestPage are : ");
         for (int i = 0; i < events.Length; i++) {
            Event anEvent = events[i];
            Console.WriteLine("Event: " + anEvent.GetType().ToString());
         }
      } else {
         Console.WriteLine("No Events retrieved!");
      }
   }

   public static void Main(String[] args) {
      try {
         EventHistoryCollectorMonitor eventMonitor 
            = new EventHistoryCollectorMonitor();
         cb = AppUtil.AppUtil.initialize("EventFormat",args);
         cb.connect();         
         eventMonitor.initialize();         
         eventMonitor.createEventHistoryCollector();         
         PropertyFilterSpec eventFilterSpec = eventMonitor.createEventFilterSpec();
         eventMonitor.monitorEvents(eventFilterSpec);
         cb.disConnect();
         Console.WriteLine("Press enter to exit: ");
         Console.Read();
      } 
      catch (Exception e) {
         Console.WriteLine("Caught Exception : " +
                             " Name : " + e.Data.ToString() +
                            " Message : " + e.Message.ToString() +
                            " Trace : ");
         e.StackTrace.ToString();
      }
   }
    }
}
