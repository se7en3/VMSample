using System;
using System.Collections;
using System.IO;
using System.Reflection;
using AppUtil;
using Vim25Api;

namespace MobStartPage {
    /// <summary>
    /// Browse for contents in Vim - Hostd or Vpxd
    /// </summary>
    public class MobStartPage {
      private static AppUtil.AppUtil cb = null;
      static VimService _service;
      static ServiceContent _sic;
      /// <summary>
      /// Array of typename + all its properties
      /// </summary>
      private string[][] typeInfo = new string[][] { 
            new string[] { "ManagedEntity", "parent", "name" }, };

      class MeNode 
      {
         public ManagedObjectReference parent;
         public ManagedObjectReference node;
         public String name;
         public ArrayList children = new ArrayList();

         public MeNode(ManagedObjectReference parent, ManagedObjectReference node, String name)
         {
            this.parent = parent;
            this.node = node;
            this.name = name;
         }
      };

      public class MeNodeCompare : IComparer
      {
         int IComparer.Compare(Object l, Object r)
         {
            String lName = ((MeNode)l).name;
            String rName = ((MeNode)r).name;
            return ( (new CaseInsensitiveComparer()).Compare(lName, rName) );
         }
      };
      /// <summary>
      /// Retrieve inventory from the given root 
      /// </summary>
      private void PrintInventory() {
         try {

            // Retrieve Contents recursively starting at the root folder 
            // and using the default property collector.            
            ObjectContent[] ocary = 
               cb.getServiceUtil().GetContentsRecursively(null, null, typeInfo, true);

            Hashtable nodes = new Hashtable();
            MeNode root = null;

            for (int oci = 0; oci < ocary.Length; oci++) {
               ObjectContent oc = ocary[oci];
               ManagedObjectReference mor = oc.obj;
               DynamicProperty[] pcary = oc.propSet;

               if (pcary != null) {
                  ManagedObjectReference parent = null;
                  String name = null;
                  for (int pci = 0; pci < pcary.Length; pci++) 
                  {
                     DynamicProperty pc = pcary[pci];
                     if (pc != null) {
                        if("name" == pc.name) 
                        {
                           name = pc.val as String;
                        }
                        if("parent" == pc.name)
                        {
                           parent = pc.val as ManagedObjectReference;
                        }
                     }
                  }
                  MeNode node = new MeNode(parent, mor, name);
                  if(parent == null)
                  {
                     root = node;
                  }
                  nodes.Add(node.node.Value, node);
               }
            }
            // Organize the nodes into a 'tree'
            foreach (String key in nodes.Keys)
            {
               MeNode meNode = nodes[key] as MeNode;
               if(meNode.parent != null)
               {
                  ((MeNode)nodes[meNode.parent.Value]).children.Add(meNode);
               }
            }
            
            String mobUrl = cb.getServiceUrl();
            mobUrl = mobUrl.Replace("/sdk", "/mob");
            if(mobUrl.IndexOf("mob") == -1)
            {
               mobUrl += "/mob";
            }
            mobUrl += "/?moid=";

            // Map of ManagedEntity to doc file
            Hashtable typeToDoc = new Hashtable();
            typeToDoc["ComputeResource"] = "/vim.ComputeResource.html";
            typeToDoc["ClusterComputeResource"] = "/vim.ClusterComputeResource.html";
            typeToDoc["Datacenter"] = "/vim.Datacenter.html";
            typeToDoc["Folder"] = "/vim.Folder.html";
            typeToDoc["HostSystem"] = "/vim.HostSystem.html";
            typeToDoc["ResourcePool"] = "/vim.ResourcePool.html";
            typeToDoc["VirtualMachine"] = "/vim.VirtualMachine.html";

            Hashtable typeToImage = new Hashtable();
            typeToImage["ComputeResource"] = "compute-resource.png";
            typeToImage["ClusterComputeResource"] = "compute-resource.png";
            typeToImage["Datacenter"] = "datacenter.png";
            typeToImage["Folder"] = "folder-open.png";
            typeToImage["HostSystem"] = "host.png";
            typeToImage["ResourcePool"] = "resourcePool.png";
            typeToImage["VirtualMachine"] = "virtualMachine.png";

            String docRoot = cb.get_option("docref");
            if (docRoot != null)
            {
                // Try to determine where we are in sample tree
                // SDK/samples_2_0/DotNet/cs/MobStartPage/bin/Debug
                String cDir = Directory.GetCurrentDirectory();
                if (cDir.EndsWith("Debug") || cDir.EndsWith("Release"))
                {
                    docRoot = "../../../../../../doc/ReferenceGuide";
                }
                else if (cDir.EndsWith("MobStartPage"))
                {
                    docRoot = "../../../../doc/ReferenceGuide";
                }
                else if (cDir.EndsWith("cs"))
                {
                    docRoot = "../../../doc/ReferenceGuide";
                }

                bool docsFound = docRoot != null;
                // Not Url?
                if (docsFound && docRoot.IndexOf("http") == -1)
                {
                    String testFile = docRoot.Replace("/", "\\") + "\\index.html";
                    docsFound = File.Exists(testFile);
                }
                if (!docsFound)
                {
                    //log.LogLine("Warning: Can't find docs at: " + docRoot);
                    docRoot = null;
                }

                // Write out as html into string
                StringWriter nodeHtml = new StringWriter();
                PrintNode(root, nodeHtml, mobUrl, docRoot, typeToDoc, typeToImage);
                nodeHtml.Close();

                Assembly assembly = Assembly.GetExecutingAssembly();

                CopyFileFromResource(assembly, "MobStartPage.index.html", "index.html");
                CopyFileFromResource(assembly, "MobStartPage.compute-resource.png", "compute-resource.png");
                CopyFileFromResource(assembly, "MobStartPage.datacenter.png", "datacenter.png");
                CopyFileFromResource(assembly, "MobStartPage.folder.png", "folder.png");
                CopyFileFromResource(assembly, "MobStartPage.folder-open.png", "folder-open.png");
                CopyFileFromResource(assembly, "MobStartPage.host.png", "host.png");
                CopyFileFromResource(assembly, "MobStartPage.resourcePool.png", "resourcePool.png");
                CopyFileFromResource(assembly, "MobStartPage.virtualMachine.png", "virtualMachine.png");

                // Get and write inventory.html
                Stream stream = assembly.GetManifestResourceStream("MobStartPage.inventory.html");
                StreamReader reader = new StreamReader(stream);
                String fileData = reader.ReadToEnd();

                using (StreamWriter file = new StreamWriter("inventory.html"))
                {
                    String serverName = cb.getServiceUrl();
                    int lastCharacter = serverName.LastIndexOfAny(":/".ToCharArray());
                    if (lastCharacter != -1)
                    {
                        serverName = serverName.Substring(0, lastCharacter);
                    }
                    int firstCharacter = serverName.LastIndexOf('/');
                    if (firstCharacter != -1)
                    {
                        serverName = serverName.Substring(firstCharacter + 1);
                    }

                    String output = String.Format(fileData, serverName, nodeHtml.ToString());
                    file.WriteLine(output);
                }

                System.Diagnostics.Process.Start("index.html");

                cb.log.LogLine("Done Printing Inventory");
            }
            else
            {
                System.Console.WriteLine("Please provide the valid reference-doc-location.");
            }
         } catch (Exception e) {            
            cb.log.LogLine("MobStartPage : Failed Getting Contents");
            throw e;
         }
      }

