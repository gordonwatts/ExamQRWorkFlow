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

            // Load, if there, the exam roster id stuff
            var examRoster = RosterForExamUtils.LoadDBFileForScanFile(inputFile);

            // Build a catalog of the PDF file
            var inputPDFFile = new PDFFileHandler(inputFile);
            var pageGroups = inputPDFFile
                .GetPagesInfo()
                .Select(qrTagText => (qrTagText.pageNum, qrTagText.tagText.ParseTagString()))
                .GroupBy(tinfo => GroupID(tinfo.Item2.pageNumber));

            foreach (var g in pageGroups)
            {
                Console.WriteLine($"{g.Key}:");

                // Order the pages to make it easy to deal with
                var pages = g
                    .OrderBy(qrInfo => GetName(examRoster, qrInfo.Item2.paperID, qrInfo.Item1).last)
                    .ThenBy(qrInfo => GetName(examRoster, qrInfo.Item2.paperID, qrInfo.Item1).first)
                    .ThenBy(qrInfo => qrInfo.Item2.pageNumber);

                inputPDFFile.CopyPages(new FileInfo($"{inputFile.Directory.FullName}\\{Path.GetFileNameWithoutExtension(inputFile.Name)} - {g.Key}.pdf"),
                    pages.Select(p => p.Item1));
                //foreach (var p in pages)
                //{
                //    Console.WriteLine($"  {p.Item1}: {p.Item2.paperID} - {p.Item2.pageNumber}");
                //}

                Console.WriteLine();
            }

            // Save out the roster file
            RosterForExamUtils.SaveDBFile(inputFile, examRoster);
        }

        /// <summary>
        /// Return the name. If no name, make one up that references a page number.
        /// </summary>
        /// <param name="examRoster"></param>
        /// <param name="paperID"></param>
        /// <param name="item1"></param>
        /// <returns></returns>
        private static (string last, string first) GetName(IDictionary<int, RosterForExamUtils.NameInfo> examRoster, int paperID, int filePageNumber)
        {
            if (examRoster.TryGetValue(paperID, out RosterForExamUtils.NameInfo ni))
            {
                return (ni.LastName, ni.FirstName);
            }
            examRoster[paperID] = new RosterForExamUtils.NameInfo() { ExamID = paperID, FirstName = $"{filePageNumber}", LastName = $"{filePageNumber}" };
            return ($"{filePageNumber}", $"{filePageNumber}");
        }

        /// <summary>
        /// The groupings we will return for this exam.
        /// </summary>
        private static Dictionary<int, string> grouping = new Dictionary<int, string>()
        {
            {1, "intro" },
            {2, "intro" },
            {3, "prob1" },
            {4, "prob1" },
            {5, "prob2" },
            {6, "prob2" },
            {7, "prob3" },
            {8, "prob3" },
            {9, "prob4" },
            {10, "prob4" },
            {11, "prob5" },
            {12, "prob5" },
        };

        /// <summary>
        /// The grouping for a particular page. Allows more than one page to be grouped together.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        private static string GroupID(int pageNumber)
        {
            if (grouping.TryGetValue(pageNumber, out string groupName))
            {
                return groupName;
            }
            throw new ArgumentException($"Do not know how to group page '{pageNumber}'.");
        }
    }
}
