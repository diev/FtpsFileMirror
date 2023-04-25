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

using System.Reflection;
using System.Text;

namespace FtpsClient;

public static class UserSettingsManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="userProfile">true: C:\ProgramData or false: C:\Users\{UserName}\AppData\Local</param>
    /// <param name="addCompany">Company or nothing</param>
    /// <param name="addVersion">Version or nothing</param>
    /// <returns>C:\Users\{UserName}\AppData\Local\[Company]\FtpsClient\[Version]\usersettings.json or
    /// C:\ProgramData\[Company]\FtpsClient\[Version]\usersettings.json</returns>
    public static string FilePath(string fileName = "usersettings.json", bool userProfile = true, bool addCompany = true, bool addVersion = false)
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyName = assembly?.GetName();

        StringBuilder sb = new();

        string appData = Environment.GetFolderPath(userProfile
            ? Environment.SpecialFolder.LocalApplicationData // C:\Users\{UserName}\AppData\Local
            : Environment.SpecialFolder.CommonApplicationData); // C:\ProgramData

        sb.Append(appData).Append(Path.DirectorySeparatorChar);

        var company = assembly?.GetCustomAttributes<AssemblyCompanyAttribute>().FirstOrDefault()?.Company; // ?? "Company";
        if (addCompany && company != null)
        {
            sb.Append(company).Append(Path.DirectorySeparatorChar);
        }

        var app = assemblyName?.Name ?? Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
        sb.Append(app).Append(Path.DirectorySeparatorChar);

        var version = assemblyName?.Version?.ToString(); // ?? "1.0.0.0";
        if (addVersion && version != null)
        {
            sb.Append(version).Append(Path.DirectorySeparatorChar);
        }

        sb.Append(fileName);

        //return Path.Combine(appData,
        //    //company,
        //    app,
        //    //version, 
        //    fileName);

        return sb.ToString();
    }
}
