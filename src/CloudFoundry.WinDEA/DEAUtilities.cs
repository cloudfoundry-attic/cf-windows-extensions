namespace CloudFoundry.WinDEA
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Text;
    using System.Net;
    using System.Security.Cryptography;
    using System.Web;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using CloudFoundry.Utilities;
    using SharpCompress.Reader;
    using SharpCompress.Reader.Tar;
    using SharpCompress.Common;
    using SharpCompress.Writer;

    /// <summary>
    /// A class containing a set of file- and process-related methods. 
    /// </summary>
    public sealed class DEAUtilities
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="DEAUtilities"/> class from being created.
        /// </summary>
        private DEAUtilities()
        {
        }

        /// <summary>
        /// This methid just makes sure the SharpCompress assembly is loded for the current process.
        /// It is useful when making arhive operations (e.g. ExtractArchive) in a limited impersonated context.
        /// </summary>
        static public void InitizlizeExtractor()
        {
            using (SharpCompress.Archive.ArchiveFactory.Create(ArchiveType.Zip)) { }
            using (SharpCompress.Archive.ArchiveFactory.Create(ArchiveType.Tar)) { }
        }

        /// <summary>
        /// Extract all files from a zip or tar.gz archive to the specified directory.
        /// </summary>
        /// <param name="archiveFile">Path of the archive file</param>
        /// <param name="outputPath">Destination directory for the extracted files</param>
        static public void ExtractArchive(string archiveFile, string outputPath)
        {
            using (var fileStream = File.OpenRead(archiveFile))
            {
                using (var inputReader = ReaderFactory.Open(fileStream))
                {
                    if (inputReader.ArchiveType == ArchiveType.GZip)
                    {
                        if (!inputReader.MoveToNextEntry())
                        {
                            throw new Exception("MoveToNextEntry failed");
                        }

                        using (var tarStream = inputReader.OpenEntryStream())
                        {
                            using (var tarReader = TarReader.Open(tarStream))
                            {
                                tarReader.WriteAllToDirectory(outputPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                            }

                            // this is necessary when the tar is empty
                            tarStream.SkipEntry();

                        }

                    }
                    else if (inputReader.ArchiveType == ArchiveType.Zip || inputReader.ArchiveType == ArchiveType.Tar)
                    {
                        inputReader.WriteAllToDirectory(outputPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported archive type for the input file", "archiveFile");
                    }
                }

            }
        }

        /// <summary>
        /// Create an archive from a directory. It will iterate through all directories recursively.
        /// </summary>
        /// <param name="folderPath">Folder/directory to archive</param>
        /// <param name="outputFile">Ouput path, including file name, of the resulting archive</param>
        /// <param name="useZip">true for zip format; false for tar.gz format</param>
        static public void CreateArchive(string folderPath, string outputFile, bool useZip)
        {
            using (var outputStream = File.OpenWrite(outputFile))
            {

                ArchiveType atype;
                var cinfo = new CompressionInfo();

                if (useZip)
                {
                    atype = ArchiveType.Zip;
                    cinfo.Type = CompressionType.Deflate;
                    cinfo.DeflateCompressionLevel = SharpCompress.Compressor.Deflate.CompressionLevel.Default;
                }
                else
                {
                    atype = ArchiveType.Tar;
                    cinfo.Type = CompressionType.GZip;
                    cinfo.DeflateCompressionLevel = SharpCompress.Compressor.Deflate.CompressionLevel.Default;
                }

                using (var awriter = WriterFactory.Open(outputStream, atype, cinfo))
                {
                    awriter.WriteAll(folderPath, "*", SearchOption.AllDirectories);
                }


            }
        }


        /// <summary>
        /// Converts a Ruby date string into a DateTime.
        /// </summary>
        /// <param name="date">The string to convert.</param>
        /// <returns>The converted data.</returns>
        public static DateTime DateTimeFromRubyString(string date)
        {
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
            dateFormat.SetAllDateTimePatterns(new string[] { "yyyy-MM-dd HH:mm:ss zzz" }, 'Y');
            return DateTime.Parse(date, dateFormat);
        }

        /// <summary>
        /// Returns the number of cores on the current machine.
        /// </summary>
        /// <returns>The number of cores on the current machine.</returns>
        public static int NumberOfCores()
        {
            // todo: stefi: maybe this is not a precise way to get the number of physical cores of a machine
            return Environment.ProcessorCount;
        }

        /// <summary>
        /// Tries to write a file to a directory to make sure writing is allowed.
        /// </summary>
        /// <param name="directory"> The directory to write in.</param>
        public static void EnsureWritableDirectory(string directory)
        {
            string testFile = Path.Combine(directory, string.Format(CultureInfo.InvariantCulture, Strings.NatsMessageDeaSentinel, Process.GetCurrentProcess().Id));
            File.WriteAllText(testFile, string.Empty);
            File.Delete(testFile);
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }


            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
                file.CopyTo(temppath, false);
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {

                foreach (DirectoryInfo subdir in dirs)
                {
                    // Create the subdirectory.
                    string temppath = Path.Combine(destDirName, subdir.Name);

                    // Copy the subdirectories.
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        public static string HttpUploadFile(string url, FileInfo file, string paramName, string contentType, string authorization)
        {
            string boundary = Guid.NewGuid().ToString("N");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.Headers[HttpRequestHeader.Authorization] = "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authorization));

            // diable this to allow streaming big files, without beeing out of memory.
            request.AllowWriteStreamBuffering = false;

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);
            byte[] trailerBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            request.ContentLength = boundaryBytes.Length + headerBytes.Length + trailerBytes.Length + file.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                requestStream.Write(headerBytes, 0, headerBytes.Length);

                FileStream fileStream = file.OpenRead();

                // fileStream.CopyTo(requestStream, 1024 * 1024);

                int bufferSize = 1024 * 1024;

                byte[] buffer = new byte[bufferSize];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                    requestStream.Flush();
                }
                fileStream.Close();


                requestStream.Write(trailerBytes, 0, trailerBytes.Length);
                requestStream.Close();

            }

            using (var respnse = request.GetResponse())
            {
                Stream responseStream = respnse.GetResponseStream();
                StreamReader responseReader = new StreamReader(responseStream);
                return responseReader.ReadToEnd();
            }
        }

        public static Uri GetHmacedUri(string uri, string key, string[] paramsToVerify)
        {
            UriBuilder result = new UriBuilder(uri);
            NameValueCollection param = HttpUtility.ParseQueryString(result.Query);
            NameValueCollection verifiedParams = HttpUtility.ParseQueryString(string.Empty);
            foreach (string str in paramsToVerify)
            {
                verifiedParams[str] = HttpUtility.UrlEncode(param[str]);
            }

            string pathAndQuery = result.Path + "?" + verifiedParams.ToString();

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            HMACSHA512 hmacsha512 = new HMACSHA512(keyByte);
            byte[] computeHash = hmacsha512.ComputeHash(encoding.GetBytes(pathAndQuery));
            string hash = BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
            verifiedParams["hmac"] = hash;
            result.Query = verifiedParams.ToString();
            return result.Uri;
        }

        public static bool VerifyHmacedUri(string uri, string key, string[] paramsToVerify)
        {
            UriBuilder result = new UriBuilder(uri);
            NameValueCollection param = HttpUtility.ParseQueryString(result.Query);
            NameValueCollection verifiedParams = HttpUtility.ParseQueryString(string.Empty);
            foreach (string str in paramsToVerify)
            {
                verifiedParams[str] = param[str];
            }

            string pathAndQuery = result.Path + "?" + verifiedParams.ToString();

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(key);
            HMACSHA512 hmacsha512 = new HMACSHA512(keyByte);
            byte[] computeHash = hmacsha512.ComputeHash(encoding.GetBytes(pathAndQuery));
            string hash = BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
            StringComparer comparer = StringComparer.InvariantCultureIgnoreCase;
            if (comparer.Compare(hash, param["hmac"]) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckUrlAge(string url)
        {
            Uri uri = new Uri(url);
            int urlAge = int.Parse(HttpUtility.ParseQueryString(uri.Query)["timestamp"]) - RubyCompatibility.DateTimeToEpochSeconds(DateTime.Now);
            if (urlAge > 60 * 60)
            {
                return false;
            }
            return true;
        }

        public static void RemoveReadOnlyAttribute(string dir)
        {
            string[] files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileAttributes attribute = File.GetAttributes(file);
                File.SetAttributes(file, attribute & ~FileAttributes.ReadOnly);
            }
        }
    }
}
