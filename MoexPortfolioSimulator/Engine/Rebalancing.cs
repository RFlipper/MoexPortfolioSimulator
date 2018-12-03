using System;
using log4net;

namespace MoexPortfolioSimulator.Engine
{
    public class Rebalancing
    {
        private static ILog logger => LogManager.GetLogger(typeof(Rebalancing));

        private RebalancingPeriod period;
        public int MaxPercentForShare { get; }
        private DateTime _lastRebalancingDate;

        public Rebalancing(int maxPercentForShare, RebalancingPeriod period, DateTime initDate)
        {
            this.MaxPercentForShare = maxPercentForShare;
            this.period = period;
            _lastRebalancingDate = initDate;
        }

        public bool IsRebalanceNeeded(DateTime currentDate)
        {
            //logger.Debug("current rebalance date " + _lastRebalancingDate);
            var nextRebalancingDate = _lastRebalancingDate;
            //logger.Debug("next rebalance date " + nextRebalancingDate);

            if (period == RebalancingPeriod.Yearly)
            {
                nextRebalancingDate = nextRebalancingDate.AddYears(1);
            }
            if (nextRebalancingDate <= currentDate)
            {
                return true;
            }

            return false;
        }

        public DateTime LastRebalancingDate
        {
            get { return _lastRebalancingDate; }
            set { _lastRebalancingDate = value; }
        }
    }

    public enum RebalancingPeriod
    {
        Yearly,
        Monthly,
        ByDividends,
        ByDividendsAndCoupons
    }
}