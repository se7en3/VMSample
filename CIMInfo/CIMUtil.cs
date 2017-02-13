using System;
using System.Collections;
using AppUtil;
using Vim25Api;
using System.Net;

namespace CIMInfo
{
    class CIMUtil
    {
        static VimService _service;
        static ServiceContent _sic; 
        public static string getCIMSessionId (Vim25Api.ManagedObjectReference hmor1, String[] args, Cookie cookie)
        {
            AppUtil.AppUtil ecb = null;
            try
            {
                ecb = AppUtil.AppUtil.initialize("GetCIMSessioId"
                                                 , args);
                ecb.connect(cookie);
                _service = ecb.getConnection()._service;
                _sic = ecb.getConnection().ServiceContent;
                ManagedObjectReference hmor = VersionUtil.convertManagedObjectReference(hmor1);
                string sessionId = _service.AcquireCimServicesTicket(hmor).sessionId;
                return sessionId;
            }
            catch (Exception e)
            {
                ecb.log.LogLine("Get GetSessionID : Failed Connect");
                throw e;
            }
            finally
            {
                ecb.log.LogLine("Ended GetSessionID");
                ecb.log.Close();
            }
        }
    }
}
