using System;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Operation
    {
        public DateTime OperationDate { get; }
        public OperationType operationType { get; }
        public decimal amount { get; }
        public string symbolName { get; }
        public long quantity { get; }
        public decimal price { get; }
        public decimal volume { get; }

        //Constructor for deposit and withdrawal
        public Operation(DateTime operationDate, OperationType type, decimal amount)
        {
            this.OperationDate = operationDate;
            this.operationType = type;
            this.amount = amount;
        }
        
        //Constructor for trades
        public Operation(DateTime operationDate, OperationType type, Symbol symbol, long quantity, decimal price, decimal volume)
        {
            this.OperationDate = operationDate;
            this.operationType = type;
            this.symbolName = symbol.SymbolName;
            this.quantity = quantity;
            this.price = price;
            this.volume = volume;
        }

        public override string ToString()
        {
            if (operationType == OperationType.Deposit)
            {
                return $"{nameof(OperationDate)}: {OperationDate}, {nameof(operationType)}: {operationType}, {nameof(amount)}: {amount}";
            }
            else
            {
                return $"{nameof(OperationDate)}: {OperationDate}, {nameof(operationType)}: {operationType}, {nameof(symbolName)}: {symbolName}, {nameof(quantity)}: {quantity}, {nameof(price)}: {price}, {nameof(volume)}: {volume}";

            }
        }
    }


    public enum OperationType
    {
        Buy = 0,
        Sell = 1,
        Deposit = 2,
        Withdrawal = 3
    }
}