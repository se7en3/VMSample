using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.Security.Principal;
using System.Net;
using AppUtil;
using Vim25Api;
using WSManAutomation;

namespace CIMInfo
{
    /// <summary>
    /// This is a CIMInfo versioning sample which retrieves the details of CIM_Fan like 
    /// Activecooling,Caption,CommunicationStatus etc.
    /// <summary>
    class CIMInfo
    {
        private WSManClass wsman = new WSManClass();
        private static AppUtil.AppUtil cb = null;
        /// <summary>
        /// IF apiVersion >= 2.5  Using subroutine AcquireCimServicesTicket in CIMUtil class 
        /// is used to acquire the session id and its passed in both username and password.
        /// otherwise Username and Password is passed.
        /// The getCIMSessionId subroutine retrieve session-id.
        /// <summary>
        private void doOperation(String[] args)
        {
            String apiType = cb.getConnection()._sic.about.apiType;
            ArrayList supportedVersions = VersionUtil.getSupportedVersions(cb.get_option("url"));
            if (apiType.Equals("HostAgent"))
            {
                string url = cb.get_option("url");
                string username = "";
                string password = "";
                string hostname = url.Substring(0, url.IndexOf("/sdk"));
                hostname = hostname.Substring(8);
                ManagedObjectReference hmor = cb.getConnection()._service.FindByIp(cb.getConnection().ServiceContent.searchIndex,
                                                               null, hostname, false);
                if (hmor != null)
                {
                    Cookie cookie = cb.getConnection()._service.CookieContainer.GetCookies(
                                    new Uri(cb.get_option("url")))[0];
                    string cimSessionId = CIMUtil.getCIMSessionId(hmor, args, cookie);
                    username = cimSessionId;
                    password = cimSessionId;
                }
                else
                {
                    System.Console.WriteLine("Host " + hostname + " not found");
                    return;
                }

                string cimurl = url.Substring(0, url.IndexOf("/sdk")) + "/wsman";
                IWSManSession session = createWSManConnection(cimurl, username, password);

                string urlString = "http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_Fan";
                /// IWSManEnumerator enumerates all the CPU_Fan CIM Instances
                IWSManEnumerator enumeratorFans = (IWSManEnumerator)
                                           session.Enumerate(urlString,
                                           null, null, wsman.SessionFlagUseBasic() & wsman.SessionFlagCredUsernamePassword() &
                                           wsman.SessionFlagSkipCACheck() & wsman.SessionFlagUseNoAuthentication());

                while (!enumeratorFans.AtEndOfStream)
                {
                    String response = enumeratorFans.ReadItem();
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.LoadXml(response);
                    /// displayPrettyXML subroutine displays XML after formatting 
                    /// in command prompt.
                    displayPrettyXML(xDoc);
                }
            }
            else
            {
                System.Console.WriteLine("Support for VC Server not implemented");
            }
        }
        ///The displayPrettyXML subroutine formats the data output
        private void displayPrettyXML(XmlDocument xDoc)
        {
            if (xDoc != null)
            {
                string classname = removeNs(xDoc.FirstChild.Name);
                Console.WriteLine("\n\n************CIM Instance " + classname + "************\n\n");
                System.Console.WriteLine("Class Name : " + classname);
                if (xDoc.FirstChild.HasChildNodes)
                {
                    XmlNodeList nl = xDoc.FirstChild.ChildNodes;
                    for (int i = 0; i < nl.Count; i++)
                    {
                        String name = nl.Item(i).Name;
                        String value = nl.Item(i).InnerText;
                        System.Console.WriteLine(removeNs(name) + " = " + value);
                    }
                }
            }
        }

        private string removeNs(String data)
        {
            data = data.Substring(data.IndexOf(":") + 1);
            return data;
        }


        /// <summary>
        /// Create Session
        /// </summary>
        /// <param name="cimurl">URL of CIM schema for CIM_Fan</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        private IWSManSession createWSManConnection(String cimUrl, String username, String password)
        {
            IWSManConnectionOptions co = wsman.CreateConnectionOptions() as IWSManConnectionOptions;
            co.UserName = username;
            co.Password = password;

            IWSManSession session = (IWSManSession)
            wsman.CreateSession(cimUrl,
                                wsman.SessionFlagUseBasic() |
                                wsman.SessionFlagCredUsernamePassword() |
                                wsman.SessionFlagSkipCACheck() |
                                wsman.SessionFlagSkipCNCheck(),
                                co);
            return session;
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary> 
        public static void Main(String[] args)
        {
            CIMInfo obj = new CIMInfo();
            cb = AppUtil.AppUtil.initialize("CIMInfo", args);
            cb.connect();
            obj.doOperation(args);
            cb.disConnect();
            Console.Write("\nPress any key to exit: ");
            Console.Read();
            Environment.Exit(1);
        }
    }
}
