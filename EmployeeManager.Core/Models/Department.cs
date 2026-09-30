namespace EmployeeManager.Core.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    //temporary employee assignment for this department
    public ICollection<EmployeeDepartmentAssignment> EmployeeAssignments { get; set; }
        = new List<EmployeeDepartmentAssignment>();
}