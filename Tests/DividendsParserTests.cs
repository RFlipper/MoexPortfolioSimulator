using System;
using System.Threading.Tasks;
using MoexPortfolioSimulator.Data;
using MoexPortfolioSimulator.Data.Providers;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class Tests
    {
        [Test]
        public async Task VerifyDivsCount()
        {
            var divs = await Dohod.GetDividendsBySymbol("sber");
            Assert.True(divs != null && divs.Count >= 16);
        }
        
        [Test]
        public async Task VerifyDivsTableParser()
        {
            var expectedDiv = new Dividend(DateTime.Parse("04.06.2018"), new decimal(0.0034535));
            var divs = await Dohod.GetDividendsBySymbol("vtbr");
            Assert.True(divs != null && divs.Count > 0);
            Assert.Contains(expectedDiv, divs);
        }
    }
}