using PriceOfBread.SimCore;
using Xunit;

namespace PriceOfBread.Tests;

/// <summary>(1391) 暦の述語（Calendar の整数の関数）が、置き換え前の剰余の式（手のリテラル）と四半期の全日で一致する。
/// 突然変異：IsMonthEndDay を 1 日ずらす・NextDayInQuarter を % なしにする → 赤。</summary>
public class CalendarDayTests
{
    [Fact]
    public void 暦の述語は旧の式と全日で一致する()
    {
        for (int d = 0; d < 90; d++)
        {
            Assert.Equal(d == 0, Calendar.IsQuarterStartDay(d));
            Assert.Equal(d == 89, Calendar.IsQuarterEndDay(d));
            Assert.Equal(d % 30 == 0, Calendar.IsMonthStartDay(d));
            Assert.Equal((d + 1) % 30 == 0, Calendar.IsMonthEndDay(d));
            Assert.Equal(d % 30, Calendar.DayInMonth(d));
            Assert.Equal((d + 1) % 90, Calendar.NextDayInQuarter(d));
            for (int h = 1; h <= 30; h++)
            {
                if (30 % h != 0) continue;   // 旬の長さ＝30 の約数（1・2・3・5・6・10・15・30）
                Assert.Equal(d % h == 0, Calendar.IsSpanStartDay(d, h));
                Assert.Equal((d + 1) % h == 0, Calendar.IsSpanEndDay(d, h));
            }
            if ((d + 1) % 30 == 0) Assert.Equal((d + 1) / 30 - 1, Calendar.MonthIndexOfMonthEndDay(d));
        }
        foreach (long t in new long[] { 0, 29, 30, 359, 360, 389, 390, 1000, 240000 })
        {
            Assert.Equal((int)((t - 30) / 360), Calendar.HarvestYearOfTick(t));
            Assert.Equal((int)(t / 30), Calendar.MonthIndexOfTick(t));
        }
    }
}
