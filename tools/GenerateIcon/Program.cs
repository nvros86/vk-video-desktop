using System.Drawing;
using System.Drawing.Imaging;

const int[] Sizes = [16, 32, 48, 64, 128, 256];

string sourcePath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VKVideoDesktop.App", "Assets", "Square150x150Logo.scale-200.png");

string outputPath = args.Length > 1
    ? args[1]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "VKVideoDesktop.App", "Assets", "icon.ico");

sourcePath = Path.GetFullPath(sourcePath);
outputPath = Path.GetFullPath(outputPath);

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Исходный файл не найден: {sourcePath}");
    return 1;
}

Console.WriteLine($"Источник: {sourcePath}");
Console.WriteLine($"Цель: {outputPath}");

using var sourceImage = Image.FromFile(sourcePath);
Console.WriteLine($"Размер исходника: {sourceImage.Width}x{sourceImage.Height}");

var pngDataList = new List<byte[]>();

using (var ms = new MemoryStream())
{
    foreach (int size in Sizes)
    {
        ms.SetLength(0);
        using (var bitmap = new Bitmap(size, size))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.DrawImage(sourceImage, 0, 0, size, size);
            bitmap.Save(ms, ImageFormat.Png);
        }
        var data = ms.ToArray();
        pngDataList.Add(data);
        Console.WriteLine($"  {size}x{size}: {data.Length} bytes");
    }
}

int count = Sizes.Length;
int headerSize = 6 + (count * 16);

int totalSize = headerSize;
foreach (var d in pngDataList) totalSize += d.Length;

var output = new byte[totalSize];

output[0] = 0;
output[1] = 0;
output[2] = 1;
output[3] = 0;
output[4] = (byte)(count & 0xFF);
output[5] = (byte)((count >> 8) & 0xFF);

int curOffset = headerSize;
for (int i = 0; i < count; i++)
{
    int entryBase = 6 + (i * 16);
    int w = Sizes[i] == 256 ? 0 : Sizes[i];
    int h = Sizes[i] == 256 ? 0 : Sizes[i];
    int dataLen = pngDataList[i].Length;

    output[entryBase + 0] = (byte)w;
    output[entryBase + 1] = (byte)h;
    output[entryBase + 2] = 0;
    output[entryBase + 3] = 0;
    output[entryBase + 4] = 1;
    output[entryBase + 5] = 0;
    output[entryBase + 6] = 32;
    output[entryBase + 7] = 0;

    output[entryBase + 8] = (byte)(dataLen & 0xFF);
    output[entryBase + 9] = (byte)((dataLen >> 8) & 0xFF);
    output[entryBase + 10] = (byte)((dataLen >> 16) & 0xFF);
    output[entryBase + 11] = (byte)((dataLen >> 24) & 0xFF);

    output[entryBase + 12] = (byte)(curOffset & 0xFF);
    output[entryBase + 13] = (byte)((curOffset >> 8) & 0xFF);
    output[entryBase + 14] = (byte)((curOffset >> 16) & 0xFF);
    output[entryBase + 15] = (byte)((curOffset >> 24) & 0xFF);

    curOffset += dataLen;
}

int pos = headerSize;
for (int i = 0; i < count; i++)
{
    Array.Copy(pngDataList[i], 0, output, pos, pngDataList[i].Length);
    pos += pngDataList[i].Length;
}

File.WriteAllBytes(outputPath, output);

Console.WriteLine($"ICO создан: {outputPath} ({output.Length} байт)");
return 0;
