using System;

namespace SingleOPC.Models
{
    public class OpcModel
    {
        /// <summary>
        /// 读取Opc的值
        /// </summary>
        public string OpcValue { get; set; }
        /// <summary>
        /// 读取Opc值的时间
        /// </summary>
        public string DateTime { get; set; }
    }
}