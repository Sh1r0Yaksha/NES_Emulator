using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NES
{
    public class PPU
    {
        byte[,] TblName = new byte[2, 1024];
	    byte[,] TblPattern = new byte[2, 4096];
	    byte[] TblPalette = new byte[32];


        short Scanline = 0;
        short Cycle = 0;

#region Read/Write
        public byte CPU_Read(ushort address, bool readOnly = false)
        {
            byte data = 0x00;

            switch (address)
            {
            case 0x0000: // Control
                break;
            case 0x0001: // Mask
                break;
            case 0x0002: // Status
                break;
            case 0x0003: // OAM Address
                break;
            case 0x0004: // OAM Data
                break;
            case 0x0005: // Scroll
                break;
            case 0x0006: // PPU Address
                break;
            case 0x0007: // PPU Data
                break;
            }

            return data;
        }

        public void CPU_Write(ushort address, byte data)
        {
            switch (address)
            {
            case 0x0000: // Control
                break;
            case 0x0001: // Mask
                break;
            case 0x0002: // Status
                break;
            case 0x0003: // OAM Address
                break;
            case 0x0004: // OAM Data
                break;
            case 0x0005: // Scroll
                break;
            case 0x0006: // PPU Address
                break;
            case 0x0007: // PPU Data
                break;
            }
        }

        public byte PPU_Read(ushort address, bool readOnly = false)
        {
            address &= 0x3FFF;

            if (Bus.Cartridge.PPU_Read(address,out byte data))
            {

            }

            return data;
        }

        public void PPU_Write(ushort address, byte data)
        {
            address &= 0x3FFF;

            if (Bus.Cartridge.PPU_Write(address, data))
            {

            }   
        }
#endregion

#region Interface
        public void Clock()
        {
            // Advance renderer - it never stops, it's relentless
            Cycle++;
            if (Cycle >= 341)
            {
                Cycle = 0;
                Scanline++;
                if (Scanline >= 261)
                {
                    Scanline = -1;
                    frame_complete = true;
                }
            }
        }
#endregion

#region Debugging
        bool frame_complete = false;
#endregion
    }
}