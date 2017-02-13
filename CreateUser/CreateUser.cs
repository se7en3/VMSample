using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using Vim25Api;
using AppUtil;
using VMware.Security.CredentialStore;

namespace CreateUser
{
    public class CreateUser
    {
        private static AppUtil.AppUtil cb = null;

        public CreateUser(string[] args)
        {
            cb = AppUtil.AppUtil.initialize("CreateUser", null, args);
        }

        public bool CreateNewUser()
        {
            try
            {
                cb.connect();
                ManagedObjectReference hostLocalAccountManager =
                   cb._connection._sic.accountManager;


                ManagedObjectReference hostAuthorizationManager =
                   cb._connection._sic.authorizationManager;

                String userName = GenerateRandomString();
                String password = GenerateRandomString();

                // Create an user
                HostAccountSpec hostAccountSpec = new HostAccountSpec();
                hostAccountSpec.id = userName;
                hostAccountSpec.password = password;
                hostAccountSpec.description = "User Description";
                cb._connection._service.CreateUser(hostLocalAccountManager,
                                                           hostAccountSpec);

                ManagedObjectReference rootFolder =
                   cb._connection._sic.rootFolder;

                Permission per = new Permission();
                per.group = false;
                per.principal = userName;
                per.roleId = -1;
                per.propagate = true;
                per.entity = rootFolder;

                cb._connection._service.SetEntityPermissions(hostAuthorizationManager,
                                                                     rootFolder,
                                                                     new Permission[] { per });

                ICredentialStore csObj = CredentialStoreFactory.CreateCredentialStore();
                var createUserResult = csObj.AddPassword(GetServerName(), userName, password.ToCharArray());
                if (createUserResult)
                {
                    Console.WriteLine("Successfully created user and populate the "
                                       + "credential store");
                    return true;
                }

                return false;
            }
            finally
            {
                cb.disConnect();
            }
        }

        public String GenerateRandomString()
        {
            // To make sure this method does not return the same random string
            // for successively fast calls
            System.Threading.Thread.Sleep(500);

            Random rand = new Random();

            char randChar1 = (char)rand.Next(97, 123); //a-z
            char randChar2 = (char)rand.Next(97, 123); //a-z
            char randChar3 = (char)rand.Next(97, 123); //a-z
            char randChar4 = (char)rand.Next(97, 123); //a-z

            randChar2 = char.ToUpper(randChar2);
            randChar4 = char.ToUpper(randChar4);

            int randNum1 = rand.Next(1000, 10000); //1000-9999
            int randNum2 = rand.Next(1000, 10000); //1000-9999

            string strRandom = randChar1.ToString() +
                randChar2.ToString() +
                randChar3.ToString() +
                randChar4.ToString() +
                "_" +
                randNum1.ToString() + "_" +
                randNum2.ToString();

            return strRandom;
        }

        public String GetServerName()
        {
            if (cb.get_option("server") != null)
            {
                return cb.get_option("server");
            }
            else
            {
                String urlString = cb.get_option("url");
                if (urlString.IndexOf("https://") != -1)
                {
                    int sind = 8;
                    int lind = urlString.IndexOf("/sdk");
                    return urlString.Substring(sind, lind - 8);
                }
                else if (urlString.IndexOf("http://") != -1)
                {
                    int sind = 7;
                    int lind = urlString.IndexOf("/sdk");
                    return urlString.Substring(sind, lind);
                }
                else
                {
                    return urlString;
                }
            }
        }

        public static void Main(String[] args)
        {
            try
            {
                Console.WriteLine("Note: This sample works for ESX hosts only.");
                var createUserSample = new CreateUser(args);
                createUserSample.CreateNewUser();
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