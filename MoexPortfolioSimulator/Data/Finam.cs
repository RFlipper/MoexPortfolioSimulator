using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using log4net;

namespace MoexPortfolioSimulator.Data
{
    public class Finam
    {
        private static readonly HttpClient client = new HttpClient();
        public static List<FinamSecurity> securities = new List<FinamSecurity>();
        private ILog logger => LogManager.GetLogger(GetType());
        public Dictionary<Symbol, HashSet<Quote>> loadedSecurities = new Dictionary<Symbol, HashSet<Quote>>();


        public HashSet<Quote> LoadQuotes(Symbol symbol)
        {
            if (securities.Count == 0)
            {
                securities = GetSecuritiesList();
            }

            int marketId = 1;
           
            
            var localDateFrom = symbol.InitDateFrom;
            var localDateTo = symbol.InitDateTo;
            
            for (int i = 0; i < (symbol.InitDateTo.Subtract(symbol.InitDateFrom).Days) / 365; i++)
            {
                localDateFrom = symbol.InitDateFrom.AddYears(i * 1);
                
                localDateTo = localDateFrom.AddYears(1);
                if (localDateTo > symbol.InitDateTo)
                {
                    localDateTo = symbol.InitDateTo;
                }

                if (localDateTo > DateTime.Now)
                {
                    localDateTo = DateTime.Today;
                }

                int em = GetSecurityCode(symbol, marketId);
                HashSet<Quote> quotes = GetQuotes(symbol, marketId, em, localDateFrom, localDateTo, FinamDataPeriod.Monthly);

                if (loadedSecurities.ContainsKey(symbol))
                {
                    loadedSecurities[symbol].UnionWith(quotes);
                }
                else
                {
                    loadedSecurities.Add(symbol, quotes);
                }
                
            }

            return loadedSecurities[symbol];
        }

        private static int GetSecurityCode(Symbol symbol, int marketId)
        {
            foreach (FinamSecurity security in securities)
            {
                if (security.Code == $"'{symbol.SymbolName}'" && int.Parse(security.MarketId) == marketId)
                {
                    return int.Parse(security.Id);
                }
            }
            
            throw new ApplicationException($"Can't find symbol {symbol.SymbolName} in Finam list");
        }

        private HashSet<Quote> GetQuotes(Symbol symbol, int marketId, int em, DateTime dateFrom, DateTime dateTo, FinamDataPeriod period)
        {
            HashSet<Quote> resultSet = new HashSet<Quote>();
            string rawDataQuotes;
            string rawDataQuotesFromFile = GetRawDataQuotesFromFile(symbol, marketId, em, dateFrom, dateTo, period);
            rawDataQuotes = rawDataQuotesFromFile;
            
            if (string.IsNullOrEmpty(rawDataQuotesFromFile))
            {
                string rawDataQuotesFromServer = GetRawDataQuotesFromServer(symbol, marketId, em, dateFrom, dateTo, period);
                rawDataQuotes = rawDataQuotesFromServer;
                AppendResponseToFile(symbol, rawDataQuotesFromServer, period);
            }

            resultSet.UnionWith(ExtractQuotes(rawDataQuotes));
            

            var lastQuoteDate = dateFrom;
            foreach (Quote quote in resultSet)
            {
                if (quote.Date > lastQuoteDate)
                {
                    lastQuoteDate = quote.Date;
                }
            }

            if (lastQuoteDate < dateTo)
            {
                string rawDataQuotesFromServer = GetRawDataQuotesFromServer(symbol, marketId, em, lastQuoteDate, dateTo, period);
                AppendResponseToFile(symbol, rawDataQuotesFromServer, period);
                resultSet.UnionWith(ExtractQuotes(rawDataQuotes));
            }
            
            return resultSet;
        }

        private static HashSet<Quote> ExtractQuotes(string rawDataQuotes)
        {
            HashSet<Quote> resultSet = new HashSet<Quote>();
            var data = rawDataQuotes.Replace('\r', '\n');
            var dataLines = data.Split('\n');

            foreach (string dataLine in dataLines)
            {
                if (dataLine.Contains("<TICKER>") || dataLine.Equals(""))
                {
                    continue;
                }

                resultSet.Add(new Quote(dataLine));
            }

            return resultSet;
        }

