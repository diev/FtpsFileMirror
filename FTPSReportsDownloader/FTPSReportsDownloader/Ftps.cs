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
using System.IO;

using static FTPSReportsDownload.Helper;

namespace FTPSReportsDownload
{
    public static class Ftps
    {
        private static readonly string _updateHistory = "/UpdateHistory.txt";

        public static int Sync()
        {
            var downloadDirectory = GetConfigValue("DownloadDirectory");
            var historyFile = GetConfigValue("DownloadHistory", false)
                ?? Path.Combine(downloadDirectory, Path.GetFileName(_updateHistory));
            
            if (!int.TryParse(GetConfigValue("DownloadDays", false), out int daysBefore))
            {
                daysBefore = 14;
            }

            int compare = CompareSizeOfFile(_updateHistory, historyFile);
            int counter = 0;

            if (compare == 0)
            {
                Log("Нет обновлений.");
                return 0;
            }

            if (compare > 0)
            {
                var list = DownloadHistory(_updateHistory, historyFile);
                Log($"Есть обновления ({list.Length}).");

                foreach (var file in list)
                {
                    if (DownloadFile(file))
                    {
                        counter++;
                    }
                }
            }
            else
            {
                DownloadFile(_updateHistory, historyFile);
                var list = File.ReadAllLines(historyFile);
                Log($"Перезагрузка за последние {daysBefore} дней.");
                var dateFrom = DateTime.Now.AddDays(-daysBefore).ToString("yyyyMMdd");

                foreach (var file in list)
                {
                    // /EQ/20230526/PC01101_EQMLIST_001_260523_025153489.xml.p7s.zip.p7e
                    if (file[3] == '/' && string.Compare(file, 4, dateFrom, 0, 8) > 0)
                    {
                        if (DownloadFile(file))
                        {
                            counter++;
                        }
                    }
                }
            }

            Log($"Загружено {counter}.");
            return 0;
        }
    }
}
