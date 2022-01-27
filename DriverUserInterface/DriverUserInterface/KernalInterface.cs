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
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct _KERNEL_READ_REQUEST
        {
            [FieldOffset(0)] public long ProcessId;
            [FieldOffset(24)] public long Address;
            [FieldOffset(32)] public long Address2;
            [FieldOffset(8)] public long pBuffer;
            [FieldOffset(16)] public long Size;
        }

        //[StructLayout(LayoutKind.Explicit)]
        public unsafe struct _KERNEL_WRITE_REQUEST
        {
            public long ProcessId;
            public long Address;
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
                    _KERNEL_READ_REQUEST* lpInBuffer,
                    UInt32 nInBufferSize,
                    _KERNEL_READ_REQUEST* lpOutBuffer,
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

        uint i = 7;
        public unsafe void* ReadVirtualMemory(long processId, long readAddress, long size)
        {
            _KERNEL_READ_REQUEST* readRequestLink;
            _KERNEL_READ_REQUEST readRequest;
            readRequestLink = &readRequest;
            void* readValue = (void*)0;

            if (hDriver == (IntPtr)(-1))
            {
                return (void*)0;
            }
            i++;
            readRequest.ProcessId = processId;
            readRequest.Address = 150;
            readRequest.Address2 = 151;
            readRequest.pBuffer = 99;
            readRequest.Size = 100;


            uint bytes = 0;
            var test = new NativeOverlapped();

            int size2 = Marshal.SizeOf(readRequest.GetType());
            //IntPtr ptr = Marshal.AllocHGlobal(size2);
            //Marshal.StructureToPtr(readRequest, ptr, false);

            var z = sizeof(_KERNEL_READ_REQUEST*);

            if (DeviceIoControl(
                hDriver,
                IoReadRequest(),
                readRequestLink,
                32,
                readRequestLink,
                32,
                ref bytes,
                ref test))
            {
                
                return readValue;
            }

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
