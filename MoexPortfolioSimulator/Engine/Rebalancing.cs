using System;
using System.Collections.Generic;
using log4net;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Rebalancing
    {
        private static ILog logger => LogManager.GetLogger(typeof(Rebalancing));

        public RebalancingPeriod Period{ get; }
        public decimal MaxPercentForShare { get; }
        private readonly DateTime _initDate;

        public Rebalancing(decimal maxPercentForShare, RebalancingPeriod period, DateTime initDate)
        {
            this.MaxPercentForShare = maxPercentForShare;
            this.Period = period;
            this._initDate = initDate;
        }

        public bool IsRebalancingNeeded(DateTime currentDate, Symbol symbol, Operations operations)
        {
            Operations rOps = operations.GetAllRebalancingOperations(symbol);

            DateTime rebDate = _initDate;

            if (rOps.Count > 0)
            {
                rebDate = rOps.GetLatestOperation().OperationDate;
            }
            
            switch (Period)
            {
                case RebalancingPeriod.Yearly:
                {
                    if (rebDate.AddYears(1) <= currentDate)
                    {
                        return true;
                    }

                    break;
                }
                case RebalancingPeriod.Monthly:
                {
                    if (rebDate.AddMonths(1) <= currentDate)
                    {
                        return true;
                    }

                    break;
                }
                case RebalancingPeriod.ByDividends:
                    break;
                case RebalancingPeriod.ByDividendsAndCoupons:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return false;
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