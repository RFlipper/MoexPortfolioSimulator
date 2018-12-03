using System;

namespace MoexPortfolioSimulator.Engine
{
    public class Statistics
    {
        public static double GetAnnualReturn(decimal startMoney, decimal endMoney, int years)
        {
            return Math.Pow((double)(endMoney / startMoney), ((double)1 / years)) - 1;
        }
        
        public static double GetAnnualReturn(decimal startMoney, decimal endMoney, DateTime dateFrom, DateTime dateTo)
        {
            return GetAnnualReturn(startMoney, endMoney, dateTo.Subtract(dateFrom).Days / 365);
        }
    }
}