        private string GetRawDataQuotesFromServer(Symbol symbol, int marketId, int em, DateTime dateFrom, DateTime dateTo, FinamDataPeriod period)
        {
            HttpResponseMessage responseMessage = client
                .GetAsync(this.PrepareGetQuotesRequest(symbol.SymbolName, marketId, em, period, dateFrom,
                    dateTo)).Result;
            string rawDataQuotes = responseMessage.Content.ReadAsStringAsync().Result;
            logger.Debug("Response from Finam is:\n" + rawDataQuotes);
            Thread.Sleep(2000);
            
            return rawDataQuotes;
        }
        
        private string GetRawDataQuotesFromFile(Symbol symbol, int marketId, int em, DateTime dateFrom, DateTime dateTo, FinamDataPeriod period)
        {
            string fileName = $"resources\\{symbol.SymbolName}_{period}.txt";
            
            if (!File.Exists(fileName)) return null;
            
            StringBuilder resultText = new StringBuilder();
            string text = File.ReadAllText(fileName);
            foreach (string s in text.Split('\n'))
            {
                if (s == "")
                {
                    continue;
                }
                var quote = new Quote(s);
                if (quote.Date >= dateFrom.Date && quote.Date <= dateTo.Date)
                {
                    resultText.Append(s.Replace('\r', '\n'));
                }
            }

            logger.Debug("Loaded quotes from file: \n" + resultText.ToString());
            return resultText.ToString();
        }

        private static void AppendResponseToFile(Symbol symbol, string rawDataQuotes, FinamDataPeriod period)
        {
            var dataLines = rawDataQuotes.Split('\n');
            string[] s = dataLines[0].Replace("\n", "").Split(',');
            if (s.Length != 9)
            {
                throw new ApplicationException("Error " + dataLines[0]);
            }

            System.IO.Directory.CreateDirectory("resources");
            System.IO.File.AppendAllText($"resources\\{symbol.SymbolName}_{period}.txt", rawDataQuotes);
        }

        public string PrepareGetQuotesRequest(string symbolName, int marketId, int em, FinamDataPeriod period, DateTime partDateFrom, DateTime partDateTo)
        {
            string request = $"http://export.finam.ru/" +
                             $"{symbolName}_{partDateFrom.ToString("yyMMdd")}_{partDateTo.ToString("yyMMdd")}.txt?" +
                             $"market={marketId}&" +
                             $"em={em}&" +
                             $"code={symbolName}&apply=0&" +
                             $"df={partDateFrom.Day}&" +
                             $"mf={partDateFrom.Month-1}&" +
                             $"yf={partDateFrom.Year}&" +
                             $"from={partDateFrom.ToShortDateString()}&" +
                             $"dt={partDateTo.Day}&" +
                             $"mt={partDateTo.Month-1}&" +
                             $"yt={partDateTo.Year}&" +
                             $"to={partDateTo.ToShortDateString()}&" +
                             $"p={(int) period}&" +
                             $"f={symbolName}_{partDateFrom.ToString("yyMMdd")}_{partDateTo.ToString("yyMMdd")}&" +
                             "e=.txt&" +
                             $"cn={symbolName}&" +
                             $"dtf=1&" +
                             $"tmf=1&" +
                             "MSOR=1&mstime=on&mstimever=1&sep=1&sep2=1&datf=1";
            
            logger.Debug("Request to Finam is:\n" + request);
            return request;
        }

