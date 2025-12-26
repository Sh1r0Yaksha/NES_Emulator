public class LogEntry
{
    public ushort PC;
    public byte A, X, Y, P, STKP;

    // Parse a line like: "C000  4C F5 C5  ... A:00 X:00 Y:00 P:24 SP:FD ..."
    public static LogEntry FromLine(string line)
    {
        // 1. Sanitize: Trim whitespace and invisible characters
        line = line.Trim();
        
        // Safety: If line is too short to be valid, return null (skip it)
        if (line.Length < 10) return null;

        LogEntry entry = new LogEntry();


        // --- PARSE PC ---
        // PC is always the first 4 characters: "C8A5..."
        entry.PC = Convert.ToUInt16(line.Substring(0, 4), 16);

        // --- PARSE REGISTERS ---
        // We use IndexOf because the disassembly in the middle varies in length,
        // pushing the registers to different positions on the line.
        
        int iA = line.IndexOf("A:");
        int iX = line.IndexOf("X:");
        int iY = line.IndexOf("Y:");
        int iP = line.IndexOf("P:");
        int iSP = line.IndexOf("SP:");

        // Safety: If any tag is missing, this isn't a valid log line
        if (iA == -1 || iX == -1 || iY == -1 || iP == -1 || iSP == -1)
            return null;

        // Extract 2 hex characters after "A:"
        entry.A = Convert.ToByte(line.Substring(iA + 2, 2), 16);
        
        // Extract 2 hex characters after "X:"
        entry.X = Convert.ToByte(line.Substring(iX + 2, 2), 16);
        
        // Extract 2 hex characters after "Y:"
        entry.Y = Convert.ToByte(line.Substring(iY + 2, 2), 16);
        
        // Extract 2 hex characters after "P:"
        entry.P = Convert.ToByte(line.Substring(iP + 2, 2), 16);
        
        // Extract 2 hex characters after "SP:"
        entry.STKP = Convert.ToByte(line.Substring(iSP + 3, 2), 16);
        return entry;
    }
}