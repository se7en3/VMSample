using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Principal;
using System.Web.Services.Protocols;
using System.Runtime.InteropServices;
using System.Xml;

using WSManAutomation;
using WbemScripting;

using SSPI;
using Vim25Api;
using AppUtil;

namespace SSPICIMClient
{
   class SSPICIMClient
   {
      protected VimService _service;
      protected ServiceContent _sic;
      protected ManagedObjectReference _svcRef;

      private void DoConnect(String url, String hostName)
      {
          System.Net.ServicePointManager.CertificatePolicy = new CertPolicy();
         _svcRef = new ManagedObjectReference();
         _svcRef.type = "ServiceInstance";
         _svcRef.Value = "ServiceInstance";
         _service = new VimService();
         _service.Url = url;
         _service.Timeout = 600000; //The value can be set to some higher value also.
         _service.CookieContainer = new System.Net.CookieContainer();
         _sic = _service.RetrieveServiceContent(_svcRef);

         if (_sic.sessionManager != null)
         {
            Boolean flag = true;
            SSPIClientCredential clientCred = new SSPIClientCredential(SSPIClientCredential.Package.Negotiate);
            SSPIClientContext clientContext = new SSPIClientContext(clientCred,
                                                                    "",
                                                                    SSPIClientContext.ContextAttributeFlags.None);
            
            //ManagedObjectReference hostmor = _service.FindByIp(_sic.searchIndex, null,
            //                                                   hostName,false);
             while (flag)
            {
               try
               {
                  _service.LoginBySSPI(_sic.sessionManager, Convert.ToBase64String(clientContext.Token), "en");
                  flag = false;
               }
               catch (Exception e)
               {
                  SoapException se = (SoapException)e;
                  clientContext.Initialize(Convert.FromBase64String(se.Detail.InnerText));
                  try
                  {
                     Console.WriteLine("Time " + _service.CurrentTime(_svcRef));
                     flag = false;
                  }
                  catch (Exception ex)
                  {
                     flag = true;
                  }
               }
            }                    
            //HostServiceTicket cimTicket = _service.AcquireCimServicesTicket(hostmor);
            //String sessionId = cimTicket.sessionId;
            //GetComputeSystem(sessionId, hostName);
         }
      }

      private static void GetComputeSystem(String CimSessionId, String HostName)
      {
         WSManClass wsman = new WSManClass();
         IWSManConnectionOptions co = wsman.CreateConnectionOptions() as IWSManConnectionOptions;
         co.UserName = CimSessionId;
         co.Password = CimSessionId;

         IWSManSession session = (IWSManSession)
         wsman.CreateSession("https://" + HostName + "/wsman",
                             wsman.SessionFlagUseBasic() |
                             wsman.SessionFlagCredUsernamePassword() |
                             wsman.SessionFlagSkipCACheck() |
                             wsman.SessionFlagSkipCNCheck(),
                             co);
         try
         {
            String url1 = "http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_ComputerSystem";           
            IWSManEnumerator enumerator = (IWSManEnumerator)
            session.Enumerate(url1,
                              null, null, wsman.SessionFlagUseBasic() & wsman.SessionFlagCredUsernamePassword() &
            wsman.SessionFlagSkipCACheck() & wsman.SessionFlagUseNoAuthentication());
            while (!enumerator.AtEndOfStream)
            {
               string response = enumerator.ReadItem();
               Console.Write(response);
            }
         }
         catch (System.Exception ex)
         {
            Console.Write(ex.ToString() + "\n\n" + session.Error);
         }
      }

      public static void Main(String[] args)
      {
         SSPICIMClient obj = new SSPICIMClient();            
         obj.DoConnect(args[0], args[1]);            
      }
   }
}
