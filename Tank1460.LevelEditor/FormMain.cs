using System.Text;

namespace Tank1460.LevelEditor;

public partial class FormMain : Form
{
    public FormMain()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        var fileName = textBox2.Text;

        var level1Address = (int)numericUpDown2.Value;
        var targetLevelNumber = (int)numericUpDown1.Value;

        textBox1.Text = ParseLevelFromRom(fileName, targetLevelNumber, level1Address);
    }

    private static string ParseLevelFromRom(string fileName, int targetLevelNumber, int level1Address = 0x308A)
    {
        byte[] romBytes;
        using (var binaryReader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            romBytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

        const int levelLengthInBytes = 0x5B;
        var targetLevelAddress = level1Address + (targetLevelNumber - 1) * levelLengthInBytes;

        var levelBytes = new byte[levelLengthInBytes];
        Array.Copy(romBytes, targetLevelAddress, levelBytes, 0, levelLengthInBytes);

        var decodedData = new char[26, 26];

        // Укладываем байты в матрицу с переводом в наши символьные обозначения.
        for (var i = 0; i < levelBytes.Length; i++)
        {
            var x = (i % 7) * 4;

            var y = (i / 7) * 2;

            var chars = DecodeHex(levelBytes[i] >> 4); // старшие биты
            decodedData[x + 0, y + 0] = chars[0];
            decodedData[x + 1, y + 0] = chars[1];
            decodedData[x + 0, y + 1] = chars[2];
            decodedData[x + 1, y + 1] = chars[3];

            if (x == 24)
                continue; // Каждая строка заканчивается на лишние четыре байта с 0xD.

            chars = DecodeHex(levelBytes[i] & 0x0F); // младшие биты
            decodedData[x + 2, y + 0] = chars[0];
            decodedData[x + 3, y + 0] = chars[1];
            decodedData[x + 2, y + 1] = chars[2];
            decodedData[x + 3, y + 1] = chars[3];

        }

        // Захардкоженные сущности на всех уровнях.
        //decodedData[0, 0] = decodedData[12, 0] = decodedData[24, 0] = 'R';
        //decodedData[8, 24] = '1';
        //decodedData[12, 24] = 's';
        //decodedData[16, 24] = '2';
        //decodedData[11, 23] = decodedData[12, 23] = decodedData[13, 23] = decodedData[14, 23] =
        //    decodedData[11, 24] = decodedData[14, 24] = decodedData[11, 25] = decodedData[14, 25] = 'X';

        var sb = new StringBuilder();
        for (var y = 0; y < decodedData.GetLength(1); y++)
        {
            for (var x = 0; x < decodedData.GetLength(0); x++)
                sb.Append(decodedData[x, y] == '\0' ? '?' : decodedData[x, y]);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string DecodeHex(int p0) =>
        p0 switch
        {
            0x0 => ".X.X",
            0x1 => "..XX",
            0x2 => "X.X.",
            0x3 => "XX..",
            0x4 => "XXXX",
            0x5 => ".Q.Q",
            0x6 => "..QQ",
            0x7 => "Q.Q.",
            0x8 => "QQ..",
            0x9 => "QQQQ",
            0xA => "~~~~",
            0xB => "####",
            0xC => "////",
            _ => "...."
        };

    private static int SearchInByteArray(byte[] source, byte[] pattern)
    {
        var firstCharLastPos = source.Length - pattern.Length + 1;
        for (var i = 0; i < firstCharLastPos; i++)
        {
            if (source[i] != pattern[0])
                continue;

            for (var j = pattern.Length - 1; j >= 1; j--)
            {
                if (source[i + j] != pattern[j]) break;
                if (j == 1) return i;
            }
        }
        return -1;
    }

    private void button2_Click(object sender, EventArgs e)
    {
        var fileName = textBox2.Text;
        var level1Address = TryFindLevel1Address(fileName);

        numericUpDown2.Value = level1Address;
    }

    private int TryFindLevel1Address(string fileName)
    {
        byte[] romBytes;
        using (var binaryReader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            romBytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

        // В хаках начальный адрес уровней заранее неизвестен, поэтому ищем его по характерной предваряющей последовательности.
        var prefixPattern = new byte[] { 0x18, 0x69, 0x10, 0x85, 0x57, 0xc9, 0xe0, 0xd0, 0xbd, 0x60 };

        var prefixAddress = SearchInByteArray(romBytes, prefixPattern);
        if (prefixAddress == -1)
            return -1;

        return prefixAddress + prefixPattern.Length;
    }
}