        public List<FinamSecurity> GetSecuritiesList()
        {
            logger.Info("Get Finam Securities List");
            HttpResponseMessage responseMessage = client.GetAsync("https://www.finam.ru/cache/icharts/icharts.js").Result;
            responseMessage.EnsureSuccessStatusCode();
            string response = responseMessage.Content.ReadAsStringAsync().Result;

            string[] arraySets = response.Split('=');
            string[] arrayIds = arraySets[1].Split('[')[1].Split(']')[0].Split(',');

            string names = arraySets[2].Split('[')[1].Split(']')[0];

            List<string> arrayNames = new List<string>();

            string name = "";

            for (int i = 1; i < names.Length; i++)
            {
                if ((names[i] == '\'' && i + 1 == names.Length)
                    ||
                    (names[i] == '\'' && names[i + 1] == ',' && names[i + 2] == '\''))
                {
                    arrayNames.Add(name);
                    name = "";
                    i += 2;
                }
                else
                {
                    name += names[i];
                }
            }
            
            string[] arrayCodes = arraySets[3].Split('[')[1].Split(']')[0].Split(',');
            string[] arrayMarkets = arraySets[4].Split('[')[1].Split(']')[0].Split(',');
            string[] arrayDecp = arraySets[5].Split('{')[1].Split('}')[0].Split(',');
            string[] arrayEmitentChild = arraySets[7].Split('[')[1].Split(']')[0].Split(',');
            string[] arrayEmitentUrls = arraySets[8].Split('{')[1].Split('}')[0].Split(',');

            var securitiesList = new List<FinamSecurity>();

            for (int i = 0; i < arrayIds.Length; i++)
            {
                securitiesList.Add(new FinamSecurity());
                securitiesList[i].Code = arrayCodes[i]; 
                securitiesList[i].Decp = arrayDecp[i].Split(':')[1];
                securitiesList[i].EmitentChild = arrayEmitentChild[i];
                securitiesList[i].Id = arrayIds[i];
                securitiesList[i].Name = arrayNames[i];
                securitiesList[i].Url = arrayEmitentUrls[i].Split(':')[1];

                securitiesList[i].MarketId = arrayMarkets[i];

                securitiesList[i].Market = GetMarketNameFromId(int.Parse(securitiesList[i].MarketId));
            }

            return securitiesList;
        }

        private static string GetMarketNameFromId(int marketId)
        {
            string marketName = "";
            
            switch (Convert.ToInt32(marketId))
            {
                case 200:
                    marketName = "МосБиржа топ";
                    break;
                case 1:
                    marketName = "МосБиржа акции";
                    break;
                case 14:
                    marketName = "МосБиржа фьючерсы";
                    break;
                case 41:
                    marketName = "Курс рубля";
                    break;
                case 45:
                    marketName = "МосБиржа валютный рынок";
                    break;
                case 2:
                    marketName = "МосБиржа облигации";
                    break;
                case 12:
                    marketName = "МосБиржа внесписочные облигации";
                    break;
                case 29:
                    marketName = "МосБиржа пифы";
                    break;
                case 515:
                    marketName = "Мосбиржа ETF";
                    break;
                case 8:
                    marketName = "Расписки";
                    break;
                case 519:
                    marketName = "Еврооблигации";
                    break;
                case 517:
                    marketName = "Санкт-Петербургская биржа";
                    break;
                case 6:
                    marketName = "Мировые Индексы";
                    break;
                case 24:
                    marketName = "Товары";
                    break;
                case 5:
                    marketName = "Мировые валюты";
                    break;
                case 25:
                    marketName = "Акции США(BATS)";
                    break;
                case 7:
                    marketName = "Фьючерсы США";
                    break;
                case 27:
                    marketName = "Отрасли экономики США";
                    break;
                case 26:
                    marketName = "Гособлигации США";
                    break;
                case 28:
                    marketName = "ETF";
                    break;
                case 30:
                    marketName = "Индексы мировой экономики";
                    break;
                case 91:
                    marketName = "Российские индексы";
                    break;
                case 3:
                    marketName = "РТС";
                    break;
                case 20:
                    marketName = "RTS Board";
                    break;
                case 10:
                    marketName = "РТС-GAZ";
                    break;
                case 17:
                    marketName = "ФОРТС Архив";
                    break;
                case 31:
                    marketName = "Сырье Архив";
                    break;
                case 38:
                    marketName = "RTS Standard Архив";
                    break;
                case 16:
                    marketName = "ММВБ Архив";
                    break;
                case 18:
                    marketName = "РТС Архив";
                    break;
                case 9:
                    marketName = "СПФБ Архив";
                    break;
                case 32:
                    marketName = "РТС-BOARD Архив";
                    break;
                case 39:
                    marketName = "Расписки Архив";
                    break;
                case -1:
                    marketName = "Отрасли";
                    break;
            }

            return marketName;
        }
    }

    public enum FinamDataPeriod
    {
        Monthly = 10,
        Weekly = 9,
        Daily = 8,
        H1 = 7,
        M30 = 6,
        M15 = 5,
        M10 = 4,
        M5 = 3,
        M1 = 2
    }
}