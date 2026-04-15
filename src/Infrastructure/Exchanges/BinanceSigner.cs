using System.Security.Cryptography;
using System.Text;

namespace Oc.BinGrid.Infrastructure.Exchanges
{
    /// <summary>
    /// 币安签名工具类：专门处理 HMACSHA256 算法
    /// </summary>
    internal static class BinanceSigner
    {
        /// <summary>
        /// 根据原始查询字符串和私钥生成十六进制签名
        /// </summary>
        /// <param name="totalParams">完整的待签名字符串（包含 timestamp, recvWindow 等）</param>
        /// <param name="secretKey">交易所提供的 ApiSecret</param>
        /// <returns>64位小写十六进制签名字符串</returns>
        public static string CreateSignature(string totalParams, string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey))
                throw new ArgumentException("ApiSecret 不能为空");

            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] messageBytes = Encoding.UTF8.GetBytes(totalParams);

            using var hmacsha256 = new HMACSHA256(keyBytes);
            byte[] hashBytes = hmacsha256.ComputeHash(messageBytes);

            // 将字节数组转换为小写的十六进制字符串
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// 辅助方法：将 Dictionary 参数转换为签名所需的 QueryString 格式
        /// </summary>
        public static string BuildQueryString(IDictionary<string, object> parameters)
        {
            // 按照币安要求，参数顺序必须与发送时一致
            return string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
