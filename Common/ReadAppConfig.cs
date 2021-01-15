using System.Configuration;

namespace SingleOPC.Common
{
    public class ReadAppConfig
    {
        /// <summary>
        /// 读取AppSettings.config中逗号分割的字符配置
        /// </summary>
        /// <param name="configuration">Configuration对象</param>
        /// <param name="settingName">AppSettings.config中的配置key</param>
        /// <returns></returns>
        public static string[] GetStrArray(Configuration configuration,string settingName)//读取配置
        {
            string[] split = configuration.AppSettings.Settings[settingName].Value.Split(',');
            for (int i = 0; i < split.Length; i++)
            {
                split[i] = split[i].Trim();
            }
            return split;
        }

        public static string GetStr(Configuration configuration, string settingName)
        {
            string value = configuration.AppSettings.Settings[settingName].Value;
            return value;
        }
    }
}