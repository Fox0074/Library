using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverUserInterface
{
    internal class DayzOffs
    {
        public int World = 0x4089990;
        public int BulletTable =  0xD70; //Wordl + 
        public int NearEntityTable = 0xEB8;//Wordl + 
        public int FarEntityTable = 0x1000;//Wordl + 
        public int CommsTable = 0x18F8;//Wordl + 
        public int SlowEntityTable = 0x1F68;//Wordl + 
        public int ItemTables = 0x1FB8;//Wordl + 
        public int LocalPlayer = 0x28B8;//Wordl + 
    }
}
