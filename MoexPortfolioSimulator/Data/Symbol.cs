using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MoexPortfolioSimulator.Data.Providers;

namespace MoexPortfolioSimulator.Data
{
    public class Symbol
    {
        private ILog logger => LogManager.GetLogger(GetType());

       
        public string Code { get; }
        public string shortName { get; }
        public DateTime InitDateTo { get; }
        public DateTime InitDateFrom { get; }
        public FinamDataPeriod timeFrame { get; }
        public int LotSize { get; }
        public HashSet<Quote> Quotes { get; set; } = new HashSet<Quote>();
        public HashSet<Dividend> Dividends { get; set; } = new HashSet<Dividend>();


        public Symbol(string symbol, DateTime dateFrom,  DateTime dateTo, FinamDataPeriod timeFrame)
        {
            this.Code = symbol;
            this.InitDateTo = dateTo;
            this.timeFrame = timeFrame;
            this.InitDateFrom = dateFrom;
        }
        
        
        /// <summary>
        /// Get Quote for particular Date. It use only Date value, e.g. "12.12.2018'
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public Quote GetDailyQuote(DateTime date)
        {
            foreach (Quote symbolQuote in Quotes)
            {
                if (symbolQuote.Date.Equals(date.Date))
                {
                    return symbolQuote;
                }
            }
            //logger.Debug($"Can't find quote for {date} {Code}");
            return null;            
        }
        
        /// <summary>
        /// Get latest available Quote for the symbol. It use only Date value, e.g. "12.12.2018'
        /// </summary>
        /// <returns></returns>
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
            return string.Equals(Code, other.Code) && InitDateTo.Equals(other.InitDateTo) && InitDateFrom.Equals(other.InitDateFrom) && LotSize == other.LotSize && Equals(Quotes, other.Quotes);
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
                int hashCode = (Code != null ? Code.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ InitDateTo.GetHashCode();
                hashCode = (hashCode * 397) ^ InitDateFrom.GetHashCode();
                hashCode = (hashCode * 397) ^ LotSize;
                hashCode = (hashCode * 397) ^ (Quotes != null ? Quotes.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Code)}: {Code}, {nameof(InitDateTo)}: {InitDateTo}, {nameof(InitDateFrom)}: {InitDateFrom}";
        }
    }
}