using System;
using System.Dynamic;
using CPU;

namespace tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // --- SETUP ---
            cpu6502 cpu = new cpu6502();

            // Load Nestest ROM (Ensure 64KB RAM as discussed before)
            LoadNestest(cpu);

            // Setup CPU Start State for Nestest
            cpu.PC = 0xC000;
            cpu.Cycles = 7;
            cpu.SetFlag(cpu6502.FLAGS6502.I, true);
            cpu.STKP = 0xFD;

            // --- STEP 1: CREATE COVERAGE DICTIONARY ---
            // Key = Instruction Name (e.g., "LDA"), Value = Has it passed?
            Dictionary<string, bool> coverage = new Dictionary<string, bool>();

            // Iterate through your CPU's Lookup table to populate keys
            // Assuming cpu.Lookup is accessible or you have a static list
            foreach (var instruction in cpu.Lookup) 
            {
                if (!string.IsNullOrEmpty(instruction.Name) && !coverage.ContainsKey(instruction.Name))
                {
                    coverage.Add(instruction.Name, false);
                }
            }

            // --- STEP 2: LOAD LOG LINES ---
            string[] logLines = File.ReadAllLines("nestest.log");
            Console.WriteLine($"Loaded {logLines.Length} lines of golden log.");

            // --- STEP 3: RUN AND COMPARE ---
            int lineNum = 0;
            try
            {
                foreach (string rawLine in logLines)
                {
                    lineNum++;

                    // 1. Skip empty strings immediately
                    if (string.IsNullOrWhiteSpace(rawLine)) continue;

                    // 2. Parse the line
                    LogEntry expected = LogEntry.FromLine(rawLine);

                    // 3. CRITICAL CHECK: Did parsing fail? (BOM, Header, or bad format)
                    if (expected == null)
                    {
                        // Just skip this line and move to the next one
                        continue; 
                    }

                    // Mark Instruction as Tested
                    byte opcode = Bus.Read(cpu.PC, true);
                    string instName = string.Empty;
                    
                    // Safety check for lookup table bounds
                    if (cpu.Lookup[opcode].Name != null)
                    {
                        instName = cpu.Lookup[opcode].Name;
                        if (coverage.ContainsKey(instName)) coverage[instName] = true;
                    }

                    // 4. NOW it is safe to compare
                    if (cpu.PC != expected.PC ||
                        cpu.A != expected.A ||
                        cpu.X != expected.X ||
                        cpu.Y != expected.Y ||
                        cpu.STKP != expected.STKP || 
                        (cpu.STATUS & 0xEF) != (expected.P & 0xEF))
                    {
                        // 1. Read the opcode at the current PC to see what instruction failed
                        

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n[FAILED] Mismatch at Line " + lineNum);
                        Console.WriteLine($"Instruction: {instName} (Opcode: {opcode:X2})");
                        Console.WriteLine($"Expected: PC:{expected.PC:X4} A:{expected.A:X2} X:{expected.X:X2} Y:{expected.Y:X2} P:{expected.P:X2} SP:{expected.STKP:X2}");
                        Console.WriteLine($"Actual:   PC:{cpu.PC:X4} A:{cpu.A:X2} X:{cpu.X:X2} Y:{cpu.Y:X2} P:{cpu.STATUS:X2} SP:{cpu.STKP:X2}");
                        Console.ResetColor();
                        return; 
                    }

                    // ... (Rest of your execution logic: Mark coverage, Clock loop) ...
                
                    // Execute
                    cpu.Clock();
                    while (!cpu.Complete()) cpu.Clock();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error at line {lineNum}: {ex.Message}");
                Console.WriteLine(ex.StackTrace); // Print stack trace to see exactly where
            }

            // --- STEP 4: PRINT COVERAGE REPORT ---
            Console.WriteLine("\n--- Instruction Coverage Report ---");
            int passed = coverage.Count(x => x.Value);
            int total = coverage.Count;
            
            Console.WriteLine($"Coverage: {passed}/{total} Instructions executed.");
            
            // Print Untested Instructions
            Console.WriteLine("Untested Instructions:");
            foreach (var item in coverage.Where(x => x.Value == false))
            {
                Console.Write(item.Key + " ");
            }
            Console.WriteLine();
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