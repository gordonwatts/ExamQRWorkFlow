using ExamWorkflowLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamCombiner
{
    class Program
    {
        /// <summary>
        /// Given a set of PDF files, scan them, extract all pages, and put them into individual exams by
        /// exam ID number
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var sourceDirectory = new DirectoryInfo(args[0]);
            if (!sourceDirectory.Exists)
            {
                throw new DirectoryNotFoundException($"Can't find ${args[0]}");
            }

            var rosterDirectory = new DirectoryInfo(args[1]);
            if (!rosterDirectory.Exists)
            {
                throw new DirectoryNotFoundException($"Can't find roster directory ${args[1]}");
            }

            // Load in the roster.
            var roster = rosterDirectory
                .EnumerateFiles("*-roster-db.csv")
                .SelectMany(f => RosterForExamUtils.LoadDBFile(f))
                .ToDictionary(k => k.Key, k => k.Value);

            // for all PDF files in that directory, get a scan of the file, page number, etc.
            var allpages = sourceDirectory.GetFiles("*.pdf")
                .Select(pdf => new PDFFileHandler(pdf))
                .SelectMany(pdfScanner => pdfScanner.GetPagesInfo().Select(pg => (scanner: pdfScanner, pnum: pg.pageNum, tag: pg.tagText.ParseTagString())));

            // Organize them by exam number and pages in that exam.
            var byexam = allpages
                .GroupBy(e => e.tag.paperID);

            // Prepare the directory for output.
            var outputDir = new DirectoryInfo($"{sourceDirectory.FullName}\\Collated");
            if (!outputDir.Exists)
            {
                outputDir.Create();
                outputDir.Refresh();
            }

            // Loop over every exam, and write them out.
            foreach (var exam in byexam)
            {
                var exam_name = roster.ContainsKey(exam.Key)
                    ? $"{roster[exam.Key].LastName}, {roster[exam.Key].FirstName}"
                    : exam.Key.ToString();
                Console.WriteLine(exam_name);

                using (var pdf = new PDFFileWriter(new FileInfo($"{outputDir.FullName}\\{exam_name}.pdf")))
                {
                    var orderedPages = exam.OrderBy(pgs => pgs.tag.pageNumber);
                    foreach (var p in orderedPages)
                    {
                        pdf.AddPage(p.scanner, p.pnum);
                    }

                    // Loook for missing pages
                    var pageNumbers = orderedPages.Select(p => p.tag.pageNumber).ToArray();
                    var maxPage = pageNumbers.Max();
                    var missingPages = Enumerable.Range(1, maxPage)
                        .Where(n => !pageNumbers.Contains(n))
                        .ToArray();

                    if (missingPages.Length > 0)
                    {
                        var l = string.Join(", ", missingPages.Select(i => i.ToString()));
                        Console.WriteLine($"  Missing pages: {l}");
                    }
                }
            }
        }
    }
}