      void CopyFileFromResource(Assembly assembly, String resourceName, String filename)
      {

         // Get and write index.html
         Stream stream = assembly.GetManifestResourceStream(resourceName);
         BinaryReader reader = new BinaryReader(stream);
         using(FileStream file = new FileStream(filename, FileMode.Create))
         {
            do 
            {
               byte[] buffer = reader.ReadBytes(10240);
               if(buffer.Length <= 0) 
                  break;
               file.Write(buffer, 0, buffer.Length);
            } while (true);
         }
      }


      void PrintNode(MeNode node, TextWriter file, String mobUrl, 
         String docRoot, Hashtable typeToDoc, Hashtable typeToImage)
      {
         String mobLink = mobUrl + System.Web.HttpUtility.UrlEncode(node.node.Value);
         String page = typeToDoc[node.node.type] as String;
         String image = typeToImage[node.node.type] as String;
         String link = null;
         if(page != null && docRoot != null)
         {
            link = docRoot + page;
         }
         if(node.children.Count > 0)
         {
            node.children.Sort(new MeNodeCompare());

            file.WriteLine("<li style=\"list-style-image:url("+image+")\"><a target=\"mob\" href=\""+mobLink+"\">"+node.name+"</a></li>");
            if(link != null) 
            {
               file.WriteLine("<a target=\"mob\" href=\""+link+"\">?</a>");
            }
            file.WriteLine("<ul>");
            foreach (MeNode lNode in node.children)
            {
               PrintNode(lNode, file, mobUrl, docRoot, typeToDoc, typeToImage);
            }
            file.WriteLine("</ul>");
         }
         else
         {
            file.WriteLine("<li style=\"list-style-image:url("+image+")\"><a target=\"mob\" href=\""+mobLink+"\">"+node.name+"</a></li>");
            if(link != null) 
            {
               file.WriteLine("<a target=\"mob\" href=\""+link+"\">?</a>");
            }
         }
      }
      public static OptionSpec[] constructOptions()
      {
         OptionSpec[] useroptions = new OptionSpec[1];
         useroptions[0] = new OptionSpec("docref", "String", 1
                                         , "Document Reference"
                                         , null);
         return useroptions;
      }

     /// <summary>
     /// The main entry point for the application.
     /// </summary>
      public static void Main(String[] args)
      {
         MobStartPage obj = new MobStartPage();
         cb = AppUtil.AppUtil.initialize("MobStartPage",
                                         MobStartPage.constructOptions()
                                         , args);
         cb.connect();
         obj.PrintInventory();
         cb.disConnect();
      }
   }
}
