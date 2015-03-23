namespace CloudFoundry.WinDEA.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CloudFoundry.Utilities.Json;

    class StagingStartMessageResponse : JsonConvertibleObject
    {
        [JsonName("task_id")]
        public string TaskId { get; set; }

        [JsonName("task_log")]
        public string TaskLog { get; set; }

        [JsonName("task_streaming_log_url")]
        public string TaskStreamingLogURL { get; set; }

        [JsonName("detected_buildpack")]
        public string DetectedBuildpack { get; set; }

        [JsonName("error")]
        public string Error { get; set; }

        [JsonName("droplet_sha1")]
        public string DropletSHA { get; set; }
    }
}
