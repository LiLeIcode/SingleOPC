using System;
using System.Net;

namespace SingleOPC.Common
{
    /// <summary>
    /// 本机ip
    /// </summary>
    public class LocalIpHost
    {
        /// <summary>
        /// 读取本机IP和名称
        /// </summary>
        /// <param name="hostIp">本机IP</param>
        /// <param name="hostName">本机名称</param>
        public static void ReadIpHost(out string hostIp,out string hostName)
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Environment.MachineName);
            if (ipHost.AddressList.Length > 0)
            {
                hostIp = ipHost.AddressList[0].ToString();
                IPHostEntry ipHostName = Dns.GetHostEntry(hostIp);
                hostName = ipHostName.HostName;
            }
            else
            {
                hostIp = "获取本机IP失败";
                hostName = "获取本机名称失败";
            }
        }
    }
}