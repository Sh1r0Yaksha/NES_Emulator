using System;
using System.Dynamic;
using CPU;

namespace tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 1. Setup CPU and Bus
            
            cpu6502 cpu = new cpu6502();

            // 2. Load ROM
            LoadNestest(cpu);

            // 3. Initialize State for Automated Test
            cpu.PC = 0xC000;
            cpu.Cycles = 7; 
            cpu.SetFlag(cpu6502.FLAGS6502.I, true); // Disable Interrupts
            cpu.STKP = 0xFD;                  // Stack Pointer defaults to FD

            Console.WriteLine("Running nestest...");
            
            // 4. Open File Stream
            using (StreamWriter sw = new StreamWriter("output.log"))
            {
                // Run for enough instructions to cover the basic tests (approx 8991 lines in standard log)
                // You can increase this limit later.
                int maxInstructions = 10000; 

                for (int i = 0; i < maxInstructions; i++)
                {
                    // A. Log State BEFORE execution
                    string logLine = LogState(cpu);
                    sw.WriteLine(logLine);

                    // B. Run One Instruction
                    cpu.Clock();
                    while (!cpu.Complete())
                    {
                        cpu.Clock();
                    }
                    
                    // Stop if PC loops to itself (common trap) or hits 0 (crash)
                    // Note: 0xC66E is a common "Test Finished" loop in nestest
                    if (cpu.PC == 0xC66E) 
                    {
                        Console.WriteLine("Test Finished (Reached loop at C66E).");
                        break; 
                    }
                }
            }
            
            Console.WriteLine("Done. Check output.log");
        }

        static void LoadNestest(cpu6502 cpu)
        {
            string file = "nestest.nes";
            if (!File.Exists(file))
            {
                Console.WriteLine("Error: nestest.nes not found!");
                return;
            }

            byte[] romData = File.ReadAllBytes(file);

            // Skip 16-byte header, load 16KB PRG
            int prgStart = 16;
            int prgSize = 16384;

            for (int i = 0; i < prgSize; i++)
            {
                byte b = romData[prgStart + i];
                // Mirror at 0xC000 and 0x8000
                cpu.Write((ushort)(0xC000 + i), b);
                cpu.Write((ushort)(0x8000 + i), b);
            }
            
            // Set Reset Vector (FFFC/FFFD) to C000 just in case your CPU resets
            cpu.Write(0xFFFC, 0x00);
            cpu.Write(0xFFFD, 0xC0);
        }

        static string LogState(cpu6502 cpu)
        {
            // The golden log format has a lot of extra info (Disassembly, PPU, Cycles).
            // We only have the CPU right now, so we match the REGISTERS column.
            
            // NESTEST LOG LINE EXAMPLE:
            // C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD PPU:  0, 21 CYC:7
            
            // OUR LOG LINE:
            // C000  A:00 X:00 Y:00 P:24 SP:FD

            // NOTE ON STATUS REGISTER (P):
            // The 'Unused' bit (bit 5) is physically always read as 1 on the NES.
            // We force it to 1 here for the log to match Nintendulator.
            byte p = (byte)(cpu.STATUS | 0x20);

            return string.Format("{0:X4}  A:{1:X2} X:{2:X2} Y:{3:X2} P:{4:X2} SP:{5:X2}",
                cpu.PC,
                cpu.A,
                cpu.X,
                cpu.Y,
                p, 
                cpu.STKP
            );
        }
    }
}