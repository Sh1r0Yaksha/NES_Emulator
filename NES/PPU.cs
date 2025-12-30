namespace NES
{
    public class PPU
    {
#region MAIN INTERFACE & RESOURCES
        
        // The Output Screen
        // Format: 0xAABBGGRR (uint) - 256x240 resolution
        public uint[] ScreenBuffer = new uint[256 * 240];
        
        // Debug Views (Optional, for Pattern Table visualizer)
        public uint[] PatternTable = new uint[128 * 128];
        public uint[] NameTable = new uint[2 * 256 * 240]; // 2 Nametables visualization

        // System State
        public int Scanline = 0;
        public int Cycle = 0;
        public bool frameComplete = false;
        public bool nmi = false; // Non-Maskable Interrupt signal to CPU

        // Internal RAM
        private byte[,] tblName = new byte[2, 1024]; // 2KB VRAM (2 Nametables)
        private byte[] tblPalette = new byte[32];    // 32 Bytes Palette RAM

        // OAM (Object Attribute Memory) - Stores up to 64 sprites (4 bytes each)
        public byte[] OAM = new byte[256];
        private byte oam_addr = 0;

        // Secondary OAM (8 sprites for current scanline)
        private struct ObjectData
        {
            public byte y;
            public byte id;
            public byte attribute;
            public byte x;
        }

        private ObjectData[] spriteScanline = new ObjectData[8];
        private byte sprite_count;

        // Sprite Zero Hit Detection
        private bool bSpriteZeroHitPossible = false;
        private bool bSpriteZeroBeingRendered = false;


        public PPU()
        {
            // Initialize Palette with default black to prevent crashes
            // for(int i=0; i<32; i++) tblPalette[i] = 0x00;

            // Entry 0 is always the "Universal Background"
            tblPalette[0] = 0x00; // 0x00 = Gray
            
            // Set the first 3 colors of Palette 0 to visible colors
            tblPalette[1] = 0x30; // 0x30 = White
            tblPalette[2] = 0x21; // 0x21 = Light Blue
            tblPalette[3] = 0x16; // 0x16 = Red

            // Fill the rest of the 32-byte palette with 0x00 to prevent null errors
            for (int i = 4; i < 32; i++)
            {
                tblPalette[i] = 0x00;
            }

            for (int i = 0; i < 8; i++)
            {
                spriteScanline[i] = new ObjectData();
            }
        }
#endregion


#region REGISTERS & FLAGS
        // $2000: Controller Register
        public struct ControlRegister
        {
            public byte Reg;
            public byte nametable_x        { get => (byte)(Reg & 0x01); }
            public byte nametable_y        { get => (byte)((Reg >> 1) & 0x01); }
            public byte increment_mode     { get => (byte)((Reg >> 2) & 0x01); }
            public byte pattern_sprite     { get => (byte)((Reg >> 3) & 0x01); }
            public byte pattern_background { get => (byte)((Reg >> 4) & 0x01); }
            public byte sprite_size        { get => (byte)((Reg >> 5) & 0x01); }
            public byte slave_mode         { get => (byte)((Reg >> 6) & 0x01); } // Unused
            public byte enable_nmi         { get => (byte)((Reg >> 7) & 0x01); }
        }
        public ControlRegister control;

        // $2001: Mask Register (Render Switches)
        public struct MaskRegister
        {
            public byte Reg;
            public bool grayscale          { get => (Reg & 0x01) != 0; }
            public bool render_background_left { get => (Reg & 0x02) != 0; }
            public bool render_sprites_left    { get => (Reg & 0x04) != 0; }
            public bool render_background  { get => (Reg & 0x08) != 0; }
            public bool render_sprites     { get => (Reg & 0x10) != 0; }
            public bool emphasize_red      { get => (Reg & 0x20) != 0; }
            public bool emphasize_green    { get => (Reg & 0x40) != 0; }
            public bool emphasize_blue     { get => (Reg & 0x80) != 0; }
        }
        public MaskRegister mask;

        // $2002: Status Register
        public struct StatusRegister
        {
            public byte Reg;
            public byte sprite_overflow    { set { if(value > 0) Reg |= 0x20; else Reg &= 0xDF; } }
            public byte sprite_zero_hit    { set { if(value > 0) Reg |= 0x40; else Reg &= 0xBF; } }
            public byte vertical_blank     { set { if(value > 0) Reg |= 0x80; else Reg &= 0x7F; } get => (byte)((Reg >> 7) & 0x01); }
        }
        public StatusRegister status;

        // internal: Loopy Register (VRAM Address helper)
        public struct LoopyRegister
        {
            public ushort Reg;
            public ushort CoarseX    { get => (ushort)(Reg & 0x001F); set => Reg = (ushort)((Reg & ~0x001F) | (value & 0x001F)); }
            public ushort CoarseY    { get => (ushort)((Reg >> 5) & 0x001F); set => Reg = (ushort)((Reg & ~(0x001F << 5)) | ((value & 0x001F) << 5)); }
            public byte NametableX   { get => (byte)((Reg >> 10) & 0x01); set => Reg = (ushort)((Reg & ~(0x01 << 10)) | ((value & 0x01) << 10)); }
            public byte NametableY   { get => (byte)((Reg >> 11) & 0x01); set => Reg = (ushort)((Reg & ~(0x01 << 11)) | ((value & 0x01) << 11)); }
            public ushort FineY      { get => (ushort)((Reg >> 12) & 0x07); set => Reg = (ushort)((Reg & ~(0x07 << 12)) | ((value & 0x07) << 12)); }
        }

        private LoopyRegister vram_addr; // 'v' (Active Address)
        private LoopyRegister tram_addr; // 't' (Temp Address / Scroll Latch)
        private byte fine_x;             // 'x' (Fine X Scroll 0-7)
        private byte address_latch = 0;  // 'w' (Write toggle: 0=High, 1=Low)
        private byte ppu_data_buffer = 0; // Read Buffer for $2007
#endregion

#region CPU BUS COMMUNICATION (Registers $2000-$2007)

        public byte CPU_Read(ushort addr, bool readOnly = false)
        {
            byte data = 0x00;
            switch (addr % 8) // Mirror every 8 bytes
            {
                case 0x0002: // Status
                    // Only top 3 bits are interesting. Lower 5 are often garbage (or last written data)
                    data = (byte)((status.Reg & 0xE0) | (ppu_data_buffer & 0x1F)); 
                    status.vertical_blank = 0; // Reading status clears VBlank flag
                    address_latch = 0;         // Reading status resets address latch
                    break;
                case 0x0004: // OAM Data
                    data = OAM[oam_addr];
                    break;
                case 0x0007: // PPU Data
                    // Reading data from VRAM is delayed by one read cycle
                    data = ppu_data_buffer;
                    ppu_data_buffer = PPU_Read(vram_addr.Reg);

                    // However, Palette memory is instant
                    if (vram_addr.Reg >= 0x3F00) data = ppu_data_buffer;

                    // Auto-increment address
                    vram_addr.Reg += (ushort)(control.increment_mode == 1 ? 32 : 1);
                    break;
            }
            return data;
        }

        public void CPU_Write(ushort addr, byte data)
        {
            switch (addr % 8)
            {
                case 0x0000: // Control
                    control.Reg = data;
                    // Loading Control sets the Nametable bits of 't'
                    tram_addr.NametableX = control.nametable_x;
                    tram_addr.NametableY = control.nametable_y;
                    break;
                case 0x0001: // Mask
                    mask.Reg = data;
                    break;
                case 0x0003: // OAM Address
                    oam_addr = data;
                    break;
                case 0x0004: // OAM Data
                    OAM[oam_addr] = data;
                    oam_addr++; // Auto-increment
                    break;
                case 0x0005: // Scroll
                    if (address_latch == 0)
                    {
                        // First Write: Fine X and Coarse X
                        fine_x = (byte)(data & 0x07);
                        tram_addr.CoarseX = (ushort)(data >> 3);
                        address_latch = 1;
                    }
                    else
                    {
                        // Second Write: Fine Y and Coarse Y
                        tram_addr.FineY = (ushort)(data & 0x07);
                        tram_addr.CoarseY = (ushort)(data >> 3);
                        address_latch = 0;
                    }
                    break;
                case 0x0006: // PPU Address
                    if (address_latch == 0)
                    {
                        // First Write: High Byte
                        // Only lower 6 bits of high byte are valid (0x3FFF max address)
                        tram_addr.Reg = (ushort)(((ushort)(data & 0x3F) << 8) | (tram_addr.Reg & 0x00FF));
                        address_latch = 1;
                    }
                    else
                    {
                        // Second Write: Low Byte
                        tram_addr.Reg = (ushort)((tram_addr.Reg & 0xFF00) | data);
                        vram_addr = tram_addr; // Update active address 'v'
                        address_latch = 0;
                    }
                    break;
                case 0x0007: // PPU Data
                    PPU_Write(vram_addr.Reg, data);
                    vram_addr.Reg += (ushort)(control.increment_mode == 1 ? 32 : 1);
                    break;
            }
        }
#endregion

#region PPU BUS COMMUNICATION (VRAM / Cartridge)

        private byte PPU_Read(ushort addr)
        {
            addr &= 0x3FFF;
            
            // Cartridge (Pattern Tables)
            if (Bus.Cartridge.PPU_Read(addr, out byte data))
            {
                return data;
            }
            // VRAM (Nametables)
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                addr &= 0x0FFF;
                // Vertical Mirroring (Simplification - Cartridge usually controls this)
                if (Bus.Cartridge.mirror == Cartridge.MIRROR.VERTICAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF) return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0400 && addr <= 0x07FF) return tblName[1, addr & 0x03FF];
                    if (addr >= 0x0800 && addr <= 0x0BFF) return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0C00 && addr <= 0x0FFF) return tblName[1, addr & 0x03FF];
                }
                else if (Bus.Cartridge.mirror == Cartridge.MIRROR.HORIZONTAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF) return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0400 && addr <= 0x07FF) return tblName[0, addr & 0x03FF];
                    if (addr >= 0x0800 && addr <= 0x0BFF) return tblName[1, addr & 0x03FF];
                    if (addr >= 0x0C00 && addr <= 0x0FFF) return tblName[1, addr & 0x03FF];
                }
                return 0x00;
            }

            // Palette RAM
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                addr &= 0x001F;
                // Mirrors: 0x10, 0x14, 0x18, 0x1C mirror 0x00, 0x04, 0x08, 0x0C
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                return tblPalette[addr];
            }
            
            return 0x00;
        }

        private void PPU_Write(ushort addr, byte data)
        {
            addr &= 0x3FFF;

            if (Bus.Cartridge.PPU_Write(addr, data))
            {
                // Cartridge Handled it
            }
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                addr &= 0x0FFF;
                if (Bus.Cartridge.mirror == Cartridge.MIRROR.VERTICAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF) tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0400 && addr <= 0x07FF) tblName[1, addr & 0x03FF] = data;
                    if (addr >= 0x0800 && addr <= 0x0BFF) tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0C00 && addr <= 0x0FFF) tblName[1, addr & 0x03FF] = data;
                }
                else if (Bus.Cartridge.mirror == Cartridge.MIRROR.HORIZONTAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF) tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0400 && addr <= 0x07FF) tblName[0, addr & 0x03FF] = data;
                    if (addr >= 0x0800 && addr <= 0x0BFF) tblName[1, addr & 0x03FF] = data;
                    if (addr >= 0x0C00 && addr <= 0x0FFF) tblName[1, addr & 0x03FF] = data;
                }
            }
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                tblPalette[addr] = data;
            }
        }

