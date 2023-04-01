#region License
//------------------------------------------------------------------------------
// Copyright (c) Dmitrii Evdokimov
// Open ource software https://github.com/diev/
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

using FluentFTP;
using FluentFTP.Proxy.SyncProxy;

var ftpsHost = AppContext.GetData("Ftps.Host") as string ?? "localhost";
var ftpsPort = int.Parse(AppContext.GetData("Ftps.Port") as string ?? "21");
var ftpsValidateAnyCertificate = bool.Parse(AppContext.GetData("Ftps.ValidateAnyCertificate") as string ?? "false");
var ftpsUser = AppContext.GetData("Ftps.User") as string ?? "anonymous";
var ftpsPass = AppContext.GetData("Ftps.Pass") as string ?? "anonymous";
var ftpsRoot = AppContext.GetData("Ftps.Root") as string ?? "/";

var proxyUse = bool.Parse(AppContext.GetData("Proxy.Use") as string ?? "false");
var proxyHost = AppContext.GetData("Proxy.Host") as string ?? "localhost";
var proxyPort = int.Parse(AppContext.GetData("Proxy.Port") as string ?? "3128");
var proxyAnonymous = bool.Parse(AppContext.GetData("Proxy.Anonymous") as string ?? "false");
var proxyUser = AppContext.GetData("Proxy.User") as string ?? "anonymous";
var proxyPass = AppContext.GetData("Proxy.Pass") as string ?? "anonymous";

var mirrorPath = AppContext.GetData("Mirror.Path") as string ?? ".";

var logToConsole = bool.Parse(AppContext.GetData("LogToConsole") as string ?? "false");

if (proxyUse)
{
    ProxyDownload();
}
else
{
    DirectDownload();
}

void DirectDownload()
{
    using var client = new FtpClient(ftpsHost, ftpsUser, ftpsPass, ftpsPort);

    client.Config.ValidateAnyCertificate = ftpsValidateAnyCertificate;
    client.Config.LogToConsole = logToConsole;

    client.AutoConnect();
    client.DownloadDirectory(mirrorPath, ftpsRoot, FtpFolderSyncMode.Update, FtpLocalExists.Skip);
    client.Disconnect();
}

void ProxyDownload()
{
    var profile = new FtpProxyProfile()
    {
        ProxyHost = proxyHost,
        ProxyPort = proxyPort,
        FtpHost = ftpsHost,
        FtpPort = ftpsPort,
        FtpCredentials = new NetworkCredential(ftpsUser, ftpsPass)
    };

    if (!proxyAnonymous)
    {
        profile.ProxyCredentials = new NetworkCredential(proxyUser, proxyPass);
    }

    using var client = new FtpClientHttp11Proxy(profile);

    client.Config.ValidateAnyCertificate = ftpsValidateAnyCertificate;
    client.Config.LogToConsole = logToConsole;

    client.AutoConnect(); //TODO
    client.DownloadDirectory(mirrorPath, ftpsRoot, FtpFolderSyncMode.Update, FtpLocalExists.Skip);
    client.Disconnect();
}
