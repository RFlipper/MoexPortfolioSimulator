using System;
using System.Collections.Generic;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Portfolio
    {
        public Dictionary<Symbol, long> Storage { get; } = new Dictionary<Symbol, long>();

        public long getQuantity(Symbol symbol)
        {
            if (Storage.ContainsKey(symbol))
            {
                return Storage[symbol]; 
            }
            throw new ApplicationException($"There is not {symbol} in Portfolio");
        }

        public void Add(Symbol symbol, long quantity)
        {
            if (Storage.ContainsKey(symbol))
            {
                Storage[symbol] += quantity;
            }
            else
            {
                Storage.Add(symbol, quantity);
            }
        }
        
        public void Remove(Symbol symbol, long quantity)
        {
            if (Storage.ContainsKey(symbol))
            {
                if (Storage[symbol] == quantity)
                {
                    Storage.Remove(symbol);
                }else if (Storage[symbol] > quantity)
                {
                    Storage[symbol] -= quantity;
                }
                else
                {
                    throw new ApplicationException("Quantity to remove is greater than we have");
                }
            }
            else
            {
                throw new ApplicationException("Can't remove non-existent symbol");
            }
        }
    }
}