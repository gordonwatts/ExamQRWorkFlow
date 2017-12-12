using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace TestReadImage
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load test image
            var img = Image.FromFile("junk.png") as Bitmap;

            var qrDecoder = new BarcodeReader();
            qrDecoder.Options.PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };
            qrDecoder.Options.TryHarder = true;

            // Could scaling it down make a difference?
            //img = ScaleImage(0.25f, img);

            // How about making it *really* black and white?
            //img = ConvertToBW(img);

            // Crop from bottom?
            //img = CropFromBottom(40, img);

            // Cropping the lower faint line off it?
            //img.Save("newjunk.png", ImageFormat.Png);

            var result = qrDecoder.Decode(img);
            if (result == null)
            {
                Console.WriteLine("Nothing found");
            } else
            {
                Console.WriteLine($"Got it: {result.Text}");
            }

        }

        /// <summary>
        /// Scale an image by a certain fraction.
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        static Bitmap ScaleImage(float fraction, Bitmap source)
        {
            int newWidth = (int) (source.Width * fraction);
            int newHeight = (int)(source.Height * fraction);

            var result = new Bitmap(newWidth, newHeight);

            using (var gr = Graphics.FromImage(result))
            {
                gr.DrawImage(source, new Rectangle(0, 0, newWidth, newHeight));
            }

            return result;
        }

        /// <summary>
        /// Apply a threshold to see if we can convert greyscale to B&W.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static Bitmap ConvertToBW(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height);

            for (int i_x = 0; i_x < source.Width; i_x++)
            {
                for (int i_y = 0; i_y < source.Height; i_y++)
                {
                    var p = source.GetPixel(i_x, i_y);
                    int rgb = p.R + p.G + p.B;
                    var color = rgb > 600
                        ? Color.White
                        : Color.Black;
                    result.SetPixel(i_x, i_y, color);
                }
            }

            return result;
        }

        /// <summary>
        /// Crop part of the image off the bottom.
        /// </summary>
        /// <param name="height"></param>
        /// <param name="pageImage"></param>
        /// <returns></returns>
        static Bitmap CropFromBottom(int height, Bitmap pageImage)
        {
            var newImage = new Bitmap(pageImage.Width, pageImage.Height - height);
            using (var gr = Graphics.FromImage(newImage))
            {
                gr.DrawImage(pageImage, new Rectangle(0, 0, pageImage.Width, pageImage.Height - height),
                    new Rectangle(0, 0, pageImage.Width, pageImage.Height - height),
                    GraphicsUnit.Pixel);
            }
            return newImage;
        }
    }
}
