using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Api;

namespace BO.Implementation
{
    public class AutorizeHelper
    {
        public static bool CheckIPAccess(string userIp)
        {
            bool deniedAccess = true;
           
            if (ServerProperties.Instance.EnableBlockUnKnowIP == 1)
            {
                if (String.IsNullOrEmpty(userIp))
                    return true;

                string[] allowedIps = ServerProperties.Instance.AllowedIPList.Split(';');

                if (allowedIps.Any(ip => ip == userIp))
                {
                    return false;
                }

                string[] userIps = userIp.Split('.');

                if (userIps.Length != 4)
                   return true;

                if (String.IsNullOrEmpty(ServerProperties.Instance.AllowedIPList))
                    return true;

                foreach (var allowedIp in allowedIps)
                {
                    if (allowedIp.IndexOf("*", System.StringComparison.Ordinal) > 0)
                    {
                        if (allowedIp.Remove(allowedIp.Length - 2, 2) ==
                            String.Format("{0}.{1}.{2}", userIps[0], userIps[1], userIps[2]))
                            return false;
                    }
                    
                }
                return true;
            }
            else
            {
                deniedAccess = false;
            }
            return deniedAccess;
        }
    }
}
