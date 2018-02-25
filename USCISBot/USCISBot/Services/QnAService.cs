using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;

namespace USCISBot.Services
{
    public class QnAService
    {
        string responseString = string.Empty;
        
        static string knowledgebaseId = ConfigurationManager.AppSettings["QnAAppId"]; // Use knowledge base id created.
        static string qnamakerSubscriptionKey = ConfigurationManager.AppSettings["QnASecrete"]; //Use subscription key assigned to you.

        public static string GetAnswer(string question)
        {
            //Build the URI
            Uri qnamakerUriBase = new Uri("https://westus.api.cognitive.microsoft.com/qnamaker/v1.0");
            var builder = new UriBuilder($"{qnamakerUriBase}/knowledgebases/{knowledgebaseId}/generateAnswer");
            string responseString;
            //Add the question as part of the body
            string postBody = $"{{\"question\": \"{question}\"}}";

            //Send the POST request
            using (WebClient client = new WebClient())
            {
                //Set the encoding to UTF8
                client.Encoding = System.Text.Encoding.UTF8;

                //Add the subscription key header
                client.Headers.Add("Ocp-Apim-Subscription-Key", qnamakerSubscriptionKey);
                client.Headers.Add("Content-Type", "application/json");
                responseString = client.UploadString(builder.Uri, postBody);
            }

            QnAMakerResult response;
            try
            {
                response = JsonConvert.DeserializeObject<QnAMakerResult>(responseString);
            }
            catch
            {
                throw new Exception("Unable to deserialize QnA Maker response string.");
            }

            return response.Answer;
        }
    }

    internal class QnAMakerResult
    {
        /// <summary>
        /// The top answer found in the QnA Service.
        /// </summary>
        [JsonProperty(PropertyName = "answer")]
        public string Answer { get; set; }

        /// <summary>
        /// The score in range [0, 100] corresponding to the top answer found in the QnA    Service.
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public double Score { get; set; }
    }
}