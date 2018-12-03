using System;
using System.Globalization;

namespace MoexPortfolioSimulator.Data
{
    public class Quote
    {

        public string Ticker => ticker;

        public string Period => period;

        public DateTime Date => date;

        public DateTime Time => time;

        public decimal Open => open;

        public decimal High => high;

        public decimal Low => low;

        public decimal Close => close;

        public decimal Vol => vol;

        private string ticker;
        private string period;
        private DateTime date;
        private DateTime time;
        private decimal open;
        private decimal high;
        private decimal low;
        private decimal close;
        private long vol;

        public Quote(string dataLine)
        {
            string formatDate = "yyyyMMdd";
            string formatTime = formatDate + "hhmmss";
            if (dataLine.Contains("<TICKER>"))
            {
                throw new ApplicationException("Headers line received");
            }

            string[] s = dataLine.Replace("\n", "").Split(',');
            if (s.Length != 9)
            {

                throw new ApplicationException("Error" + dataLine);
            }
            ticker = s[0];
            period = s[1];
            date = DateTime.ParseExact(s[2], formatDate, CultureInfo.InvariantCulture);
            time = DateTime.ParseExact(s[2] + s[3], formatTime, CultureInfo.InvariantCulture);
            open = decimal.Parse(s[4], CultureInfo.InvariantCulture);
            high = decimal.Parse(s[5], CultureInfo.InvariantCulture);
            low = decimal.Parse(s[6], CultureInfo.InvariantCulture);
            close = decimal.Parse(s[7], CultureInfo.InvariantCulture);
            vol = long.Parse(s[8]);
        }

        protected bool Equals(Quote other)
        {
            return string.Equals(ticker, other.ticker) && string.Equals(period, other.period) && date.Equals(other.date) && time.Equals(other.time) && open == other.open && high == other.high && low == other.low && close == other.close && vol == other.vol;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Quote) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (ticker != null ? ticker.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (period != null ? period.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ date.GetHashCode();
                hashCode = (hashCode * 397) ^ time.GetHashCode();
                hashCode = (hashCode * 397) ^ open.GetHashCode();
                hashCode = (hashCode * 397) ^ high.GetHashCode();
                hashCode = (hashCode * 397) ^ low.GetHashCode();
                hashCode = (hashCode * 397) ^ close.GetHashCode();
                hashCode = (hashCode * 397) ^ vol.GetHashCode();
                return hashCode;
            }
        }
    }
}

//<TICKER>,<PER>,<DATE>,<TIME>,<OPEN>,<HIGH>,<LOW>,<CLOSE>,<VOL>
//SBER,M,20170101,000000,173.4100000,181.6800000,163.3400000,172.2000000,988723570