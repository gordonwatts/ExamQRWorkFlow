using System;
using System.Collections.Generic;
using System.Drawing;
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
            var img = Image.FromFile("junk.png");

            var qrDecoder = new BarcodeReader();
            qrDecoder.Options.PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };
            qrDecoder.Options.TryHarder = true;

            var result = qrDecoder.Decode(img as Bitmap);
            if (result == null)
            {
                Console.WriteLine("Nothing found");
            } else
            {
                Console.WriteLine($"Got it: {result.Text}");
            }

        }
    }
}
