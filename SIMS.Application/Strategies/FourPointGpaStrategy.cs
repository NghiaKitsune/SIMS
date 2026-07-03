using SIMS.Domain;

namespace SIMS.Application.Strategies;

// International programme: each 0-10 grade is converted linearly to a 0-4 scale
// (grade / 10 * 4) and the results are averaged. Pure function, so it can be
// unit-tested in isolation with Theory/InlineData.
public class FourPointGpaStrategy : IGpaCalculationStrategy
{
    public double Calculate(IEnumerable<double> grades)
    {
        var list = grades.ToList();
        if (list.Count == 0) return 0.0;
        var converted = list.Select(g => g / 10.0 * 4.0);
        return Math.Round(converted.Average(), 2);
    }
}
