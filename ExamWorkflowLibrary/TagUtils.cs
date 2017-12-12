using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamWorkflowLibrary
{
    public static class TagUtils
    {
        public class TagInfo
        {
            public string classInfo;
            public string scanID;
            public int paperID;
            public int pageNumber;
        }

        /// <summary>
        /// Parse a standard tag string
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TagInfo ParseTagString (this string source)
        {
            var split = source.Split('/');
            if (split.Length != 4)
            {
                throw new InvalidOperationException($"Tag '{source}' does not have the proper format. Bail!");
            }

            return new TagInfo()
            {
                classInfo = split[0],
                scanID = split[1],
                paperID = int.Parse(split[2]),
                pageNumber = int.Parse(split[3])
            };
        }
    }
}
