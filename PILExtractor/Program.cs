using System.Drawing;

const int PIL_HEADER_SIZE = 0x10;
const int PIM_HEADER_SIZE = 0x20;

// get file to convert
Console.Write("Input the name of the PIL file (including extension): ");
string pimfile = Console.ReadLine();

// check if file exists
if (!File.Exists(pimfile))
{
    Console.WriteLine("Invalid Filename\nExiting Execution");
    return;
}

// read file data
byte[] data = File.ReadAllBytes(pimfile);

// read PIL header data
int pim_count = data[0];
int[] pim_headers = new int[pim_count];

// get starting points of all pim files
for (int p = 0; p < pim_count; p++)
{
    if (p == 0)
    {
        pim_headers[p] = PIL_HEADER_SIZE;
    }
    else
    {
        int width = data[pim_headers[p - 1]] + (data[pim_headers[p - 1] + 1] * 256);
        int height = data[pim_headers[p - 1] + 2] + (data[pim_headers[p - 1] + 3] * 256);
        int bit_depth = data[pim_headers[p - 1] + 4] + (data[pim_headers[p - 1] + 5] * 256);

        int color_count;
        if (bit_depth == 0x20)
            color_count = 0;
        else if (bit_depth == 0x08)
            color_count = 256;
        else
            color_count = 16;

        if (bit_depth == 4)
        {
            pim_headers[p] = pim_headers[p - 1] + PIM_HEADER_SIZE + (color_count * 0x04) + (width * height) / 2;
        }
        else if (bit_depth == 8)
        {
            pim_headers[p] = pim_headers[p - 1] + PIM_HEADER_SIZE + (color_count * 0x04) + (width * height);
        }
        else if (bit_depth == 32)
        {
            pim_headers[p] = pim_headers[p - 1] + PIM_HEADER_SIZE + (width * height * 4);
        }
    }
}

// most of this code is the same as the PIMtoBMP code, just with needed modifications
for (int p = 0; p < pim_count; p++)
{
    int width = data[pim_headers[p]] + (data[pim_headers[p] + 1] * 256);
    int height = data[pim_headers[p] + 2] + (data[pim_headers[p] + 3] * 256);
    int bit_depth = data[pim_headers[p] + 4] + (data[pim_headers[p] + 5] * 256);
    int color_count;
    if (bit_depth == 0x20)
        color_count = 0;
    else if (bit_depth == 0x08)
        color_count = 256;
    else
        color_count = 16;
    int palette_start = pim_headers[p] + PIM_HEADER_SIZE;
    int pixel_start = palette_start + (4 * color_count);

    string filename = string.Empty;
    for (int f = 8; f < 32; f++)
    {
        if (data[pim_headers[p] + f] == 0)
            break;
        filename += (char)(data[pim_headers[p] + f]);
    }
    filename += ".bmp";

    Color[] palette = new Color[color_count];
    for (int i = 0; i < color_count; i++)
    {
        int alpha = ((data[palette_start + (4 * i) + 3]) * 2);
        if (alpha > 255)
            alpha = 255;
        int red = data[palette_start + (4 * i)];
        int green = data[palette_start + (4 * i) + 1];
        int blue = data[palette_start + (4 * i) + 2];

        palette[i] = Color.FromArgb(alpha, red, green, blue);
    }

    Bitmap pimbmp = new Bitmap(width, height);
    switch (bit_depth)
    {
        case 4:
            {
                int data_length = PIM_HEADER_SIZE + (color_count * 0x04) + (width * height) / 2;
                bool[] bitdata = new bool[(data_length - (pixel_start - pim_headers[p])) * 8];
                for (int i = pixel_start; i < data_length + pim_headers[p]; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        bitdata[((i - pixel_start) * 8) + j] = ((data[i] >> (7 - j)) % 2) == 1;
                    }
                }

                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        int bit_index = ((h * width) + w) * bit_depth;
                        int color_index = 0;

                        for (int i = 0; i < bit_depth; i++)
                            color_index += (int)(Math.Pow(2, bit_depth - i - 1) * Convert.ToInt32(bitdata[bit_index + i]));

                        pimbmp.SetPixel(w + (2 * ((w + 1) % 2) - 1), h, palette[color_index]);
                    }
                }
                break;
            }
        case 8:
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        int color_index = data[pixel_start + (h * width) + w];
                        pimbmp.SetPixel(w, h, palette[color_index]);
                    }
                }
                break;
            }
        case 32:
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        int red = data[pixel_start + (4 * ((h * width) + w))];
                        int green = data[pixel_start + (4 * ((h * width) + w)) + 1];
                        int blue = data[pixel_start + (4 * ((h * width) + w)) + 2];
                        int alpha = data[pixel_start + (4 * ((h * width) + w)) + 3] * 2;
                        if (alpha > 255)
                            alpha = 255;
                        pimbmp.SetPixel(w, h, Color.FromArgb(alpha, red, green, blue));
                    }
                }
                break;
            }
        default:
            {
                Console.WriteLine("Unknown Bit-Depth/Incorrect Data Type\nExiting Execution");
                return;
            }
    }

    pimbmp.Save(filename);
    if (File.Exists(filename))
        Console.WriteLine("New Bitmap Saved as " + filename);
    else
        Console.WriteLine("Could not create file");
}