using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManager.Infrastructure;

public class EmployeeDepartmentAssignmentRepository
    : IEmployeeDepartmentAssignmentRepository
{
    protected readonly AppDbContext _context;

    public EmployeeDepartmentAssignmentRepository(AppDbContext context)
    {
        _context = context;
    }

    // get assignment by id
    public async Task<EmployeeDepartmentAssignment?> GetAssignmentById(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeDepartmentAssignments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);
    }

    // create new assignment
    public async Task<EmployeeDepartmentAssignment> CreateAssignment(
        EmployeeDepartmentAssignment assignment,
        CancellationToken cancellationToken = default)
    {
        _context.EmployeeDepartmentAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return assignment;
    }

    // update current assignment
    public async Task<EmployeeDepartmentAssignment?> UpdateAssignment(
        int id,
        DateTime assignmentDate,
        AssignmentStatus status,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.EmployeeDepartmentAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        if (assignment is null)
            return null;

        assignment.AssignmentDate = assignmentDate;
        assignment.Status = status;

        await _context.SaveChangesAsync(cancellationToken);

        return assignment;
    }

    // save current changes
    public async Task SaveChanges(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    // delete assignment if exists
    public async Task<bool> DeleteAssignmentIfExists(
        int id,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.EmployeeDepartmentAssignments
            .FirstOrDefaultAsync(a => a.AssignmentId == id, cancellationToken);

        if (assignment is null)
            return false;

        _context.EmployeeDepartmentAssignments.Remove(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // check if employee exists
    public async Task<bool> EmployeeExists(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == employeeId, cancellationToken);
    }

    // get employee by id
    public async Task<Employee?> GetEmployeeById(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
    }

    // check if department exists
    public async Task<bool> DepartmentExists(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId, cancellationToken);
    }

    // check if another active assignment exists
    public async Task<bool> HasOtherActiveAssignment(
        int employeeId,
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeDepartmentAssignments
            .AsNoTracking()
            .AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.Status == AssignmentStatus.Active &&
                a.AssignmentId != assignmentId,
                cancellationToken);
    }
}