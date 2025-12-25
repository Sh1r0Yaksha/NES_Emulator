namespace CPU
{
    public static class Bus
    {
        // Devices on the bus
        public static byte[] RAM = new byte[64 * 1024]; // RAM is fixed to be 64KB but might change it to own object later

        public static void Write(ushort address, byte data)
        {
            // 0x00 refers to location 0 in RAM, while 0xFF refers to location 255
            if (address >= 0x0000 && address <= 0xFFFF)
		        RAM[address] = data;
        }

        public static byte Read(ushort address, bool readOnly = false)
        {
            if (address >= 0x0000 && address <= 0xFFFF)
                return RAM[address];

            return 0x00;
        }
    }
}


