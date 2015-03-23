namespace CloudFoundry.WinDEA.DirectoryServer
{
    using CloudFoundry.Configuration;
    using System.Configuration;    

    /// <summary>
    /// Helper class for getting the DEA's configuration element from the config file.
    /// </summary>
    public static class DirectoryConfiguration
    {
        /// <summary>
        /// Gets the DEA config element.
        /// </summary>
        /// <returns>A DEAElement that contains all DEA configuration settings, including the Directory Server.</returns>
        public static DEAElement ReadConfig()
        {
            CloudFoundrySection cfSection = (CloudFoundrySection)ConfigurationManager.GetSection("cloudfoundry");
            return cfSection.DEA;
        }
    }
}