using System;
using System.Collections.Generic;
using System.Linq;
using MoexPortfolioSimulator.Data;

namespace MoexPortfolioSimulator.Engine
{
    public class Operations : List<Operation>
    {
        public DateTime GetLastRebalancingDate(Symbol symbol)
        {
            DateTime lastRebalancingDate = DateTime.MinValue;
            
            foreach (Operation operation in this)
            {
                if (operation.SymbolCode.Equals(symbol.Code) && operation.isRebalanced)
                {
                    if (operation.OperationDate > lastRebalancingDate)
                    {
                        lastRebalancingDate = operation.OperationDate;
                    }
                }
            }

            return lastRebalancingDate;
        }
        
        public Operation GetLatestOperation()
        {
            Operation latestOperation = this.First();
            
            foreach (Operation operation in this)
            {
                if (operation.OperationDate > latestOperation.OperationDate)
                {
                    latestOperation = operation;
                }
            }

            return latestOperation;
        }
        
        public Operations GetAllRebalancingOperations(Symbol symbol)
        {
            var ops = new Operations();
            
            foreach (Operation operation in this)
            {
                if ((operation.OperationType == OperationType.Buy || operation.OperationType == OperationType.Sell) && 
                    operation.SymbolCode.Equals(symbol.Code) && operation.isRebalanced)
                {
                    ops.Add(operation);
                }
            }

            return ops;
        }
    }
}