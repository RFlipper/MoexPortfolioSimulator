using System.Net.Http;
using System.Threading.Tasks;
using log4net;

namespace MoexPortfolioSimulator.Helpers
{
    public class RequestsHelper
    {
        private static ILog logger => LogManager.GetLogger(typeof(RequestsHelper));

        private static readonly HttpClient Client = new HttpClient();
        
        public static async Task<string> SendGetRequest(string url)
        {
            logger.Debug("Send Get request: " + url);
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Client.SendAsync(msg);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }
    }
}