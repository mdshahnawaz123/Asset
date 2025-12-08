using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Services
{
    public static class MachineHelper
    {
        public static string GetMachineId()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    var guid = key?.GetValue("MachineGuid") as string;
                    if (!string.IsNullOrWhiteSpace(guid)) return guid;
                }
            }
            catch { /* ignore */ }

            return Environment.MachineName ?? "unknown-machine";
        }
    }
}
