using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemoryWorker
{
    internal class MemoryHelper
    {
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwAccess, bool inherit, int pid);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, Int64 address, byte[] buffer, int size, IntPtr outSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        public static byte[] ReadMemory(Process process, long startAdress, int bytesCount)
        {
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            int bytesRead = 0;
            byte[] buffer = new byte[bytesCount];

            ReadProcessMemory(processHandle, startAdress, buffer, buffer.Length, (IntPtr)bytesRead);

            return buffer;
        }

        public static bool WriteMemory(Process process, byte[] bytesToWrite, long bytesPos)
        {
            IntPtr processHandle = process.Handle;
            int bytesWritten = 0;
 
            var z = WriteProcessMemory(processHandle, (IntPtr)bytesPos, bytesToWrite, bytesToWrite.Length, ref bytesWritten);
            return bytesWritten > 0;
        }
    }
}
