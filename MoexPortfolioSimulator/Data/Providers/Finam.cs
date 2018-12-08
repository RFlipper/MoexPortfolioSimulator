using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using MoexPortfolioSimulator.Helpers;

namespace MoexPortfolioSimulator.Data.Providers
{
    public class Finam
    {
        public static List<FinamSecurity> securities = new List<FinamSecurity>();
        private ILog logger => LogManager.GetLogger(GetType());
        public Dictionary<Symbol, HashSet<Quote>> loadedSecurities = new Dictionary<Symbol, HashSet<Quote>>();


        public async Task<HashSet<Quote>> LoadQuotes(Symbol symbol)
        {
            if (securities.Count == 0)
            {
                securities = await GetSecuritiesList();
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
                HashSet<Quote> quotes = await GetQuotes(symbol, marketId, em, localDateFrom, localDateTo, symbol.timeFrame);

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
                if (security.Code == $"'{symbol.Code}'" && int.Parse(security.MarketId) == marketId)
                {
                    return int.Parse(security.Id);
                }
            }
            
            throw new ApplicationException($"Can't find symbol {symbol.Code} in Finam list");
        }

        private async Task<HashSet<Quote>> GetQuotes(Symbol symbol, int marketId, int em, DateTime dateFrom, DateTime dateTo, FinamDataPeriod period)
        {
            HashSet<Quote> resultSet = new HashSet<Quote>();
            
            HashSet<Quote> quotesFromFile = GetQuotesFromFile(symbol, marketId, em, dateFrom, dateTo, period);
            resultSet.UnionWith(quotesFromFile);        

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
                if (period == FinamDataPeriod.Monthly)
                {
                    if (dateTo.Subtract(lastQuoteDate).Days < 30)
                    {
                        return resultSet;
                    }
                }

                if (period == FinamDataPeriod.Daily)
                {
                    if (dateTo.Subtract(lastQuoteDate).Days < 5)
                    {
                        return resultSet;
                    }
                }
                
                string rawDataQuotesFromServer = await GetRawDataQuotesFromServer(symbol, marketId, em, lastQuoteDate, dateTo, period);
                AppendResponseToFile(symbol, rawDataQuotesFromServer, period);
                resultSet.UnionWith(ExtractQuotes(rawDataQuotesFromServer));
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

        private async Task<string> GetRawDataQuotesFromServer(Symbol symbol, int marketId, int em, DateTime dateFrom, DateTime dateTo, FinamDataPeriod period)
        {
            string url = PrepareGetQuotesRequest(symbol.Code, marketId, em, period, dateFrom, dateTo);
            string rawDataQuotes = await RequestsHelper.SendGetRequest(url);

            logger.Debug("Response from Finam is:\n" + rawDataQuotes);
            await Task.Delay(5000);
            
            return rawDataQuotes;
        }
        
        private HashSet<Quote> GetQuotesFromFile(Symbol symbol, int marketId, int em, DateTime dateFrom, DateTime dateTo, FinamDataPeriod period)
        {
            string fileName = $"{symbol.Code}_{period}";
            
            if (!FilesHelper.IsFileExists(fileName)) return new HashSet<Quote>();
            
            HashSet<Quote> quotes = new HashSet<Quote>();
            string text = FilesHelper.ReadFromFile(fileName);
            foreach (string s in text.Split('\n'))
            {
                if (s == "")
                {
                    continue;
                }
                
                var quote = new Quote(s);
                if (quote.Date >= dateFrom.Date && quote.Date <= dateTo.Date)
                {
                    quotes.Add(quote);
                }
            }

            logger.Debug($"Loaded quotes from file: {symbol.Code} {dateFrom} - {dateTo} {period}");
            return quotes;
        }

        private static void AppendResponseToFile(Symbol symbol, string rawDataQuotes, FinamDataPeriod period)
        {
            var dataLines = rawDataQuotes.Split('\n');
            string[] s = dataLines[0].Replace("\n", "").Split(',');
            if (s.Length != 9)
            {
                throw new ApplicationException("Error " + dataLines[0]);
            }

            FilesHelper.AppendToFile($"{symbol.Code}_{period}", rawDataQuotes);
        }

        public string PrepareGetQuotesRequest(string symbolName, int marketId, int em, FinamDataPeriod period, DateTime partDateFrom, DateTime partDateTo)
        {
            return $"http://export.finam.ru/" +
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
        }

        public async Task<List<FinamSecurity>> GetSecuritiesList()
        {
            const string securitiesFileName = "Finam-securities-list";
            
            logger.Info("Get Finam Securities List");
            
            string response;
            if (FilesHelper.IsFileExists(securitiesFileName))
            {
                response = FilesHelper.ReadFromFile(securitiesFileName);
            }
            else
            {
                response = await RequestsHelper.SendGetRequest("https://www.finam.ru/cache/icharts/icharts.js");
                FilesHelper.SaveToFile(securitiesFileName, response);
            }

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