namespace CloudFoundry.WinDEA
{
    using System.Reflection;
    
    /// <summary>
    /// The basic data related to a plugin
    /// </summary>
    internal struct PluginData
    {
        /// <summary>
        /// the name of the class implementing the IAgentPlugin interface
        /// </summary>
        public string ClassName;

        /// <summary>
        /// the path of the library containing the class that implements the IAgentPlugin interface
        /// </summary>
        public string FilePath;

        /// <summary>
        /// Constructor for the plugin.
        /// </summary>
        public ConstructorInfo PluginConstructor;

        /// <summary>
        /// App domain for the plugin.
        /// </summary>
        public System.AppDomain PluginDomain;
    }
}
