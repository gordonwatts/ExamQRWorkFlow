using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;

namespace ExamWorkflowLibrary
{
    /// <summary>
    /// Low level code to extract information from PDF file.
    /// </summary>
    public class PDFScanner : IDisposable
    {
        private PdfReader _reader;

        public PDFScanner(FileInfo file)
        {
            _reader = new PdfReader(file.FullName);
        }

        /// <summary>
        /// Clean up when we are done.
        /// </summary>
        public void Dispose()
        {
            _reader.Dispose();
        }

        public IEnumerable<string> GetPagesInfo()
        {
            var qrDecoder = new BarcodeReader();
            qrDecoder.Options.PossibleFormats = new List<BarcodeFormat>() { BarcodeFormat.QR_CODE };
            qrDecoder.Options.TryHarder = true;
            for (int i = 1; i <= _reader.NumberOfPages; i++)
            {
                // Get the first image from the page. We'll just assume for now.
                var pageDict = _reader.GetPageN(i);
                var pageImage = GetImagesFromPdfDict(pageDict)
                    .Select(img => img as Bitmap)
                    .Where(img => img != null).First();

                pageImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                pageImage = CropIt(500, 500, pageImage);
                pageImage = ConvertToBW(pageImage);

                // Scan the QR code. If we can't find it, apply successivly harder and harder
                // median filters. We do this progresively because the filter is very expensive
                // (partly b.c. I'm not doing it efficiently, I suppose).
                var r = Enumerable.Range(0, 3)
                    .Select(m => m * 2)
                    .Select(m => m == 0 ? pageImage : MedianFilter(pageImage, m))
                    .Select(img => qrDecoder.Decode(img))
                    .Where(code => code != null)
                    .FirstOrDefault();

                var txt = r == null ? "" : r.Text;
                yield return $"{txt}";
            }
        }

        /// <summary>
        /// Return a croped image from a given image.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="pageImage"></param>
        /// <returns></returns>
        private static Bitmap CropIt(int width, int height, Bitmap pageImage)
        {
            var newImage = new Bitmap(width, height);
            using (var gr = Graphics.FromImage(newImage))
            {
                gr.DrawImage(pageImage, new Rectangle(0, 0, width, height), new Rectangle(0, pageImage.Height-height, width, height), GraphicsUnit.Pixel);
            }
            return newImage;
        }

        /// <summary>
        /// Apply a threshold to see if we can convert greyscale to B&W.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://stackoverflow.com/questions/5963380/unsafe-image-noise-removal-in-c-sharp-error-bitmap-region-is-already-locked
        /// Could probably use that to create a much more efficient version of this.
        /// </remarks>
        private static Bitmap ConvertToBW(Bitmap source)
        {
            var result = new Bitmap(source.Width, source.Height);

            for (int i_x = 0; i_x < source.Width; i_x++)
            {
                for (int i_y = 0; i_y < source.Height; i_y++)
                {
                    var p = source.GetPixel(i_x, i_y);
                    int rgb = p.R + p.G + p.B;
                    var color = rgb > 710
                        ? Color.White
                        : Color.Black;
                    result.SetPixel(i_x, i_y, color);
                }
            }

            return result;
        }

        /// <summary>
        /// Get back images from the PDF file page.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        private IList<Image> GetImagesFromPdfDict(PdfDictionary dict)
        {
            var images = new List<System.Drawing.Image>();
            var res = (PdfDictionary)(PdfReader.GetPdfObject(dict.Get(PdfName.RESOURCES)));
            var xobj = (PdfDictionary)(PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT)));

            if (xobj != null)
            {
                foreach (PdfName name in xobj.Keys)
                {
                    PdfObject obj = xobj.Get(name);
                    if (obj.IsIndirect())
                    {
                        PdfDictionary tg = (PdfDictionary)(PdfReader.GetPdfObject(obj));
                        PdfName subtype = (PdfName)(PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE)));
                        if (PdfName.IMAGE.Equals(subtype))
                        {
                            int xrefIdx = ((PRIndirectReference)obj).Number;
                            PdfObject pdfObj = _reader.GetPdfObject(xrefIdx);
                            PdfStream str = (PdfStream)(pdfObj);

                            iTextSharp.text.pdf.parser.PdfImageObject pdfImage =
                                new iTextSharp.text.pdf.parser.PdfImageObject((PRStream)str);
                            System.Drawing.Image img = pdfImage.GetDrawingImage();

                            images.Add(img);
                        }
                        else if (PdfName.FORM.Equals(subtype) || PdfName.GROUP.Equals(subtype))
                        {
                            images.AddRange(GetImagesFromPdfDict(tg));
                        }
                    }
                }
            }

            return images;
        }

        public static Bitmap MedianFilter(Bitmap Image, int Size)
        {
            Bitmap TempBitmap = Image;
            Bitmap NewBitmap = new Bitmap(TempBitmap.Width, TempBitmap.Height);
            Graphics NewGraphics = Graphics.FromImage(NewBitmap);
            NewGraphics.DrawImage(TempBitmap, new Rectangle(0, 0, TempBitmap.Width, TempBitmap.Height), new Rectangle(0, 0, TempBitmap.Width, TempBitmap.Height), GraphicsUnit.Pixel);
            NewGraphics.Dispose();
            Random TempRandom = new Random();
            int ApetureMin = -(Size / 2);
            int ApetureMax = (Size / 2);
            for (int x = 0; x < NewBitmap.Width; ++x)
            {
                for (int y = 0; y < NewBitmap.Height; ++y)
                {
                    var RValues = new List<int>();
                    var GValues = new List<int>();
                    var BValues = new List<int>();
                    for (int x2 = ApetureMin; x2 < ApetureMax; ++x2)
                    {
                        int TempX = x + x2;
                        if (TempX >= 0 && TempX < NewBitmap.Width)
                        {
                            for (int y2 = ApetureMin; y2 < ApetureMax; ++y2)
                            {
                                int TempY = y + y2;
                                if (TempY >= 0 && TempY < NewBitmap.Height)
                                {
                                    Color TempColor = TempBitmap.GetPixel(TempX, TempY);
                                    RValues.Add(TempColor.R);
                                    GValues.Add(TempColor.G);
                                    BValues.Add(TempColor.B);
                                }
                            }
                        }
                    }
                    RValues.Sort();
                    GValues.Sort();
                    BValues.Sort();
                    Color MedianPixel = Color.FromArgb(RValues[RValues.Count / 2],
                        GValues[GValues.Count / 2],
                        BValues[BValues.Count / 2]);
                    NewBitmap.SetPixel(x, y, MedianPixel);
                }
            }
            return NewBitmap;
        }
    }
}
