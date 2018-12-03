using System;
using log4net;
using log4net.Config;
using MoexPortfolioSimulator.Strategies;

namespace MoexPortfolioSimulator
{
    internal class Program
    {
        private static ILog logger => LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            XmlConfigurator.Configure();
            new MoexRebalance().Run();
        }
    }
}