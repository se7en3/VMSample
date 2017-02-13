using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using AppUtil;
using Vim25Api;

namespace Create
{
    ///<summary>
    ///This sample is used to create the Folder, Datacenter, cluster 
    ///and also to add the StandAlone Host.
    ///</summary>
    ///<param name="parentName">Required: Specifies the name of the parent folder</param>
    ///<param name="itemType">Required: Host-Standalone|Cluster| DataCeneter|Folder</param>
    ///<param name="itemName">Required: Name of the item being added: For Host please specify the name of the host machine </param>
    ///<remarks>
    ///Create Folder,Cluster, Datacenter and StandAlone Host
    ///--url [webserviceurl]
    ///--username [username] --password [password]  --parentName [Parent Name]
    ///--itemType [ItemType] --itemName [Item name] 
    ///</remarks>
    public class Create
    {
        private static AppUtil.AppUtil cb = null;

        public Create(string[] args)
        {
            cb = AppUtil.AppUtil.initialize("Create", Create.ConstructOptions(), args);
        }

        private String getParentName()
        {
            return cb.get_option("parentName");
        }

        public String GetItemType()
        {
            return cb.get_option("itemType");
        }

        public String GetItemName()
        {
            return cb.get_option("itemName");
        }

        private String getparentType()
        {
            return cb.get_option("parentType");
        }

        private static String getUserName()
        {
            Console.WriteLine("Enter the userName for the host: ");
            return (Console.ReadLine());

        }

        private static String getPassword()
        {
            Console.WriteLine("Enter the password for the host: ");
            return (Console.ReadLine());
        }

        private static int getPort()
        {
            Console.WriteLine("Enter the port for the host : "
                  + "[Hit enter for default:] ");

            String portStr = Console.ReadLine();
            if ((portStr == null) || portStr.Length == 0)
                return 902;
            else
                return int.Parse(portStr);
        }

        private static String getLicense()
        {
            Console.WriteLine("Enter the LicenseKey: ");
            return (Console.ReadLine());
        }

        private static String getThumbPrint()
        {
            Console.WriteLine("Enter the  thumbprint of the SSL certificate, which the host is expected to have: "
                + "Format should be A8:BE:0D:FE:A7:1F:8C:15:3A:B2:B3:9D:69:8A:C4:DF:47:42:04:36");
            return (Console.ReadLine());
        }

        public bool CreateManagedEntity()
        {
            try
            {
                cb.connect();

                var itemType = GetItemType();
                var itemName = GetItemName();

                ManagedObjectReference folderMoRef = cb.getServiceUtil()
                      .GetDecendentMoRef(null, "Folder", getParentName());

                if (folderMoRef == null)
                {
                    Console.WriteLine("Parent folder '" + getParentName()
                          + "' not found");
                }
                else
                {
                    if (itemType.Equals("Folder"))
                    {
                        cb.getConnection()._service.CreateFolder(
                            folderMoRef, itemName);

                        Console.WriteLine("Sucessfully created '{0}' '{1}'", itemType, itemName);

                        return true;
                    }
                    else if (itemType.Equals("Datacenter"))
                    {
                        cb.getConnection()._service.CreateDatacenter(
                            folderMoRef, itemName);

                        Console.WriteLine("Sucessfully created '{0}' '{1}'", itemType, itemName);

                        return true;
                    }
                    else if (itemType.Equals("Cluster"))
                    {
                        ClusterConfigSpec clusterSpec = new ClusterConfigSpec();
                        cb.getConnection()._service.CreateCluster(
                            folderMoRef, itemName, clusterSpec);

                        Console.WriteLine("Sucessfully created '{0}' '{1}'", itemType, itemName);

                        return true;
                    }
                    else if (itemType.Equals("Host-Standalone"))
                    {
                        HostConnectSpec hostSpec = new HostConnectSpec();
                        hostSpec.hostName = itemName;
                        hostSpec.userName = getUserName();
                        hostSpec.password = getPassword();
                        hostSpec.port = getPort();  //this method will create a problem with automation
                        hostSpec.sslThumbprint = getThumbPrint();

                        ComputeResourceConfigSpec configSpec = new ComputeResourceConfigSpec();

                        ManagedObjectReference taskMoRef = cb.getConnection()._service
                              .AddStandaloneHost_Task(folderMoRef, hostSpec,
                                   configSpec, false, getLicense());

                        if (taskMoRef != null)
                        {
                            String status = cb.getServiceUtil().WaitForTask(
                                  taskMoRef);
                            if (status.Equals("sucess"))
                            {
                                Console.WriteLine("Sucessfully created::"
                                      + itemName);
                            }
                            else
                            {
                                Console.WriteLine("Host'" + itemName
                                   + " not created::");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unknown Type. Allowed types are:");
                        Console.WriteLine(" Host-Standalone");
                        Console.WriteLine(" Cluster");
                        Console.WriteLine(" Datacenter");
                        Console.WriteLine(" Folder");
                    }
                }

                return false;
            }
            finally
            {
                cb.disConnect();
            }
        }

        private static OptionSpec[] ConstructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[3];
            useroptions[0] = new OptionSpec("parentName", "String", 1
                                           , "Specifies the name of the parent folder"
                                           , null);
            useroptions[1] = new OptionSpec("itemType", "String", 1,
                                            "Host-Standalone | Cluster | Folder",
                                            null);
            useroptions[2] = new OptionSpec("itemName", "String", 1,
                                            "Name of the item being added: For Host-Standalone, "
                                            + "please specify the name of the host machine.",
                                            null);
            return useroptions;
        }

        public static void Main(String[] args)
        {
            try
            {
                var createSample = new Create(args);
                createSample.CreateManagedEntity();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Press <Enter> to exit...");
            Console.Read();
        }
    }
}
