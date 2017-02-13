using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace VMPowerStateAlarm
{
    class VMPowerStateAlarm
    {
        private static AppUtil.AppUtil cb = null;      
   private ManagedObjectReference _virtualMachine=null;
   private ManagedObjectReference  _alarmManager;
    
   private void getVmMor(String vmName)  {
      _virtualMachine 
         = cb.getServiceUtil().GetDecendentMoRef(null, "VirtualMachine", vmName);
   }
   
   private StateAlarmExpression createStateAlarmExpression() 
       {    
      StateAlarmExpression expression = new StateAlarmExpression();
      expression.type="VirtualMachine";
      expression.statePath="runtime.powerState";
      expression.@operator= StateAlarmOperator.isEqual;
      expression.red="poweredOff";
      return expression;
   }
   
   private MethodAction createPowerOnAction() {
      MethodAction action = new MethodAction();
      action.name="PowerOnVM_Task";
      MethodActionArgument argument = new MethodActionArgument();
      argument.value=null;
      action.argument= new MethodActionArgument[] { argument };
      return action;
   }
   
   private AlarmTriggeringAction createAlarmTriggerAction(MethodAction methodAction) 
       {
      AlarmTriggeringAction alarmAction = new AlarmTriggeringAction();
      alarmAction.yellow2red = true;
      alarmAction.action=methodAction;
      return alarmAction;
   }
   
   private AlarmSpec createAlarmSpec(AlarmAction action, AlarmExpression expression)
       {      
      AlarmSpec spec = new AlarmSpec();
      spec.action=action;
      spec.expression=expression;
      spec.name=cb.get_option("alarm");
      spec.description="Monitor VM state and send email if VM power's off";
      spec.enabled=true;      
      return spec;
   }

   private void createAlarm(AlarmSpec alarmSpec)  {   
      try {
         _alarmManager = cb.getConnection()._sic.alarmManager;
         ManagedObjectReference alarm 
            = cb.getConnection()._service.CreateAlarm(_alarmManager, 
                                                          _virtualMachine,
                                                          alarmSpec);
         Console.WriteLine("Successfully created Alarm: " + cb.get_option("alarm"));
          
          }
      catch(SoapException e) {
               if (e.Detail.FirstChild.LocalName.Equals("DuplicateNameFault"))
               {
                   Console.WriteLine(e.Message.ToString());
               }
               else if (e.Detail.FirstChild.LocalName.Equals("InvalidRequestFault"))
               {
                   Console.WriteLine("Alarm Creation is not supported on ESX server.");
               }    
               else if (e.Detail.FirstChild.LocalName.Equals("InvalidArgumentFault"))
               {
                   Console.WriteLine(e.Message.ToString());
               }
               else if (e.Detail.FirstChild.LocalName.Equals("InvalidNameFault"))
               {
                   Console.WriteLine(e.Message.ToString());
               }
               else if (e.Detail.FirstChild.LocalName.Equals("RuntimeFault"))
               {
                   Console.WriteLine(e.Message.ToString());
               }
               else {
                   throw e;
               }
                 
      }
   }    
   private static OptionSpec[] constructOptions() {
      OptionSpec [] useroptions = new OptionSpec[2];
      useroptions[0] = new OptionSpec("vmname","String",1
                                     ,"Name of the virtual machine"
                                     ,null);
      useroptions[1] = new OptionSpec("alarm","String",1,
                                      "Name of the alarm",
                                      null);
      return useroptions;
   }
   public static void Main(String[] args)  {
      VMPowerStateAlarm obj = new VMPowerStateAlarm();    
      cb = AppUtil.AppUtil.initialize("VMPowerStateAlarm"
                              ,VMPowerStateAlarm.constructOptions()
                              ,args);
      cb.connect();
      String apitype = cb.getConnection()._sic.about.apiType;
      if (apitype != "HostAgent")
      {
          obj.getVmMor(cb.get_option("vmname"));
          if (obj._virtualMachine != null)
          {
              ObjectContent[] oc = cb.getServiceUtil().GetObjectProperties
                  (cb.getConnection().PropCol, obj._virtualMachine,
                     new String[] { "config" });
              VirtualMachineConfigInfo vmConfig = 
                  (VirtualMachineConfigInfo)oc[0].propSet[0].val;
              if (!vmConfig.template)
              {
                  StateAlarmExpression expression = obj.createStateAlarmExpression();
                  MethodAction methodAction = obj.createPowerOnAction();
                  AlarmAction alarmAction
                     = (AlarmAction)obj.createAlarmTriggerAction(methodAction);
                  AlarmSpec alarmSpec = obj.createAlarmSpec(alarmAction, expression);
                  obj.createAlarm(alarmSpec);
              }
              else
              {
                  Console.WriteLine("Virtual Machine name specified " 
                     + cb.get_option("vmname") + " is a template");
              }
          }
          else
          {
              Console.WriteLine("Virtual Machine " + cb.get_option("vmname") 
                 + " Not Found");
          }
      }
      else
      {
          Console.WriteLine("Alarm Creation is not supported on an ESX server.");
      }
      cb.disConnect();
      Console.WriteLine("Please enter to exit.");
      Console.Read();
      
   }
    }
}
