using System;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Connect
{
    /// <summary>
    /// Simple client that only exercises the login/logout
    /// </summary>
    public class Connect
    {
        private AppUtil.AppUtil cb = null;

        public Connect(string[] args)
        {
            cb = AppUtil.AppUtil.initialize("Connect", args);
        }

        /// <summary>
        /// Gets the current date and time of the server
        /// </summary>
        /// <param name="args">server connection parameters</param>
        /// <returns>current server date and time</returns>
        public DateTime ConnectToServer()
        {
            try
            {
                cb.connect();
                Console.WriteLine("Successfully connected");
                var serverTime = cb.getConnection().Service.CurrentTime(cb.getConnection().ServiceRef);
                Console.WriteLine("Got Server Time");
                return serverTime;
            }
            finally
            {
                cb.disConnect();
                Console.WriteLine("Successfully disconnected");
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(ValidateServerCertificate);

            try
            {
                var connectSample = new Connect(args);
                var serverTime = connectSample.ConnectToServer();
                Console.WriteLine("Server Time -: {0}", serverTime);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                while (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException);
                    e = e.InnerException;
                }
            }

            Console.WriteLine("Press <Enter> to exit...");
            Console.Read();
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // replace with proper validation
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            else return false;
        }
    }
}
