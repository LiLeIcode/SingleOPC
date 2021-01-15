using System;
using System.Configuration;
using System.Runtime.InteropServices;
using OPCAutomation;
using Quartz;
using ServiceStack.Redis;
using SingleOPC.Common;
using SingleOPC.Models;
using SingleOPC.OPC;

namespace SingleOPC.Task
{
    public class OpcTask : IJob
    {
        private OpcMain _opcMain;
        private OPCServer _opcServer = null;
        private static readonly Configuration Configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            if (_opcServer == null)
            {
                _opcServer = new OPCServer();
            }
            string[] filter = ReadAppConfig.GetStrArray(Configuration, "filter");
            _opcMain = OpcMain.GetOpcMain(_opcServer, new RedisClient(ReadAppConfig.GetStr(Configuration,"redisHost")));
            _opcMain.ReadOpcServer(filter, new GroupPropertiesModel()
            {
                DefaultGroupIsActive = true,
                DefaultGroupDeadBand = 0,
                IsActive = true,
                IsSubscribed = true,
                UpdateRate = 1000
            });
        }

    }
}
