using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NES
{
    public struct sHeader
    {
        // iNES Format Header
        public byte[] Name;
        public byte PRG_ROM_Chunks;
        public byte CHR_ROM_chunks;
        public byte Mapper1;
        public byte Mapper2;
        public byte PRG_RAM_size;
        public byte TV_System1;
        public byte TV_System2;
        public byte[] Unused;
    }
}