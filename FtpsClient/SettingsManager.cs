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

//TODO

using System.Text.Json;

namespace FtpsClient;

//public class MySettings : SettingsManager<MySettings>
//{
//    public string? Property { get; set; }
//}

/// <summary>
/// https://reddeveloper.ru/questions/ekvivalent-usersettings-applicationsettings-v-wpf-net-5-net-6-ili-net-core-47AXM
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SettingsManager<T> where T : SettingsManager<T>, new()
{
    private static readonly string _filePath = UserSettingsManager.FilePath($"{typeof(T).Name}.json");

    public static T Instance { get; private set; } = new T();

    public static void Load()
    {
        if (File.Exists(_filePath))
        {
            Instance = JsonSerializer.Deserialize<T>(File.ReadAllText(_filePath))!;
        }
    }

    public static void Save()
    {
        string json = JsonSerializer.Serialize(Instance);
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, json);
    }

    public static void Upgrade()
    {
        //TODO version to version
    }
}
