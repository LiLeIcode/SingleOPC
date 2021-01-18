using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using OPCAutomation;
using ServiceStack.Redis;
using ServiceStack.Text;
using SingleOPC.Common;
using SingleOPC.Enum;
using SingleOPC.Models;

namespace SingleOPC.OPC
{
    public class OpcMain
    {
        
        private OPCServer _opcServer;
        private readonly List<OpcData> _bindingData = new List<OpcData>();
        private string _hostIp, _hostName;
        private string _kepServerName;
        private OPCGroups _opcGroups;
        private OPCGroup _opcGroup;
        private OPCItems _opcItems;
        private int _itmHandleServer;
        private readonly Dictionary<int, string> _dic;
        private readonly Dictionary<int, string> _serviceDic;
        private readonly RedisClient _client;
        private int _count = (int)RedisStoreEnum.Number;//设置redis一组只存1个数据
        private static OpcMain _opcMain = null;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        private OpcMain(OPCServer opcServer, RedisClient redisClient)
        {
            Log.Info("进入私有构造器初始化");
            _opcServer = opcServer;
            _dic = new Dictionary<int, string>();
            _serviceDic = new Dictionary<int, string>();
            _client = redisClient;
            log4net.Config.XmlConfigurator.Configure();
        }

        public static OpcMain GetOpcMain(OPCServer opcServer, RedisClient redisClient)
        {
            if (_opcMain == null)
            {
                lock ("obj")
                {
                    if (_opcMain == null)
                    {
                        Log.Info("opcMain is null");
                        _opcMain = new OpcMain(opcServer, redisClient);
                        return _opcMain;
                    }
                    Log.Info("opcMain is not null");
                    
                }
            }
            return _opcMain;
        }


        public void ReadOpcServer(string[] filter, GroupPropertiesModel groupPropertiesModel)
        {
            if (_opcServer != null && _opcGroups != null && _opcGroup != null && _opcItems != null)
            {
                Log.Info("没有有对象为null");
                AddDataChangeEvent();
            }
            else
            {
                Log.Info("有对象为null");
                LocalIpHost.ReadIpHost(out _hostIp, out _hostName);
                try
                {
                    object opcServers = _opcServer.GetOPCServers(_hostName);
                    foreach (string turn in (Array)opcServers)
                    {
                        _kepServerName = turn;
                    }
                }
                catch (Exception exception)
                {
                    Log.Error("初始化Opc出错：" + exception);
                    throw;
                }
                //连接opc
                try
                {
                    //KEPware.KEPServerEx.V4
                    //127.0.0.1
                    Log.Info($"连接Opc:{_kepServerName},{_hostIp}");
                    _opcServer.Connect(_kepServerName, _hostIp);
                }
                catch (Exception e)
                {
                    Log.Error("连接Opc出错：" + e);
                    throw;
                }
                RecurBrowse(_opcServer.CreateBrowser(), filter);
                try
                {
                    _opcGroups = _opcServer.OPCGroups;
                    _opcGroup = _opcGroups.Add("OPC.NetGroup");
                    SetGroupProperty(groupPropertiesModel);
                    _opcItems = _opcGroup.OPCItems;

                    for (int i = 0; i < _bindingData.Count; i++)
                    {
                        try
                        {
                            OPCItem item = _opcItems.AddItem(_bindingData[i].OpcName, i);
                            if (item != null)
                            {
                                _itmHandleServer = item.ServerHandle;
                                _dic.Add(item.ClientHandle, _bindingData[i].OpcName);
                                _serviceDic.Add(_itmHandleServer, _bindingData[i].OpcName);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error("客户端服务器句柄赋值失败：" + e);
                            throw;
                        }

                        foreach (KeyValuePair<int, string> keyValuePair in _serviceDic)
                        {
                            try
                            {
                                ReadOPCValue(keyValuePair.Key);
                            }
                            catch (Exception e)
                            {
                                // ignored
                            }
                        }

                    }
                    Log.Info("第一次装载_opcGroup数据改变监听事件");
                    _opcGroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(opcGroup_DataChange);
                }
                catch (Exception e)
                {
                    Log.Error("设置opcGroups,opcGroup,opcItems:"+e);
                    throw;
                }
            }

        }

        private void ReadOPCValue(int handle)
        {
           
            OPCItem bItem = _opcItems.GetOPCItem(handle);
            int[] temp = new int[2] { 0, bItem.ServerHandle };
            Array serverHandles = (Array)temp;
            Array Errors;
            int cancelID;
            _opcGroup.AsyncRead(1, ref serverHandles, out Errors, 2009, out cancelID);
            OpcData data = _bindingData.FirstOrDefault(x => x.OpcName == _serviceDic[handle]);
            data.OpcValue = bItem.Value.ToString();
            data.OpcTime = DateTime.Now.ToString();
            Log.Info($"ReadOPCValue读取数据。。。{data.OpcName}-{data.OpcValue}-{ data.OpcTime}");
        }

        private void opcGroup_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            if (_count <= 0)
            {
                UnloadEvent();
                return;
            }
            Log.Info($"首次加载程序_count:{_count}");
            --_count;
            Log.Info($"次数减过之后_count:{_count}");

            for (int i = 1; i <= NumItems; i++)
            {
                string opcName = _dic[(int) ClientHandles.GetValue(i)];
                OpcData data = _bindingData.FirstOrDefault(x => x.OpcName == opcName);
                if (data != null)
                {
                    data.OpcValue = ItemValues.GetValue(i).ToString();
                    Log.Info($"opcGroup_DataChange读取数据为：{data.OpcName}--{data.OpcValue}");
                    data.OpcTime = DateTime.Now.ToString();
                    OpcModel model = new OpcModel()
                    {
                        DateTime = DateTime.Now.ToString(),
                        OpcValue = data.OpcValue
                    };
                    string serializeToString = JsonSerializer.SerializeToString(model);
                    _client.Lists[opcName].Push(serializeToString);
                    _client.Expire(opcName, 50);//50秒过期
                }
            }

        }

