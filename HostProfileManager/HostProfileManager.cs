using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppUtil;
using Vim25Api;

namespace HostProfileManager
{
    ///<summary>
    ///This sample demonstrates HostProfileManager and ProfileComplainceManager
    ///</summary>
    ///<param name="sourcehostname">Required: Name of the host</param>
    ///<param name="entityname">Required: Attached Entity Name</param>
    ///<param name="entitytype">Required: Attached Entity Type [It can by HostSystem ,
    ///cluster compute resource etc</param>
    ///<remarks>
    /// Create hostprofile
    ///--url [URLString] --username [User] --password [Password]
    ///--sourcehostname [sourcehostname] --entityname [entityname] --entitytype [entitytype]
    ///</remarks>
    public class HostProfileManager
    {
        private static AppUtil.AppUtil cb = null;
        private static ManagedObjectReference hostprofileManager = null;
        private static ManagedObjectReference profilecomplianceManager = null;

        private void ProfileManager()
        {
            string hostName = cb.get_option("sourcehostname");
            string entityType = cb.get_option("entitytype");
            string entityName = cb.get_option("entityname");
            try
            {
                hostprofileManager = cb._connection._sic.hostProfileManager;
                profilecomplianceManager = cb._connection._sic.complianceManager;
                ManagedObjectReference hostmor = cb._svcUtil.getEntityByName("HostSystem", hostName);
                ManagedObjectReference hostProfile = CreateHostProfile(hostmor, hostName);
                ManagedObjectReference attachMoref = cb._svcUtil.getEntityByName(entityType, entityName);
                List<ManagedObjectReference> entityMorList = new List<ManagedObjectReference>();
                entityMorList.Add(attachMoref);
                ManagedObjectReference[] entityList = entityMorList.ToArray();
                AttachProfileWithManagedEntity(hostProfile, entityList);
                PrintProfilesAssociatedWithEntity(attachMoref);

                List<ManagedObjectReference> hpmor = new List<ManagedObjectReference>();
                List<ManagedObjectReference> hamor = new List<ManagedObjectReference>();
                hpmor.Add(hostProfile);
                hamor.Add(attachMoref);
                if (entityType.Equals("HostSystem"))
                {
                    UpdateReferenceHost(hostProfile, attachMoref);
                    HostConfigSpec hostConfigSpec =
                      ExecuteHostProfile(hostProfile, attachMoref);
                    if (hostConfigSpec != null)
                    {
                        ConfigurationTasksToBeAppliedOnHost(hostConfigSpec, attachMoref);
                        if (CheckProfileCompliance(hpmor.ToArray(), hamor.ToArray()))
                        {
                            ApplyConfigurationToHost(attachMoref, hostConfigSpec);
                        }
                    }
                }
                else
                {
                    CheckProfileCompliance(hpmor.ToArray(), hamor.ToArray());
                }
                DetachHostFromProfile(hostProfile, hamor.ToArray());
                DeleteHostProfile(hostProfile);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }
        /// <summary>
        /// Destroy the Profile
        /// </summary>
        /// <param name="hostProfile">ManagedObjectReference</param>
        private void DeleteHostProfile(ManagedObjectReference hostProfile)
        {
            Console.WriteLine("Deleting Profile");
            Console.WriteLine("---------------");
            cb._connection._service.DestroyProfile(hostProfile);
            Console.WriteLine("Profile Deleted : " + hostProfile.Value);
        }

        /// <summary>
        /// Detach a profile from a managed entity.
        /// </summary>
        /// <param name="hostProfile">ManagedObjectReference</param>
        /// <param name="managedObjectReferences">ManagedObjectReference[]</param>
        private void DetachHostFromProfile(ManagedObjectReference hostProfile,
                                                  ManagedObjectReference[] managedObjectReferences)
        {
            Console.WriteLine("------------------------");
            Console.WriteLine("* Detach Host From Profile");
            Console.WriteLine("------------------------");
            cb._connection._service.DissociateProfile(hostProfile, managedObjectReferences);
            Console.WriteLine("Detached Host : "
                  + managedObjectReferences.GetValue(0) + " From Profile : "
                  + hostProfile.Value);
        }
        /// <summary>
        /// Setting the host to maintenance mode and apply the configuration to the host.
        /// </summary>
        /// <param name="attachHostMoref">ManagedObjectReference</param>
        /// <param name="hostConfigSpec">HostConfigSpec</param>
        private void ApplyConfigurationToHost(ManagedObjectReference attachHostMoref,
                                                      HostConfigSpec hostConfigSpec)
        {
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("* Applying Configuration changes or HostProfile to Host");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine("Putting Host in Maintenance Mode");
            ManagedObjectReference mainmodetask =
                  cb._connection._service.EnterMaintenanceMode_Task(attachHostMoref, 0, false, true, null);
            if (mainmodetask != null)
            {
                String status = cb.getServiceUtil().WaitForTask(
                      mainmodetask);
                if (status.Equals("sucess"))
                {
                    Console.WriteLine("Success: Entered Maintenance Mode");
                }
                else
                {
                    throw new Exception(status);
                }
            }
            Console.WriteLine("Applying Profile to Host");
            ManagedObjectReference apphostconftask =
                  cb._connection._service.ApplyHostConfig_Task(hostprofileManager, attachHostMoref,
                        hostConfigSpec, null);
            if (apphostconftask != null)
            {
                String status = cb.getServiceUtil().WaitForTask(
                      mainmodetask);
                if (status.Equals("sucess"))
                {
                    Console.WriteLine("Success: Apply Configuration to Host");
                }
                else
                {
                    throw new Exception(status);
                }
            }
        }
        /// <summary>
        /// Generate a list of configuration tasks that will be performed on the host
        ///during HostProfile application.
        /// </summary>
        /// <param name="hostConfigSpec">HostConfigSpec</param>
        /// <param name="attachHostMoref">ManagedObjectReference</param>
        private void ConfigurationTasksToBeAppliedOnHost(HostConfigSpec hostConfigSpec,
                                                                ManagedObjectReference attachHostMoref)
        {
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine("* Config Tasks on the Host during HostProfile Application");
            Console.WriteLine("-------------------------------------------------------");
            HostProfileManagerConfigTaskList hostProfileManagerConfigTaskList =
                 cb._connection._service.GenerateConfigTaskList(hostprofileManager, hostConfigSpec,
                        attachHostMoref);
            LocalizableMessage[] taskMessages =
                  hostProfileManagerConfigTaskList.taskDescription;
            if (taskMessages != null)
            {
                foreach (LocalizableMessage taskMessage in taskMessages)
                {
                    Console.WriteLine("Message : " + taskMessage.message);
                }
            }
            else
            {
                Console.WriteLine("There are no configuration changes to be made");
            }
        }

        /// <summary>
        /// Execute the Profile Engine to calculate the list of configuration changes
        ///needed for the host.
        /// </summary>
        /// <param name="hostProfile">ManagedObjectReference</param>
        /// <param name="attachHostMoref">ManagedObjectReference</param>
        /// <returns>HostConfigSpec</returns>
        private HostConfigSpec ExecuteHostProfile(ManagedObjectReference hostProfile,
                                                         ManagedObjectReference attachHostMoref)
        {
            Console.WriteLine("------------------------------");
            Console.WriteLine("* Executing Profile Against Host");
            Console.WriteLine("------------------------------");
            ProfileExecuteResult profileExecuteResult =
                  cb._connection._service.ExecuteHostProfile(hostProfile, attachHostMoref, null);
            Console.WriteLine("Status : " + profileExecuteResult.status);
            if (profileExecuteResult.status.Equals("success"))
            {
                Console.WriteLine("Valid HostConfigSpec representing "
                      + "Configuration changes to be made on host");
                return profileExecuteResult.configSpec;
            }
            if (profileExecuteResult.status.Equals("error"))
            {
                Console.WriteLine("List of Errors");
                foreach (ProfileExecuteError profileExecuteError in profileExecuteResult.error)
                {
                    Console.WriteLine("    " + profileExecuteError.message.message);
                }
                return null;
            }
            return null;
        }
        /// <summary>
        /// Update the reference host in use by the HostProfile.
        /// </summary>
        /// <param name="hostProfile">ManagedObjectReference</param>
        /// <param name="attachHostMoref">ManagedObjectReference</param>
        private void UpdateReferenceHost(ManagedObjectReference hostProfile,
                                                ManagedObjectReference attachHostMoref)
        {
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("* Updating Reference Host for the Profile");
            Console.WriteLine("--------------------------------------");
            cb._connection._service.UpdateReferenceHost(hostProfile, attachHostMoref);
            Console.WriteLine("Updated Host Profile : " + hostProfile.Value
                  + " Reference to " + attachHostMoref.Value);
        }

        /// <summary>
        /// Check compliance of an entity against a Profile.
        /// </summary>
        /// <param name="profiles">ManagedObjectReference[]</param>
        /// <param name="entities">ManagedObjectReference[]</param>
        /// <returns>Boolean</returns>
        private Boolean CheckProfileCompliance(ManagedObjectReference[] profiles,
                                                     ManagedObjectReference[] entities)
        {
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("* Checking Complaince of Entity against Profile");
            Console.WriteLine("---------------------------------------------");
            ManagedObjectReference cpctask =
                  cb._connection._service.CheckCompliance_Task(profilecomplianceManager, profiles,
                        entities);
            if (cpctask != null)
            {
                String status = cb.getServiceUtil().WaitForTask(
                      cpctask);
                if (status.Equals("sucess"))
                {
                    Console.WriteLine("Success: Entered Maintenance Mode");
                }
                else
                {
                    throw new Exception(status);
                }
            }
            string[] type = new string[] { "info.result" };
            Object result = null;
            ObjectContent[] objContent = cb.getServiceUtil().GetObjectProperties(null, cpctask, type);
            if (objContent != null)
            {
                foreach (ObjectContent oc in objContent)
                {
                    DynamicProperty[] dps = oc.propSet;
                    if (dps != null)
                    {
                        foreach (DynamicProperty dp in dps)
                        {
                            result = dp.val;
                        }
                    }
                }
            }
            return ComplianceStatusAndResults(result);
        }

        /// <summary>
        /// Checking for the compliance status and results. If compliance is
        ///"nonCompliant", it lists all the compliance failures.
        /// </summary>
        /// <param name="result">Object</param>
        /// <returns>Boolean</returns>
        private Boolean ComplianceStatusAndResults(Object result)
        {
            List<ComplianceResult> complianceResults =
                  ((ComplianceResult[])result).ToList();
            foreach (ComplianceResult complianceResult in complianceResults)
            {
                Console.WriteLine("Host : " + complianceResult.entity.Value);
                Console.WriteLine("Profile : " + complianceResult.profile.Value);
                Console.WriteLine("Compliance Status : "
                      + complianceResult.complianceStatus);
                if (complianceResult.complianceStatus.Equals("nonCompliant"))
                {
                    Console.WriteLine("Compliance Failure Reason");
                    foreach (ComplianceFailure complianceFailure in complianceResult.failure)
                    {
                        Console.WriteLine(" " + complianceFailure.message.message);
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the profile(s) to which this entity is associated. The list of
        ///profiles will only include profiles known to this profileManager.
        /// </summary>
        /// <param name="attachMoref">ManagedObjectReference</param>
        private void PrintProfilesAssociatedWithEntity(ManagedObjectReference attachMoref)
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine("* Finding Associated Profiles with Host");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("Profiles");
            ManagedObjectReference[] profiles =
                cb._connection._service.FindAssociatedProfile(hostprofileManager, attachMoref);
            foreach (ManagedObjectReference profile in profiles)
            {
                Console.WriteLine(profile.Value);
            }
        }

        /// <summary>
        /// Associate a profile with a managed entity. The created hostProfile is
        /// attached to a hostEntityMoref (ATTACH_HOST_ENTITY_NAME). We attach only
        ///one host to the host profile
        /// </summary>
        /// <param name="hostProfile">ManagedObjectReference</param>
        /// <param name="hMor">ManagedObjectReference[]</param>
        private void AttachProfileWithManagedEntity(ManagedObjectReference hostProfile,
            ManagedObjectReference[] hMor)
        {
            Console.WriteLine("------------------------");
            Console.WriteLine("* Associating Host Profile");
            Console.WriteLine("------------------------");
            cb._connection._service.AssociateProfile(hostProfile, hMor);
            Console.WriteLine("Associated " + hostProfile.Value + " with "
            + hMor.GetValue(0)); //How to get value at array position 0
        }

        /// <summary>
        /// Create a profile from the specified CreateSpec.HostProfileHostBasedConfigSpec is 
        /// created from the hostEntitymoref(create_host_entity_name) reference. Using
        /// this spec a hostProfile is created.
        /// </summary>
        /// <param name="hostmor">ManagedObjectReference</param>
        /// <param name="hostName">string</param>
        /// <returns>ManagedObjectReference</returns>
        private ManagedObjectReference CreateHostProfile(ManagedObjectReference hostmor, string hostName)
        {

            HostProfileHostBasedConfigSpec hostProfileHostBasedConfigSpec =
              new HostProfileHostBasedConfigSpec();
            hostProfileHostBasedConfigSpec.host = hostmor;

            hostProfileHostBasedConfigSpec.annotation = "SDK Sample Host Profile";
            hostProfileHostBasedConfigSpec.enabled = false;
            hostProfileHostBasedConfigSpec.enabledSpecified = true;
            hostProfileHostBasedConfigSpec.name = "SDK Profile3w43" + hostName;
            Console.WriteLine("--------------------");
            Console.WriteLine("* Creating Host Profile");
            Console.WriteLine("--------------------");
            ManagedObjectReference hostProfile =
            cb._connection._service.CreateProfile(hostprofileManager,
                  hostProfileHostBasedConfigSpec);
            Console.WriteLine("Profile : " + hostProfile.Value);
            Console.WriteLine("Host : " + hostmor.Value);
            return hostProfile;
        }

        private static OptionSpec[] constructOptions()
        {
            OptionSpec[] useroptions = new OptionSpec[3];
            useroptions[0] = new OptionSpec("sourcehostname", "String", 1
                                           , "Name of the host"
                                           , null);
            useroptions[1] = new OptionSpec("entityname", "String", 1,
                                            "Attached Entity Name",
                                              null);
            useroptions[2] = new OptionSpec("entitytype", "String", 1,
                                            "Attached Entity Type",
                                             null);
            return useroptions;
        }

        public static void Main(string[] args)
        {
            try
            {
                HostProfileManager app = new HostProfileManager();
                cb = AppUtil.AppUtil.initialize("HostProfileManager",
                                        HostProfileManager.constructOptions(),
                                        args);
                cb.connect();
                app.ProfileManager();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            cb.disConnect();
            Console.Read();
        }
    }
}
