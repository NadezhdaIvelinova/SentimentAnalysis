using Google.Cloud.Language.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadiSentimentAnalysis
{
    public enum Sentiment
    {
        Positive,
        Neutral,
        Negative
    }

    public class SentimentAnalysisResult
    {
        public int Index { get; set; }
        public string OriginalText { get; set; }
        public AnalyzeSentimentResponse SentimentResponse { get; set; }
        public Sentiment Sentiment { get => GetSentiment(); }

        private Sentiment GetSentiment() 
        {
            double score = SentimentResponse.DocumentSentiment.Score;
            double magnitude = SentimentResponse.DocumentSentiment.Magnitude;
            double weightedScore = score switch
            {
                var score_ when score_ >= 0 => score * magnitude,
                var score_ when score_ < 0 => -Math.Abs(score * magnitude)
            };

            if (score < - 0.1)
            {
                return Sentiment.Negative;
            }
            else if (score > 0.2 )
            {
                return Sentiment.Positive;
            }
            else
            {
                return Sentiment.Neutral;
            }

            
        }
    }
}
