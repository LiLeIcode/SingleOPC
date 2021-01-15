namespace SingleOPC.Models
{
    /// <summary>
    /// 组属性类
    /// </summary>
    public class GroupPropertiesModel
    {
        /// <summary>
        /// 默认组处于活动状态
        /// </summary>
        public bool DefaultGroupIsActive { get; set; }
        /// <summary>
        /// 默认组处于死亡状态
        /// </summary>
        public int DefaultGroupDeadBand { get; set; }
        /// <summary>
        /// 是否读取
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// 是否订阅
        /// </summary>
        public bool IsSubscribed { get; set; }
        /// <summary>
        /// 读取的频率，单位毫秒
        /// </summary>
        public int UpdateRate { get; set; }

    }
}