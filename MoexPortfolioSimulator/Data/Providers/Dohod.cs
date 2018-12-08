using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using HtmlAgilityPack;
using log4net;
using MoexPortfolioSimulator.Helpers;

namespace MoexPortfolioSimulator.Data.Providers
{
    public class Dohod
    {
        private static  ILog logger => LogManager.GetLogger(typeof(Dohod));

        private static string baseUrl = "https://dohod.ru/ik/analytics/dividend";
        private static string formatDate = "dd.MM.yyyy";

        public static async Task<HashSet<Dividend>> GetDividendsBySymbol(Symbol symbol)
        {
            var divs = await GetDividendsByCode(symbol.Code);
            return divs;
        }
        
        public static async Task<HashSet<Dividend>> GetDividendsByCode(string code)
        {
            var divs = new HashSet<Dividend>();
            
            if (string.IsNullOrEmpty(code))
            {
                throw new ApplicationException("Symbol name can't be empty");
            }

            var divsFileName = $"Div_{code}";
            
            logger.Info($"Get Dividends for {code}");
            
            string response;
            if (FilesHelper.IsFileExists(divsFileName))
            {
                response = FilesHelper.ReadFromFile(divsFileName);
            }
            else
            {
                response = await RequestsHelper.SendGetRequest($"{baseUrl}/{code.ToLower()}");
                FilesHelper.SaveToFile(divsFileName, response);
            }
            
            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            HtmlNode htmlDivsTable = doc.GetElementbyId("leftside-col").SelectNodes("table[tr/th//text()[contains(., 'Дата закрытия реестра')]]")[0];
            HtmlNodeCollection htmlDivs = htmlDivsTable.SelectNodes("tr");

            foreach (HtmlNode htmlDiv in htmlDivs)
            {
                var cells = htmlDiv.SelectNodes("td");
                if (cells != null && cells.Count > 0)
                {
                    if (cells[0].InnerText.Contains("прогноз"))
                    {
                        continue;
                    }
                    var date = DateTime.ParseExact(cells[0].InnerText.Trim(), formatDate, CultureInfo.InvariantCulture);
                    var amount = decimal.Parse(cells[2].InnerText.Trim(), CultureInfo.InvariantCulture);
                    var dividend = new Dividend(date, amount);
                    divs.Add(dividend);
                }
            }

            return divs;
        }
    }
}