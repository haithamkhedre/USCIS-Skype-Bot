using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace USCIS
{
    public static class UscisService
    {
        private const string UscisCheckUrl = "https://egov.uscis.gov/casestatus/mycasestatus.do";
        private const string statusPattern = "<div class=\"current-status-sec\">((.|\\n)*?)<\\/div>";
        private const string summaryPattern = "<div class=\"rows text-center\">((.|\\n)*?)<\\/div>";

        public static bool GetCaseStatus(string caseNumber, out string status , out string summary)
        {
            try
            {
                var webClient = new WebClient();
                var collection = new NameValueCollection
                {
                    {"appReceiptNum", caseNumber},
                    {"initCaseSearch", "CHECK STATUS"}
                };

                webClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                var response = webClient.UploadValues(UscisCheckUrl, collection);
                var html = Encoding.UTF8.GetString(response);
                var match = Regex.Matches(html, statusPattern);
                var group = match[0].Groups[1];
                status = Regex.Replace(group.Value, "<strong>.*?<\\/strong>", "");
                status = Regex.Replace(status, "<span.*?<\\/span>", "");
                status = status.Trim();

                match = Regex.Matches(html, summaryPattern);
                group = match[0].Groups[1];
                summary = Regex.Replace(group.Value, "<.*?>", "");
                summary = Regex.Replace(summary, @"\t|\r|\n", "");
                summary = summary.Split(new string[] { "   " },StringSplitOptions.RemoveEmptyEntries)[1].Trim();

                return true;
            }
            catch (WebException)
            {
                status = "";
                summary = "Sorry, we can't connect to USCIS server. Please try again later";
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                status = "";
                summary =
                    "Sorry, it seems, that USCIS doesn't know your case number. Please check your case number and then try again";
                return false;
            }
            catch (Exception)
            {
                status = "";
                summary = "Sorry, some problem was encountered. Please try again later";
                return false;
            }
        }
    }
}