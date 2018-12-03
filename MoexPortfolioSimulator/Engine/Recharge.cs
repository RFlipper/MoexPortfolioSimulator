namespace MoexPortfolioSimulator.Engine
{
    public class Recharge
    {
        public decimal Amount { get; }
        public RechargePeriod Period { get; }

        public Recharge(decimal amount, RechargePeriod period)
        {
            this.Amount = amount;
            this.Period = period;
        }
    }

    public enum RechargePeriod
    {
        Monthly,
        Yearly
    }
}