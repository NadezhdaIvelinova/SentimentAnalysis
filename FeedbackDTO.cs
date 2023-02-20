using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadiSentimentAnalysis
{
    public class FeedbackDTO
    {
        public string Body { get; set; }

        public static IEnumerable<FeedbackDTO> FromFile(string filePath)
        {
            using var sr = new StreamReader(filePath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                yield return new FeedbackDTO { Body = line };
            }
        }
    }
}
