namespace CloudFoundry.WinDEA.Messages
{
    using System.Collections.Generic;
    using CloudFoundry.Utilities;
    using CloudFoundry.Utilities.Json;

    /// <summary>
    /// This class is a representation of a DEA advertise message response.
    /// </summary>
    public class DeaAdvertiseMessage : JsonConvertibleObject
    {
        /// <summary>
        /// Gets or sets the id of the DEA service.
        /// </summary>
        [JsonName("id")]
        public string Id
        {
            get;
            set;
        }


        /// <summary>
        /// Gets or sets the ip of the machine hosting the Windows DEA.
        /// </summary>
        [JsonName("ip")]
        public string Ip
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the supported runtimes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Convention."),
        System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Convention.")]
        [JsonName("stacks")]
        public List<string> Stacks
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the physical memory available on the machine hosting the Windows DEA.
        /// </summary>
        [JsonName("physical_memory")]
        public long PhysicalMemory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current available memory in MiB.
        /// </summary>
        [JsonName("available_memory")]
        public long AvailableMemory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the available disk space on the machine hosting the Windows DEA.
        /// </summary>
        [JsonName("available_disk")]
        public long AvailableDisk
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of instances running on the DEA per app.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Convention."),
        JsonName("app_id_to_count")]
        public Dictionary<string, int> AppIdCount
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the placement properties for the Windows DEA.
        /// </summary>
        [JsonName("placement_properties")]
        public DeaAdvertiseMessagePlacementProperties PlacementProperties
        {
            get;
            set;
        }
    }

    /// <summary>
    /// This class is a representation of the placement properties section of a DEA advertise message.
    /// </summary>
    public class DeaAdvertiseMessagePlacementProperties
    {
        /// <summary>
        /// Gets or sets the zone for DEA placement.
        /// </summary>
        [JsonName("zone")]
        public string Zone
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the zones available for DEA placement.
        /// </summary>
        /// <value>
        [JsonName("zones")]
        public string[] Zones
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the availability zones for DEA placement.
        /// </summary>
        /// <value>
        [JsonName("availability_zone")]
        public string AvailabilityZone
        {
            get;
            set;
        }

    }
}
