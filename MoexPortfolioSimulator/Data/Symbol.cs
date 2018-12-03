using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using log4net;

namespace MoexPortfolioSimulator.Data
{
    public class Symbol
    {
        private ILog logger => LogManager.GetLogger(GetType());

       
        public string SymbolName { get; }
        public DateTime InitDateTo { get; }
        public DateTime InitDateFrom { get; }
        public int LotSize { get; }
        private static readonly HttpClient client = new HttpClient();
        public HashSet<Quote> Quotes { get; set; } = new HashSet<Quote>();


        public Symbol(string symbol, DateTime dateFrom,  DateTime dateTo)
        {
            this.SymbolName = symbol;
            this.InitDateTo = dateTo;
            this.InitDateFrom = dateFrom;
        }
        
        public Quote GetDailyQuote(DateTime date)
        {
            foreach (Quote symbolQuote in Quotes)
            {
                if (symbolQuote.Date.Equals(date.Date))
                {
                    return symbolQuote;
                }
            }
            
            throw new ApplicationException($"Can't find quote for {date} {SymbolName}");
        }
        
        public Quote GetLastDailyQuote()
        {
            Quote last = Quotes.First();
            foreach (Quote symbolQuote in Quotes)
            {
                if (symbolQuote.Date > last.Date)
                {
                    last = symbolQuote;
                }
            }

            return last;
        }

        protected bool Equals(Symbol other)
        {
            return string.Equals(SymbolName, other.SymbolName) && InitDateTo.Equals(other.InitDateTo) && InitDateFrom.Equals(other.InitDateFrom) && LotSize == other.LotSize && Equals(Quotes, other.Quotes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Symbol) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (SymbolName != null ? SymbolName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ InitDateTo.GetHashCode();
                hashCode = (hashCode * 397) ^ InitDateFrom.GetHashCode();
                hashCode = (hashCode * 397) ^ LotSize;
                hashCode = (hashCode * 397) ^ (Quotes != null ? Quotes.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(SymbolName)}: {SymbolName}, {nameof(InitDateTo)}: {InitDateTo}, {nameof(InitDateFrom)}: {InitDateFrom}";
        }
    }
}