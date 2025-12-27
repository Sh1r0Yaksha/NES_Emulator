namespace NES
{
    public static class Bus
    {
        // Devices on the bus
        public static byte[] RAM = new byte[64 * 1024]; // RAM is fixed to be 64KB but might change it to own object later

        public static void Write(ushort address, byte data)
        {
            // 0x00 refers to location 0 in RAM, while 0xFF refers to location 255
            if (address < RAM.Length)
                RAM[address] = data;
        }

        public static byte Read(ushort address, bool readOnly = false)
        {
            // Check against the ARRAY SIZE, not 0xFFFF
            if (address < RAM.Length) 
                return RAM[address];

            // If address is outside RAM (e.g., ROM or unmapped), return 0 for now
            return 0x00; 
        }
    }
}