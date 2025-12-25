namespace CPU
{
    public class cpu6502
    {
        public cpu6502()
        {
            // Row 0 (0x00 - 0x0F)
            Lookup[0x00] = new INSTRUCTION("BRK", BRK, IMM, 7);
            Lookup[0x01] = new INSTRUCTION("ORA", ORA, INX, 6);
            Lookup[0x05] = new INSTRUCTION("ORA", ORA, ZP0, 3);
            Lookup[0x06] = new INSTRUCTION("ASL", ASL, ZP0, 5);
            Lookup[0x08] = new INSTRUCTION("PHP", PHP, IMP, 3);
            Lookup[0x09] = new INSTRUCTION("ORA", ORA, IMM, 2);
            Lookup[0x0A] = new INSTRUCTION("ASL", ASL, IMP, 2);
            Lookup[0x0D] = new INSTRUCTION("ORA", ORA, ABS, 4);
            Lookup[0x0E] = new INSTRUCTION("ASL", ASL, ABS, 6);

            // Row 1 (0x10 - 0x1F)
            Lookup[0x10] = new INSTRUCTION("BPL", BPL, REL, 2);
            Lookup[0x11] = new INSTRUCTION("ORA", ORA, IZY, 5);
            Lookup[0x15] = new INSTRUCTION("ORA", ORA, ZPX, 4);
            Lookup[0x16] = new INSTRUCTION("ASL", ASL, ZPX, 6);
            Lookup[0x18] = new INSTRUCTION("CLC", CLC, IMP, 2);
            Lookup[0x19] = new INSTRUCTION("ORA", ORA, ABY, 4);
            Lookup[0x1D] = new INSTRUCTION("ORA", ORA, ABX, 4);
            Lookup[0x1E] = new INSTRUCTION("ASL", ASL, ABX, 7);

            // Row 2 (0x20 - 0x2F)
            Lookup[0x20] = new INSTRUCTION("JSR", JSR, ABS, 6);
            Lookup[0x21] = new INSTRUCTION("AND", AND, IZX, 6);
            Lookup[0x24] = new INSTRUCTION("BIT", BIT, ZP0, 3);
            Lookup[0x25] = new INSTRUCTION("AND", AND, ZP0, 3);
            Lookup[0x26] = new INSTRUCTION("ROL", ROL, ZP0, 5);
            Lookup[0x28] = new INSTRUCTION("PLP", PLP, IMP, 4);
            Lookup[0x29] = new INSTRUCTION("AND", AND, IMM, 2);
            Lookup[0x2A] = new INSTRUCTION("ROL", ROL, IMP, 2);
            Lookup[0x2C] = new INSTRUCTION("BIT", BIT, ABS, 4);
            Lookup[0x2D] = new INSTRUCTION("AND", AND, ABS, 4);
            Lookup[0x2E] = new INSTRUCTION("ROL", ROL, ABS, 6);

            // Row 3 (0x30 - 0x3F)
            Lookup[0x30] = new INSTRUCTION("BMI", BMI, REL, 2);
            Lookup[0x31] = new INSTRUCTION("AND", AND, IZY, 5);
            Lookup[0x35] = new INSTRUCTION("AND", AND, ZPX, 4);
            Lookup[0x36] = new INSTRUCTION("ROL", ROL, ZPX, 6);
            Lookup[0x38] = new INSTRUCTION("SEC", SEC, IMP, 2);
            Lookup[0x39] = new INSTRUCTION("AND", AND, ABY, 4);
            Lookup[0x3D] = new INSTRUCTION("AND", AND, ABX, 4);
            Lookup[0x3E] = new INSTRUCTION("ROL", ROL, ABX, 7);

            // Row 4 (0x40 - 0x4F)
            Lookup[0x40] = new INSTRUCTION("RTI", RTI, IMP, 6);
            Lookup[0x41] = new INSTRUCTION("EOR", EOR, IZX, 6);
            Lookup[0x45] = new INSTRUCTION("EOR", EOR, ZP0, 3);
            Lookup[0x46] = new INSTRUCTION("LSR", LSR, ZP0, 5);
            Lookup[0x48] = new INSTRUCTION("PHA", PHA, IMP, 3);
            Lookup[0x49] = new INSTRUCTION("EOR", EOR, IMM, 2);
            Lookup[0x4A] = new INSTRUCTION("LSR", LSR, IMP, 2);
            Lookup[0x4C] = new INSTRUCTION("JMP", JMP, ABS, 3);
            Lookup[0x4D] = new INSTRUCTION("EOR", EOR, ABS, 4);
            Lookup[0x4E] = new INSTRUCTION("LSR", LSR, ABS, 6);

            // Row 5 (0x50 - 0x5F)
            Lookup[0x50] = new INSTRUCTION("BVC", BVC, REL, 2);
            Lookup[0x51] = new INSTRUCTION("EOR", EOR, IZY, 5);
            Lookup[0x55] = new INSTRUCTION("EOR", EOR, ZPX, 4);
            Lookup[0x56] = new INSTRUCTION("LSR", LSR, ZPX, 6);
            Lookup[0x58] = new INSTRUCTION("CLI", CLI, IMP, 2);
            Lookup[0x59] = new INSTRUCTION("EOR", EOR, ABY, 4);
            Lookup[0x5D] = new INSTRUCTION("EOR", EOR, ABX, 4);
            Lookup[0x5E] = new INSTRUCTION("LSR", LSR, ABX, 7);

            // Row 6 (0x60 - 0x6F)
            Lookup[0x60] = new INSTRUCTION("RTS", RTS, IMP, 6);
            Lookup[0x61] = new INSTRUCTION("ADC", ADC, IZX, 6);
            Lookup[0x65] = new INSTRUCTION("ADC", ADC, ZP0, 3);
            Lookup[0x66] = new INSTRUCTION("ROR", ROR, ZP0, 5);
            Lookup[0x68] = new INSTRUCTION("PLA", PLA, IMP, 4);
            Lookup[0x69] = new INSTRUCTION("ADC", ADC, IMM, 2);
            Lookup[0x6A] = new INSTRUCTION("ROR", ROR, IMP, 2);
            Lookup[0x6C] = new INSTRUCTION("JMP", JMP, IND, 5);
            Lookup[0x6D] = new INSTRUCTION("ADC", ADC, ABS, 4);
            Lookup[0x6E] = new INSTRUCTION("ROR", ROR, ABS, 6);

            // Row 7 (0x70 - 0x7F)
            Lookup[0x70] = new INSTRUCTION("BVS", BVS, REL, 2);
            Lookup[0x71] = new INSTRUCTION("ADC", ADC, IZY, 5);
            Lookup[0x75] = new INSTRUCTION("ADC", ADC, ZPX, 4);
            Lookup[0x76] = new INSTRUCTION("ROR", ROR, ZPX, 6);
            Lookup[0x78] = new INSTRUCTION("SEI", SEI, IMP, 2);
            Lookup[0x79] = new INSTRUCTION("ADC", ADC, ABY, 4);
            Lookup[0x7D] = new INSTRUCTION("ADC", ADC, ABX, 4);
            Lookup[0x7E] = new INSTRUCTION("ROR", ROR, ABX, 7);

            // Row 8 (0x80 - 0x8F)
            Lookup[0x81] = new INSTRUCTION("STA", STA, IZX, 6);
            Lookup[0x84] = new INSTRUCTION("STY", STY, ZP0, 3);
            Lookup[0x85] = new INSTRUCTION("STA", STA, ZP0, 3);
            Lookup[0x86] = new INSTRUCTION("STX", STX, ZP0, 3);
            Lookup[0x88] = new INSTRUCTION("DEY", DEY, IMP, 2);
            Lookup[0x8A] = new INSTRUCTION("TXA", TXA, IMP, 2);
            Lookup[0x8C] = new INSTRUCTION("STY", STY, ABS, 4);
            Lookup[0x8D] = new INSTRUCTION("STA", STA, ABS, 4);
            Lookup[0x8E] = new INSTRUCTION("STX", STX, ABS, 4);

            // Row 9 (0x90 - 0x9F)
            Lookup[0x90] = new INSTRUCTION("BCC", BCC, REL, 2);
            Lookup[0x91] = new INSTRUCTION("STA", STA, IZY, 6);
            Lookup[0x94] = new INSTRUCTION("STY", STY, ZPX, 4);
            Lookup[0x95] = new INSTRUCTION("STA", STA, ZPX, 4);
            Lookup[0x96] = new INSTRUCTION("STX", STX, ZPY, 4);
            Lookup[0x98] = new INSTRUCTION("TYA", TYA, IMP, 2);
            Lookup[0x99] = new INSTRUCTION("STA", STA, ABY, 5);
            Lookup[0x9A] = new INSTRUCTION("TXS", TXS, IMP, 2);
            Lookup[0x9D] = new INSTRUCTION("STA", STA, ABX, 5);

            // Row A (0xA0 - 0xAF)
            Lookup[0xA0] = new INSTRUCTION("LDY", LDY, IMM, 2);
            Lookup[0xA1] = new INSTRUCTION("LDA", LDA, IZX, 6);
            Lookup[0xA2] = new INSTRUCTION("LDX", LDX, IMM, 2);
            Lookup[0xA4] = new INSTRUCTION("LDY", LDY, ZP0, 3);
            Lookup[0xA5] = new INSTRUCTION("LDA", LDA, ZP0, 3);
            Lookup[0xA6] = new INSTRUCTION("LDX", LDX, ZP0, 3);
            Lookup[0xA8] = new INSTRUCTION("TAY", TAY, IMP, 2);
            Lookup[0xA9] = new INSTRUCTION("LDA", LDA, IMM, 2);
            Lookup[0xAA] = new INSTRUCTION("TAX", TAX, IMP, 2);
            Lookup[0xAC] = new INSTRUCTION("LDY", LDY, ABS, 4);
            Lookup[0xAD] = new INSTRUCTION("LDA", LDA, ABS, 4);
            Lookup[0xAE] = new INSTRUCTION("LDX", LDX, ABS, 4);

            // Row B (0xB0 - 0xBF)
            Lookup[0xB0] = new INSTRUCTION("BCS", BCS, REL, 2);
            Lookup[0xB1] = new INSTRUCTION("LDA", LDA, IZY, 5);
            Lookup[0xB4] = new INSTRUCTION("LDY", LDY, ZPX, 4);
            Lookup[0xB5] = new INSTRUCTION("LDA", LDA, ZPX, 4);
            Lookup[0xB6] = new INSTRUCTION("LDX", LDX, ZPY, 4);
            Lookup[0xB8] = new INSTRUCTION("CLV", CLV, IMP, 2);
            Lookup[0xB9] = new INSTRUCTION("LDA", LDA, ABY, 4);
            Lookup[0xBA] = new INSTRUCTION("TSX", TSX, IMP, 2);
            Lookup[0xBC] = new INSTRUCTION("LDY", LDY, ABX, 4);
            Lookup[0xBD] = new INSTRUCTION("LDA", LDA, ABX, 4);
            Lookup[0xBE] = new INSTRUCTION("LDX", LDX, ABY, 4);

            // Row C (0xC0 - 0xCF)
            Lookup[0xC0] = new INSTRUCTION("CPY", CPY, IMM, 2);
            Lookup[0xC1] = new INSTRUCTION("CMP", CMP, IZX, 6);
            Lookup[0xC4] = new INSTRUCTION("CPY", CPY, ZP0, 3);
            Lookup[0xC5] = new INSTRUCTION("CMP", CMP, ZP0, 3);
            Lookup[0xC6] = new INSTRUCTION("DEC", DEC, ZP0, 5);
            Lookup[0xC8] = new INSTRUCTION("INY", INY, IMP, 2);
            Lookup[0xC9] = new INSTRUCTION("CMP", CMP, IMM, 2);
            Lookup[0xCA] = new INSTRUCTION("DEX", DEX, IMP, 2);
            Lookup[0xCC] = new INSTRUCTION("CPY", CPY, ABS, 4);
            Lookup[0xCD] = new INSTRUCTION("CMP", CMP, ABS, 4);
            Lookup[0xCE] = new INSTRUCTION("DEC", DEC, ABS, 6);

            // Row D (0xD0 - 0xDF)
            Lookup[0xD0] = new INSTRUCTION("BNE", BNE, REL, 2);
            Lookup[0xD1] = new INSTRUCTION("CMP", CMP, IZY, 5);
            Lookup[0xD5] = new INSTRUCTION("CMP", CMP, ZPX, 4);
            Lookup[0xD6] = new INSTRUCTION("DEC", DEC, ZPX, 6);
            Lookup[0xD8] = new INSTRUCTION("CLD", CLD, IMP, 2);
            Lookup[0xD9] = new INSTRUCTION("CMP", CMP, ABY, 4);
            Lookup[0xDA] = new INSTRUCTION("NOP", NOP, IMP, 2);
            Lookup[0xDD] = new INSTRUCTION("CMP", CMP, ABX, 4);
            Lookup[0xDE] = new INSTRUCTION("DEC", DEC, ABX, 7);

            // Row E (0xE0 - 0xEF)
            Lookup[0xE0] = new INSTRUCTION("CPX", CPX, IMM, 2);
            Lookup[0xE1] = new INSTRUCTION("SBC", SBC, IZX, 6);
            Lookup[0xE4] = new INSTRUCTION("CPX", CPX, ZP0, 3);
            Lookup[0xE5] = new INSTRUCTION("SBC", SBC, ZP0, 3);
            Lookup[0xE6] = new INSTRUCTION("INC", INC, ZP0, 5);
            Lookup[0xE8] = new INSTRUCTION("INX", INX, IMP, 2);
            Lookup[0xE9] = new INSTRUCTION("SBC", SBC, IMM, 2);
            Lookup[0xEA] = new INSTRUCTION("NOP", NOP, IMP, 2);
            Lookup[0xEC] = new INSTRUCTION("CPX", CPX, ABS, 4);
            Lookup[0xED] = new INSTRUCTION("SBC", SBC, ABS, 4);
            Lookup[0xEE] = new INSTRUCTION("INC", INC, ABS, 6);

            // Row F (0xF0 - 0xFF)
            Lookup[0xF0] = new INSTRUCTION("BEQ", BEQ, REL, 2);
            Lookup[0xF1] = new INSTRUCTION("SBC", SBC, IZY, 5);
            Lookup[0xF5] = new INSTRUCTION("SBC", SBC, ZPX, 4);
            Lookup[0xF6] = new INSTRUCTION("INC", INC, ZPX, 6);
            Lookup[0xF8] = new INSTRUCTION("SED", SED, IMP, 2);
            Lookup[0xF9] = new INSTRUCTION("SBC", SBC, ABY, 4);
            Lookup[0xFA] = new INSTRUCTION("NOP", NOP, IMP, 2);
            Lookup[0xFD] = new INSTRUCTION("SBC", SBC, ABX, 4);
            Lookup[0xFE] = new INSTRUCTION("INC", INC, ABX, 7);
        }


#region Registers
        public byte A = 0x00; // Accumulator
        public byte X = 0x00; // X Register
        public byte Y = 0x00; // Y Register
        public byte STKP = 0x00; // Stack Pointer
        public ushort PC = 0x0000; // Program Counter
        public byte STATUS = 0x00; // Status Register
#endregion

#region External Functions
        // Reset Interrupt - Forces CPU into known state
        public void Reset()
        {
            // Get address to set program counter to
	        AddrAbs = 0xFFFC;
            ushort lo = Read((ushort)(AddrAbs + 0));
	        ushort hi = Read((ushort)(AddrAbs + 1));

            // Left Shift 'hi' by 8, OR it with 'lo' to the complete address and then set PC
	        PC = (ushort)((hi << 8) | lo);

            // Reset internal registers
            A = 0;
            X = 0;
            Y = 0;
            STKP = 0xFD;
            STATUS = 0x00 | (byte)FLAGS6502.U;

            // Clear internal helper variables
            AddrRel = 0x0000;
	        AddrAbs = 0x0000;
	        Fetched = 0x00;

            // Reset takes time
	        Cycles = 8;
        } 

        // Interrupt Request - Executes an instruction at a specific location
        // Only happens if the "disable interrupt" flag is 0
        // Current instruction is allowed to finish. A programmable address
        // is read form hard coded location 0xFFFE, which is subsequently
        // set to the program counter.

        public void IRQ()
        {
            // If interrupts are allowed
            if (GetFlag(FLAGS6502.I) == 0)
            {
                // Push the program counter to the stack. It's 16-bits don't
        		// forget so that takes two pushes
                
                Write((ushort)(STKP_Start + STKP), (byte)((PC >> 8) & 0x00FF));
                STKP--;
                Write((ushort)(STKP_Start + STKP), (byte)(PC & 0x00FF));
                STKP--;

                // Then Push the status register to the stack
                SetFlag(FLAGS6502.B, false);
                SetFlag(FLAGS6502.U, true);
                SetFlag(FLAGS6502.I, true);
                Write((ushort)(STKP_Start + STKP), STATUS);

                STKP--;

                // Read new program counter location from fixed address
                AddrAbs = 0xFFFE;
                ushort lo = Read((ushort)(AddrAbs + 0));
                ushort hi = Read((ushort)(AddrAbs + 1));
                PC = (ushort)((hi << 8) | lo);

                Cycles = 7;
            }
            
        }

        // A Non-Maskable Interrupt cannot be ignored. It behaves in exactly the
        // same way as a regular IRQ, but reads the new program counter address
        // from location 0xFFFA.
        public void NMI()
        {
            Write((ushort)(STKP_Start + STKP), (byte)((PC >> 8) & 0x00FF));
            STKP--;
            Write((ushort)(STKP_Start + STKP), (byte)(PC & 0x00FF));
            STKP--;

            SetFlag(FLAGS6502.B, false);
            SetFlag(FLAGS6502.U, true);
            SetFlag(FLAGS6502.I, true);
            Write((ushort)(STKP_Start + STKP), STATUS);
            STKP--;

            AddrAbs = 0xFFFA;
            ushort lo = Read((ushort)(AddrAbs + 0));
            ushort hi = Read((ushort)(AddrAbs + 1));
            PC = (ushort)((hi << 8) | lo);

            Cycles = 8;

        }

        // Perform one clock cycle's worth of update
        public void clock()
        {
            if (Cycles == 0)
            {
                // No Cycles of current instruction left, move to next instruction
                Opcode = Read(PC);

                // Always set the unused status flag bit to 1
		        SetFlag(FLAGS6502.U, true);

                PC++; // Move PC to next instruction

                // Get Starting number of cycles
        		Cycles = Lookup[Opcode].Cycles;

                // Perform fetch of intermediate data using the
                // required addressing mode
        		byte additional_cycle1 = Lookup[Opcode].AddressMode();
                
                // Perform operation
		        byte additional_cycle2 = Lookup[Opcode].Operate();

                // The address mode and opcode may have altered the number
                // of cycles this instruction requires before its completed
        		Cycles += (byte)(additional_cycle1 & additional_cycle2);

                // Always set the unused status flag bit to 1
		        SetFlag(FLAGS6502.U, true);
            }

            // Increment global clock count - This is actually unused unless logging is enabled
            // but I've kept it in because its a handy watch variable for debugging
            ClockCount++;

            // Decrement the number of cycles remaining for this instruction
            Cycles--;
        }

        // Indicates the current instruction has completed by returning true. This is
        // a utility function to enable "step-by-step" execution, without manually 
        // clocking every cycle
        public bool Complete()
        {
            return false;           
        }
#endregion

#region Flags
        // The status register stores 8 flags. Ive enumerated these here for ease
        // of access. You can access the status register directly since its public.
        // The bits have different interpretations depending upon the context and 
        // instruction being executed.
        [Flags]
        enum FLAGS6502 : byte
        {
            C = (1 << 0),	// Carry Bit
            Z = (1 << 1),	// Zero
            I = (1 << 2),	// Disable Interrupts
            D = (1 << 3),	// Decimal Mode (unused in this implementation)
            B = (1 << 4),	// Break
            U = (1 << 5),	// Unused
            V = (1 << 6),	// Overflow
            N = (1 << 7),	// Negative
        };

#endregion

#region Internal CPU Instructions
        private byte GetFlag(FLAGS6502 f)
        {
            return (STATUS & (byte)f) > 0 ? (byte)1 : (byte)0;
        }

        private void SetFlag(FLAGS6502 f, bool v)
        {
            if (v)
		        STATUS |= (byte)f;
            else
                STATUS &= (byte)~f;
        }

        // Helper function to get the complete address from 
        // the two lo and hi fragments of PC
        private ushort GetAddressFromPC(out bool pageChange, out ushort lo, out ushort hi)
        {
            pageChange = false;
            lo = Read(PC);
            PC++;
            hi = Read(PC);
            PC++;

            ushort addr = (ushort)((hi << 8) | lo);

            if ((addr & 0xFF00) != (hi << 8))
                pageChange = true;

            return addr;
        }

        // Assistive variables to facilitate emulation
        private byte Fetched = 0x00;   // Represents the working input value to the ALU
        private ushort Temp = 0x0000; // A convenience variable used everywhere
        private ushort AddrAbs = 0x0000; // All used memory addresses end up in here
        private ushort AddrRel = 0x00;   // Represents absolute address following a branch
        private byte Opcode = 0x00;   // Is the instruction byte
        private byte Cycles = 0;	   // Counts how many cycles the instruction has remaining
        private uint ClockCount = 0;	   // A global accumulation of the number of clocks

        private ushort STKP_Start = 0x0100;
        private ushort STKP_End = 0x01FF;

        // Linkage to the communications bus
        private byte Read(ushort address)
        {
            return Bus.Read(address);
        }

        private void Write(ushort address, byte data)
        {
            Bus.Write(address, data);
        }

        byte Fetch()
        {
            if (!(Lookup[Opcode].AddressMode == IMP))
                Fetched = Read(AddrAbs);
            return Fetched;
        }

        public struct INSTRUCTION
        {
            public string Name;
            public Func<byte> Operate;
            public Func<byte> AddressMode;
            public byte Cycles;

            public INSTRUCTION(string name, Func<byte> operate, Func<byte> addr_mode, byte cycles)
            {
                Name = name;
                Operate = operate;
                AddressMode = addr_mode;
                Cycles = cycles;
            }
        }

        public INSTRUCTION[] Lookup = new INSTRUCTION[256];
#endregion

#region Addressing Modes
        // Address Mode: Implied
        // There is no additional data required for this instruction. The instruction
        // does something very simple like like sets a status bit. However, we will
        // target the accumulator, for instructions like PHA
        byte IMP()
        {
            Fetched = A;
            return 0x00;
        }

        // Address Mode: Immediate
        // The instruction expects the next byte to be used as a value, so we'll prep
        // the read address to point to the next byte
        byte IMM()
        {
            AddrAbs = PC++;
            return 0x00;
        }	

        // Address Mode: Zero Page
        // To save program bytes, zero page addressing allows you to absolutely address
        // a location in first 0xFF bytes of address range. Clearly this only requires
        // one byte instead of the usual two.
        byte ZP0()
        {
            AddrAbs = Read(PC);	
            PC++;
            AddrAbs &= 0x00FF;
            return 0x00;
        }	

        // Address Mode: Zero Page with X Offset
        // Fundamentally the same as Zero Page addressing, but the contents of the X Register
        // is added to the supplied single byte address. This is useful for iterating through
        // ranges within the first page.
        byte ZPX()
        {
            AddrAbs = (ushort)(Read(PC) + X);
            PC++;
            AddrAbs &= 0x00FF;
            return 0x00;
        }

        // Address Mode: Zero Page with Y Offset
        // Same as above but uses Y Register for offset
        byte ZPY()
        {
            AddrAbs = (ushort)(Read(PC) + Y);
            PC++;
            AddrAbs &= 0x00FF;
            return 0x00;
        }

        // Address Mode: Relative
        // This address mode is exclusive to branch instructions. The address
        // must reside within -128 to +127 of the branch instruction, i.e.
        // you cant directly branch to any address in the addressable range.	
        byte REL()
        {
            AddrRel = Read(PC);
            PC++;
            if ((AddrRel & 0x80) == 1) // 0x80 = 128
                AddrRel |= 0xFF00;
            return 0x00;
        }

        // Address Mode: Absolute 
        // A full 16-bit address is loaded and used
        byte ABS()
        {
            AddrAbs = GetAddressFromPC(out _, out _, out _);
            return 0x00;
        }

        // Address Mode: Absolute with X Offset
        // Fundamentally the same as absolute addressing, but the contents of the X Register
        // is added to the supplied two byte address. If the resulting address changes
        // the page, an additional clock cycle is required	
        byte ABX()
        {
            bool pageChange;
            AddrAbs = GetAddressFromPC(out pageChange, out _, out _);
            AddrAbs += X;

            if (pageChange)
                return 0x01;
            else
                return 0x00;	
        }
        
        // Address Mode: Absolute with Y Offset
        // Fundamentally the same as absolute addressing, but the contents of the Y Register
        // is added to the supplied two byte address. If the resulting address changes
        // the page, an additional clock cycle is required
        byte ABY()
        {
            bool pageChange;
            AddrAbs = GetAddressFromPC(out pageChange, out _, out _);
            AddrAbs += Y;

            if (pageChange)
                return 0x01;
            else
                return 0x00;
        }

        // Note: The next 3 address modes use indirection (aka Pointers!)

        // Address Mode: Indirect
        // The supplied 16-bit address is read to get the actual 16-bit address. This is
        // instruction is unusual in that it has a bug in the hardware! To emulate its
        // function accurately, we also need to emulate this bug. If the low byte of the
        // supplied address is 0xFF, then to read the high byte of the actual address
        // we need to cross a page boundary. This doesn't actually work on the chip as 
        // designed, instead it wraps back around in the same page, yielding an 
        // invalid actual address
        byte IND()
        {
            ushort lo;
            ushort hi;

            AddrAbs = GetAddressFromPC(out _, out lo, out hi);

            ushort ptr = (ushort)((hi << 8) | hi);

            if (lo == 0x00FF) // Simulate page boundary hardware bug
            {
                AddrAbs = (ushort)((Read((ushort)(ptr & 0xFF00)) << 8) | Read((ushort)(ptr + 0)));
            }
            else // Behave normally
            {
                AddrAbs = (ushort)((Read((ushort)(ptr + 1)) << 8) | Read((ushort)(ptr + 0)));
            }
            return 0x00;
        }	

        // Address Mode: Indirect X
        // The supplied 8-bit address is offset by X Register to index
        // a location in page 0x00. The actual 16-bit address is read 
        // from this location
        byte IZX()
        {
            ushort t = Read(PC);
            PC++;

            ushort lo = Read((ushort)(t + X & 0x00FF));
            ushort hi = Read((ushort)((ushort)(t + X + 1) & 0x00FF));

            AddrAbs = (ushort)((hi << 8) | lo);
            return 0x00;
        }	

        // Address Mode: Indirect Y
        // The supplied 8-bit address indexes a location in page 0x00. From 
        // here the actual 16-bit address is read, and the contents of
        // Y Register is added to it to offset it. If the offset causes a
        // change in page then an additional clock cycle is required.
        byte IZY()
        {
            ushort t = Read(PC);
            PC++;

            ushort lo = Read((ushort)(t & 0x00FF));
            ushort hi = Read((ushort)((t + 1) & 0x00FF));

            AddrAbs = (ushort)((hi << 8) | lo);
            AddrAbs += Y;
            
            if ((AddrAbs & 0xFF00) != (hi << 8))
                return 0x01;
            else
                return 0x00;
        }
#endregion

#region Opcodes 
        // Only Official Opcodes


        byte ADC()
        {
            // Grab the data that we are adding to the accumulator
        	Fetch();

            // Add is performed in 16-bit domain for emulation to capture any
            // carry bit, which will exist in bit 8 of the 16-bit word
            Temp = (ushort)(A + Fetched + GetFlag(FLAGS6502.C));
            
            // The carry flag out exists in the high byte bit 0
	        SetFlag(FLAGS6502.C, Temp > 255);

            // The Zero flag is set if the result is 0
	        SetFlag(FLAGS6502.Z, (Temp & 0x00FF) == 0);
            
            // The signed Overflow flag using complements
	        SetFlag(FLAGS6502.V, (~(A ^ Fetched) & (A ^ Temp) & 0x0080) == 1);

            // The negative flag is set to the most significant bit of the result
	        SetFlag(FLAGS6502.N, (Temp & 0x80) == 1);

            // Load the result into the accumulator (it's 8-bit dont forget!)
	        A = (byte)(Temp & 0x00FF);

            // This instruction has the potential to require an additional clock cycle
	        return 0x01;
	    }	

        // Instruction: Bitwise Logic AND
        // Function:    A = A & M
        // Flags Out:   N, Z
        byte AND()
        {
            Fetch();
            A = (byte)(A & Fetched);
            SetFlag(FLAGS6502.Z, A == 0x00);
            SetFlag(FLAGS6502.N, (A & 0x80) == 1);
            return 0x01;
        }
        byte ASL()
        {
            return 0x00;
        }	
        byte BCC()
        {
            return 0x00;
        }
        byte BCS()
        {
            return 0x00;
        }
        byte BEQ()
        {
            return 0x00;
        }	
        byte BIT()
        {
            return 0x00;
        }	
        byte BMI()
        {
            return 0x00;
        }
        byte BNE()
        {
            return 0x00;
        }
	    byte BPL()
        {
            return 0x00;
        }
	    byte BRK()
        {
            return 0x00;
        }
	    byte BVC()
        {
            return 0x00;
        }

        byte BVS()
        {
            return 0x00;
        }
	    byte CLC()
        {
            return 0x00;
        }
	    byte CLD()
        {
            return 0x00;
        }
	    byte CLI()
        {
            return 0x00;
        }

        byte CLV()
        {
            return 0x00;
        }
	    byte CMP()
        {
            return 0x00;
        }
	    byte CPX()
        {
            return 0x00;
        }
	    byte CPY()
        {
            return 0x00;
        }

        byte DEC()
        {
            return 0x00;
        }
	    byte DEX()
        {
            return 0x00;
        }
	    byte DEY()
        {
            return 0x00;
        }
	    byte EOR()
        {
            return 0x00;
        }

        byte INC()
        {
            return 0x00;
        }
	    byte INX()
        {
            return 0x00;
        }
	    byte INY()
        {
            return 0x00;
        }
	    byte JMP()
        {
            return 0x00;
        }

        byte JSR()
        {
            return 0x00;
        }
	    byte LDA()
        {
            return 0x00;
        }
	    byte LDX()
        {
            return 0x00;
        }
	    byte LDY()
        {
            return 0x00;
        }

        byte LSR()
        {
            return 0x00;
        }
	    byte NOP()
        {
            return 0x00;
        }
	    byte ORA()
        {
            return 0x00;
        }
	    byte PHA()
        {
            return 0x00;
        }

        byte PHP()
        {
            return 0x00;
        }
	    byte PLA()
        {
            return 0x00;
        }
	    byte PLP()
        {
            return 0x00;
        }
	    byte ROL()
        {
            return 0x00;
        }

        byte ROR()
        {
            return 0x00;
        }
	    byte RTI()
        {
            return 0x00;
        }
	    byte RTS()
        {
            return 0x00;
        }
	    byte SBC()
        {
            Fetch();

            // Operating in 16-bit domain to capture carry out
	
            // We can invert the bottom 8 bits with bitwise xor
            ushort value = (ushort)(Fetched ^ 0x00FF);

            // Notice this is exactly the same as addition from here!
	        Temp = (ushort)(A + value + GetFlag(FLAGS6502.C));

            SetFlag(FLAGS6502.C, (Temp & 0xFF00) == 1);
            SetFlag(FLAGS6502.Z, (Temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.V, ((Temp ^ A) & (Temp ^ value) & 0x0080) == 1);
            SetFlag(FLAGS6502.N, (Temp & 0x0080) == 1);
            A = (byte)(Temp & 0x00FF);

            return 0x01;
        }

        byte SEC()
        {
            return 0x00;
        }
	    byte SED()
        {
            return 0x00;
        }
	    byte SEI()
        {
            return 0x00;
        }
	    byte STA()
        {
            return 0x00;
        }

        byte STX()
        {
            return 0x00;
        }
	    byte STY()
        {
            return 0x00;
        }
	    byte TAX()
        {
            return 0x00;
        }
	    byte TAY()
        {
            return 0x00;
        }

        byte TSX()
        {
            return 0x00;
        }
	    byte TXA()
        {
            return 0x00;
        }
	    byte TXS()
        {
            return 0x00;
        }
	    byte TYA()
        {
            return 0x00;
        }	
#endregion

    }
}