using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Account
    {
        private ILog logger => LogManager.GetLogger(GetType());
        
        private decimal startMoney;
        public decimal CurrentMoney { get; private set; }
        public Operations Operations { get; } = new Operations();
        private List<FeeConfiguration> fees = new List<FeeConfiguration>();
        public Portfolio Portfolio { get; } = new Portfolio();
        
        

        public Account(decimal startMoney, DateTime date)
        {
            this.startMoney = startMoney;
            this.CurrentMoney = startMoney;
            var oper = new Operation(date, OperationType.Deposit, startMoney);
            logger.Info("New operation: " + oper);
            Operations.Add(oper);
        }

        public void Deposit(decimal moneyAmount, DateTime date)
        {
            CurrentMoney += moneyAmount;
            var oper = new Operation(date, OperationType.Deposit, moneyAmount);
            logger.Info("New operation: " + oper);
            Operations.Add(oper);
        }


        public void SellAll(Symbol symbol, DateTime date)
        {
            var quote = symbol.GetDailyQuote(date);
            var quantity = Portfolio.getQuantity(symbol);

            var volume = quote.Open * quantity; //TODO ADD FEES
            CurrentMoney += volume;
            var oper = new Operation(date, OperationType.Sell, symbol, quantity, quote.Open, volume);
            logger.Info("New operation: " + oper);
            Operations.Add(oper);

            Portfolio.Remove(symbol, quantity);
        }

        public Operation Sell(Symbol symbol, decimal money, DateTime date)
        {
            var symbolQuote = symbol.GetDailyQuote(date);
            int quantity = (int) (money / symbolQuote.Open);
            decimal remainder = money - (quantity * symbolQuote.Open);
            decimal volume = quantity * symbolQuote.Open; //TODO: add FEE and LOTS
            Operation oper = null;

            if (quantity == 0)
            {
                quantity = 1;
            }
            
            CurrentMoney += volume;
            oper = new Operation(date, OperationType.Sell, symbol, quantity, symbolQuote.Open, volume);
            logger.Info("New operation: " + oper);
            Operations.Add(oper);
            Portfolio.Remove(symbol, quantity);
            
            return oper;
        }


        public Operation Buy(Symbol symbol, decimal money, DateTime date)
        {
            var symbolQuote = symbol.GetDailyQuote(date);
            if (symbolQuote == null)
            {
                return null;
            }

            Operation oper = null;
            int quantity = (int) (money / symbolQuote.Open);
            decimal remainder = money - (quantity * symbolQuote.Open);
            decimal volume = quantity * symbolQuote.Open; //TODO: add FEE and LOTS
            
            if (quantity > 0 && CurrentMoney >= volume)
            {
                CurrentMoney -= volume;
                oper = new Operation(date, OperationType.Buy, symbol, quantity, symbolQuote.Open, volume);
                logger.Info("New operation: " + oper);
                Operations.Add(oper);
                Portfolio.Add(symbol, quantity);
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

            return oper;
        }

        public decimal GetPortfolioValue(DateTime currentDate)
        {
            decimal totalValue = 0;
            foreach (KeyValuePair<Symbol,long> pair in Portfolio.Storage)
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
            foreach (Operation operation in Operations)
            {
                if (operation.OperationType == OperationType.Deposit)
                {
                    totalMoney += operation.Amount;
                }
            }

            return totalMoney;
        }

        public void Rebalance(Symbol symbol, Rebalancing rebalancing, DateTime currentDate)
        {
            Rebalance(new List<Symbol> {symbol}, rebalancing, currentDate);
        }
        
        public void Rebalance(List<Symbol> symbols, Rebalancing reb, DateTime currentDate)
        {
            logger.Info($"{currentDate} START REBALANCING");
            decimal money = CurrentMoney + GetPortfolioValue(currentDate);

            logger.Debug("Total account value " + money);

            decimal moneyForEachSymbol = money / 100 * reb.MaxPercentForShare; // There is a rebalancing issue, when we have to sell some stocks but can't because of big lot or share price
            logger.Debug("Money for each symbol " + moneyForEachSymbol);


            //sell cycle
            foreach (Symbol symbol in symbols)
            {
                logger.Debug("Sell balancing cycle for " + symbol.Code);
                var symbolQuote = symbol.GetDailyQuote(currentDate);
                long quantity = Portfolio.getQuantity(symbol);
                decimal sharesCost = quantity * symbolQuote.Open;
                logger.Debug($"Cost of {symbol.Code}: " + sharesCost);

                var moneyForTrade = (long) (moneyForEachSymbol - sharesCost);
                logger.Debug("Money for trade: " + moneyForTrade);

                if (moneyForTrade < 0 && moneyForTrade < symbolQuote.Open)
                {
                    logger.Debug("Current cash: " + GetCurrentMoneyFormatted());
                    Operation oper = Sell(symbol, Math.Abs(moneyForTrade), currentDate);
                    if (oper != null)
                    {
                        oper.isRebalanced = true;
                    }
                    logger.Debug("Current cash: " + GetCurrentMoneyFormatted());
                }
                else
                {
                    logger.Info($"No needed to Sell {symbol.Code}");
                }
            }
            
            //buy cycle
            foreach (Symbol symbol in symbols)
            {
                logger.Debug("Buy balancing cycle for " + symbol.Code);
                var symbolQuote = symbol.GetDailyQuote(currentDate);
                long quantity = Portfolio.getQuantity(symbol);
                decimal sharesCost = quantity * symbolQuote.Open;
                logger.Debug($"Cost of {symbol.Code}: " + sharesCost);

                decimal moneyForTrade = moneyForEachSymbol - sharesCost;
                logger.Debug("Money for trade: " + moneyForTrade);

                if (moneyForTrade > 0)
                {
                    if (moneyForTrade > CurrentMoney)
                    {
                        moneyForEachSymbol = moneyForEachSymbol - (moneyForTrade - CurrentMoney); // There is a rebalancing issue, when we have to sell some stocks but can't because of big lot or share price
                        moneyForTrade = moneyForTrade - (moneyForTrade - CurrentMoney);
                    }
                    
                    logger.Debug("Current cash: " + GetCurrentMoneyFormatted());
                    Operation operation = Buy(symbol, moneyForTrade, currentDate);
                    if (operation != null)
                    {
                        operation.isRebalanced = true;
                    }
                    logger.Debug("Current cash: " + GetCurrentMoneyFormatted());
                }
                else
                {
                    logger.Info($"No needed to Buy {symbol.Code}");
                }
            }
            
            logger.Info($"{currentDate} END REBALANCING. CASH: " + GetCurrentMoneyFormatted());
        }

        public string GetCurrentMoneyFormatted()
        {
            return GetMoneyFormatted(CurrentMoney);
        }
        
        public string GetMoneyFormatted(decimal money)
        {
            return $"{money:C}";
        }

        public bool ThereWereTradesThisPeriod(DateTime dateTime, TradeSchedule tradeSchedule)
        {
            if (tradeSchedule == TradeSchedule.Yearly)
            {
                foreach (Operation operation in Operations)
                {
                    if (operation.OperationDate.Year == dateTime.Year && 
                        (operation.OperationType == OperationType.Buy || operation.OperationType == OperationType.Sell))
                    {
                        return true;
                    }
                }
            }
            
            if (tradeSchedule == TradeSchedule.Monthly)
            {
                foreach (Operation operation in Operations)
                {
                    if (operation.OperationDate.Month == dateTime.Month &&
                        operation.OperationDate.Year == dateTime.Year &&
                       (operation.OperationType == OperationType.Buy || operation.OperationType == OperationType.Sell))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public bool ThereWereTradesThisPeriod(DateTime dateTime, TradeSchedule tradeSchedule, Symbol symbol)
        {
            if (tradeSchedule == TradeSchedule.Yearly)
            {
                foreach (Operation operation in Operations)
                {
                    if (operation.OperationDate.Year == dateTime.Year && 
                       (operation.OperationType == OperationType.Buy || operation.OperationType == OperationType.Sell) &&
                        operation.SymbolCode.Equals(symbol.Code) && operation.isRebalanced == false)
                    {
                        return true;
                    }
                }
            }
            
            if (tradeSchedule == TradeSchedule.Monthly)
            {
                foreach (Operation operation in Operations)
                {
                    if (operation.OperationDate.Month == dateTime.Month &&
                        operation.OperationDate.Year == dateTime.Year &&
                        (operation.OperationType == OperationType.Buy || operation.OperationType == OperationType.Sell) &&
                        operation.SymbolCode.Equals(symbol.Code) && operation.isRebalanced == false)
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
                foreach (Operation operation in Operations)
                {
                    if (operation.OperationDate.Year == dateTime.Year &&
                        operation.OperationType == OperationType.Deposit)
                    {
                        return true;
                    }
                }
            }
            
            if (rechargePeriod == RechargePeriod.Monthly)
            {
                foreach (Operation operation in Operations)
                {
                    if (operation.OperationDate.Month == dateTime.Month &&
                        operation.OperationDate.Year == dateTime.Year &&
                        operation.OperationType == OperationType.Deposit)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public void PayDividends(DateTime currentDate, List<Symbol> symbols)
        {
            foreach (Symbol symbol in symbols)
            {
                List<Dividend> dividends = symbol.Dividends.Where(s => s.Date.Date.Equals(currentDate)).ToList();
                
                if (dividends.Count > 0)
                {
                    if (PayDividend(currentDate, dividends.First(), symbol))
                    {
                        symbol.Dividends.Remove(dividends.First());
 
                    }
                }
            }
        }

        private bool PayDividend(DateTime currentDate, Dividend dividend, Symbol symbol)
        {
            long quantity = Portfolio.getQuantity(symbol);

            if (quantity == 0)
            {
                return false;
            }

            decimal amount = quantity * dividend.Amount;

            logger.Info($"{currentDate} PAID DIVIDENDS FOR {symbol.Code} {amount}");
            Operation oper = new Operation(currentDate, OperationType.Dividends, amount, symbol);
            logger.Info("New operation: " + oper);
            Operations.Add(oper);
            CurrentMoney += amount;
            return true;
        }
    }
}