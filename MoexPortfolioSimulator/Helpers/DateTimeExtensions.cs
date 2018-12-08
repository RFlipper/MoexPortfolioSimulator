using System;

namespace MoexPortfolioSimulator.Helpers
{
    public static class DateTimeExtensions
    {
        public static bool IsWorkingDay(this DateTime date)
        {
            bool isNyHoliday = date.Month.Equals(1) && date.Day < 12;
            bool isWeekEnds = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
            return !isWeekEnds && !isNyHoliday;
        }
    }
}