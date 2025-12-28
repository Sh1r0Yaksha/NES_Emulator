using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NES
{
    public class Mapper000 : Mapper
    {
        public Mapper000(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks){}

        public override bool CPU_MapRead(ushort addr, out uint mappedAddr)
        {
            // if PRGROM is 16KB
            //     CPU Address Bus          PRG ROM
            //     0x8000 -> 0xBFFF: Map    0x0000 -> 0x3FFF
            //     0xC000 -> 0xFFFF: Mirror 0x0000 -> 0x3FFF
            // if PRGROM is 32KB
            //     CPU Address Bus          PRG ROM
            //     0x8000 -> 0xFFFF: Map    0x0000 -> 0x7FFF	
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mappedAddr = (uint)(addr & (PRGBanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }
            else
            {
                mappedAddr = 0;
                return false;    
            }
            
        }

        public override bool CPU_MapWrite(ushort addr, out uint mappedAddr)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mappedAddr = (uint)(addr & (PRGBanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }
            else
            {
                mappedAddr = 0;
                return false;
            }
        }

        public override bool PPU_MapRead(ushort addr, out uint mappedAddr)
        {
            // There is no mapping required for PPU
            // PPU Address Bus          CHR ROM
            // 0x0000 -> 0x1FFF: Map    0x0000 -> 0x1FFF
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                mappedAddr = addr;
                return true;
            }
            else
            {
                mappedAddr = 0;
                return false;    
            }
        }

        public override bool PPU_MapWrite(ushort addr, out uint mappedAddr)
        {
            if (addr >= 0x0000 && addr <= 0x1FFF && CHRBanks == 0)
            {
                // Treat as RAM
                mappedAddr = addr;
                return true;
            }
            else
            {
                mappedAddr = 0;
                return false;    
            }
        }
    }
}