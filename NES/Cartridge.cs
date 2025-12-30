using System.IO;

namespace NES
{
    public class Cartridge
    {
        public Cartridge(string filename)
        {
            // 1. Open the file
            if (!File.Exists(filename)) return;

            using (BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                // 2. Read the Header (16 bytes)
                sHeader header = new sHeader();   
                header.Name = reader.ReadBytes(4);
                header.PRG_ROM_Chunks = reader.ReadByte();
                header.CHR_ROM_chunks = reader.ReadByte();
                header.Mapper1 = reader.ReadByte();
                header.Mapper2 = reader.ReadByte();
                header.PRG_RAM_size = reader.ReadByte();
                header.TV_System1 = reader.ReadByte();
                header.TV_System2 = reader.ReadByte();
                header.Unused = reader.ReadBytes(5);

                // 3. Skip "Trainer" if present
                // Bit 2 of mapper1 indicates a 512-byte trainer is present before PRG data
                if ((header.Mapper1 & 0x04) != 0)
                {
                    reader.BaseStream.Seek(512, SeekOrigin.Current);
                }

                // 4. Determine Mapper ID
                // Lower 4 bits of mapper ID are in header.mapper1 (upper nibble)
                // Upper 4 bits of mapper ID are in header.mapper2 (upper nibble)
                nMapperID = (byte)(((header.Mapper2 >> 4) << 4) | (header.Mapper1 >> 4));

                // 5. Determine Mirroring
                // Bit 0 of mapper1 determines mirroring (0 = Horizontal, 1 = Vertical)
                mirror = (header.Mapper1 & 0x01) != 0 ? MIRROR.VERTICAL : MIRROR.HORIZONTAL;

                // 6. File Format Detection
                // (Standard iNES format is usually Type 1 logic)
                byte nFileType = 1;

                if (nFileType == 0)
                {

                }

                if (nFileType == 1)
                {
                    // 7. Load PRG Memory
                    nPRGBanks = header.PRG_ROM_Chunks;
                    // Calculate size: banks * 16KB
                    int prgSize = nPRGBanks * 16384; 
                    vPRGMemory = new byte[prgSize];
                    
                    // Read bytes directly into the array
                    // Note: ReadBytes might return fewer bytes if EOF, but usually fine for ROMs
                    vPRGMemory = reader.ReadBytes(prgSize);

                    // 8. Load CHR Memory
                    nCHRBanks = header.CHR_ROM_chunks;
                    // Calculate size: banks * 8KB
                    int chrSize = nCHRBanks * 8192;
                    vCHRMemory = new byte[chrSize];

                    // If chunks is 0, typically means the board uses RAM for CHR, 
                    // but we still allocate space/handle it based on your emulator design.
                    if (chrSize > 0)
                    {
                        vCHRMemory = reader.ReadBytes(chrSize);
                    }
                }

                if (nFileType == 2)
                {

                }

                // 9. Load Appropriate Mapper
                switch (nMapperID)
                {
                    // You will need to create these Mapper classes later!
                    case 0: 
                        pMapper = new Mapper000(nPRGBanks, nCHRBanks); 
                        break;
                    default:
                        Console.WriteLine($"Unsupported Mapper: {nMapperID}");
                        break;
                }

                bImageValid = true;
            }
        }

        public bool ImageValid()
        {
            return bImageValid;
        }

        public enum MIRROR
        {
            HORIZONTAL,
            VERTICAL,
            ONESCREEN_LO,
            ONESCREEN_HI,
        } 
        public MIRROR mirror = MIRROR.HORIZONTAL;

        bool bImageValid = false;

        byte nMapperID = 0;
        byte nPRGBanks = 0;
        byte nCHRBanks = 0;

        byte[] vPRGMemory;
        byte[] vCHRMemory;

        Mapper pMapper;

#region Read/Write

        // Communication with Main Bus
        public bool CPU_Read(ushort address,out byte data)
        {
            if (pMapper.CPU_MapRead(address, out uint mapped_addr))
            {
                data = vPRGMemory[mapped_addr];
                return true;
            }
            else
            {
                data = 0x00;
                return false;
            }
                
        }
        public bool CPU_Write(ushort address, byte data)
        {
        if (pMapper.CPU_MapWrite(address, out uint mapped_addr))
        {
            vPRGMemory[mapped_addr] = data;
            return true;
        }
        else
            return false;
        }

        // Communication with PPU Bus
        public bool PPU_Read(ushort address,out byte data)
        {
            // DEBUG LOG: Only print for address 0x0010 (Start of '0' character usually)
            if (address == 0x0010) 
            {
                Console.WriteLine($"[DEBUG] PPU Read $0010. CHR_Size: {vCHRMemory.Length}");
            }
            if (pMapper.PPU_MapRead(address,out uint mapped_addr))
            {
                // 2. Fetch the byte from the CHR array loaded earlier
                // Check bounds just in case
                if (mapped_addr < vCHRMemory.Length)
                {
                    if (address == 0x0010) Console.WriteLine($"[DEBUG] Mapper mapped to {mapped_addr}");
                    data = vCHRMemory[mapped_addr];
                    return true;
                }
            }    
            data = 0x00;
            return false;
        }
        public bool PPU_Write(ushort address, byte data)
        {
            if (pMapper.PPU_MapRead(address,out uint mapped_addr))
            {
                vCHRMemory[mapped_addr] = data;
                return true;
            }
            else
                return false;
        }
#endregion
    }
}