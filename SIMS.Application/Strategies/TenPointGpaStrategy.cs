using SIMS.Domain;

namespace SIMS.Application.Strategies;

// Domestic programme: grades are already on a 0-10 scale, so the GPA is their
// simple mean. A pure function of its input — no dependencies, trivially tested.
public class TenPointGpaStrategy : IGpaCalculationStrategy
{
    public double Calculate(IEnumerable<double> grades)
    {
        var list = grades.ToList();
        if (list.Count == 0) return 0.0;
        return Math.Round(list.Average(), 2);
    }
}
