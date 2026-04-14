namespace Ocean.BinGrid
{
    public class GridAccount
    {
        /// <summary>
        /// key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>
        /// secret
        /// </summary>
        public string ApiSecret { get; set; } = string.Empty;
        /// <summary>
        /// 统一账户API
        /// </summary>
        public bool Portfolio { get; set; }
    }
}