#endregion

#region RENDERING PIPELINE (Variables)

        private byte bg_next_tile_id;
        private byte bg_next_tile_attrib;
        private byte bg_next_tile_lsb;
        private byte bg_next_tile_msb;

        private ushort bg_shifter_pattern_lo;
        private ushort bg_shifter_pattern_hi;
        private ushort bg_shifter_attrib_lo;
        private ushort bg_shifter_attrib_hi;

        // Sprite Shift Registers
        private byte[] sprite_shifter_pattern_lo = new byte[8];
        private byte[] sprite_shifter_pattern_hi = new byte[8];

        private byte oam_dma_page = 0x00;
        private byte oam_dma_addr = 0x00;
        private byte oam_dma_data = 0x00;
        private bool oam_dma_transfer = false;
        private bool oam_dma_dummy = true;



#endregion

#region CLOCK (The Heartbeat)

        public void Clock()
        {
            // ==============================================================================
            // 1. VISIBLE SCANLINE RENDERING (Lines 0 - 239) and PRE-RENDER (Line -1)
            // ==============================================================================
            if (Scanline >= -1 && Scanline < 240)
            {
                // --------------------------------------------------------------------------
                // A. BACKGROUND PROCESSING (Shift Registers & Fetches)
                // --------------------------------------------------------------------------
                // Runs on cycles 2-257 (Visible) and 321-337 (Prefetch for next line)
                if ((Cycle >= 2 && Cycle < 258) || (Cycle >= 321 && Cycle < 338))
                {
                    UpdateShifters(); // Shift bits left by 1

                    // Every 8 cycles, we fetch new data into the 'Next' registers
                    switch ((Cycle - 1) % 8)
                    {
                        case 0:
                            LoadBackgroundShifters(); // Transfer 'Next' to 'Shifters'
                            // Fetch Tile ID
                            bg_next_tile_id = PPU_Read((ushort)(0x2000 | (vram_addr.Reg & 0x0FFF)));
                            break;
                        case 2:
                            // Fetch Attribute (Palette)
                            bg_next_tile_attrib = PPU_Read((ushort)(0x23C0 | (vram_addr.NametableY << 11)
                                                                        | (vram_addr.NametableX << 10)
                                                                        | ((vram_addr.CoarseY >> 2) << 3)
                                                                        | (vram_addr.CoarseX >> 2)));
                            if ((vram_addr.CoarseY & 0x02) != 0) bg_next_tile_attrib >>= 4;
                            if ((vram_addr.CoarseX & 0x02) != 0) bg_next_tile_attrib >>= 2;
                            bg_next_tile_attrib &= 0x03;
                            break;
                        case 4:
                            // Fetch Low Bit Plane
                            bg_next_tile_lsb = PPU_Read((ushort)((control.pattern_background > 0 ? 0x1000 : 0x0000)
                                                                + (bg_next_tile_id << 4)
                                                                + vram_addr.FineY));
                            break;
                        case 6:
                            // Fetch High Bit Plane
                            bg_next_tile_msb = PPU_Read((ushort)((control.pattern_background > 0 ? 0x1000 : 0x0000)
                                                                + (bg_next_tile_id << 4)
                                                                + vram_addr.FineY + 8));
                            break;
                        case 7:
                            IncrementScrollX();
                            break;
                    }
                }

                // End of Scanline Housekeeping
                if (Cycle == 256) IncrementScrollY();
                
                // Reset X scroll at start of new line
                if (Cycle == 257) 
                { 
                    LoadBackgroundShifters(); 
                    TransferAddressX(); 
                }

                if (Cycle == 257 && Scanline >= 0)
                {
                    EvaluateSprites();
                    LoadSpriteShifters();
                }

                // Reset Y scroll at end of VBlank (Pre-render line)
                if (Scanline == -1 && Cycle >= 280 && Cycle < 305) 
                { 
                    TransferAddressY(); 
                }

                if (Scanline == -1 && Cycle == 1)
                {
                    status.vertical_blank = 0;
                    status.sprite_zero_hit = 0;
                    status.sprite_overflow = 0;
                }
            }

            // ==============================================================================
            // 2. V-BLANK INTERRUPT (Line 241)
            // ==============================================================================
            if (Scanline == 241 && Cycle == 1)
            {
                status.vertical_blank = 1;
                if (control.enable_nmi > 0) nmi = true;
            }

            // ==============================================================================
            // 3. PIXEL COMPOSITOR (Background + Sprites)
            // ==============================================================================
            byte bg_pixel = 0x00;
            byte bg_palette = 0x00;

            if (mask.render_background && Scanline >= 0 && Scanline < 240 && Cycle >= 1 && Cycle < 257)
            {
                ushort bit_mux = (ushort)(0x8000 >> fine_x);
                byte p0 = (byte)((bg_shifter_pattern_lo & bit_mux) > 0 ? 1 : 0);
                byte p1 = (byte)((bg_shifter_pattern_hi & bit_mux) > 0 ? 1 : 0);
                bg_pixel = (byte)((p1 << 1) | p0);
                
                byte pal0 = (byte)((bg_shifter_attrib_lo & bit_mux) > 0 ? 1 : 0);
                byte pal1 = (byte)((bg_shifter_attrib_hi & bit_mux) > 0 ? 1 : 0);
                bg_palette = (byte)((pal1 << 1) | pal0);
            }

            byte fg_pixel = 0x00;
            byte fg_palette = 0x00;
            byte fg_priority = 0x00;

            if (mask.render_sprites && Scanline >= 0 && Scanline < 240 && Cycle >= 1 && Cycle < 257)
            {
                bSpriteZeroBeingRendered = false;
                
                for (int i = 0; i < sprite_count; i++)
                {
                    if (spriteScanline[i].x == 0)
                    {
                        byte fg_pixel_lo = (byte)((sprite_shifter_pattern_lo[i] & 0x80) > 0 ? 1 : 0);
                        byte fg_pixel_hi = (byte)((sprite_shifter_pattern_hi[i] & 0x80) > 0 ? 1 : 0);
                        fg_pixel = (byte)((fg_pixel_hi << 1) | fg_pixel_lo);
                        
                        fg_palette = (byte)((spriteScanline[i].attribute & 0x03) + 0x04);
                        fg_priority = (byte)((spriteScanline[i].attribute & 0x20) == 0 ? 1 : 0);
                        
                        if (fg_pixel != 0)
                        {
                            if (i == 0)
                                bSpriteZeroBeingRendered = true;
                            break;
                        }
                    }
                }
            }

            // Combine Background and Foreground
            byte pixel = 0x00;
            byte palette = 0x00;

            if (bg_pixel == 0 && fg_pixel == 0)
            {
                pixel = 0x00;
                palette = 0x00;
            }
            else if (bg_pixel == 0 && fg_pixel > 0)
            {
                pixel = fg_pixel;
                palette = fg_palette;
            }
            else if (bg_pixel > 0 && fg_pixel == 0)
            {
                pixel = bg_pixel;
                palette = bg_palette;
            }
            else if (bg_pixel > 0 && fg_pixel > 0)
            {
                if (fg_priority > 0)
                {
                    pixel = fg_pixel;
                    palette = fg_palette;
                }
                else
                {
                    pixel = bg_pixel;
                    palette = bg_palette;
                }
                
                // Sprite Zero Hit Detection
                if (bSpriteZeroHitPossible && bSpriteZeroBeingRendered)
                {
                    if (mask.render_background && mask.render_sprites)
                    {
                        if (!(mask.render_background_left && mask.render_sprites_left))
                        {
                            // Left column rendering is disabled, so sprite zero can only hit from pixel 9 onwards
                            if (Cycle >= 9 && Cycle < 258)
                            {
                                status.sprite_zero_hit = 1;
                            }
                        }
                        else
                        {
                            // Left column rendering is enabled, sprite zero can hit from pixel 1 onwards
                            if (Cycle >= 1 && Cycle < 258)
                            {
                                status.sprite_zero_hit = 1;
                            }
                        }
                    }
                }

            }

            if (Scanline >= 0 && Scanline < 240 && Cycle >= 1 && Cycle < 257)
            {
                ScreenBuffer[Scanline * 256 + (Cycle - 1)] = GetColorFromPalette(palette, pixel);
            }


            // ==============================================================================
            // 4. CLOCK MANAGEMENT
            // ==============================================================================
            Cycle++;
            if (Cycle >= 341)
            {
                Cycle = 0;
                Scanline++;
                if (Scanline >= 261)
                {
                    Scanline = -1;
                    frameComplete = true;
                    status.vertical_blank = 0; // Clear VBlank at start of pre-render
                }
            }
        }

