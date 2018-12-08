using System.Net.Http;
using System.Threading.Tasks;

namespace MoexPortfolioSimulator.Helpers
{
    public class RequestsHelper
    {
        private static readonly HttpClient Client = new HttpClient();
        
        public static async Task<string> SendGetRequest(string url)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Client.SendAsync(msg);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }
    }
}