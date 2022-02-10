using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DriverUserInterface
{
    public partial class EFT : Form
    {
        public static KernalInterface _driver;
        private long _baseAddres = -1;
        public static readonly long GOM = 0x14327E0;
        public long off2 = 0x142A6A0;
        public long off3 = 0x1432840;
        public static long gameObjectManager;

        public static IntPtr gameWorld = new IntPtr(0x0);

        private static readonly int[] fpsCameraStruct = new int[] { 0x30, 0x18 };
        public static IntPtr fpsCamera = new IntPtr(0x0);

        private static readonly int[] localGameWorldStruct = new int[] { 0x30, 0x18, 0x28 };
        public static IntPtr localGameWorld = new IntPtr(0x0);

        private static readonly int[] registeredPlayerStruct = new int[] { 0x60 };
        public static IntPtr registeredPlayers = new IntPtr(0x0);
        private static readonly int[] playerCountStruct = new int[] { 0x18 };

        private Process _gameProcess;

        public EFT()
        {
            InitializeComponent();
            _driver = new KernalInterface("\\\\.\\guideeh");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _gameProcess = Process.GetProcesses().FirstOrDefault(x => x.ProcessName == "EscapeFromTarkov");
            _driver._processId = _gameProcess.Id;
            _baseAddres = GetBaseAddres(_gameProcess);

            gameObjectManager = _driver.ReadVirtualMemory<long>(_baseAddres + GOM);
            var gameObjectManager2 = _driver.ReadVirtualMemory<long>(_baseAddres + off2);
            var gameObjectManager3 = _driver.ReadVirtualMemory<long>(_baseAddres + off3);
        }

        //private static long FindActiveObject(string objName)
        //{
        //    int limit = 350;
        //    StringBuilder objNames = new StringBuilder();
        //    IntPtr output = IntPtr.Zero;
        //    if (!Memory.isRunning())
        //    {
        //        return output;
        //    }
        //    for (int curObject = 0x1; curObject < limit; curObject++)
        //    {
        //        List<int> newStruct = new List<int>() { 0x18 };
        //        List<int> depth = Enumerable.Repeat(0x8, curObject).ToList();
        //        newStruct.AddRange(depth);
        //        newStruct.AddRange(new int[] { 0x10, 0x60, 0x0 });

        //        long newAddr = Base.GetPtr(gameObjectManager, newStruct.ToArray()).ToInt64();

        //        string objectName = Memory.ReadOld<string>(newAddr, 9);
        //        objNames.AppendLine(objectName);
        //        if (objectName.ToLower() == objName.ToLower())
        //        {
        //            newStruct.RemoveAt(newStruct.Count() - 1);
        //            newStruct.RemoveAt(newStruct.Count() - 1);
        //            output = Base.GetPtr(gameObjectManager, newStruct.ToArray());
        //            return output;
        //        }
        //    }
        //    return output;
        //}

        private long GetBaseAddres(Process proc)
        {
            return _driver.GetClientAddress(proc.Id);
        }
    }
}
