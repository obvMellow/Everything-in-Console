using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

public class Program
{
    #region DllImports

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    #endregion DllImports

    private const int DELAY = 10;

    private const string brightnessMap = @" .'`^,:;Il!i><~+_-?][}{1)(|\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
    private const string path = "debug";

    public static void Main()
    {
        // Print current window titles.
        Console.WriteLine("========================================Window Titles========================================");
        EnumWindows(new EnumWindowsProc(EnumWindowCallback), IntPtr.Zero);
        Console.WriteLine("========================================Window Titles========================================");

        WriteColored("[INFO] Cleaning up...\n", ConsoleColor.Yellow, new Vector2(1, 4));

        if (Directory.Exists(path))
            Directory.Delete(path, true);

#if DEBUG
        Directory.CreateDirectory(path);
        Directory.SetCurrentDirectory(path);
#endif

        string windowTitle = null;

        while (string.IsNullOrEmpty(windowTitle))
        {
            WriteColored("[INPUT] Please enter the title of the program you want to capture: ", ConsoleColor.Cyan, new Vector2(1, 5));
            windowTitle = Console.ReadLine();
        }

        float sizeMultiplier = 1.0f;
        string sizeMultiplierStr = "";

        do
        {
            WriteColored("[INPUT] Please enter a size multiplier. Default value is 1.0 (Larger values than 1.0 may result in unstable outputs): ",
                ConsoleColor.Cyan,
                new Vector2(1, 5));
            sizeMultiplierStr = Console.ReadLine();

            if (string.IsNullOrEmpty(sizeMultiplierStr))
                break;
        } while (!float.TryParse(sizeMultiplierStr, out sizeMultiplier));

        Console.Clear();

        IntPtr hWnd = FindWindow(null, windowTitle);

        if (hWnd == IntPtr.Zero)
        {
            WriteColored($"[ERROR]Windows with title \"{windowTitle}\" not found!\n", ConsoleColor.Red, new Vector2(1, 5));
            Console.ReadKey(true);
            return;
        }

        int count = 0;
        Console.CursorVisible = false;
        while (true)
        {
            // Check if the window is still present
            if (!IsWindow(hWnd))
                break;

            // Set the window to the foreground
            SetForegroundWindow(hWnd);

            // Capture the window image
            Rectangle rect = new Rectangle(0, 0, 0, 0);
            GetWindowRect(hWnd, ref rect);
            using (Bitmap bitmap = new Bitmap(rect.Width, rect.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    IntPtr hdc = graphics.GetHdc();
                    PrintWindow(hWnd, hdc, 0);
                    graphics.ReleaseHdc(hdc);
                }

                string frame = PrintBitmap(bitmap, sizeMultiplier, count);
                string[] frameRows = frame.Split('\n');
                Console.SetBufferSize(
                    frameRows[1].Length < Console.BufferWidth ? Console.BufferWidth : frameRows[1].Length,
                    frameRows.Length < Console.BufferHeight ? Console.BufferHeight : frameRows.Length);

                Console.SetCursorPosition(0, 0);
                // Console.Clear();
                Console.WriteLine(frame);

#if DEBUG
                using (StreamWriter streamWriter = File.CreateText($"ascii-image{count}.txt"))
                    streamWriter.WriteLine(frame);
#endif
            }
            count++;

            // Wait for a short time before trying again
            Thread.Sleep(DELAY);
        }
    }

    /// <summary>
    /// Writes the value to the standart output in the specified color.
    /// </summary>
    /// <param name="value">Value to be printed</param>
    /// <param name="color">Color of the value</param>
    /// <param name="index">All inclusive index of the color</param>
    public static void WriteColored(string value, ConsoleColor color, Vector2 index)
    {
        // Save the original color
        ConsoleColor originalColor = Console.ForegroundColor;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (index.X == i)
                Console.ForegroundColor = color;

            Console.Write(c);

            if (index.Y == i)
                Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Returns an ASCII string of the given bitmap.
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static string PrintBitmap(Bitmap bitmap, float sizeMultiplier, int count)
    {
        var originalImage = bitmap;

        int windowWidth = Console.WindowWidth - 1;
        int windowHeight = Console.WindowHeight - 2;

        float imageAspectRatio = originalImage.Width / (float)originalImage.Height;
        float consoleAspectRatio = (windowWidth) / ((float)windowHeight);

        int newWidth;
        int newHeight;
        if (imageAspectRatio > consoleAspectRatio)
        {
            // Image is wider than console, so scale based on width
            newWidth = (int)(windowWidth * sizeMultiplier);
            newHeight = (int)(windowWidth / imageAspectRatio * sizeMultiplier);
        }
        else
        {
            // Image is taller than console, so scale based on height
            newWidth = (int)(windowHeight * imageAspectRatio * sizeMultiplier);
            newHeight = (int)(windowHeight * sizeMultiplier);
        }

        Bitmap scaledImage = new Bitmap(originalImage, new Size(newWidth, newHeight));

        float strecthFactor = 2f;

        Bitmap stretchedImage = new Bitmap((int)(scaledImage.Width * strecthFactor), (int)(scaledImage.Height * strecthFactor));
        using (Graphics g = Graphics.FromImage(stretchedImage))
        {
            g.ScaleTransform(strecthFactor, 1.0f);
            g.DrawImage(scaledImage, 0, 0);
        }

#if DEBUG
        stretchedImage.Save($"ascii-output{count}.bmp");
#endif

        StringBuilder frameBuilder = new StringBuilder();
        for (int y = 0; y < stretchedImage.Height / 2; y++)
        {
            for (int x = 0; x < stretchedImage.Width; x++)
            {
                int bIndex = (int)(stretchedImage.GetPixel(x, y).GetBrightness() * brightnessMap.Length);

                if (bIndex < 0)
                {
                    bIndex = 0;
                }
                else if (bIndex >= brightnessMap.Length)
                {
                    bIndex = brightnessMap.Length - 1;
                }

                frameBuilder.Append(brightnessMap[bIndex]);
            }
            frameBuilder.AppendLine();
        }

        return frameBuilder.ToString();
    }

    /// <summary>
    /// Debug function for printing processes.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="lParam"></param>
    /// <returns></returns>
    private static bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
    {
        const int MaxTitleLength = 260;

        StringBuilder titleBuilder = new StringBuilder(MaxTitleLength);
        GetWindowText(hWnd, titleBuilder, MaxTitleLength);

        string title = titleBuilder.ToString();
        if (!string.IsNullOrEmpty(title))
        {
            Console.WriteLine($"    {title}");
        }

        return true;
    }
}