namespace CloudFoundry.WinDEA.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CloudFoundry.Utilities;
    using CloudFoundry.Utilities.Json;

    /// <summary>
    /// This class contains information about the usage limits of a droplet.
    /// </summary>
    public class StartRequestDropletLimits : JsonConvertibleObject
    {
        /// <summary>
        /// Gets or sets the maximum memory limit in megabytes.
        /// </summary>
        [JsonName("mem")]
        public long? MemoryMbytes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum disk usage in megabytes.
        /// </summary>
        [JsonName("disk")]
        public long? DiskMbytes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum number of open files and sockets.
        /// </summary>
        [JsonName("fds")]
        public long? FileDescriptors
        {
            get;
            set;
        }
    }
}
