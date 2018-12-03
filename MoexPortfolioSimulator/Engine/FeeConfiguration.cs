namespace MoexPortfolioSimulator.Engine
{
    public class FeeConfiguration
    {
        public decimal Fee { get; }
        public FeeType Type { get; }
        public ApplicableTo ApplicableTo { get; }

        public FeeConfiguration(decimal fee, FeeType type, ApplicableTo applicableTo)
        {
            this.Type = type;
            ApplicableTo = applicableTo;
            this.Fee = fee;
        }
    }

    public enum FeeType
    {
        Percent,
        Fix
    }
    
    public enum ApplicableTo
    {
        Trade,
        Monthly
    }
}