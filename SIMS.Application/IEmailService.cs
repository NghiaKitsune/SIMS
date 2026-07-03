using SIMS.Domain;

namespace SIMS.Application;

// Kept separate from the services that trigger it so that changing the email
// format touches only the email component, never registration or enrolment
// logic (SRP). Injected as an abstraction so it can be mocked in tests.
public interface IEmailService
{
    void SendRegistrationConfirmation(Student student);
    void SendEnrolmentConfirmation(Student student, Course course);
}
