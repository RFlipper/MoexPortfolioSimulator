using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MoexPortfolioSimulator.Data;
using MoexPortfolioSimulator.Data.Providers;

namespace MoexPortfolioSimulator.Strategies
{
    public class StrategiesBase
    {
        protected static async Task<Dictionary<string, Symbol>> LoadSymbols(string[] symbolsNames, DateTime dateFrom, DateTime dateTo)
        {
            var resultMap = new Dictionary<string, Symbol>();
            
            var finam = new Finam();
            
            foreach (string symbolName in symbolsNames)
            {
                var symbol = new Symbol(symbolName, dateFrom, dateTo);
                symbol.Quotes = await finam.LoadQuotes(symbol);

                resultMap.Add(symbolName, symbol);
            }

            return resultMap;
        }
    }
}