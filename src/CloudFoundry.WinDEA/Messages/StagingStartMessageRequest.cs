namespace CloudFoundry.WinDEA.Messages
{
    using System.Collections.Generic;
    using CloudFoundry.Utilities;
    using CloudFoundry.Utilities.Json;
   
    public class StagingStartMessageRequest : JsonConvertibleObject
    {
        public StagingStartMessageRequest()
        {
            this.StartMessage = new DeaStartMessageRequest();
            this.Properties = new StagingStartRequestProperties();
        }

        [JsonName("app_id")]
        public string AppID { get; set; }

        [JsonName("task_id")]
        public string TaskID { get; set; }

        [JsonName("download_uri")]
        public string DownloadURI { get; set; }

        [JsonName("upload_uri")]
        public string UploadURI { get; set; }

        [JsonName("buildpack_cache_download_uri")]
        public string BuildpackCacheDownloadURI { get; set; }

        [JsonName("buildpack_cache_upload_uri")]
        public string BuildpackCacheUploadURI { get; set; }

        [JsonName("properties")]
        public StagingStartRequestProperties Properties { get; set; }

        [JsonName("start_message")]
        public DeaStartMessageRequest StartMessage { get; set; }

        [JsonName("admin_buildpacks")]
        public StagingStartRequestAdminBuildpack[] AdminBuildpacks { get; set; }
    }

    class StagingStopMessageRequest : JsonConvertibleObject
    {
        [JsonName("app_id")]
        public string AppID { get; set; }
    }


    // Ref: https://github.com/cloudfoundry/dea_ng/blob/082c6cbf5c308c35b066034115b7c37b881d2aa1/lib/dea/staging/buildpacks_message.rb
    public class StagingStartRequestAdminBuildpack : JsonConvertibleObject
    {
        [JsonName("key")]
        public string Key { get; set; }

        [JsonName("url")]
        public string Url { get; set; }
    }
}
