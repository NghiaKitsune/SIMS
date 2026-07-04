using SIMS.Application.Strategies;

namespace SIMS.Tests.Unit;

// The GPA strategies are pure functions of their grade list, so they are checked
// directly with Theory/InlineData. FourPoint converts each 0-10 grade linearly to
// a 0-4 scale (g/10*4) before averaging; TenPoint takes the simple mean. Both
// round to 2 decimal places and return 0.0 for an empty list.
public class GpaStrategyTests
{
    [Theory]
    [InlineData(new double[] { 10 }, 4.0)]        // 10/10*4 = 4.0
    [InlineData(new double[] { 9, 8 }, 3.4)]      // (3.6 + 3.2) / 2
    [InlineData(new double[] { 5 }, 2.0)]         // 5/10*4 = 2.0
    [InlineData(new double[] { 3.33 }, 1.33)]     // 1.332 rounded to 2dp
    [InlineData(new double[] { }, 0.0)]           // empty -> 0.0
    public void FourPointGpaStrategy_ConvertsToFourScaleThenAverages(double[] grades, double expected)
    {
        var strategy = new FourPointGpaStrategy();

        var gpa = strategy.Calculate(grades);

        Assert.Equal(expected, gpa, 2);
    }

    [Theory]
    [InlineData(new double[] { 8, 6 }, 7.0)]      // simple mean
    [InlineData(new double[] { 8.5 }, 8.5)]
    [InlineData(new double[] { 7, 8, 9 }, 8.0)]
    [InlineData(new double[] { 1, 2, 2 }, 1.67)]  // 1.6666.. rounded to 2dp
    [InlineData(new double[] { }, 0.0)]           // empty -> 0.0
    public void TenPointGpaStrategy_TakesMean(double[] grades, double expected)
    {
        var strategy = new TenPointGpaStrategy();

        var gpa = strategy.Calculate(grades);

        Assert.Equal(expected, gpa, 2);
    }
}
