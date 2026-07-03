using SIMS.Domain;

namespace SIMS.Application;

// Receives its GPA algorithm through constructor injection (Strategy pattern),
// so adding a new programme's rule means adding a new strategy class, not
// editing Transcript (OCP).
public class Transcript
{
    private readonly IGpaCalculationStrategy _gpaStrategy;

    public Transcript(IGpaCalculationStrategy gpaStrategy)
    {
        _gpaStrategy = gpaStrategy;
    }

    public double CalculateGpa(IEnumerable<double> grades) => _gpaStrategy.Calculate(grades);
}
