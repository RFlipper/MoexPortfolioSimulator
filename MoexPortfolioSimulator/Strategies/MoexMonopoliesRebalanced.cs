using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MoexPortfolioSimulator.Data;
using MoexPortfolioSimulator.Data.Providers;
using MoexPortfolioSimulator.Engine;
using MoexPortfolioSimulator.Helpers;

namespace MoexPortfolioSimulator.Strategies
{
    public class MoexMonopoliesRebalanced : StrategiesBase
    {
        private ILog Logger => LogManager.GetLogger(GetType());
        
        private static string[] symbolsCodes = {"SBERP", "RTKMP", "ROSN", "RSTIP", "GAZP", "GMKN"};
        private DateTime dateFrom = new DateTime(2011, 01, 01);
        private DateTime  dateTo = new DateTime(2019, 01, 01);
        private decimal startMoney = 1000000;
        private FinamDataPeriod timeFrame = FinamDataPeriod.Daily;
        private Recharge scheduledRecharge = new Recharge(1000000, RechargePeriod.Yearly);
        //TODO:ADD INFLATION
        private decimal maxPercentForShare = (decimal) 100 / symbolsCodes.Length;
        private Rebalancing rebalancing;
        private TradeSchedule buySchedule = TradeSchedule.Yearly;
        
        private Account acc;
        private Dictionary<string, Symbol> symbols;

        public MoexMonopoliesRebalanced()
        {
            symbols = LoadSymbols(symbolsCodes, dateFrom, dateTo, timeFrame).Result;
            acc = new Account(startMoney, dateFrom);
            rebalancing = new Rebalancing(maxPercentForShare, RebalancingPeriod.Yearly, dateFrom);
        }

       public void Run()
       {
            for (int i = 0; i < dateTo.Subtract(dateFrom).Days; i++)
            {
                var currentDate = dateFrom.AddDays(i);

                if (!currentDate.IsWorkingDay())
                {
                    continue;
                }
                
                if (currentDate.Equals(DateTime.Today))
                {
                    break;
                }

                Recharge(currentDate);
                GetDividends(currentDate);

                BuyOrRebalance(currentDate);
            }

           
           
            SellAllShares();

           double annualReturn = Statistics.GetAnnualReturn(acc.GetTotalInvestedMoney(), acc.CurrentMoney, dateFrom, dateTo);

            
            Logger.Info($"\nPortfolio value {acc.GetCurrentMoneyFormatted()}.\n" +
                        $"Total money invested: {acc.GetMoneyFormatted(acc.GetTotalInvestedMoney())}.\n" +
                        $"Annual return: {Math.Round(annualReturn * 100, 2)}%\n" +
                        $"Years: {dateTo.Subtract(dateFrom).Days / 365}");
        }

        private void GetDividends(DateTime currentDate)
        {
            acc.PayDividends(currentDate, symbols.Values.ToList());
        }

        private void BuyOrRebalance(DateTime currentDate)
        {
            switch (rebalancing.Period)
            {
                case RebalancingPeriod.Yearly:
                {
                    if (currentDate < dateFrom.AddYears(1))
                    {
                        Buy(currentDate);
                    }
                    else
                    {
                        Rebalance(currentDate);
                    }

                    break;
                }
                case RebalancingPeriod.Monthly:
                {
                    if (currentDate < dateFrom.AddMonths(1))
                    {
                        Buy(currentDate);
                    }
                    else
                    {
                        Rebalance(currentDate);
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
        }

        private void Buy(DateTime currentDate)
        {            
            decimal moneyForEachSymbol = acc.CurrentMoney / 100 * maxPercentForShare;

            foreach (var symbol in symbols.Values)
            {
                if (!acc.ThereWereTradesThisPeriod(currentDate, buySchedule, symbol))
                {
                    acc.Buy(symbol, moneyForEachSymbol, currentDate);
                }
            }
            
        }

        private void Rebalance(DateTime currentDate)
        {
            bool needRebalance = symbols.Values.All(symbol => rebalancing.IsRebalancingNeeded(currentDate, symbol, acc.Operations));
            if (!needRebalance)
            {
                return;
            }
            
            bool haveQuote = symbols.Values.All(symbol => symbol.GetDailyQuote(currentDate) != null);
            if (haveQuote)
            {
                acc.Rebalance(symbols.Values.ToList(), rebalancing, currentDate);
            }
            
        }

        private void SellAllShares()
        {
            foreach (Symbol symbol in symbols.Values)
            {
                acc.SellAll(symbol, symbol.GetLastDailyQuote().Date);
            }
        }

        private void Recharge(DateTime currentDate)
        {
            if (!acc.ThereWereDepositsThisPeriod(currentDate, scheduledRecharge.Period))
            {
                acc.Deposit(scheduledRecharge.Amount, currentDate);
            }
        }
    } 
}
