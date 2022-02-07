using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverUserInterface
{
    internal class DayzOffs
    {
        public static int OffText = 0x10;
        public static int OffLength = 0x8;
        public static long World = 0x408A990;

        public static int Network = 0x634;
        public static int BulletTable =  0xD70; 
        public static int NearEntityTable = 0xEB8;
        public static int FarEntityTable = 0x1000;
        public static int CommsTable = 0x18F8;
        public static int SlowEntityTable = 0x1F68;
        public static int ItemTables = 0x1FB8;
        public static int LocalPlayer = 0x28B8;

        //OFFSET_WORLD_DROPPEDITEMTABLE 0xF84
    }
}
