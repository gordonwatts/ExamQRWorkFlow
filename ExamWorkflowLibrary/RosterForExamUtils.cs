using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamWorkflowLibrary
{
    /// <summary>
    /// Utilities to track names and exam numbers
    /// </summary>
    public static class RosterForExamUtils
    {
        /// <summary>
        /// Name for an exam
        /// </summary>
        public struct NameInfo
        {
            public int ExamID;
            public string LastName;
            public string FirstName;
        }

        /// <summary>
        /// Return a dict of each page number in this file
        /// </summary>
        /// <returns></returns>
        public static IDictionary<int, NameInfo> LoadDBFileForScanFile(FileInfo scanFile)
        {
            // If not there, then nothing.
            var db_file = GetDBFile(scanFile);
            if (!db_file.Exists)
            {
                return new Dictionary<int, NameInfo>();
            }

            // Read it in. Regular CSV file so the user can edit if need be.
            return LoadDBFile(db_file);
        }

        /// <summary>
        /// Load a raw db file.
        /// </summary>
        /// <param name="db_file"></param>
        /// <returns></returns>
        public static IDictionary<int, NameInfo> LoadDBFile(FileInfo db_file)
        {
            return File.ReadAllLines(db_file.FullName)
                .Select(ln => ln.Split(','))
                .Select(sln => new NameInfo() { ExamID = int.Parse(sln[0]), LastName = sln[1], FirstName = sln[2] })
                .ToDictionary(i => i.ExamID);
        }

        /// <summary>
        /// Return a db file for the roster info
        /// </summary>
        /// <param name="scanFile"></param>
        /// <returns></returns>
        private static FileInfo GetDBFile (FileInfo scanFile)
        {
            return new FileInfo($"{scanFile.Directory.FullName}\\{Path.GetFileNameWithoutExtension(scanFile.FullName)}-roster-db.csv");
        }

        /// <summary>
        /// Save the DB file.
        /// </summary>
        /// <param name="scanFile"></param>
        /// <param name="dict"></param>
        public static void SaveDBFile (FileInfo scanFile, IDictionary<int, NameInfo> dict)
        {
            File.WriteAllLines(GetDBFile(scanFile).FullName,
                dict.Select(i => $"{i.Value.ExamID},{i.Value.LastName},{i.Value.FirstName}"));
        }
    }
}
