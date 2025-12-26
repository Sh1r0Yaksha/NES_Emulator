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

            // ---------------------------------------------------------
            // 1. THE TEST REGISTRY
            // Map the string name of the instruction to your test function
            // ---------------------------------------------------------
            var testRegistry = new Dictionary<string, Action<cpu6502>>()
            {
                { "ADC", TestADC },
                { "SBC", TestSBC },
                { "ASL", TestASL },
                { "BCC", TestBCC },
                { "BCS", TestBCS },
                { "BEQ", TestBEQ },
                { "BNE", TestBNE },
                { "BMI", TestBMI },
                { "BPL", TestBPL },
                { "BVC", TestBVC },
                { "BVS", TestBVS },
                { "BIT", TestBIT },
                { "BRK", TestBRK },
                // Add more here as you write them...
            };

            // ---------------------------------------------------------
            // 2. INPUT: SELECT TESTS TO RUN
            // You can modify this list to run specific tests or all of them.
            // ---------------------------------------------------------
            string[] testsToRun = { "ASL", "BCC", "BCS",
                                    "BEQ", "BNE", "BMI",
                                    "BPL", "BVC", "BVS",
                                    "BIT", "BRK" }; 
            
            // Or uncomment this line to run EVERYTHING in the registry:
            // string[] testsToRun = testRegistry.Keys.ToArray();

            Console.WriteLine($"Queueing {testsToRun.Length} tests...");
            Console.WriteLine("--------------------------------------------------");
            
            // ---------------------------------------------------------
            // 3. AUTOMATED TEST RUNNER
            // ---------------------------------------------------------
            List<string> failedTests = new List<string>();

            foreach (string testName in testsToRun)
            {
                if (!testRegistry.ContainsKey(testName))
                {
                    Console.WriteLine($"[WARNING] Test '{testName}' not found in registry.");
                    continue;
                }

                Console.Write($"Running Test: {testName.PadRight(10)} ... ");

                // A. CLEANUP & SETUP
                // We wipe the RAM before every test to ensure no ghosts remain.
                Array.Clear(Bus.RAM, 0, Bus.RAM.Length); 
                
                // Load the specific test program into RAM
                testRegistry[testName].Invoke(nes);
                
                // Set Reset Vector and Reset CPU
                ResetSystem(nes);

                // B. EXECUTION LOOP
                // We run for a limited number of instructions (e.g., 50) to prevent infinite loops.
                // Most of these tests finish in < 10 steps.
                int maxInstructions = 50;
                int instructionsRun = 0;

                while (instructionsRun < maxInstructions)
                {
                    // Run one instruction
                    do
                    {
                         nes.Clock();
                    } while (!nes.Complete());
                    
                    instructionsRun++;
                    
                    Console.WriteLine($"STEP {instructionsRun}: {GetDebugState(nes)}");

                    // CHECK EXIT CONDITION:
                    // All our tests are designed to end by loading a value into A.
                    // If we detect the "Success" (0x01) or "Failure" (0xFF) value, stop early.
                    // Note: We check if Opcode was LDA Immediate (0xA9) to be sure we just finished loading.
                    if (Bus.RAM[nes.PC - 2] == 0xA9) 
                    {
                        if (nes.A == 0x01 || nes.A == 0x00) break;
                    }
                }

                // C. VERIFICATION
                // Success Criteria: Accumulator (A) must == 0x01.
                if (nes.A == 0x01)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("PASS");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"FAIL (A={nes.A:X2})");
                    Console.WriteLine($"   Final State: {GetDebugState(nes)}");
                    failedTests.Add(testName);
                }
                Console.ResetColor();
            }

            // ---------------------------------------------------------
            // 4. FINAL REPORT
            // ---------------------------------------------------------
            Console.WriteLine("--------------------------------------------------");
            if (failedTests.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ALL TESTS PASSED! EXCELLENT WORK.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"SUMMARY: {failedTests.Count} TESTS FAILED.");
                foreach (var failed in failedTests) Console.WriteLine($" - {failed}");
            }
            Console.ResetColor();
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
            }
            
            Console.WriteLine("Reset Complete. System Ready at 0x8000.");
            Console.WriteLine("---------------------------------------------------------------");
        }
        public static void TestADC(cpu6502 nes)
        {
            // Program layout:
            // 8000: CLC          ; Ensure C = 0
            // 8001: LDA #$00     ; A = 0
            // 8003: ADC #$0A     ; A = 10
            // 8005: CMP #$0A     ; A must be 10, Z=1 if OK
            // 8007: BNE FAIL     ; If not 10, fail
            //
            // 8009: CLC          ; C = 0 again
            // 800A: ADC #$FF     ; 10 + 255 = 265 -> A = 9, C = 1
            // 800C: CMP #$09     ; A must be 9
            // 800E: BNE FAIL
            // 8010: BCC FAIL     ; Expect C = 1, so BCC should NOT branch
            //
            // 8012: LDA #$01     ; SUCCESS
            // 8014: BRK
            //
            // 8015: FAIL: LDA #$00
            // 8017: BRK

            ushort pc = 0x8000;

            // 0x8000: CLC
            nes.Write(pc++, 0x18);

            // 0x8001: LDA #$00
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x8003: ADC #$0A
            nes.Write(pc++, 0x69);
            nes.Write(pc++, 0x0A);

            // 0x8005: CMP #$0A
            nes.Write(pc++, 0xC9);
            nes.Write(pc++, 0x0A);

            // 0x8007: BNE FAIL
            nes.Write(pc++, 0xD0);
            nes.Write(pc++, 0x0B);   // Jump forward to 0x8014 (FAIL label at 0x8015, but branch target = FAIL LDA)

            // 0x8009: CLC
            nes.Write(pc++, 0x18);

            // 0x800A: ADC #$FF
            nes.Write(pc++, 0x69);
            nes.Write(pc++, 0xFF);

            // 0x800C: CMP #$09
            nes.Write(pc++, 0xC9);
            nes.Write(pc++, 0x09);

            // 0x800E: BNE FAIL
            nes.Write(pc++, 0xD0);
            nes.Write(pc++, 0x05);   // to FAIL

            // 0x8010: BCC FAIL (we expect C=1, so if C=0 it's a fail)
            nes.Write(pc++, 0x90);
            nes.Write(pc++, 0x03);   // to FAIL

            // 0x8012: LDA #$01 (SUCCESS)
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x01);

            // 0x8014: BRK
            nes.Write(pc++, 0x00);

            // 0x8015: FAIL: LDA #$00
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x8017: BRK
            nes.Write(pc++, 0x00);
        }


        public static void TestSBC(cpu6502 nes)
        {
            // Program layout:
            // 8000: LDA #$0A     ; A = 10
            // 8002: SEC          ; C = 1 (required before SBC chain)
            // 8003: SBC #$03     ; 10 - 3 = 7, expect A=7, C=1
            // 8005: CMP #$07
            // 8007: BNE FAIL
            // 8009: BCC FAIL     ; C should still be 1
            //
            // 800B: SBC #$08     ; 7 - 8 = -1 -> A=0xFF, C=0
            // 800D: CMP #$FF
            // 800F: BNE FAIL
            // 8011: BCS FAIL     ; C should now be 0
            //
            // 8013: LDA #$01     ; SUCCESS
            // 8015: BRK
            //
            // 8016: FAIL: LDA #$00
            // 8018: BRK

            ushort pc = 0x8000;

            // 0x8000: LDA #$0A
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x0A);

            // 0x8002: SEC
            nes.Write(pc++, 0x38);

            // 0x8003: SBC #$03
            nes.Write(pc++, 0xE9);
            nes.Write(pc++, 0x03);

            // 0x8005: CMP #$07
            nes.Write(pc++, 0xC9);
            nes.Write(pc++, 0x07);

            // 0x8007: BNE FAIL
            nes.Write(pc++, 0xD0);
            nes.Write(pc++, 0x0D);   // to FAIL

            // 0x8009: BCC FAIL (we expect C=1)
            nes.Write(pc++, 0x90);
            nes.Write(pc++, 0x0A);   // to FAIL

            // 0x800B: SBC #$08
            nes.Write(pc++, 0xE9);
            nes.Write(pc++, 0x08);

            // 0x800D: CMP #$FF
            nes.Write(pc++, 0xC9);
            nes.Write(pc++, 0xFF);

            // 0x800F: BNE FAIL
            nes.Write(pc++, 0xD0);
            nes.Write(pc++, 0x04);   // to FAIL

            // 0x8011: BCS FAIL (we expect C=0 now)
            nes.Write(pc++, 0xB0);
            nes.Write(pc++, 0x01);   // to FAIL

            // 0x8013: LDA #$01 (SUCCESS)
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x01);

            // 0x8015: BRK
            nes.Write(pc++, 0x00);

            // 0x8016: FAIL: LDA #$00
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x8018: BRK
            nes.Write(pc++, 0x00);
        }


        public static void TestASL(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST ASL (Arithmetic Shift Left)
            // ==========================================
            // Definition: Shifts all bits left one position. Bit 7 goes into Carry. Bit 0 becomes 0.
            
            // 0x8000: LDA #$01 (Load 1: Binary 0000 0001)
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0x01);

            // 0x8002: ASL A (Shift Left Accumulator) - Opcode 0x0A
            // Result: 0000 0010 (Decimal 2)
            nes.Write(0x8002, 0x0A);

            // 0x8003: ASL A (Shift Left Again)
            // Result: 0000 0100 (Decimal 4)
            nes.Write(0x8003, 0x0A);

            // 0x8004: LDA #$80 (Load 128: Binary 1000 0000)
            nes.Write(0x8004, 0xA9);
            nes.Write(0x8005, 0x80);

            // 0x8006: ASL A (Shift Left 128)
            // Result: 0000 0000 (0). Carry Flag should set (Bit 7 shifted out).
            // Zero Flag should set (Result is 0).
            nes.Write(0x8006, 0x0A);
        }

        public static void TestBCC(cpu6502 nes)
        {
            // Layout:
            // 8000: CLC              ; C = 0
            // 8001: BCC SUCCESS      ; should branch
            // 8003: LDA #$00         ; FAIL
            // 8005: BRK
            // 8006: NOP              ; padding
            // 8007: LDA #$01         ; SUCCESS
            // 8009: BRK

            nes.Write(0x8000, 0x18);       // CLC

            nes.Write(0x8001, 0x90);       // BCC +4 -> 0x8007
            nes.Write(0x8002, 0x04);

            nes.Write(0x8003, 0xA9);       // FAIL: LDA #$00
            nes.Write(0x8004, 0x00);
            nes.Write(0x8005, 0x00);       // BRK

            nes.Write(0x8006, 0xEA);       // NOP (padding if you want)

            nes.Write(0x8007, 0xA9);       // SUCCESS: LDA #$01
            nes.Write(0x8008, 0x01);
            nes.Write(0x8009, 0x00);       // BRK
        }


        public static void TestBCS(cpu6502 nes)
        {
            // 8000: SEC
            // 8001: BCS SUCCESS
            // 8003: LDA #$00 (FAIL)
            // 8005: BRK
            // 8006: NOP
            // 8007: LDA #$01 (SUCCESS)
            // 8009: BRK

            nes.Write(0x8000, 0x38);       // SEC

            nes.Write(0x8001, 0xB0);       // BCS +4 -> 0x8007
            nes.Write(0x8002, 0x04);

            nes.Write(0x8003, 0xA9);       // FAIL
            nes.Write(0x8004, 0x00);
            nes.Write(0x8005, 0x00);       // BRK

            nes.Write(0x8006, 0xEA);       // NOP

            nes.Write(0x8007, 0xA9);       // SUCCESS
            nes.Write(0x8008, 0x01);
            nes.Write(0x8009, 0x00);       // BRK
        }


        public static void TestBEQ(cpu6502 nes)
        {
            ushort pc = 0x8000;

            // 0x8000: LDA #$00 (sets Z=1)
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x8002: BEQ +4 -> 0x8008 (SUCCESS)
            nes.Write(pc++, 0xF0);
            nes.Write(pc++, 0x04);

            // 0x8004: FAIL: LDA #$00
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x8006: BRK
            nes.Write(pc++, 0x00);

            // 0x8007: SUCCESS: LDA #$01  
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x01);

            // 0x8009: BRK
            nes.Write(pc++, 0x00);
        }

        public static void TestBIT(cpu6502 nes)
        {
            // Data: 0010 = $C0 (1100 0000) -> N=1, V=1 when BIT'd
            nes.Write(0x0010, 0xC0);

            // Layout:
            // 8000: LDA #$00
            // 8002: BIT $10        ; A&M=0 -> Z=1, N=1, V=1
            //
            // Use branches that SHOULD NOT be taken if flags are correct:
            // 8004: BPL FAIL       ; N=1 -> BPL must NOT branch
            // 8006: BVC FAIL       ; V=1 -> BVC must NOT branch
            // 8008: BNE FAIL       ; Z=1 -> BNE must NOT branch
            //
            // 800A: LDA #$01       ; SUCCESS
            // 800C: BRK
            //
            // 800D: FAIL: LDA #$00
            // 800F: BRK

            ushort pc = 0x8000;

            // 0x8000: LDA #$00
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x02);

            // 0x8002: BIT $10
            nes.Write(pc++, 0x24);
            nes.Write(pc++, 0x10);

            // 0x8004: BPL FAIL (+7 -> 0x800D)
            nes.Write(pc++, 0x10);
            nes.Write(pc++, 0x07);

            // 0x8006: BVC FAIL (+5 -> 0x800D)
            nes.Write(pc++, 0x50);
            nes.Write(pc++, 0x05);

            // 0x8008: BNE FAIL (+3 -> 0x800D)
            nes.Write(pc++, 0xD0);
            nes.Write(pc++, 0x03);

            // 0x800A: SUCCESS: LDA #$01
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x01);

            // 0x800C: BRK
            nes.Write(pc++, 0x00);

            // 0x800D: FAIL: LDA #$00
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x800F: BRK
            nes.Write(pc++, 0x00);
        }


        public static void TestBMI(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST BMI (Branch if Minus / Negative Set)
            // ==========================================

            // 0x8000: LDA #$FF (Load -1 / 255) -> Sets N Flag to 1
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0xFF);

            // 0x8002: BMI +2 - Opcode 0x30
            nes.Write(0x8002, 0x30);
            nes.Write(0x8003, 0x02);

            // 0x8004: LDA #$00 (Failure)
            nes.Write(0x8004, 0xA9);
            nes.Write(0x8005, 0x00);

            // 0x8006: LDA #$01 (Success)
            nes.Write(0x8006, 0xA9);
            nes.Write(0x8007, 0x01);
        }

        public static void TestBNE(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST BNE (Branch if Not Equal / Zero Clear)
            // ==========================================

            // 0x8000: LDA #$01 (Load 1) -> Clears Z Flag (Z=0)
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0x01);

            // 0x8002: BNE +2 - Opcode 0xD0
            nes.Write(0x8002, 0xD0);
            nes.Write(0x8003, 0x02);

            // 0x8004: LDA #$FF (Failure)
            nes.Write(0x8004, 0xA9);
            nes.Write(0x8005, 0xFF);

            // 0x8006: LDA #$01 (Success)
            nes.Write(0x8006, 0xA9);
            nes.Write(0x8007, 0x01);
        }

        public static void TestBPL(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST BPL (Branch if Plus / Negative Clear)
            // ==========================================

            // 0x8000: LDA #$7F (Load +127) -> Clears N Flag (Bit 7 is 0)
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0x7F);

            // 0x8002: BPL +2 - Opcode 0x10
            nes.Write(0x8002, 0x10);
            nes.Write(0x8003, 0x02);

            // 0x8004: LDA #$FF (Failure)
            nes.Write(0x8004, 0xA9);
            nes.Write(0x8005, 0xFF);

            // 0x8006: LDA #$01 (Success)
            nes.Write(0x8006, 0xA9);
            nes.Write(0x8007, 0x01);
        }

        public static void TestBRK(cpu6502 nes)
        {
            // Set IRQ/BRK vector to $9000
            nes.Write(0xFFFE, 0x00); // low byte
            nes.Write(0xFFFF, 0x90); // high byte

            // Main program at 0x8000:
            // 8000: LDA #$44         ; marker
            // 8002: BRK
            // 8003: LDA #$00         ; if this executes, BRK failed (we shouldn't return here)
            // 8005: BRK
            ushort pc = 0x8000;

            // 0x8000: LDA #$44
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x44);

            // 0x8002: BRK
            nes.Write(pc++, 0x00);

            // 0x8003: LDA #$00 (FAIL if reached)
            nes.Write(pc++, 0xA9);
            nes.Write(pc++, 0x00);

            // 0x8005: BRK
            nes.Write(pc++, 0x00);

            // IRQ/BRK handler at 0x9000:
            // 9000: PHP              ; save status (with B set for BRK)
            // 9001: PLA              ; pull back into A (A contains P, but we only check flow here)
            // (Optionally: test that B flag is set in A)
            // 9002: LDA #$01         ; SUCCESS
            // 9004: RTI              ; return from interrupt (not required for harness, but correct)
            //
            // 9005: (reserved for FAIL path if you later add flag checks)
            ushort irq = 0x9000;

            // 0x9000: PHP
            nes.Write(irq++, 0x08);

            // 0x9001: PLA
            nes.Write(irq++, 0x68);

            // (optional: AND #$10 / BEQ FAIL to verify B=1)

            // 0x9002: LDA #$01 (SUCCESS indicator for harness)
            nes.Write(irq++, 0xA9);
            nes.Write(irq++, 0x01);

            // 0x9004: RTI
            nes.Write(irq++, 0x40);
        }

        public static void TestBVC(cpu6502 nes)
        {
            // 8000: CLV          ; V = 0
            // 8001: BVC SUCCESS
            // 8003: LDA #$00 (FAIL)
            // 8005: BRK
            // 8006: NOP
            // 8007: LDA #$01 (SUCCESS)
            // 8009: BRK

            nes.Write(0x8000, 0xB8);       // CLV

            nes.Write(0x8001, 0x50);       // BVC +4 -> 0x8007
            nes.Write(0x8002, 0x04);

            nes.Write(0x8003, 0xA9);       // FAIL
            nes.Write(0x8004, 0x00);
            nes.Write(0x8005, 0x00);       // BRK

            nes.Write(0x8006, 0xEA);       // NOP

            nes.Write(0x8007, 0xA9);       // SUCCESS
            nes.Write(0x8008, 0x01);
            nes.Write(0x8009, 0x00);       // BRK
        }


        public static void TestBVS(cpu6502 nes)
        {
            // ==========================================
            // PROGRAM: TEST BVS (Branch if Overflow Set)
            // ==========================================
            // It's hard to force Overflow with simple loads, so we'll use math.
            // 127 + 1 = 128 (0x7F + 0x01 = 0x80). 
            // Positive + Positive = Negative Result -> Sets Overflow (V).

            // 0x8000: LDA #$7F (Load 127)
            nes.Write(0x8000, 0xA9);
            nes.Write(0x8001, 0x7F);

            // 0x8002: ADC #$01 (Add 1)
            nes.Write(0x8002, 0x69);
            nes.Write(0x8003, 0x01);
            // V Flag should now be 1.

            // 0x8004: BVS +2 - Opcode 0x70
            nes.Write(0x8004, 0x70);
            nes.Write(0x8005, 0x02);

            // 0x8006: LDA #$FF (Failure)
            nes.Write(0x8006, 0xA9);
            nes.Write(0x8007, 0xFF);

            // 0x8008: LDA #$01 (Success)
            nes.Write(0x8008, 0xA9);
            nes.Write(0x8009, 0x01);
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
