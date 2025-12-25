using System;
using System.Dynamic;
using CPU;

namespace tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            cpu6502 nes = new cpu6502();

            TestSBC(nes);
            ResetSystem(nes);

            // ==========================================
            // EXECUTION LOOP
            // ==========================================
            
            while (true)
            {
                // 1. Step the CPU until one complete instruction finishes
                do
                {
                    nes.Clock();
                    Console.WriteLine($"Ran nes Clock {nes.ClockCount} times");
                } 
                while (!nes.Complete());

                // 2. Print the state AFTER the instruction finished
                Console.WriteLine(GetDebugState(nes));

                // 3. Pause for user input
                if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;
            }
        }
        public static void ResetSystem(cpu6502 nes)
        {
            // ==========================================
            // SYSTEM STARTUP
            // ==========================================

            // Set Reset Vector to 0x8000 so the CPU knows where to look
            nes.Write(0xFFFC, 0x00);
            nes.Write(0xFFFD, 0x80);

            // Perform Reset
            nes.Reset();

            // 1. FLUSH RESET CYCLES
            // The Reset takes 8 cycles. This loop runs them all so we start fresh.
            while (!nes.Complete())
            {
                nes.Clock();
                Console.WriteLine($"Ran nes Clock {nes.ClockCount} times");
            }
            
            Console.WriteLine("Reset Complete. System Ready at 0x8000.");
            Console.WriteLine("Press SPACE to step through instructions.");
            Console.WriteLine("---------------------------------------------------------------");
        }
        public static void TestADC(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST ADC
            // ==========================================
            
            // 0x8000: LDA #$00 (Load 0 into Accumulator)
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0x00);

            // 0x8002: CLC (Clear Carry Flag) - Opcode 0x18
            // If we don't do this, a random Carry bit could mess up our math.
            nes.Write(0x8002, 0x18);

            // 0x8003: ADC #$0A (Add 10 decimal) - Opcode 0x69
            nes.Write(0x8003, 0x69);
            nes.Write(0x8004, 0x0A);

            // 0x8005: ADC #$FF (Add 255 decimal) - Opcode 0x69
            // This tests the Overflow/Carry behavior.
            // 10 + 255 = 265. 
            // In 8-bit math: 265 wraps to 9. Carry flag should turn ON.
            nes.Write(0x8005, 0x69);
            nes.Write(0x8006, 0xFF);
        }

        public static void TestSBC(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST SBC
            // ==========================================

            // 0x8000: LDA #$0A (Load 10 decimal) - Opcode A9
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0x0A);

            // 0x8002: SEC (Set Carry Flag) - Opcode 38
            // REQUIRED before starting a standard subtraction chain.
            nes.Write(0x8002, 0x38);

            // 0x8003: SBC #$03 (Subtract 3) - Opcode E9
            // Math: 10 - 3 = 7.
            // Carry Flag should stay 1 (True) because result >= 0.
            nes.Write(0x8003, 0xE9);
            nes.Write(0x8004, 0x03);

            // 0x8005: SBC #$08 (Subtract 8) - Opcode E9
            // Math: 7 - 8 = -1 (wraps to 255 / 0xFF).
            // Carry Flag should become 0 (False) to indicate a Borrow occurred.
            nes.Write(0x8005, 0xE9);
            nes.Write(0x8006, 08);
        }

        // Add this inside your CPU class
        public static string GetFlagString(byte status)
        {
            // Checks each bit. If 1, prints the letter. If 0, prints a dot.
            return string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                (status & (byte)cpu6502.FLAGS6502.N) > 0 ? "N" : ".",
                (status & (byte)cpu6502.FLAGS6502.V) > 0 ? "V" : ".",
                "U", // The unused bit is usually ignored or just marked
                (status & (byte)cpu6502.FLAGS6502.B) > 0 ? "B" : ".",
                (status & (byte)cpu6502.FLAGS6502.D) > 0 ? "D" : ".",
                (status & (byte)cpu6502.FLAGS6502.I) > 0 ? "I" : ".",
                (status & (byte)cpu6502.FLAGS6502.Z) > 0 ? "Z" : ".",
                (status & (byte)cpu6502.FLAGS6502.C) > 0 ? "C" : "."
            );
        }

        public static string GetDebugState(cpu6502 cpu)
        {
            return string.Format(
                "PC:{0:X4}  A:{1:X2} X:{2:X2} Y:{3:X2}  SP:{4:X2}  P:{5:X2} [{6}]",
                cpu.PC,              // Program Counter (16-bit)
                cpu.A,               // Accumulator
                cpu.X,               // X Register
                cpu.Y,               // Y Register
                cpu.STKP,            // Stack Pointer
                cpu.STATUS,          // Status Hex Value
                GetFlagString(cpu.STATUS)  // The helper function we wrote earlier
            );
        }
    }
}
