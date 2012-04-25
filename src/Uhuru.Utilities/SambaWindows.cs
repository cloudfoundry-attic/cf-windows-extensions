﻿// -----------------------------------------------------------------------
// <copyright file="SambaWindows.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// This class contains helper functions for mounting a share as a local directory.
    /// </summary>
    public static class SambaWindows
    {
        /// <summary>
        /// Mounts a remote share as a local directory.
        /// </summary>
        /// <param name="remoteDirectory">The remote share path.</param>
        /// <param name="targetMachine">The address of the machine.</param>
        /// <param name="remoteUser">A username used for authentication to the share.</param>
        /// <param name="remotePassword">A password used for authentication to the share.</param>
        /// <param name="localUser">The local user that needs to have access to the share.</param>
        /// <param name="localPassword">The local user's password.</param>
        /// <param name="localPath">The local path that will be the mount point.</param>
        public static void Mount(string remoteDirectory, string targetMachine, string remoteUser, string remotePassword, string localUser, string localPassword, string localPath)
        {
            // mklink creates the directory if not exist
            try
            {
                using (new UserImpersonator(localUser, ".", localPassword))
                {
                    Process.Start(string.Format(CultureInfo.InvariantCulture, @"net use ""\\{0}\{1} {2}"" /USER:{3}", targetMachine, remoteDirectory, remotePassword, remoteUser));
                    Process.Start(string.Format(CultureInfo.InvariantCulture, @"mklink /d ""{0}"" ""\\{1}\{2}""", localPath, targetMachine, remoteDirectory));
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Un-mounts a local path.
        /// </summary>
        /// <param name="localPath">The path to un-mount.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unmount", Justification = "Word is added to dictionary, but the warning is still shown.")]
        public static void Unmount(string localPath)
        {
            try
            {
                Process.Start("rmdir /q " + localPath);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Persists a resource on a mounted share, and then links it.
        /// This method will make sure the folder and file structure remains the same on the local file system, while also persisting data on a share.
        /// </summary>
        /// <param name="instancePath">The directory considered to be the "root" of the resources that have to be persisted.</param>
        /// <param name="persistentItem">The directory or file that has to be persisted.</param>
        /// <param name="mountPath">The mounted directory that points to a share.</param>
        public static void Link(string instancePath, string persistentItem, string mountPath)
        {
            if (string.IsNullOrEmpty(instancePath))
            {
                throw new ArgumentNullException("instancePath");
            }

            if (string.IsNullOrEmpty(persistentItem))
            {
                throw new ArgumentNullException("instancePath");
            }

            if (string.IsNullOrEmpty(mountPath))
            {
                throw new ArgumentNullException("instancePath");
            }

            string item = string.Empty;
            if (Directory.Exists(mountPath + "\\" + persistentItem))
            {
                Directory.CreateDirectory(instancePath + "\\" + persistentItem);
                CopyFolderRecursively(instancePath + "\\" + persistentItem, mountPath + "\\" + persistentItem);
                Directory.Delete(instancePath + "\\" + persistentItem);
                try
                {
                    Process.Start("mklink /d " + instancePath + "\\" + persistentItem + " " + mountPath + "\\" + persistentItem);
                }
                catch
                {
                    throw;
                }
            }

            if (File.Exists(mountPath + "\\" + persistentItem))
            {
                string[] dirs = persistentItem.Split('\\');
                string dirname = dirs[dirs.Length - 1];

                Directory.CreateDirectory(instancePath + "\\" + dirname);
                File.Copy(instancePath + "\\" + persistentItem, mountPath + "\\" + persistentItem);
                File.Delete(instancePath + "\\" + persistentItem);
                try
                {
                    Process.Start("mklink /d " + instancePath + "\\" + persistentItem + " " + mountPath + "\\" + persistentItem);
                }
                catch
                {
                    throw;
                }
            }

            if (string.IsNullOrEmpty(item))
            {
                throw new ArgumentException("The resource couldn't be persisted. No such file or directory.");
            }
        }

        /// <summary>
        /// Copies a directory recursively, without overwriting.
        /// </summary>
        /// <param name="source">Source folder to copy.</param>
        /// <param name="destination">Destination folder.</param>
        private static void CopyFolderRecursively(string source, string destination)
        {
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
            }
            
            string[] files = Directory.GetFiles(source);

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destination, name);
                File.Copy(file, dest, false);
            }

            string[] folders = Directory.GetDirectories(source);

            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destination, name);
                CopyFolderRecursively(folder, dest);
            }
        }
    }
}
