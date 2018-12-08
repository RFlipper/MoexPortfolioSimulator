using System;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Operation
    {
        public DateTime OperationDate { get; }
        public OperationType OperationType { get; }
        public decimal Amount { get; }
        public string SymbolCode { get; }
        public long Quantity { get; }
        public decimal Price { get; }
        public decimal Volume { get; }
        public bool isRebalanced { get; set; }

        //Constructor for deposit and withdrawal
        public Operation(DateTime operationDate, OperationType type, decimal amount)
        {
            this.OperationDate = operationDate;
            this.OperationType = type;
            this.Amount = amount;
        }
        
        //Constructor for dividends
        public Operation(DateTime operationDate, OperationType type, decimal amount, Symbol symbol) : this(operationDate, type, amount)
        {
            SymbolCode = symbol.Code;
        }
        
        //Constructor for trades
        public Operation(DateTime operationDate, OperationType type, Symbol symbol, long quantity, decimal price, decimal volume, bool isRebalanced = false)
        {
            this.OperationDate = operationDate;
            this.OperationType = type;
            this.SymbolCode = symbol.Code;
            this.Quantity = quantity;
            this.Price = price;
            this.Volume = volume;
            this.isRebalanced = isRebalanced;
        }

        public override string ToString()
        {
            if (OperationType == OperationType.Deposit ||
                OperationType == OperationType.Dividends ||
                OperationType == OperationType.Withdrawal)
            {
                return $"{nameof(OperationDate)}: {OperationDate}, {nameof(OperationType)}: {OperationType}, {nameof(Amount)}: {Amount}";
            }
            else
            {
                return $"{nameof(OperationDate)}: {OperationDate}, {nameof(OperationType)}: {OperationType}, {nameof(SymbolCode)}: {SymbolCode}, {nameof(Quantity)}: {Quantity}, {nameof(Price)}: {Price}, {nameof(Volume)}: {Volume}";

            }
        }
    }


    public enum OperationType
    {
        Buy = 0,
        Sell = 1,
        Deposit = 2,
        Withdrawal = 3,
        Dividends = 4
    }
}