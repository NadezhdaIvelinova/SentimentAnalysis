using Google.Cloud.Language.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NadiSentimentAnalysis
{
    internal class Program
    {
        const int GoogleCloudNLPRequestQuotaPerMinute = 600;

        static async Task Main(string[] args)
        {
            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            var config = new ConfigurationBuilder()
                .SetBasePath(appPath)
                .AddJsonFile("appsettings.json")
            .Build();

            var logger = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            })
            .CreateLogger<Program>();


            var configDataFilePath = config.GetValue<string>("FeedbackDataFile");
            var feedbackDtos = FeedbackDTO.FromFile(configDataFilePath).ToList();

            var batches = SplitRequestsIntoBatches(feedbackDtos, GoogleCloudNLPRequestQuotaPerMinute);

            var client = new LanguageServiceClientBuilder
            {
                CredentialsPath = config.GetValue<string>("GoogleCloudCredsFile")
            }
            .Build();


            var results = new List<SentimentAnalysisResult>();

            foreach ((var batch, var index) in batches.Select((batch, index) => (batch, index)))
            {
                int batchStartIndex = index * GoogleCloudNLPRequestQuotaPerMinute;
                int batchEndIndex = Math.Min(feedbackDtos.Count, batchStartIndex + GoogleCloudNLPRequestQuotaPerMinute);

                logger.LogInformation($"Calculating rows {batchStartIndex}-{batchEndIndex}/{feedbackDtos.Count}");

                var resultTasks = feedbackDtos.Select(async (dto, index) =>
                {
                    var doc = Document.FromPlainText(dto.Body);

                    try
                    {
                        var response = await client.AnalyzeSentimentAsync(doc);

                        return new SentimentAnalysisResult
                        {
                            Index = index,
                            OriginalText = dto.Body,
                            SentimentResponse = response
                        };
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error while analysing request: {dto.Body}", ex);
                        throw;
                    }
                });

                var batchResults = await Task.WhenAll(resultTasks);
                results.AddRange(batchResults);

                if (index < batches.Count - 1)
                { 
                    logger.LogInformation("Waiting one minute to cool down for the next batch...");
                    await Task.Delay(1000 * 60);
                }
            }


            foreach (var result in results)
            {
                Console.WriteLine($"{result.Index}: sentiment:{result.Sentiment} s:{result.SentimentResponse.DocumentSentiment.Score}, m: {result.SentimentResponse.DocumentSentiment.Magnitude}; {result.OriginalText}");
            }
        }

        private static List<IEnumerable<FeedbackDTO>> SplitRequestsIntoBatches(List<FeedbackDTO> allRequests, int batchSize)
        {
            var result = new List<IEnumerable<FeedbackDTO>>();

            bool shouldContinue = true;
            int currentBatchIndex = 0;
            while (shouldContinue)
            {
                int lastIndexOfCurrentBatch = currentBatchIndex * batchSize;
                var currentBatch = allRequests.Skip(lastIndexOfCurrentBatch).Take(batchSize);
                
                if(currentBatch.Any())
                    result.Add(currentBatch);

                if (lastIndexOfCurrentBatch >= allRequests.Count)
                    break;

                currentBatchIndex++;
            }

            return result;
        }
    }
}