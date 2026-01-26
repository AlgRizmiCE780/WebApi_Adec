namespace WebApi_Adec.Models.Entity
{
    public class Student
    {
        public Guid Id { get; set; }

        public required string Name { get; set; }

        public required string Email { get; set; }
        
        public DateTime EnrollmentDate { get; set; }

    }
}
