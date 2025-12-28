using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NES
{
    public abstract class Mapper
    {   
        protected byte PRGBanks;
        protected byte CHRBanks;

        public Mapper(byte prgBanks, byte chrBanks)
        {
            PRGBanks = prgBanks;
            CHRBanks = chrBanks;
        }

        public abstract bool CPU_MapRead(ushort addr, out uint mappedAddr);
        public abstract bool CPU_MapWrite(ushort addr, out uint mappedAddr);
        public abstract bool PPU_MapRead(ushort addr, out uint mappedAddr);
        public abstract bool PPU_MapWrite(ushort addr, out uint mappedAddr);
    }
}