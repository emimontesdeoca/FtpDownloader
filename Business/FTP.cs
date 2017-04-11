﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtpDownloader.Business
{
    /// <summary>
    /// FTP class where it contais connection methods and file management.
    /// </summary>
    class FTP
    {
        #region VARIABLES

        public static FtpWebRequest request;

        /// <summary>
        /// Struct for every file or directory.
        /// </summary>
        public struct Ftplist
        {
            /// <summary>
            /// Type, it can be either "-" for file or "d" for directory.
            /// </summary>
            public string Type { get; set; }
            /// <summary>
            /// Filename.
            /// </summary>
            public string Filename { get; set; }
        }

        #endregion

        #region CONNECTION STUFF

        /// <summary>
        /// Function that creates teh FTPWebRequest.
        /// </summary>
        /// <param name="FTPDirectoryPath">FTP path.</param>
        /// <param name="username">FTP Username.</param>
        /// <param name="password">FTP Password.</param>
        /// <param name="keepAlive">Bool to close the connection after done, true by default.</param>
        /// <returns></returns>
        public FtpWebRequest CreateFtpWebRequest(string FTPDirectoryPath, string username, string password, bool keepAlive = false)
        {
            /// Clean the request, just in case.
            request = null;

            request = (FtpWebRequest)WebRequest.Create(new Uri(FTPDirectoryPath));

            request.Proxy = null;

            request.UsePassive = true;
            request.UseBinary = true;
            request.KeepAlive = keepAlive;

            request.Credentials = new NetworkCredential(username, password);

            return request;
        }

        /// <summary>
        /// Function that try to connect to the FTP.
        /// </summary>
        /// <param name="s">Settings </param>
        /// <param name="keepAlive">Request value, keep it TRUE.</param>
        public void TestConnection(string FtpFolderPath, string FtpUsername, string FtpPassword, bool keepAlive = false)
        {
            try
            {
                request = null;
                /// Creates FtpWebRequest.
                request = CreateFtpWebRequest(FtpFolderPath, FtpUsername, FtpPassword, true);

                /// Method is set to ListDirectoryDetails.
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

                /// Make the call to the FTP.
                request.GetResponse();
            }
            catch
            {
                throw new Exception();
            }

        }

        /// <summary>
        /// Method that gets the directory list.
        /// </summary>
        /// <param name="s">Settings.</param>
        /// <param name="keepAlive">Request value, keep it TRUE.</param>
        /// <returns></returns>
        public FtpWebResponse GetDirectoryList(string FtpFolderPath, string FtpUsername, string FtpPassword, bool keepAlive = false)
        {
            /// Creates FtpWebRequest.

            request = null;

            request = CreateFtpWebRequest(FtpFolderPath, FtpUsername, FtpPassword, true);

            /// Method is set to ListDirectoryDetails.
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            try
            {
                /// Return the response, try 3 times to do it.
                return (FtpWebResponse)request.GetResponse();

            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        #endregion

        #region FTP FILE MANAGEMENT

        /// <summary>
        /// Method that downloads a file from a FTP.
        /// </summary>
        /// <param name="FtpFolderPath">Path to file/directory in FTP.</param>
        /// <param name="FtpUsername">Ftp username.</param>
        /// <param name="FtpPassword">Ftp password.</param>
        /// <param name="DownloadFolderPath">Local download path.</param>
        /// <param name="UrlEncodedTorrent">Encoded URL, necessary to download.</param>
        /// <param name="filename">Filename for creating the file with same name(not encoded).</param>
        public void DownloadFile(string FtpFolderPath, string FtpUsername, string FtpPassword, string DownloadFolderPath, string UrlEncodedTorrent, string filename, string rootDownloadFolderPath)
        {
            /// Filestream necessary values.
            int bytesRead = 0;
            byte[] buffer = new byte[2048];

            /// Clean the request, I hate this fucking shit.
            request = null;

            /// Creates request and assigns it to the request.
            request = CreateFtpWebRequest(FtpFolderPath + UrlEncodedTorrent, FtpUsername, FtpPassword, true);
            request.Method = WebRequestMethods.Ftp.DownloadFile;

            try
            {
                /// Gets response from the server.
                Stream reader = request.GetResponse().GetResponseStream();

                /// Generates a file in the destination with the certain filename.
                FileStream fileStream = new FileStream(DownloadFolderPath + filename, FileMode.Create);

                /// While the file has bytes, it keeps writing on the file.
                while (true)
                {
                    bytesRead = reader.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                        break;

                    fileStream.Write(buffer, 0, bytesRead);
                }
                /// Close the file.
                fileStream.Close();
                /// Create log
                new Business.Log().WriteLog(FtpFolderPath + UrlEncodedTorrent, DownloadFolderPath + filename, rootDownloadFolderPath);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion

        #region GET FTP FILE LIST

        /// <summary>
        /// Reads entire directory for .torrents.
        /// </summary>
        /// <param name="s">Settings.</param>
        /// <returns></returns>
        public List<Ftplist> GetFileList(string FtpFolderPath, string FtpUsername, string FtpPassword)
        {
            /// Set the sourceFileList to return, is a list of ftplist, the struct generated at the beginning.
            List<Ftplist> sourceFileList = new List<Ftplist>();
            string line = "";
            try
            {
                /// Build the response and return it to this method.
                FtpWebResponse sourceResponse = GetDirectoryList(FtpFolderPath, FtpUsername, FtpPassword, true);

                /// Do the call to the FTP.
                using (Stream responseStream = sourceResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        /// Reads the filename details.
                        line = reader.ReadLine();
                        /// While the line is not null (no more lines).
                        while (line != null)
                        {
                            try
                            {
                                /// Split it by spaces.
                                string[] newfilename = line.Split(' ');
                                /// Get filename.
                                string finalfilename = newfilename.Last();
                                try
                                {
                                    /// If it is not "." or ".." or it is not a file without name(windows) and not a directory.
                                    if (newfilename.Last() == "." || newfilename.Last() == ".." || finalfilename.Substring(0, 1) == "." && line.Substring(0, 1) != "d")
                                    {
                                    }
                                    else /// Add it to the list.
                                    {
                                        /// Lets get the type of file or directory.
                                        /// How the ListDirectoryDetails works is different for each operative system.
                                        /// To explain this lets see what does a ls -a shows when you do it: -rw-r--r--. 1 root root   683 Aug 19 09:59 0001.pcap
                                        /// As you can see the first letter is either - or d, now lets see how dir works in Windows: 11/30/2004  01:40 PM <DIR> ..
                                        /// They are quite different so you have to get what is what in each string.

                                        Ftplist newitem = new Ftplist();
                                        /// If ftp is LINUX based
                                        if ((line.Substring(0, 1) == "d" || line.Substring(0, 1) == "-") && newfilename[9] != "<DIR>")
                                        {
                                            newitem.Type = line.Substring(0, 1);
                                        }
                                        /// If ftp is WINDOWS based.
                                        else if (newfilename[9] == "<DIR>")
                                        {
                                            newitem.Type = "d";
                                        }
                                        /// Filename is the same in both environments.
                                        else
                                        {
                                            newitem.Type = "-";
                                        }
                                        newitem.Filename = finalfilename;
                                        sourceFileList.Add(newitem);
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                /// Read another line (file or directory).
                                line = reader.ReadLine();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw new Exception();
            }
            /// Return the list of ftlist struct.
            return sourceFileList;
        }

        #endregion
    }
}