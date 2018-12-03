using System;
using System.Collections.Generic;
using log4net;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Account
    {
        private ILog logger => LogManager.GetLogger(GetType());
        
        private decimal startMoney;
        public decimal CurrentMoney { get; private set; }
        private List<Operation> operations = new List<Operation>();
        private List<FeeConfiguration> fees = new List<FeeConfiguration>();
        private Portfolio portfolio = new Portfolio();
        
        

        public Account(decimal startMoney, DateTime date)
        {
            this.startMoney = startMoney;
            this.CurrentMoney = startMoney;
            var oper = new Operation(date, OperationType.Deposit, startMoney);
            logger.Info("New operation: " + oper);
            operations.Add(oper);
        }

        public void Deposit(decimal moneyAmount, DateTime date)
        {
            CurrentMoney += moneyAmount;
            var oper = new Operation(date, OperationType.Deposit, moneyAmount);
            logger.Info("New operation: " + oper);
            operations.Add(oper);
        }


        public void SellAll(Symbol symbol, DateTime date)
        {
            var quote = symbol.GetDailyQuote(date);
            var quantity = portfolio.getQuantity(symbol);

            var volume = quote.Open * quantity; //TODO ADD FEES
            CurrentMoney += volume;
            var oper = new Operation(date, OperationType.Sell, symbol, quantity, quote.Open, volume);
            logger.Info("New operation: " + oper);
            operations.Add(oper);

            portfolio.Remove(symbol, quantity);
        }

        public void Sell(Symbol symbol, decimal money, DateTime date)
        {
            var symbolQuote = symbol.GetDailyQuote(date);
            int quantity = (int) (money / symbolQuote.Open);
            decimal remainder = money - (quantity * symbolQuote.Open);
            decimal volume = quantity * symbolQuote.Open; //TODO: add FEE and LOTS
            
            if (quantity > 0)
            {
                CurrentMoney += volume;
                var oper = new Operation(date, OperationType.Sell, symbol, quantity, symbolQuote.Open, volume);
                logger.Info("New operation: " + oper);
                operations.Add(oper);
                portfolio.Remove(symbol, quantity);
            }else
            {
                if (quantity == 0)
                {
                    logger.Info($"Not enough money!\nNeed {symbolQuote.Open}, but have {money}");
                }
                else
                {
                    logger.Info($"Not enough money!\nNeed {volume}, but have {CurrentMoney}");
                }
            }
        }


        public void Buy(Symbol symbol, decimal money, DateTime date)
        {
            var symbolQuote = symbol.GetDailyQuote(date);
            int quantity = (int) (money / symbolQuote.Open);
            decimal remainder = money - (quantity * symbolQuote.Open);
            decimal volume = quantity * symbolQuote.Open; //TODO: add FEE and LOTS
            
            if (quantity > 0 && CurrentMoney >= volume)
            {
                CurrentMoney -= volume;
                var oper = new Operation(date, OperationType.Buy, symbol, quantity, symbolQuote.Open, volume);
                logger.Info("New operation: " + oper);
                operations.Add(oper);
                portfolio.Add(symbol, quantity);
            }else
            {
                if (quantity == 0)
                {
                    logger.Info($"Not enough money!\nNeed {symbolQuote.Open}, but have {money}");
                }
                else
                {
                    logger.Info($"Not enough money!\nNeed {volume}, but have {CurrentMoney}");
                }
            }
        }

        public decimal GetPortfolioValue(DateTime currentDate)
        {
            decimal totalValue = 0;
            foreach (KeyValuePair<Symbol,long> pair in portfolio.Storage)
            {
                Quote quote = pair.Key.GetDailyQuote(currentDate);
                totalValue += quote.Open * pair.Value;
            }
            
            logger.Debug("Portfolio value " + totalValue);
            
            return totalValue;
        }

        public decimal GetTotalInvestedMoney()
        {
            decimal totalMoney = 0;
            foreach (Operation operation in operations)
            {
                if (operation.operationType == OperationType.Deposit)
                {
                    totalMoney += operation.amount;
                }
            }

            return totalMoney;
        }
        
        public void Rebalance(List<Symbol> symbols, Rebalancing reb, DateTime currentDate)
        {
            logger.Info("Start rebalancing");
            decimal money = CurrentMoney + GetPortfolioValue(currentDate);

            logger.Debug("Total account value " + money);

            decimal totalMoneyToSpend = money / 100 * reb.MaxPercentForShare;
            logger.Debug("Total money to spend " + totalMoneyToSpend);

            decimal moneyForEachSymbol = totalMoneyToSpend / symbols.Count; // There is a rebalancing issue, when we have to sell some stocks but can't because of big lot or share price
            logger.Debug("Money for each symbol " + moneyForEachSymbol);


            //sell cycle
            foreach (Symbol symbol in symbols)
            {
                logger.Debug("Sell balancing cycle for " + symbol.SymbolName);
                var symbolQuote = symbol.GetDailyQuote(currentDate);
                long quantity = portfolio.getQuantity(symbol);
                decimal sharesCost = quantity * symbolQuote.Open;
                logger.Debug($"Cost of {symbol.SymbolName}: " + sharesCost);

                var moneyForTrade = (long) (moneyForEachSymbol - sharesCost);
                logger.Debug("Money for trade: " + moneyForTrade);

                if (moneyForTrade < 0)
                {
                    logger.Debug("Current cash: " + getCurrentMoneyFormatted());
                    Sell(symbol, Math.Abs(moneyForTrade), currentDate);
                    logger.Debug("Current cash: " + getCurrentMoneyFormatted());
                }
                else
                {
                    logger.Info($"No needed to Sell {symbol.SymbolName}");
                }
            }
            
            //buy cycle
            foreach (Symbol symbol in symbols)
            {
                logger.Debug("Buy balancing cycle for " + symbol.SymbolName);
                var symbolQuote = symbol.GetDailyQuote(currentDate);
                long quantity = portfolio.getQuantity(symbol);
                decimal sharesCost = quantity * symbolQuote.Open;
                logger.Debug($"Cost of {symbol.SymbolName}: " + sharesCost);

                decimal moneyForTrade = moneyForEachSymbol - sharesCost;
                logger.Debug("Money for trade: " + moneyForTrade);

                if (moneyForTrade > 0)
                {
                    if (moneyForTrade > CurrentMoney)
                    {
                        moneyForEachSymbol = moneyForEachSymbol - (moneyForTrade - CurrentMoney); // There is a rebalancing issue, when we have to sell some stocks but can't because of big lot or share price
                        moneyForTrade = moneyForTrade - (moneyForTrade - CurrentMoney);
                    }
                    
                    logger.Debug("Current cash: " + getCurrentMoneyFormatted());
                    Buy(symbol, moneyForTrade, currentDate);
                    logger.Debug("Current cash: " + getCurrentMoneyFormatted());
                }
                else
                {
                    logger.Info($"No needed to Buy {symbol.SymbolName}");
                }
            }
            
            logger.Info("End rebalancing. Cash: " + getCurrentMoneyFormatted());
        }

        public string getCurrentMoneyFormatted()
        {
            return getMoneyFormatted(CurrentMoney);
        }
        
        public string getMoneyFormatted(decimal money)
        {
            return $"{money:C}";
        }

        public bool ThereWereTradesThisPeriod(DateTime dateTime, TradeSchedule tradeSchedule)
        {
            if (tradeSchedule == TradeSchedule.Yearly)
            {
                foreach (Operation operation in operations)
                {
                    if (operation.OperationDate.Year == dateTime.Year && 
                        (operation.operationType == OperationType.Buy || operation.operationType == OperationType.Sell))
                    {
                        return true;
                    }
                }
            }
            
            if (tradeSchedule == TradeSchedule.Monthly)
            {
                foreach (Operation operation in operations)
                {
                    if (operation.OperationDate.Month == dateTime.Month &&
                        operation.OperationDate.Year == dateTime.Year &&
                       (operation.operationType == OperationType.Buy || operation.operationType == OperationType.Sell))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public bool ThereWereDepositsThisPeriod(DateTime dateTime, RechargePeriod rechargePeriod)
        {
            if (rechargePeriod == RechargePeriod.Yearly)
            {
                foreach (Operation operation in operations)
                {
                    if (operation.OperationDate.Year == dateTime.Year &&
                        operation.operationType == OperationType.Deposit)
                    {
                        return true;
                    }
                }
            }
            
            if (rechargePeriod == RechargePeriod.Monthly)
            {
                foreach (Operation operation in operations)
                {
                    if (operation.OperationDate.Month == dateTime.Month &&
                        operation.OperationDate.Year == dateTime.Year &&
                        operation.operationType == OperationType.Deposit)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        
    }
}