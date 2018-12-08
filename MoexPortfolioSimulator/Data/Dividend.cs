using System;

namespace MoexPortfolioSimulator.Data
{
    public class Dividend
    {
        public Dividend(DateTime date, decimal amount)
        {
            Date = date;
            Amount = amount;
        }

        public DateTime Date { get; }
        public decimal Amount { get; }


        protected bool Equals(Dividend other)
        {
            return Date.Equals(other.Date) && Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Dividend) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Date.GetHashCode() * 397) ^ Amount.GetHashCode();
            }
        }
    }
}