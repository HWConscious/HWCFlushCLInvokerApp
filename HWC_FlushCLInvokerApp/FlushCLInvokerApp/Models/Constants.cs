namespace HWC_FlushCLInvokerApp.FlushCLInvokerApp.Models
{
    #region General constants

    /// <summary>
    /// General constants
    /// </summary>
    public static class Constants
    {
        // Directory and file related constants
        public const string RootDirectoryName = "HwConscious";
        public const string Level1DirectoryName = "FlushCLInvokerApp";
        public const string LogDirectoryName = "Logs";
        public const string LogFileName = "Log";

        // REST API related constants
        public const string RestApiEndpoint = "https://oz3yqvjaik.execute-api.us-east-1.amazonaws.com/v1";
        public const string XApiKeyValue = "kHnzbQx6PX6sLZIIwwP2E58QlLKKUHeAao4fzoX0";

        // Miscellaneous constants
        public const int FlushConcurrentListsPingFrequencyInMs = 5000;          // 5 seconds
        public const int MainCloudServicesPingFrequencyInMs = 1000 * 60 * 25;   // 25 minutes
    }

    #endregion
}
