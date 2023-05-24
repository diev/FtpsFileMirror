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

using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using FluentFTP;
using FluentFTP.Client.BaseClient;
using FluentFTP.Rules;

using Microsoft.Extensions.Configuration;

namespace FtpsClient;

internal class Program
{
    private static readonly MirrorSettings mirror = new();
    private static readonly ControlSettings control = new();

    private static Encoding _enc;

    private static int Main(string[] args)
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile(UserSettingsManager.FilePath(), true, false)
            .Build();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _enc = Encoding.GetEncoding(1251);

        if (args.Length == 0 || 
            args[0].EndsWith("?", StringComparison.Ordinal))
        {
            Usage();
            return 1;
        }
        else //if (string.IsNullOrEmpty(proxyHost))
        {
            try
            {
                Download(args, config);
            }
            catch (Exception ex)
            {
                string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm ") +
                    ex.Message + Environment.NewLine;
                
                if (ex.InnerException != null)
                {
                    text += ex.InnerException.Message + Environment.NewLine;
                }

                File.AppendAllText("FtpsClient.log", text, _enc);
            }
        }
        //else
        //{
        //    //ProxyDownload();
        //}

        return 0;
    }

    private static void Usage()
    {
        //{Environment.ProcessPath}
        var assembly = Assembly.GetExecutingAssembly();
        string usage = $@"{assembly.GetName().Name} v{assembly.GetName().Version?.ToString() ?? "?"}
{assembly.GetCustomAttributes<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description}

-?  - this help
-m  - mirror
-u  - update
-r  - by rules
-l  - by list

OS: {Environment.OSVersion}
.NET: {Environment.Version}
App Settings: {Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")}
User Settings: {UserSettingsManager.FilePath()}";

        Console.WriteLine(usage);
    }

    private static void Download(string[] args, IConfiguration config)
    {
        List<FtpResult>? resultList = null;

        using var ftpClient = new FtpClient();
        config.Bind(nameof(FtpClient), ftpClient);

        var ftpUser = new NetworkCredential();
        config.Bind(nameof(ftpUser), ftpUser);
        ftpClient.Credentials = ftpUser;

        if (!ftpClient.Config.ValidateAnyCertificate)
        {
            ftpClient.ValidateCertificate += new FtpSslValidation(OnValidateCertificate); //TODO -= ?
        }

        config.Bind(nameof(MirrorSettings), mirror);
        config.Bind(nameof(ControlSettings), control);

        //mirror = config.GetSection(nameof(MirrorSettings)).Get<MirrorSettings>();
        //control = config.GetSection(nameof(ControlSettings)).Get<ControlSettings>();

        try
        {
            ftpClient.AutoConnect();
            ftpClient.SetWorkingDirectory(mirror.Root);

            if (args.Contains("-m"))
            {
                //Mirror
                resultList = ftpClient.DownloadDirectory(mirror.Path, mirror.Root, 
                    FtpFolderSyncMode.Mirror, FtpLocalExists.Skip, FtpVerify.None);
            }
            else if (args.Contains("-u"))
            {
                //Update
                resultList = ftpClient.DownloadDirectory(mirror.Path, mirror.Root, 
                    FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None);
            }
            else if (args.Contains("-r"))
            {
                //Update by allowed rules
                var rules = new List<FtpRule>
                {
                    new FtpFolderRegexRule(true, 
                    new List<string>{ mirror.FolderRegex }, 1)
                };

                resultList = ftpClient.DownloadDirectory(mirror.Path, mirror.Root, 
                    FtpFolderSyncMode.Update, FtpLocalExists.Skip, FtpVerify.None, rules);
            }
            else if (args.Contains("-l"))
            {
                //Download by file list
                string listFile = Path.Combine(control.Path, mirror.List); // "UpdateHistory.txt"
                string lastFile = Path.Combine(control.Path, control.Last); // "last.txt"

                string? lastLine = File.Exists(lastFile)
                    ? File.ReadAllLines(lastFile, _enc).FirstOrDefault()
                    : null;

                var check = ftpClient.CompareFile(listFile, mirror.List, FtpCompareOption.Size);

                if (check == FtpCompareResult.Equal) 
                {
                    Environment.Exit(0);
                }

                var status = ftpClient.DownloadFile(listFile, mirror.List, FtpLocalExists.Resume);

                if (status != FtpStatus.Success)
                {
                    Environment.Exit(1);
                }

                var lines = File.ReadAllLines(listFile);
                IEnumerable<string> batch;

                if (lastLine != null && lines.Contains(lastLine))
                {
                    if (lastLine == lines.Last())
                    {
                        Environment.Exit(0);
                    }

                    batch = lines.SkipWhile(x => x != lastLine).Skip(1);
                }
                else
                {
                    batch = lines;
                }

                resultList = ftpClient.DownloadFiles(mirror.Path, batch);

                if (resultList!.Count > 0)
                {
                    lastLine = batch.Last();
                    File.WriteAllText(lastFile, lastLine + Environment.NewLine + 
                        "# Первая строка определяет последний скачанный файл.", _enc);
                }
            }
        }
        finally
        {
            ftpClient.Disconnect();
        }

        if (!string.IsNullOrEmpty(control.List) && resultList!.Count > 0)
        {
            StringBuilder list = new();

            foreach (var result in resultList)
            {
                if (!result.IsFailed && !result.IsSkipped && !result.IsSkippedByRule && 
                    result.Type == FtpObjectType.File)
                {
                    list.AppendLine(result.LocalPath);
                }
            }

            if (list.Length > 0)
            {
                string path = Path.Combine(control.Path, control.List);
                File.AppendAllText(path, list.ToString(), _enc);
            }
        }
    }

    private static void OnValidateCertificate(BaseFtpClient baseClient, FtpSslValidationEventArgs e)
    {
        if (control.SaveCertificate)
        {
            string ftpHost = Path.Combine(control.Path, baseClient.Host);

            File.WriteAllText(ftpHost + ".txt", e.Certificate.ToString());
            File.WriteAllBytes(ftpHost + ".cer", e.Certificate.Export(X509ContentType.Cert));
        }

        //TODO check certificate
        if (e.PolicyErrors != SslPolicyErrors.None)
        {
            e.Accept = false;
            return;
        }

        //valid
        e.Accept = true;
    }
}
