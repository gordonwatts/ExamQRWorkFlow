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
                pageImage = CropIt(400, 400, pageImage);
                pageImage.Save("junk.png", ImageFormat.Png);

                // Now get the QR Code scanner
                var r = qrDecoder.Decode(pageImage);
                yield return $"{i} - hi";
            }
        }

        /// <summary>
        /// Return a croped image from a given image.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="pageImage"></param>
        /// <returns></returns>
        private Bitmap CropIt(int width, int height, Bitmap pageImage)
        {
            var newImage = new Bitmap(width, height);
            using (var gr = Graphics.FromImage(newImage))
            {
                gr.DrawImage(pageImage, new Rectangle(0, 0, width, height), new Rectangle(0, pageImage.Height-height, width, height), GraphicsUnit.Pixel);
            }
            return newImage;
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
    }
}
