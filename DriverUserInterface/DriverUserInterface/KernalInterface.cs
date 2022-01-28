using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DriverUserInterface
{
    public class KernalInterface
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct _KERNEL_READ_REQUEST
        {
            public long ProcessId;
            public long Address;
            public long pBuffer;
            public long Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct _KERNEL_WRITE_REQUEST
        {
            public long ProcessId;
            public long Address;
            public long pBuffer;
            public long Size;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(
        [MarshalAs(UnmanagedType.LPWStr)]
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        ref long InBuffer,
        int nInBufferSize,
        ref long OutBuffer,
        int nOutBufferSize,
        ref int pBytesReturned,
        [In] ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe static extern Boolean DeviceIoControl(
                    IntPtr hDevice,
                    UInt32 dwIoControlCode,
                    ref _KERNEL_READ_REQUEST lpInBuffer,
                    UInt32 nInBufferSize,
                    ref _KERNEL_READ_REQUEST lpOutBuffer,
                    UInt32 nOutBufferSize,
                    ref UInt32 lpBytesReturned,
                    [In] ref NativeOverlapped lpOverlapped);

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;

        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;

        public IntPtr hDriver;

        public KernalInterface(string registryPath)
        {
            hDriver = CreateFile(registryPath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, (IntPtr)0, 3, 0, (IntPtr)0);
        }

        private uint IoGetClientAddress()
        {
            return (0x00000022 << 16) | (0 << 14) | (0x666 << 2) | 0;
        }

        private uint IoReadRequest()
        {
            return (0x00000022 << 16) | (0 << 14) | (0x667 << 2) | 0;
        }

        private uint IoWriteRequest()
        {
            return (0x00000022 << 16) | (0 << 14) | (0x668 << 2) | 0;
        }


        public long GetClientAddress()
        {
            if (hDriver == (IntPtr)(-1))
            {
                return 0;
            }

            long address = 0;
            int bytes = 0;
            var test = new NativeOverlapped();

            if (DeviceIoControl(hDriver, IoGetClientAddress(), ref address, sizeof(long), ref address, sizeof(long), ref bytes, ref test))
            {
                return address;
            }
            
            return 0;
        }

        public long ReadVirtualMemory(long processId, long readAddress, long size)
        {

            _KERNEL_READ_REQUEST readRequest;
            long readValue = 0;
            IntPtr ptr = Marshal.AllocHGlobal(8);
            Marshal.WriteInt64(ptr, 0, readValue);

            if (hDriver == (IntPtr)(-1))
            {
                return 0;
            }


               readRequest.ProcessId = processId;
            readRequest.Address = 970125460168;
            readRequest.pBuffer = ptr.ToInt64();
            readRequest.Size = size;


            uint bytes = 0;
            var test = new NativeOverlapped();


            if (DeviceIoControl(
                hDriver,
                IoReadRequest(),
                ref readRequest,
                32,
                ref readRequest,
                32,
                ref bytes,
                ref test))
            {
                readValue = Marshal.ReadInt64(ptr);
                Marshal.FreeHGlobal(ptr);
                return readValue;
            }

            Marshal.FreeHGlobal(ptr);
            return readValue;
        }

        public unsafe bool WriteVirtualMemory(long processId, long writeAddress, void* writeValue, long size)
        {
            //if (hDriver == (IntPtr)(-1))
            //{
            //    return false;
            //}

            //_KERNEL_READ_REQUEST writeRequest;

            //writeRequest.ProcessId = processId;
            //writeRequest.Address = writeAddress;
            //writeRequest.pBuff = writeValue;
            //writeRequest.Size = size;

            //int bytes = 0;
            //var test = new NativeOverlapped();


            //IntPtr outPtr = Marshal.AllocHGlobal(Marshal.SizeOf(writeRequest));
            //Marshal.StructureToPtr(writeRequest, outPtr, false);

            //if (DeviceIoControl(
            //    hDriver,
            //    IoWriteRequest(),
            //    outPtr,
            //    sizeof(_KERNEL_READ_REQUEST),
            //    outPtr,
            //    sizeof(_KERNEL_READ_REQUEST),
            //    ref bytes,
            //    ref test))
            //{
            //    return true;
            //}


            return false;
        }

    }
}
