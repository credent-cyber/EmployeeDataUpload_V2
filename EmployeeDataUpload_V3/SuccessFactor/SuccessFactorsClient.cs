using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeDataUpload_V3.SuccessFactorsClient
{
    public class SuccessFactorsClient
    {
        private readonly HttpClient httpClient;
        string SFUrl = ConfigurationManager.AppSettings["SFUrl"];
        string username = ConfigurationManager.AppSettings["Username"];
        string password = ConfigurationManager.AppSettings["Password"];
        

        public SuccessFactorsClient()
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api10.successfactors.com/odata/v2/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Add Basic Authentication header
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }


        public async Task<string> GetUserIdAsync(string candidateId)
        {
            try
            {
                string requestUri = $"OnboardingCandidateInfo?$format=json&$filter=candidateId eq '{candidateId}'";
                HttpResponseMessage response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(jsonResponse);

                    // Assuming the structure is something like:
                    // {
                    //   "d": {
                    //     "results": [
                    //       {
                    //         "userId": "12345",
                    //         ...
                    //       }
                    //     ]
                    //   }
                    // }

                    var results = data["d"]["results"];
                    if (results != null && results.HasValues)
                    {
                        string userId = results[0]["userId"].ToString();
                        return userId;
                    }
                    else
                    {
                        Console.WriteLine($"No User Id found for {candidateId} Candidate Id");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }
    }
}