#endregion

#region HELPER METHODS

        public void DMA_OAM(byte page, byte[] cpuRAM)
        {
            oam_dma_page = page;
            oam_dma_addr = 0x00;
            oam_dma_transfer = true;
            oam_dma_dummy = true;
            
            // Quick transfer - copy 256 bytes from CPU memory to OAM
            ushort baseAddr = (ushort)(page << 8);
            for (int i = 0; i < 256; i++)
            {
                OAM[i] = cpuRAM[baseAddr + i];
            }
        }
        private void IncrementScrollX()
        {
            if (mask.render_background || mask.render_sprites)
            {
                if (vram_addr.CoarseX == 31) { vram_addr.CoarseX = 0; vram_addr.NametableX ^= 1; }
                else vram_addr.CoarseX++;
            }
        }

        private void IncrementScrollY()
        {
            if (mask.render_background || mask.render_sprites)
            {
                if (vram_addr.FineY < 7) vram_addr.FineY++;
                else
                {
                    vram_addr.FineY = 0;
                    if (vram_addr.CoarseY == 29) { vram_addr.CoarseY = 0; vram_addr.NametableY ^= 1; }
                    else if (vram_addr.CoarseY == 31) vram_addr.CoarseY = 0;
                    else vram_addr.CoarseY++;
                }
            }
        }

        private void TransferAddressX() { if (mask.render_background || mask.render_sprites) { vram_addr.NametableX = tram_addr.NametableX; vram_addr.CoarseX = tram_addr.CoarseX; } }
        private void TransferAddressY() { if (mask.render_background || mask.render_sprites) { vram_addr.FineY = tram_addr.FineY; vram_addr.NametableY = tram_addr.NametableY; vram_addr.CoarseY = tram_addr.CoarseY; } }

        private void LoadBackgroundShifters()
        {
            bg_shifter_pattern_lo = (ushort)((bg_shifter_pattern_lo & 0xFF00) | bg_next_tile_lsb);
            bg_shifter_pattern_hi = (ushort)((bg_shifter_pattern_hi & 0xFF00) | bg_next_tile_msb);
            bg_shifter_attrib_lo  = (ushort)((bg_shifter_attrib_lo & 0xFF00) | ((bg_next_tile_attrib & 0x01) != 0 ? 0xFF : 0x00));
            bg_shifter_attrib_hi  = (ushort)((bg_shifter_attrib_hi & 0xFF00) | ((bg_next_tile_attrib & 0x02) != 0 ? 0xFF : 0x00));
        }

        private void UpdateShifters()
        {
            if (mask.render_background)
            {
                bg_shifter_pattern_lo <<= 1;
                bg_shifter_pattern_hi <<= 1;
                bg_shifter_attrib_lo <<= 1;
                bg_shifter_attrib_hi <<= 1;
            }

            if (mask.render_sprites && Cycle >= 1 && Cycle < 258)
            {
                for (int i = 0; i < sprite_count; i++)
                {
                    if (spriteScanline[i].x > 0)
                    {
                        spriteScanline[i].x--;
                    }
                    else
                    {
                        sprite_shifter_pattern_lo[i] <<= 1;
                        sprite_shifter_pattern_hi[i] <<= 1;
                    }
                }
            }
        }

        private uint GetColorFromPalette(byte palette, byte pixel)
        {
            // Transparent pixel? Return background color (Palette 0, index 0)
            if (pixel == 0) return NESPalette.Colors[PPU_Read(0x3F00) & 0x3F];
            
            // Read Palette RAM
            ushort addr = (ushort)(0x3F00 + (palette << 2) + pixel);
            return NESPalette.Colors[PPU_Read(addr) & 0x3F];
        }

        // Debug Helper: Decodes the CHR-ROM for visualization
        // i = Which Pattern Table to view (0 or 1)
        // palette = Which palette index (0-7) to use for coloring the sprite
        public void UpdatePatternTable(int i, byte palette)
        {
            // Loop through all 16x16 tiles
            for (int nTileY = 0; nTileY < 16; nTileY++)
            {
                for (int nTileX = 0; nTileX < 16; nTileX++)
                {
                    // Convert 2D tile coordinate to 1D offset
                    ushort nOffset = (ushort)(nTileY * 256 + nTileX * 16);

                    // Loop through 8 rows of pixels
                    for (int row = 0; row < 8; row++)
                    {
                        // 1. Read the Least Significant Bit (LSB) plane
                        // Pattern Table 0 is at 0x0000, Table 1 is at 0x1000
                        byte tile_lsb = PPU_Read((ushort)(i * 0x1000 + nOffset + row + 0));
                        
                        // 2. Read the Most Significant Bit (MSB) plane (Offset by 8 bytes)
                        byte tile_msb = PPU_Read((ushort)(i * 0x1000 + nOffset + row + 8));

                        // Loop through 8 pixels in the row
                        for (int col = 0; col < 8; col++)
                        {
                            // 3. Combine bits to form pixel value (0, 1, 2, or 3)
                            // We look at bit 'col', but since NES pixels are stored right-to-left
                            // (bit 7 is pixel 0), we shift carefully.
                            byte pixel = (byte)(((tile_lsb & 0x01) << 0) | ((tile_msb & 0x01) << 1));

                            // Shift the register to get the next bit for the next column
                            tile_lsb >>= 1;
                            tile_msb >>= 1;

                            // 4. Calculate position in the 128x128 debug array
                            // (7 - col) flips the X because we read LSB first (right-to-left)
                            int x = nTileX * 8 + (7 - col);
                            int y = nTileY * 8 + row;

                            // 5. Get Color and write to buffer
                            // We use the helper 'GetColorFromPalette' we already wrote
                            PatternTable[y * 128 + x] = GetColorFromPalette(palette, pixel);
                        }
                    }
                }
            }
        }

        // NES Palette (Standard)
        public static readonly uint[] Colors = new uint[]
        {
            0xFF7C7C7C, 0xFF0000FC, 0xFF0000BC, 0xFF4428BC, 0xFF940084, 0xFFA80020, 0xFFA81000, 0xFF881400,
            0xFF503000, 0xFF007800, 0xFF006800, 0xFF005800, 0xFF004058, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFBCBCBC, 0xFF0078F8, 0xFF0058F8, 0xFF6844FC, 0xFFD800CC, 0xFFE40058, 0xFFF83800, 0xFFE45C10,
            0xFFAC7C00, 0xFF00B800, 0xFF00A800, 0xFF00A844, 0xFF008888, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFF8F8F8, 0xFF3CBCFC, 0xFF6888FC, 0xFF9878F8, 0xFFF878F8, 0xFFF85898, 0xFFF87858, 0xFFFCA044,
            0xFFF8B800, 0xFFB8F818, 0xFF58D854, 0xFF58F898, 0xFF00E8D8, 0xFF787878, 0xFF000000, 0xFF000000,
            0xFFFCFCFC, 0xFFA4E4FC, 0xFFB8B8F8, 0xFFD8B8F8, 0xFFF8B8F8, 0xFFF8A4C0, 0xFFF0D0B0, 0xFFFCE0A0,
            0xFFF8D878, 0xFFD8F878, 0xFFB8F8B8, 0xFFB8F8D8, 0xFF00FCFC, 0xFFF8D8F8, 0xFF000000, 0xFF000000
        };

        private void EvaluateSprites()
        {
            sprite_count = 0;
            bSpriteZeroHitPossible = false;
            
            for (int i = 0; i < 8; i++)
            {
                sprite_shifter_pattern_lo[i] = 0;
                sprite_shifter_pattern_hi[i] = 0;
            }

            byte nOAMEntry = 0;
            while (nOAMEntry < 64 && sprite_count < 9)
            {
                int diff = Scanline - OAM[nOAMEntry * 4];
                
                int sprite_size = control.sprite_size == 0 ? 8 : 16;
                
                if (diff >= 0 && diff < sprite_size && sprite_count < 8)
                {
                    if (sprite_count < 8)
                    {
                        if (nOAMEntry == 0)
                            bSpriteZeroHitPossible = true;

                        spriteScanline[sprite_count].y = OAM[nOAMEntry * 4];
                        spriteScanline[sprite_count].id = OAM[nOAMEntry * 4 + 1];
                        spriteScanline[sprite_count].attribute = OAM[nOAMEntry * 4 + 2];
                        spriteScanline[sprite_count].x = OAM[nOAMEntry * 4 + 3];
                        sprite_count++;
                    }
                }
                nOAMEntry++;
            }
            
            status.sprite_overflow = (byte)(sprite_count >= 8 ? 1 : 0);
        }

        private void LoadSpriteShifters()
        {
            for (int i = 0; i < sprite_count; i++)
            {
                byte sprite_pattern_bits_lo, sprite_pattern_bits_hi;
                ushort sprite_pattern_addr_lo, sprite_pattern_addr_hi;

                if (control.sprite_size == 0)
                {
                    // 8x8 Sprite Mode
                    if ((spriteScanline[i].attribute & 0x80) == 0)
                    {
                        // Sprite is NOT vertically flipped
                        sprite_pattern_addr_lo = (ushort)(
                            (control.pattern_sprite << 12) |
                            (spriteScanline[i].id << 4) |
                            (Scanline - spriteScanline[i].y)
                        );
                    }
                    else
                    {
                        // Sprite IS vertically flipped
                        sprite_pattern_addr_lo = (ushort)(
                            (control.pattern_sprite << 12) |
                            (spriteScanline[i].id << 4) |
                            (7 - (Scanline - spriteScanline[i].y))
                        );
                    }
                }
                else
                {
                    // 8x16 Sprite Mode
                    if ((spriteScanline[i].attribute & 0x80) == 0)
                    {
                        // Sprite is NOT vertically flipped
                        if (Scanline - spriteScanline[i].y < 8)
                        {
                            // Top half
                            sprite_pattern_addr_lo = (ushort)(
                                ((spriteScanline[i].id & 0x01) << 12) |
                                ((spriteScanline[i].id & 0xFE) << 4) |
                                ((Scanline - spriteScanline[i].y) & 0x07)
                            );
                        }
                        else
                        {
                            // Bottom half
                            sprite_pattern_addr_lo = (ushort)(
                                ((spriteScanline[i].id & 0x01) << 12) |
                                (((spriteScanline[i].id & 0xFE) + 1) << 4) |
                                ((Scanline - spriteScanline[i].y) & 0x07)
                            );
                        }
                    }
                    else
                    {
                        // Sprite IS vertically flipped
                        if (Scanline - spriteScanline[i].y < 8)
                        {
                            // Top half (which is actually bottom when flipped)
                            sprite_pattern_addr_lo = (ushort)(
                                ((spriteScanline[i].id & 0x01) << 12) |
                                (((spriteScanline[i].id & 0xFE) + 1) << 4) |
                                (7 - (Scanline - spriteScanline[i].y) & 0x07)
                            );
                        }
                        else
                        {
                            // Bottom half (which is actually top when flipped)
                            sprite_pattern_addr_lo = (ushort)(
                                ((spriteScanline[i].id & 0x01) << 12) |
                                ((spriteScanline[i].id & 0xFE) << 4) |
                                (7 - (Scanline - spriteScanline[i].y) & 0x07)
                            );
                        }
                    }
                }

                sprite_pattern_addr_hi = (ushort)(sprite_pattern_addr_lo + 8);
                sprite_pattern_bits_lo = PPU_Read(sprite_pattern_addr_lo);
                sprite_pattern_bits_hi = PPU_Read(sprite_pattern_addr_hi);

                if ((spriteScanline[i].attribute & 0x40) != 0)
                {
                    // Horizontal flip
                    sprite_pattern_bits_lo = FlipByte(sprite_pattern_bits_lo);
                    sprite_pattern_bits_hi = FlipByte(sprite_pattern_bits_hi);
                }

                sprite_shifter_pattern_lo[i] = sprite_pattern_bits_lo;
                sprite_shifter_pattern_hi[i] = sprite_pattern_bits_hi;
            }
        }

        private byte FlipByte(byte b)
        {
            b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
            b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
            b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
            return b;
        }

#endregion
    }
}