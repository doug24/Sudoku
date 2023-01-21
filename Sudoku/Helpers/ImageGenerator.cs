using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorTools;
using QQWingLib;

namespace Sudoku
{
    public static class ImageGenerator
    {
        public static void CreateSectionImages(int from, int to)
        {
            IrregularLayout layout = new();
            for (int idx = from; idx < to; idx++)
            {
                layout.Layout = idx;
                Generate($"irr{idx + 1}", layout);
            }
        }

        public static void Generate(string name, IrregularLayout layout)
        {
            int imageWidth = 96;
            int imageHeight = 96;
            DrawingVisual visual = new();
            DrawingContext context = visual.RenderOpen();

            context.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 96, 96));
            context.DrawRectangle(Brushes.White, null, new Rect(1, 1, 94, 94));
            for (int section = 0; section < 9; section++)
            {
                if (brushes[section] is SolidColorBrush brush)
                {
                    Color c = brush.Color;
                    ColorHSV hsv = ColorHSV.ConvertFrom(c);
                    hsv.Saturation = .3;
                    Color c2 = hsv.ToColor();
                    SolidColorBrush b2 = new(c2);
                    b2.Freeze();

                    for (int offset = 0; offset < 9; offset++)
                    {
                        int cell = layout.SectionToCell(section, offset);
                        int row = QQWing.CellToRow(cell);
                        int col = QQWing.CellToColumn(cell);
                        int x = 3 + (col * 10);
                        int y = 3 + (row * 10);
                        context.DrawRectangle(b2, null, new Rect(x, y, 10, 10));
                    }
                }
            }

            context.Close();

            // Create the Bitmap and render the rectangle onto it.
            RenderTargetBitmap bmp = new(imageWidth, imageHeight, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);

            // Save the image to a location on the disk.
            string outputFile = @$"C:/Repos/Sudoku/Sudoku/Images/{name}.png";
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(new FileStream(outputFile, FileMode.Create));
        }

        private readonly static Brush[] brushes = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(255, 204, 178)),//hue= 20
            new SolidColorBrush(Color.FromRgb(201, 255, 255)),//hue=180
            new SolidColorBrush(Color.FromRgb(204, 178, 255)),//hue=260
            new SolidColorBrush(Color.FromRgb(221, 255, 204)),//hue=100
            new SolidColorBrush(Color.FromRgb(255, 178, 204)),//hue=340
            new SolidColorBrush(Color.FromRgb(221, 255, 204)),//hue=100
            new SolidColorBrush(Color.FromRgb(204, 178, 255)),//hue=260
            new SolidColorBrush(Color.FromRgb(201, 255, 255)),//hue=180
            new SolidColorBrush(Color.FromRgb(255, 204, 178)),//hue= 20
        };
    }
}
