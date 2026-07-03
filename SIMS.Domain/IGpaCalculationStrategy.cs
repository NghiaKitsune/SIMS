namespace SIMS.Domain;

// Strategy pattern: encapsulates algorithmic variance in GPA calculation
// across academic programmes (e.g. 4-point vs 10-point scales).
public interface IGpaCalculationStrategy
{
    double Calculate(IEnumerable<double> grades);
}
