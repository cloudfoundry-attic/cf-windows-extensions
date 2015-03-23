namespace CloudFoundry.WinDEA.DirectoryServer
{
    using System;

    /// <summary>
    /// Interface for the DEA that gets called by the Directory server.
    /// </summary>
    public interface IDeaClient
    {
        /// <summary>
        /// Looks up the path in the DEA.
        /// </summary>
        /// <param name="path">The path to lookup.</param>
        /// <returns>A PathLookupResponse containing the response from the DEA.</returns>
        PathLookupResponse LookupPath(Uri path);
    }

    /// <summary>
    /// This class contains a DEAs response to a lookup path query from the Directory Server.
    /// </summary>
    public class PathLookupResponse
    {
        /// <summary>
        /// Gets or sets the local path looked up by the DEA.
        /// </summary>
        public string Path
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the error (if any) the DEA encountered while looking up the path.
        /// </summary>
        public string Error
        {
            get;
            set;
        }
    }
}
