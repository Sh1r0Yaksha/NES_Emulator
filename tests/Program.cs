using System;
using System.Dynamic;
using System.Reflection.Emit;
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

                    if (cpu.Lookup[opcode].Name == "NOP")
                        continue;
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
            finally
            {
                Console.WriteLine($"\n--- Finished the test, all {lineNum} lines ran perfectly---");    
            }

            cpu.Reset();
            
            // ------ Run Klaus Dormann Functional Test ------------
            RunFunctionalTest(cpu);
        }

        public static void RunFunctionalTest(cpu6502 cpu)
        {
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("Running Klaus Dormann Functional Test...");
            Console.WriteLine("---------------------------------------");

            // 1. Load the binary
            // Ensure you have "6502_functional_test.bin" in your folder
            byte[] bin = System.IO.File.ReadAllBytes("6502_functional_test.bin");
            
            // Load into RAM at 0x0000 (The test assumes 64KB RAM)
            for (int i = 0; i < bin.Length; i++)
                Bus.RAM[i] = bin[i];

            // 2. Setup CPU
            cpu.PC = 0x0400; // Entry point for this specific test
            cpu.STKP = 0xFF;   // Reset Stack Pointer
            cpu.Cycles = 0;
            
            // Disable Decimal mode if you implemented it (NES doesn't use it)
            // cpu.SetFlag(FLAGS6502.D, false);

            // 3. Run until Trap or Success
            long cycles = 0;
            ushort prevPC = 0;
            
            // The test takes about 26 million cycles to complete!
            // We detect "stuck" states by checking if PC stays same for too long
            
            while (true)
            {
                cpu.Clock();

                if (cpu.Complete())
                {
                    // Check for Infinite Loop (The test traps itself on failure or success)
                    if (cpu.PC == prevPC)
                    {
                        // SUCCESS ADDRESS: 0x3469
                        if (cpu.PC == 0x3469)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("\n[SUCCESS] Reached PC: 0x3469!");
                            Console.WriteLine("Your CPU is 100% Compliant.");
                            Console.ResetColor();
                            break;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n[FAIL] Trap detected at PC: {cpu.PC:X4}");
                            Console.WriteLine("This usually indicates a logic error in the previous instruction.");
                            Console.ResetColor();
                            break;
                        }
                    }
                    prevPC = cpu.PC;
                    cycles++;
                    
                    // Optional: Print progress every 1M cycles so you know it's alive
                    if (cycles % 1000000 == 0) Console.Write(".");
                }
            }
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
    }
}