        private void UnloadEvent()
        {
            if (_opcGroup != null)
            {
                Log.Info("卸载opcGroup数据监听事件");
                _opcGroup.DataChange -= new DIOPCGroupEvent_DataChangeEventHandler(opcGroup_DataChange);
            }

            //if (_opcServer != null)
            //{
            //    Log.Info("关闭opc连接");
            //    //_opcServer.Disconnect();//如果关闭连接，下次重新要获取节点，重新ReadOPCValue
            //    //_opcServer = null;
            //}
        }
        //
        public void Close()
        {
            if (_opcGroup != null)
            {
                Log.Info("卸载opcGroup数据监听事件");
                _opcGroup.DataChange -= new DIOPCGroupEvent_DataChangeEventHandler(opcGroup_DataChange);
            }

            if (_opcServer != null)
            {
                Console.WriteLine("关闭连接");
                _opcServer.Disconnect();
                Console.WriteLine("_opcServer赋值null");
                _opcServer = null;
            }
        }

        private void AddDataChangeEvent()
        {
           
            Log.Info($"重新装载opcGroup数据监听事件前：_count:{_count}");
            _count = (int)RedisStoreEnum.Number;
            Log.Info($"重新装载opcGroup数据监听事件后：_count:{_count}");
            _opcGroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(opcGroup_DataChange);
        }

        private void SetGroupProperty(GroupPropertiesModel groupPropertiesModel)
        {
            Log.Info($"设置组属性：" +
                     $"{groupPropertiesModel.DefaultGroupIsActive}," +
                     $"{groupPropertiesModel.DefaultGroupDeadBand}," +
                     $"{groupPropertiesModel.IsActive}," +
                     $"{groupPropertiesModel.IsSubscribed}," +
                     $"{groupPropertiesModel.UpdateRate}");
            _opcServer.OPCGroups.DefaultGroupIsActive = groupPropertiesModel.DefaultGroupIsActive;
            _opcServer.OPCGroups.DefaultGroupDeadband = groupPropertiesModel.DefaultGroupDeadBand;
            _opcGroup.IsActive = groupPropertiesModel.IsActive;
            _opcGroup.IsSubscribed = groupPropertiesModel.IsSubscribed;
            _opcGroup.UpdateRate = groupPropertiesModel.UpdateRate;
        }

        private void RecurBrowse(OPCBrowser opcBrowser, string[] filter)
        {
            //展开分支
            opcBrowser.ShowBranches();
            //展开叶子
            opcBrowser.ShowLeafs(true);

            foreach (object turn in opcBrowser)
            {
                foreach (var r in filter)
                {
                    if (turn.ToString().ToUpper().Contains(r.ToUpper()))
                    {
                        OpcData data = new OpcData
                        {
                            OpcName = turn.ToString(),
                            OpcValue = "null",
                            OpcTime = DateTime.Now.ToString()
                        };
                        Log.Info($"绑定的数据{data.OpcName}");
                        _bindingData.Add(data);
                    }
                }
            }
        }
    }
}