namespace CloudFoundry.WinDEA.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CloudFoundry.Utilities.Json;

    class DirectoryServerRequest : JsonConvertibleObject
    {
        [JsonName("host")]
        public string Host
        {
            get;
            set;
        }

        [JsonName("port")]
        public int Port
        {
            get;
            set;
        }

        [JsonName("uris")]
        public string[] Uris
        {
            get;
            set;
        }

        [JsonName("tags")]
        public Dictionary<string,string> Tags
        {
            get;
            set;
        }
    }
}
