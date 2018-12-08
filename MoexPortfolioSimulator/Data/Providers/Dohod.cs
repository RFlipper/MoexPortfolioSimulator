using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MoexPortfolioSimulator.Helpers;

namespace MoexPortfolioSimulator.Data.Providers
{
    public class Dohod
    {
        private static string baseUrl = "https://dohod.ru/ik/analytics/dividend";
        private static string formatDate = "dd.MM.yyyy";
        
        public static async Task<List<Dividend>> GetDividendsBySymbol(string symbolName)
        {
            var divs = new List<Dividend>();
            
            if (string.IsNullOrEmpty(symbolName))
            {
                throw new ApplicationException("Symbol name can't be empty");
            }

            string response = await RequestsHelper.SendGetRequest($"{baseUrl}/{symbolName}");
            
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
                    var date = DateTime.ParseExact(cells[0].InnerText, formatDate, CultureInfo.InvariantCulture);
                    var amount = decimal.Parse(cells[2].InnerText.Trim(), CultureInfo.InvariantCulture);
                    var dividend = new Dividend(date, amount);
                    divs.Add(dividend);
                }
            }

            return divs;
        }
    }
}