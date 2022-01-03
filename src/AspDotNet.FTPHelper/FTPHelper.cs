using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace AspDotNet.FTPHelper
{
    public class FTPHelper
    {
        #region Constructors
        public FTPHelper(string host)
        {
            this.Host = host;
            this.Port = 0x15;
            this.Username = null;
            this.Password = null;
            this.Domain = null;
            this.usePassive = true;
            this.TestConnection();
        }

        public FTPHelper(string host, int port)
        {
            this.Host = host;
            this.Port = port;
            this.Username = null;
            this.Password = null;
            this.Domain = null;
            this.usePassive = true;
            this.TestConnection();
        }

        public FTPHelper(string host, string username, string password)
        {
            this.Host = host;
            this.Port = 0x15;
            this.Username = username;
            this.Password = password;
            this.Domain = "";
            this.usePassive = true;
            this.TestConnection();
        }

        public FTPHelper(string host, int port, string username, string password)
        {
            this.Host = host;
            this.Port = port;
            this.Username = username;
            this.Password = password;
            this.Domain = "";
            this.TestConnection();
        }

        public FTPHelper(string host, string username, string password, bool passive)
        {
            this.Host = host;
            this.Port = 0x15;
            this.Username = username;
            this.Password = password;
            this.Domain = "";
            this.usePassive = passive;
            this.TestConnection();
        }

        public FTPHelper(string host, string username, string password, string domain)
        {
            this.Host = host;
            this.Port = 0x15;
            this.Username = username;
            this.Password = password;
            this.Domain = domain;
            this.TestConnection();
        }

        public FTPHelper(string host, int port, string username, string password, string domain)
        {
            this.Host = host;
            this.Port = port;
            this.Username = username;
            this.Password = password;
            this.Domain = domain;
            this.TestConnection();
        }

        #endregion

        #region Connection
        private FtpWebRequest Connection(string prependUri)
        {
            prependUri = prependUri.Replace(@"\", "/");
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri("ftp://" + this.Host + prependUri));
            request.UsePassive = this.usePassive;
            request.EnableSsl = this.enableSsl;
            if (!string.IsNullOrWhiteSpace(this.Username))
            {
                request.Credentials = new NetworkCredential(this.Username, this.Password);
            }
            return request;
        }

        private void TestConnection()
        {
            try
            {
                FtpWebRequest request = this.Connection("");
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.GetResponse().Close();
            }
            catch (WebException exception)
            {
                if (exception.Message != "The remote server returned an error: (530) Not logged in.")
                {
                    throw;
                }
                if (string.IsNullOrWhiteSpace(this.Username))
                {
                    throw new Exception("The remote server requires login credentials.", exception);
                }
                throw new Exception("The login credentials provided were rejected by the remote server.", exception);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Helping Methods
        public void FTPCreateDirectory(string directory)
        {
            FtpWebRequest request = this.Connection(directory);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            response.GetResponseStream().Close();
            response.Close();
        }
        public bool FTPDirectoryExists(string directory)
        {
            bool flag = true;
            try
            {
                FtpWebRequest request = this.Connection(directory);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                ((FtpWebResponse)request.GetResponse()).Close();
            }
            catch (WebException)
            {
                flag = false;
            }
            return flag;
        }

        public void LocalCreateDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
        }

        public bool LocalDirectoryExists(string directory)
        {
            return Directory.Exists(directory);
        }

        #endregion

        #region Methods
        public void UploadFile(FileStream srcFile, string ftpDir)
        {
            if (!this.FTPDirectoryExists(ftpDir))
            {
                this.FTPCreateDirectory(ftpDir);
            }
            FtpWebRequest request = this.Connection(ftpDir + "/" + Path.GetFileName(srcFile.Name));
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.KeepAlive = false;
            request.UseBinary = true;
            request.ContentLength = srcFile.Length;
            byte[] buffer = new byte[0x800];
            try
            {
                Stream requestStream = request.GetRequestStream();
                int count = srcFile.Read(buffer, 0, 0x800);
                while (true)
                {
                    if (count == 0)
                    {
                        requestStream.Flush();
                        requestStream.Close();
                        break;
                    }
                    requestStream.Write(buffer, 0, count);
                    count = srcFile.Read(buffer, 0, 0x800);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<string> GetFilesFromFolder(string path)
        {
            List<string> files = new List<string>();
            try
            {
                //Create FTP Request.
                FtpWebRequest request = this.Connection(path);
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string names = reader.ReadToEnd();

                reader.Close();
                response.Close();

                files = names.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return files;
        }

        public void DownloadFile(string ftpDirAndFile, string destDir)
        {
            try
            {
                if (!this.LocalDirectoryExists(destDir))
                {
                    this.LocalCreateDirectory(destDir);
                }
                FtpWebRequest request = this.Connection(ftpDirAndFile);
                request.KeepAlive = true;
                request.UseBinary = true;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            using (StreamWriter writer = new StreamWriter(destDir + "/" + Path.GetFileName(ftpDirAndFile)))
                            {
                                writer.Write(reader.ReadToEnd());
                                writer.Flush();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteFile(string filePath)        {            try            {                FtpWebRequest request = this.Connection(filePath);                request.Method = WebRequestMethods.Ftp.DeleteFile;                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())                {                    return true;                }            }            catch (Exception ex)            {
                throw ex;
            }        }
        #endregion

        #region Properties
        private string Host { get; set; }

        private int Port { get; set; }

        private string Username { get; set; }

        private string Password { get; set; }

        private string Domain { get; set; }

        private bool usePassive { get; set; }
        private bool enableSsl { get; set; } = true;
        #endregion

    }

}
