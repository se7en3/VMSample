using System;
using System.Collections.Generic;
using System.Web.Services.Protocols;
using System.Text;
using AppUtil;
using Vim25Api;

namespace VMEventHistoryCollectorMonitor
{
   public class VMEventHistoryCollectorMonitor
    {
       private static AppUtil.AppUtil cb = null;   
   private VimService _service;           // All webservice methods   
   private ServiceContent _sic;            
   private ManagedObjectReference _propCol; // PropertyCollector Reference
   private ManagedObjectReference _searchIndex;
   
   // EventManager and EventHistoryCollector References
   private ManagedObjectReference _eventManager;
   private ManagedObjectReference _eventHistoryCollector;      

   /**
    * Initialize the necessary Managed Object References needed here
    */
   private void initialize() {
      _sic = cb.getConnection()._sic;
      _service = cb.getConnection()._service;
      // The SearchIndex Reference is present in the ServiceInstanceContent
      _searchIndex = _sic.searchIndex;
      
      // The PropertyCollector and EventManager References are present
      // in the ServiceInstanceContent
      _propCol = _sic.propertyCollector;
      _eventManager = _sic.eventManager;
   }
   
   private ManagedObjectReference _virtualMachine;
  
   public Boolean findVirtualMachine()  {
      String vmPath = cb.get_option("vmpath");
     // Console.Write(vmPath);
      _virtualMachine = _service.FindByInventoryPath(_searchIndex, vmPath);
      if(_virtualMachine !=null){
         return true;
      }
      else return false;
   }
   
   private void createEventHistoryCollector()  {
      // Create an Entity Event Filter Spec to 
      // specify the MoRef of the VM to be get events filtered for 
      EventFilterSpecByEntity entitySpec = new EventFilterSpecByEntity();
      entitySpec.entity =_virtualMachine;
      // we are only interested in getting events for the VM
      entitySpec.recursion = EventFilterSpecRecursionOption.self;
      // set the entity spec in the EventFilter
      EventFilterSpec eventFilter = new EventFilterSpec();
      eventFilter.entity = entitySpec;
      // create the EventHistoryCollector to monitor events for a VM 
      // and get the ManagedObjectReference of the EventHistoryCollector returned
      _eventHistoryCollector = 
      _service.CreateCollectorForEvents(_eventManager, eventFilter);
   }

   private PropertyFilterSpec createEventFilterSpec() {
      // Set up a PropertySpec to use the latestPage attribute 
      // of the EventHistoryCollector

      PropertySpec propSpec = new PropertySpec();
      propSpec.all = false;
      propSpec.pathSet=new String[] { "latestPage" };
      propSpec.type =_eventHistoryCollector.type;

      // PropertySpecs are wrapped in a PropertySpec array
      PropertySpec[] propSpecAry = new PropertySpec[] { propSpec };
         
      // Set up an ObjectSpec with the above PropertySpec for the
      // EventHistoryCollector we just created
      // as the Root or Starting Object to get Attributes for.
      ObjectSpec objSpec = new ObjectSpec();
      objSpec.obj =_eventHistoryCollector;
      objSpec.skip = false;
         
      // Get Event objects in "latestPage" from "EventHistoryCollector"
      // and no "traversl" further, so, no SelectionSpec is specified 
      objSpec.selectSet= new SelectionSpec[] { };
         
      // ObjectSpecs are wrapped in an ObjectSpec array
      ObjectSpec[] objSpecAry = new ObjectSpec[] { objSpec };
         
      PropertyFilterSpec spec = new PropertyFilterSpec();
      spec.propSet= propSpecAry;
      spec.objectSet= objSpecAry;
      return spec;
   }

   private void monitorEvents(PropertyFilterSpec spec)  {
      // Get all Events returned from the EventHistoryCollector
      // This will result in a large number of events, depending on the
      // page size of the latestPage.
      try {
         ObjectContent[] objectContents =
              cb._svcUtil.retrievePropertiesEx(_propCol, 
                                          new PropertyFilterSpec[] { spec });
         // Print out class names of the Events we got back 
         if (objectContents != null) {
           
            Event[] events = (Event[])objectContents[0].propSet[0].val;
            //Event[] events = (Event[])objectContents[0].getPropSet(0).getVal();
            
            Console.WriteLine("Events In the latestPage are: ");
            for (int i = 0; i < events.Length; i++) {
               Event anEvent = events[i];
               Console.WriteLine("Event: " + anEvent.GetType().ToString());
            }
         } else {
            Console.WriteLine("No Events retrieved!");
         }
      }
    
      catch(SoapException e) {
               if (e.Detail.FirstChild.LocalName.Equals("InvalidRequest"))
               {
                   Console.WriteLine(" InvalidRequest: vmPath may be wrong");
               }
         
      }
      catch(Exception e){
         Console.WriteLine("Error");
         e.StackTrace.ToString();
      } 
   }
   
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[1];
      useroptions[0] = new OptionSpec("vmpath","String",1
                                     ,"A VM Inventory Path"
                                     ,null);
      return useroptions;
   }
   
   public static void Main(String[] args) {
      try {
         VMEventHistoryCollectorMonitor eventMonitor = 
            new VMEventHistoryCollectorMonitor();
         cb = AppUtil.AppUtil.initialize("VMEventHistoryCollectorMonitor",
                                 VMEventHistoryCollectorMonitor.constructOptions(),
                                 args);
         cb.connect();        
         eventMonitor.initialize();
       //  eventMonitor.findVirtualMachine();
         if(eventMonitor.findVirtualMachine()) {
            eventMonitor.createEventHistoryCollector();
            PropertyFilterSpec eventFilterSpec = eventMonitor.createEventFilterSpec();
            eventMonitor.monitorEvents(eventFilterSpec);
         }
         else {
            Console.WriteLine("Virtual Machine not found from the vmPath specified");
         }        
         cb.disConnect();
         Console.WriteLine("Press enter to exit: ");
         Console.Read();
         } 
      catch (Exception e) {
         Console.WriteLine("Caught Exception : " +
                            " Name : " + e.Data.ToString() +
                            " Message : " + e.Message.ToString() +
                            " Trace : ");
         Console.Read();
      }
   }
    }
}
