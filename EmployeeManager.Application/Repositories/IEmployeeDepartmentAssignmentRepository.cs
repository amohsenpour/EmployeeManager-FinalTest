using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Repositories;

//defines the database operations needed for temporary department assignment
public interface IEmployeeDepartmentAssignmentRepository
{
    //finds an assignment by its id
    Task<EmployeeDepartmentAssignment?> GetAssignmentById(
        int id,
        CancellationToken cancellationToken);

    // creete and save new assignment
    Task<EmployeeDepartmentAssignment> CreateAssignment(
        EmployeeDepartmentAssignment assignment,
        CancellationToken cancellationToken);

    // update current assignment
    Task<EmployeeDepartmentAssignment?> UpdateAssignment(
        int id,
        DateTime assignmentDate,
        AssignmentStatus status,
        CancellationToken cancellationToken);

    //saveing changes made to current assignment
    Task SaveChanges(CancellationToken cancellationToken);

    // delete assignment if exists
    Task<bool> DeleteAssignmentIfExists(
        int id,
        CancellationToken cancellationToken);

    //check if employee exists
    Task<bool> EmployeeExists(
        int employeeId,
        CancellationToken cancellationToken);

    //checks if department exists
    Task<bool> DepartmentExists(
        int departmentId,
        CancellationToken cancellationToken);

    // checks if another assignment exists for employee.
    Task<bool> HasOtherActiveAssignment(
        int employeeId,
        int assignmentId,
        CancellationToken cancellationToken);

    // gets employee to check permanent deparment 
    Task<Employee?> GetEmployeeById(
        int employeeId,
        CancellationToken cancellationToken);
}