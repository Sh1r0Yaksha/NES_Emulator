using System.ComponentModel;

namespace NES
{
    public static class Bus
    {
        
        public static string cartridgePath = string.Empty;
        // Devices on the bus

        // The 6502 derived processor
        public static CPU cpu = new CPU();

        // The 2C02 Picture Processing Unit
        public static PPU ppu = new PPU();

        // The Cartridge or "GamePak"
        public static Cartridge Cartridge;

        private static int nSystemClockCounter = 0;

        // Controller state
        public static byte controller1 = 0x00;
        public static byte controller2 = 0x00;

        // Controller shift registers (for reading)
        private static byte controller1_shift = 0x00;
        private static byte controller2_shift = 0x00;

        // Strobe state
        private static bool controller_strobe = false;


        // 2KB of RAM
        public static byte[] RAM = new byte[2 * 1024];

#region Main bus Read Write

        public static void CPU_Write(ushort address, byte data)
        {
            // if (Cartridge.CPU_Write(address, data))
            // {
            //     // The cartridge "sees all" and has the facility to veto
            //     // the propagation of the bus transaction if it requires.
            //     // This allows the cartridge to map any address to some
            //     // other data, including the facility to divert transactions
            //     // with other physical devices. The NES does not do this
            //     // but I figured it might be quite a flexible way of adding
            //     // "custom" hardware to the NES in the future!
            // }

            if (Cartridge.CPU_Write(address, data))
            {
                // The cartridge handled the write (e.g. Mapper registers)
                // We can return early, or let it fall through if you want "Bus Conflict" simulation 
                // (but for Mapper 0, return is fine).
                return; 
            }
            
            if (address >= 0x0000 && address <= 0x1FFF)
            {
                // System RAM Address Range. The range covers 8KB, though
                // there is only 2KB available. That 2KB is "mirrored"
                // through this address range. Using bitwise AND to mask
                // the bottom 11 bits is the same as addr % 2048.
                RAM[address & 0x07FF] = data;
            }

            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                // PPU Address range. The PPU only has 8 primary registers
                // and these are repeated throughout this range. We can
                // use bitwise AND operation to mask the bottom 3 bits, 
                // which is the equivalent of addr % 8.
                ppu.CPU_Write((ushort)(address & 0x0007), data);
            }

            else if (address == 0x4014)
            {
                // OAMDMA - Copy 256 bytes from CPU RAM to PPU OAM
                // The data byte specifies the high byte of the RAM address
                // e.g., writing 0x02 copies from $0200-$02FF
                ushort baseAddr = (ushort)(data << 8);
                
                for (int i = 0; i < 256; i++)
                {
                    ppu.OAM[i] = RAM[(baseAddr + i) & 0x07FF]; // Mask for RAM mirroring
                }
                
                // DMA takes CPU cycles - approximately 513 or 514 cycles
                // depending on whether it starts on an odd or even cycle
                // For now, we'll add 513 cycles (you can refine this later)
                cpu.Cycles += (byte)(513 + (nSystemClockCounter % 2));
            } 

            else if (address == 0x4016)
            {
                // Controller strobe
                // Writing 1 then 0 latches the controller state
                controller_strobe = (data & 0x01) != 0;
                
                if (controller_strobe)
                {
                    // While strobe is high, reload the shift registers
                    controller1_shift = controller1;
                    controller2_shift = controller2;
                }
            }
      
        }

        public static byte CPU_Read(ushort address, bool readOnly = false)
        {
            byte data = 0x00;
            if (Cartridge.CPU_Read(address, out data))
            {
                // Cartridge supplied the data. We are done.
            }
            if (address >= 0x0000 && address <= 0x1FFF)
            {
                // System RAM Address Range, mirrored every 2048
                data = RAM[address & 0x07FF];
            }
            else if (address >= 0x2000 && address <= 0x3FFF)
            {
                // PPU Address range, mirrored every 8
                data = ppu.CPU_Read((ushort)(address & 0x0007), readOnly);
            }
            else if (address == 0x4016)
            {
                // Controller 1 Read
                // Returns the current bit in the shift register (bit 0)
                // Then shifts right for the next read
                data = (byte)((controller1_shift & 0x01) | 0x40);
                
                if (!controller_strobe)
                {
                    controller1_shift >>= 1;
                }
            }
            else if (address == 0x4017)
            {
                // Controller 2 Read
                data = (byte)((controller2_shift & 0x01) | 0x40);
                
                if (!controller_strobe)
                {
                    controller2_shift >>= 1;
                }
            }

            return data;
        }

#endregion

        
#region System Interface
        // Resets the system
        public static void Reset()
        {
            cpu.Reset();
	        nSystemClockCounter = 0;   
        }
        // Clocks the system - a single whole system tick
        public static void Clock()
        {
            // Clocking. The heart and soul of an emulator. The running
            // frequency is controlled by whatever calls this function.
            // So here we "divide" the clock as necessary and call
            // the peripheral devices clock() function at the correct
            // times.

            // The fastest clock frequency the digital system cares
            // about is equivalent to the PPU clock. So the PPU is clocked
            // each time this function is called.
            ppu.Clock();

            // The CPU runs 3 times slower than the PPU so we only call its
            // clock() function every 3 times this function is called. We
            // have a global counter to keep track of this.
            if (nSystemClockCounter % 3 == 0)
            {
                cpu.Clock();
            }

            if (ppu.nmi)
            {
                ppu.nmi = false; // Acknowledge the signal
                cpu.NMI();       // Trigger the interrupt on the CPU
            }

            nSystemClockCounter++;
        }

        public static void SetControllerState(int controllerNum, byte state)
        {
            if (controllerNum == 1)
                controller1 = state;
            else if (controllerNum == 2)
                controller2 = state;
        }


#endregion
    }
}