namespace EmployeeManager.Core.Models;

public class EmployeeDepartmentAssignment
{
    // Primary key.
    public int AssignmentId { get; set; }

    // Foreign key to Employee.
    public int EmployeeId { get; set; }

    // Foreign key to Department.
    public int DepartmentId { get; set; }

    // Date the temporary assignment becomes effective.
    public DateTime AssignmentDate { get; set; }

    // Current status of the assignment.
    public AssignmentStatus Status { get; set; }

    // Navigation property to the related employee.
    public Employee Employee { get; set; } = null!;

    // Navigation property to the related department.
    public Department Department { get; set; } = null!;
}