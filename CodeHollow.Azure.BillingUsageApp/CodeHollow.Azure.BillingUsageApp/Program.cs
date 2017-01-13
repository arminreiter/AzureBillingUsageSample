using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CodeHollow.Azure.BillingUsageApp
{
    class Program
    {
        #region settings

        // Azure AD Settings
        static readonly string TENANT = ConfigurationManager.AppSettings["Tenant"];
        static readonly string CLIENTID = ConfigurationManager.AppSettings["ClientId"];
        static readonly string CLIENTSECRET = ConfigurationManager.AppSettings["ClientSecret"];
        static readonly string SUBSCRIPTIONID = ConfigurationManager.AppSettings["SubscriptionId"];
        static readonly string REDIRECTURL = ConfigurationManager.AppSettings["RedirectUrl"];

        static readonly string SERVICEURL = "https://login.microsoftonline.com";
        static readonly string RESOURCE = "https://management.azure.com/";

        // API Settings
        static readonly string APIVERSION = "2015-06-01-preview";
        static readonly string AGGREGATIONGRANULARITY = "Daily"; // Daily or Hourly
        static readonly bool SHOWDETAILS = true;

        // Application settings
        static readonly string CSVFILEPATH = ConfigurationManager.AppSettings["CsvFilePath"];

        #endregion
        
        static void Main(string[] args)
        {
            Console.WriteLine("This app will return the azure usage date for a specific period.");

            DateTime startDate = ReadDate("Please enter the start date:");
            DateTime endDate = ReadDate("Please enter the end date:");
            if(startDate > endDate)
            {
                Console.WriteLine("Start date must be before the end date! Press key to exit");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Get OAuth token...");
            var token = AzureAuthenticationHelper.GetOAuthTokenFromAAD(SERVICEURL, TENANT, RESOURCE, REDIRECTURL, CLIENTID, CLIENTSECRET);

            Console.WriteLine("Token received, read usage data...");
            var usageData = GetUsageData(token, startDate, endDate);
            
            if (usageData == null)
            {
                Console.WriteLine("Received data is empty, press key to exit");
                Console.Read();
                return;
            }

            Console.WriteLine("Data received and parsed! Create csv file...");

            string csv = CreateCsv(usageData);
            
            System.IO.File.WriteAllText(CSVFILEPATH, csv, Encoding.UTF8);

            Console.WriteLine("CSV file successfully created. Press key to exit");
            Console.Read();
        }
        
        public static UsageData GetUsageData(string token, DateTime startDate, DateTime endDate)
        { 
            DateTimeOffset startTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
            DateTimeOffset endTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0, DateTimeKind.Utc);


            string st = WebUtility.UrlEncode(startTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            string et = WebUtility.UrlEncode(endTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));
            string url = $"https://management.azure.com/subscriptions/{SUBSCRIPTIONID}/providers/Microsoft.Commerce/UsageAggregates?api-version={APIVERSION}&reportedStartTime={st}&reportedEndTime={et}&aggregationGranularity={AGGREGATIONGRANULARITY}&showDetails={SHOWDETAILS.ToString().ToLower()}";
            
            string data = GetData(url, token);
            if(String.IsNullOrEmpty(data))
                return null;

            var usageData = JsonConvert.DeserializeObject<UsageData>(data);

            // read data from the usagedata api as long as the continuationtoken is set.
            // usage data api returns 1000 values per api call, to receive all values,  
            // we have to call the url stored in nextLink property.
            while(!String.IsNullOrEmpty(usageData.NextLink))
            {
                string next = GetData(usageData.NextLink, token);
                var nextUsageData = JsonConvert.DeserializeObject<UsageData>(next);
                usageData.Value.AddRange(nextUsageData.Value);
                usageData.NextLink = nextUsageData.NextLink;
            }

            return usageData;
        }

        /// <summary>
        /// reads data from a url including the oauth token.
        /// </summary>
        /// <param name="url">service url</param>
        /// <param name="token">oauth token</param>
        /// <returns></returns>
        public static string GetData(string url, string token)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = client.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("An error occurred! That's what I got:");
                Console.WriteLine(response.ToString());

                var x = response.Content.ReadAsStringAsync();
                x.Wait();
                Console.WriteLine("Content: " + x.Result);
                return string.Empty;
            }

            var readTask = response.Content.ReadAsStringAsync();
            readTask.Wait();
            return readTask.Result;
        }

        /// <summary>
        /// creates a csv file from the usage data
        /// </summary>
        /// <param name="data">data from the usage api</param>
        /// <returns>csv file as string</returns>
        public static string CreateCsv(UsageData data)
        {
            StringBuilder sb = new StringBuilder();

            string[] columns = new string[] 
            {
                "id", "name", "type",
                "properties/subscriptionId", "properties/usageStartTime", "properties/usageEndTime",
                "properties/meterName", "properties/meterCategory", "properties/unit",
                "properties/instanceData", "properties/meterId", "properties/MeterRegion",
                "properties/quantity", "properties/infoFields"
            };

            string header = string.Join(";", columns);
            sb.AppendLine(header);
            
            data.Value.ForEach(x =>
            {
                string[] values = new string[]
                {
                    x.Id, x.Name, x.Type,
                    x.Properties.SubscriptionId, x.Properties.UsageStartTime, x.Properties.UsageEndTime,
                    x.Properties.MeterName, x.Properties.MeterCategory,x.Properties.Unit,
                    x.Properties.InstanceDataRaw, x.Properties.MeterId, x.Properties.MeterRegion,
                    x.Properties.Quantity.ToString(), JsonConvert.SerializeObject(x.Properties.InfoFields)
                };

                sb.AppendLine(string.Join(";", values));
            });

            return sb.ToString();
        }

        /// <summary>
        /// reads the date from the console as long as it is a date
        /// </summary>
        /// <param name="promptText"></param>
        /// <returns></returns>
        private static DateTime ReadDate(string promptText)
        {
            Console.WriteLine(promptText);
            string dateString;
            DateTime returnDate;
            bool ok = true;

            do
            {
                if (!ok)
                    Console.WriteLine("Entered string is not a valid dateime");
                dateString = Console.ReadLine();
                ok = DateTime.TryParse(dateString, out returnDate);
            }
            while (!ok);

            return returnDate;
        }
    }
}
