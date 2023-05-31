#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Open source software https://github.com/diev/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FTPSReportsDownload
{
    public static class Helper
    {
        private static readonly string _server = "ftp://" + GetConfigValue("Server");
        private static readonly NetworkCredential _user = new NetworkCredential(GetConfigValue("UserName"), GetConfigValue("Password"));
        private static readonly string _downloadDirectory = GetConfigValue("DownloadDirectory");
        private static readonly string _log = GetConfigValue("DownloadLog", DateTime.Now);
        private static readonly Encoding _encoding = Encoding.GetEncoding(1251);

        static Helper()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            CreatePath(_downloadDirectory);
            CreatePath(_log);
        }

        public static FtpWebResponse GetResponse(string serverPath, string method = WebRequestMethods.Ftp.DownloadFile, long contentOffset = 0)
        {
            var path = serverPath.Replace(@"\", "/");
            var request = (FtpWebRequest)WebRequest.Create(_server + path);
            request.Proxy = null; // Игнорировать настройки прокси.
            request.Credentials = _user;
            request.Method = method;
            request.EnableSsl = true;
            request.UseBinary = true;
            request.ContentLength = contentOffset;

            string log = $"< {method} {serverPath}";

            if (contentOffset > 0)
            {
                log += $" {contentOffset}";
            }

            Log(log);
            var response = (FtpWebResponse)request.GetResponse();

            if (response.StatusCode == FtpStatusCode.DataAlreadyOpen || response.StatusCode == FtpStatusCode.OpeningData)
            {
                //skip information
            }
            else
            {
                Log($"> {response.StatusDescription}", false);
            }

            return response;
        }

        public static string[] DownloadHistory(string serverPath, string localPath)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    FileMode mode = File.Exists(localPath) ? FileMode.Append : FileMode.Create;
                    long resumePosition;

                    using (var writeStream = new FileStream(localPath, mode))
                    {
                        resumePosition = writeStream.Position;

                        using (var responseStream = GetResponse(serverPath, WebRequestMethods.Ftp.DownloadFile, resumePosition).GetResponseStream())
                        {
                            responseStream.CopyToAsync(memoryStream).Wait();
                        }

                        memoryStream.Position = resumePosition;
                        memoryStream.CopyToAsync(writeStream).Wait();
                    }

                    memoryStream.Position = resumePosition;
                    var lines = new List<string>();

                    using (var reader = new StreamReader(memoryStream)) //, Encoding.ASCII))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }

                    return lines.ToArray();
                }
            }
            catch (WebException e)
            {
                Log("Download file -> Error: " + e.Message);
                return new string[] {};
            }
        }

        public static bool DownloadFile(string serverPath, string localPath = null, bool resume = false)
        {
            try
            {
                var path = localPath ?? Path.Combine(_downloadDirectory, Path.GetFileName(serverPath));
                FileMode mode = (File.Exists(path) && resume) ? FileMode.Append : FileMode.Create;

                using (var writeStream = new FileStream(path, mode))
                {
                    using (var response = GetResponse(serverPath, WebRequestMethods.Ftp.DownloadFile, writeStream.Position))
                    using (var responseStream = response.GetResponseStream())
                    {
                        responseStream.CopyTo(writeStream);
                    }
                }

                return true;
            }
            catch (WebException e)
            {
                var x = (FtpWebResponse)e.Response;

                if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    Log("Файл был удален в связи с истечением срока давности.");
                    return true; // it's normal to skip it
                }

                Log("Download file -> Error: " + e.Message);
                return false;
            }
        }

        public static int CompareSizeOfFile(string serverPath, string localPath = null)
        {
            try
            {
                var response = GetResponse(serverPath, WebRequestMethods.Ftp.GetFileSize);
                var serverSize = response.ContentLength;
                response.Close();

                var path = localPath ?? Path.Combine(_downloadDirectory, Path.GetFileName(serverPath));

                if (!File.Exists(path))
                {
                    return -1;
                }

                var localSize = new FileInfo(path).Length;

                return (serverSize == localSize)
                    ? 0
                    : (serverSize > localSize)
                        ? 1
                        : -1;
            }
            catch (WebException e)
            {
                var x = (FtpWebResponse)e.Response;

                if (x.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    Log("Файл был удален в связи с истечением срока давности.");
                    return 0;
                }

                Log("Size of file -> Error: " + e.Message);
                return -1;
            }
        }

        public static string GetConfigValue(string key, bool isRequired = true, string message = null)
        {
            string result = ConfigurationManager.AppSettings[key];

            if (result == null)
            {
                if (isRequired)
                {
                    Log(message ?? $"Ошибка в конфиг. файле: Параметр '{key}' не задан в конфиг. файле.");
                    Environment.Exit(1);
                }
            }

            return result;
        }

        public static string GetConfigValue(string key, DateTime now)
        {
            return string.Format(GetConfigValue(key), now);
        }

        public static void CreatePath(string path)
        {
            var extractedPath = path.Contains('.')
                ? Path.GetDirectoryName(Path.GetFullPath(path))
                : path;

            if (!Directory.Exists(extractedPath))
            {
                Directory.CreateDirectory(extractedPath);
            }
        }

        public static void Log(string text, bool newLine = true)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    var message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + text;

                    if (newLine)
                    {
                        Console.WriteLine(message);
                        File.AppendAllText(_log, message + Environment.NewLine, _encoding);
                    }
                    else
                    {
                        Console.Write(message);
                        File.AppendAllText(_log, message, _encoding);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка: Не могу записать лог файл.");
                Console.WriteLine("Детали ошибки: " + e.Message + Environment.NewLine);
                Console.WriteLine("Подсказка: Проверьте настройку параметра 'DownloadLog' в конфиг. файле.");
            }
        }
    }
}
