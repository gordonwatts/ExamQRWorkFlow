using ExamWorkflowLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamSplitter
{
    class Program
    {
        /// <summary>
        /// Split input PDF file by page numbers
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var inputFile = new FileInfo(args[0]);
            if (!inputFile.Exists)
            {
                throw new FileNotFoundException(inputFile.FullName);
            }

            // Build a catalog of the PDF file
            foreach (var r in new PDFScanner(inputFile).GetPagesInfo())
            {
                Console.WriteLine(r);
            }

        }
    }
}
