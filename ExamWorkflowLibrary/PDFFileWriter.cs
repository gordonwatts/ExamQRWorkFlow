using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamWorkflowLibrary
{
    /// <summary>
    /// Writing out a PDF file
    /// </summary>
    public class PDFFileWriter : IDisposable
    {
        private FileStream _binaryWriter;
        private Document _doc;
        private PdfCopy _pdfWriter;
        private FileInfo _file;

        /// <summary>
        /// Open a PDF file for output.
        /// </summary>
        /// <param name="file"></param>
        public PDFFileWriter(FileInfo file)
        {
            _file = file;
        }

        /// <summary>
        /// Create the file
        /// </summary>
        /// <remarks>
        /// This is seriously bad design - we should have this returning somethign and hide everything
        /// above. But this is just to get it going... it won't be here 6 months from now, right? :-)
        /// </remarks>
        private void CreatePDFFile()
        {
            if (_binaryWriter == null)
            {
                _binaryWriter = _file.Create();
                _doc = new iTextSharp.text.Document();
                _pdfWriter = new PdfCopy(_doc, _binaryWriter);
                _doc.Open();
            }
        }

        /// <summary>
        /// Release our resources
        /// </summary>
        public void Dispose()
        {
            if (_pdfWriter != null)
            {
                _pdfWriter.Close();
                _pdfWriter.Dispose();
                _binaryWriter.Dispose();
            }
        }

        /// <summary>
        /// Add a page from another source to this guy.
        /// </summary>
        /// <param name="scanner"></param>
        /// <param name="pnum"></param>
        public void AddPage(PDFFileHandler scanner, int pnum)
        {
            CreatePDFFile();
            scanner.AddPageTo(_pdfWriter, pnum);
        }
    }
}
