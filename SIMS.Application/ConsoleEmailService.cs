using SIMS.Domain;

namespace SIMS.Application;

// A no-SMTP implementation: it writes the notification to the console. This is
// sufficient for the assignment scenario and keeps the email concern isolated,
// so changing how confirmations are delivered never touches the services that
// trigger them (SRP).
public class ConsoleEmailService : IEmailService
{
    public void SendRegistrationConfirmation(Student student)
    {
        Console.WriteLine($"[Email] Registration confirmed for {student.Username} ({student.Email}).");
    }

    public void SendEnrolmentConfirmation(Student student, Course course)
    {
        Console.WriteLine($"[Email] Enrolment confirmed: {student.Username} -> {course.Code} {course.Title}.");
    }
}
