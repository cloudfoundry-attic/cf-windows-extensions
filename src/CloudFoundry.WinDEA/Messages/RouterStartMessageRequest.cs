namespace CloudFoundry.WinDEA.Messages
{
    using System.Collections.Generic;
    using CloudFoundry.Utilities;
    using CloudFoundry.Utilities.Json;

    /// <summary>
    /// This class is a representation of a DEA status message response.
    /// </summary>
    public class RouterStartMessageRequest : JsonConvertibleObject
    {
        /// <summary>
        /// Gets or sets the minimumRegisterIntervalInSeconds.
        /// </summary>
        [JsonName("minimumRegisterIntervalInSeconds")]
        public int MinimumRegisterIntervalInSeconds
        {
            get;
            set;
        }
    }
}
