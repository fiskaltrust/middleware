using System.Text;

namespace fiskaltrust.Middleware.SCU.ES.TicketBAI.Helpers;

public class CRC8Calculator
{
    private readonly byte[] table = new byte[256];

    public CRC8Calculator()
    {
        GenerateTable();
    }

    public byte ComputeChecksum(string input)
    {
        var data = Encoding.UTF8.GetBytes(input);
        byte crc = 0;
        foreach (var b in data)
        {
            crc = table[crc ^ b];
        }
        return crc;
    }

    private void GenerateTable()
    {
        byte polynomial = 0x07;
        for (var i = 0; i < 256; i++)
        {
            var temp = (byte) i;
            for (byte j = 0; j < 8; j++)
            {
                if ((temp & 0x80) != 0)
                {
                    temp = (byte) ((temp << 1) ^ polynomial);
                }
                else
                {
                    temp <<= 1;
                }
            }
            table[i] = temp;
        }
    }
}
