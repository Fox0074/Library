using System;
using System.Collections.Generic;
using System.Diagnostics;
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


        //TODO: Дублирование кода
        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe static extern Boolean DeviceIoControl(
            IntPtr hDevice,
            long dwIoControlCode,
            ref _KERNEL_READ_REQUEST lpInBuffer,
            long nInBufferSize,
            ref _KERNEL_READ_REQUEST lpOutBuffer,
            long nOutBufferSize,
            ref long lpBytesReturned,
            [In] ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe static extern Boolean DeviceIoControl(
            IntPtr hDevice,
            long dwIoControlCode,
            ref _KERNEL_WRITE_REQUEST lpInBuffer,
            long nInBufferSize,
            ref _KERNEL_WRITE_REQUEST lpOutBuffer,
            long nOutBufferSize,
            ref long lpBytesReturned,
            [In] ref NativeOverlapped lpOverlapped);

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;

        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;

        private const uint GET_CLIENT_ADDRESS = 0x480;
        private const uint READ_REQUEST = 0x481;
        private const uint WRITE_REQUEST = 0x482;

        public IntPtr hDriver;

        public KernalInterface(string registryPath)
        {
            hDriver = CreateFile(registryPath, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, (IntPtr)0, 3, 0, (IntPtr)0);
        }

        private uint IoMethodRequest(uint value)
        {
            return (0x00000022 << 16) | (0 << 14) | (value << 2) | 0;
        }


        public long GetClientAddress(long pid)
        {
            if (hDriver == (IntPtr)(-1))
            {
                return 0;
            }

            long address = pid;
            int bytes = 0;
            var test = new NativeOverlapped();

            if (DeviceIoControl(hDriver, IoMethodRequest(GET_CLIENT_ADDRESS), ref address, sizeof(long), ref address, sizeof(long), ref bytes, ref test))
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
                return 0;

            readRequest.ProcessId = processId;
            readRequest.Address = readAddress;
            readRequest.pBuffer = ptr.ToInt64();
            readRequest.Size = size;

            long bytes = 0;
            var test = new NativeOverlapped();

            if (DeviceIoControl(
                hDriver,
                IoMethodRequest(READ_REQUEST),
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

        public float ReadVirtualMemoryFloat(long processId, long readAddress, long size)
        {

            _KERNEL_READ_REQUEST readRequest;
            float readValue = 0;
            IntPtr ptr = Marshal.AllocHGlobal(4);
            Marshal.WriteInt64(ptr, 0, 0);

            if (hDriver == (IntPtr)(-1))
                return 0;

            readRequest.ProcessId = processId;
            readRequest.Address = readAddress;
            readRequest.pBuffer = ptr.ToInt64();
            readRequest.Size = size;

            long bytes = 0;
            var test = new NativeOverlapped();

            if (DeviceIoControl(
                hDriver,
                IoMethodRequest(READ_REQUEST),
                ref readRequest,
                32,
                ref readRequest,
                32,
                ref bytes,
                ref test))
            {
                float[] x = new float[1];
                Marshal.Copy(ptr, x, 0, 1);
                Marshal.FreeHGlobal(ptr);
                return x.First();
            }

            Marshal.FreeHGlobal(ptr);
            return -1;
        }

        public T Read<T>(IntPtr address)
        {
            try
            {
                var size = Marshal.SizeOf(typeof(T));
                var buffer = new byte[size];

                Marshal.Copy(address, buffer, 0, size);
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                var data = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                handle.Free();

                return data;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Exception thrown: {0}", e);

                return default(T);
            }
        }

        public unsafe T ReadVirtualMemoryAny<T>(long processId, long readAddress)
        {

            _KERNEL_READ_REQUEST readRequest;
            var size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            var buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, size);

            if (hDriver == (IntPtr)(-1))
                return default(T);

            readRequest.ProcessId = processId;
            readRequest.Address = readAddress;
            readRequest.pBuffer = ptr.ToInt64();
            readRequest.Size = size;

            long bytes = 0;
            var test = new NativeOverlapped();

            if (DeviceIoControl(
                hDriver,
                IoMethodRequest(READ_REQUEST),
                ref readRequest,
                32,
                ref readRequest,
                32,
                ref bytes,
                ref test))
            {
     
                Marshal.Copy(ptr, buffer,0, size);
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                var data = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                handle.Free();
                Marshal.FreeHGlobal(ptr);
                return data;
            }

            Marshal.FreeHGlobal(ptr);
            return default(T);
        }

        public bool WriteVirtualMemory(long processId, long writeAddress, long writeValue, long size)
        {
            _KERNEL_WRITE_REQUEST writeRequest;
            IntPtr ptr = Marshal.AllocHGlobal(8);
            Marshal.WriteInt64(ptr, 0, writeValue);

            if (hDriver == (IntPtr)(-1))
                return false;

            writeRequest.ProcessId = processId;
            writeRequest.Address = writeAddress;
            writeRequest.pBuffer = ptr.ToInt64();
            writeRequest.Size = size;

            long bytes = 0;
            var test = new NativeOverlapped();

            if (DeviceIoControl(
                hDriver,
                IoMethodRequest(WRITE_REQUEST),
                ref writeRequest,
                32,
                ref writeRequest,
                32,
                ref bytes,
                ref test))
            {
                Marshal.FreeHGlobal(ptr);
                return true;
            }

            Marshal.FreeHGlobal(ptr);
            return false;
        }

    }
}
