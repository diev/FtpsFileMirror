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

using Lib;


namespace FTPSReportsDownload
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine(App.Version);

            if (args.Length > 0 ) // Usage
            {
                Console.WriteLine(App.Description);
                Console.WriteLine("Set all appSettings in the '.config' file.");
                return 1;
            }

            // Just work
            return Ftps.Sync();
        }
    }
}
