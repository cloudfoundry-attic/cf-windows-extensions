using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudFoundry.Utilities;
using CloudFoundry.Utilities.Json;

namespace CloudFoundry.WinDEA.Messages
{
    class LogyardInstanceRequest : JsonConvertibleObject
    {
        [JsonName("appguid")]
        public string AppGUID { get; set; }

        [JsonName("appname")]
        public string AppName { get; set; }

        [JsonName("appspace")]
        public string AppSpace { get; set; }

        [JsonName("type")]
        public string Type { get; set; }

        [JsonName("index")]
        public int Index { get; set; }

        [JsonName("docker_id")]
        public string DockerId { get; set; }

        [JsonName("rootpath")]
        public string RootPath { get; set; }

        [JsonName("logfiles")]
        public Dictionary<string, string> LogFiles { get; set; }

    }
}
