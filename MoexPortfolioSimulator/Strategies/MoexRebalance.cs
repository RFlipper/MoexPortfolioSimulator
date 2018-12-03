using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MoexPortfolioSimulator.Data;
using MoexPortfolioSimulator.Engine;

namespace MoexPortfolioSimulator.Strategies
{
    public class MoexRebalance : StrategiesBase
    {
        private ILog Logger => LogManager.GetLogger(GetType());
        
        private string[] symbolsNames = {"SBER", "RTKMP", "ROSN", "RSTIP", "GAZP", "GMKN"};
        private DateTime dateFrom = new DateTime(2009, 01, 01);
        private DateTime  dateTo = new DateTime(2019, 01, 01);
        private decimal startMoney = 720000;
        private Recharge scheduledRecharge = new Recharge(0, RechargePeriod.Yearly);
        //TODO:ADD INFLATION AND DIVIDENDS
        private int maxPercentForShares = 100;
        private Rebalancing rebalancing;
        private TradeSchedule buySchedule = TradeSchedule.Yearly;
        
        private Account acc;
        private Dictionary<string, Symbol> symbols;

        public MoexRebalance()
        {
            symbols = LoadSymbols(symbolsNames, dateFrom, dateTo);
            acc = new Account(startMoney, dateFrom);
            rebalancing = new Rebalancing(maxPercentForShares, RebalancingPeriod.Yearly, dateFrom);
        }

       public void Run()
       {
            int lastRebalancYear = dateFrom.Year;
           
            for (int i = 0; i < dateTo.Subtract(dateFrom).Days; i++)
            {
                var currentDate = dateFrom.AddDays(i);
                
                if (currentDate.Equals(DateTime.Today))
                {
                    break;
                }
                
                Recharge(currentDate);

                if (rebalancing.IsRebalanceNeeded(currentDate))
                {
                    currentDate = currentDate.AddDays(-currentDate.Day+1);
                    acc.Rebalance(symbols.Select(s => s.Value).ToList(), rebalancing, currentDate);
                    rebalancing.LastRebalancingDate = currentDate;
                }
                

                if (!acc.ThereWereTradesThisPeriod(currentDate, buySchedule))
                {
                    decimal totalMoneyToSpend = acc.CurrentMoney / 100 * maxPercentForShares;
                    decimal moneyForEachSymbol = totalMoneyToSpend / symbols.Count;
                    foreach (var symbol in symbols.Values)
                    {
                        acc.Buy(symbol, moneyForEachSymbol, currentDate);
                    }
                }
            }

            SellAllShares();

           double annualReturn = Statistics.GetAnnualReturn(startMoney, acc.CurrentMoney, dateFrom, dateTo);

            
            Logger.Info($"\nPortfolio value {acc.getCurrentMoneyFormatted()}.\n" +
                        $"Total money invested: {acc.getMoneyFormatted(acc.GetTotalInvestedMoney())}.\n" +
                        $"Annual return: {Math.Round(annualReturn * 100, 2)}%");
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
