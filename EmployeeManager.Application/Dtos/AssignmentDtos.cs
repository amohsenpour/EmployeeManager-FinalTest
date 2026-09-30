using System.ComponentModel.DataAnnotations;
using EmployeeManager.Core.Models;

namespace EmployeeManager.Application.Dtos;

/// <summary>
///what the api returns for an employee department assignment
/// </summary>
public record AssignmentResponse(
    int AssignmentId,
    int EmployeeId,
    int DepartmentId,
    DateTime AssignmentDate,
    AssignmentStatus Status);

/// <summary>
///request body for post /api/assignment
///status is not included because every new assignment has schedule to start
/// </summary>
public class CreateAssignmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "EmployeeId must be a positive integer.")]
    public int EmployeeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "DepartmentId must be a positive integer.")]
    public int DepartmentId { get; set; }

    [Required]
    public DateTime? AssignmentDate { get; set; }
}

/// <summary>
/// request body for Put /api/assignment/{id}.
///only AssignmentDate and Status can be changed.
/// </summary>
public class UpdateAssignmentRequest
{
    [Required]
    public DateTime? AssignmentDate { get; set; }

    [Required]
    public AssignmentStatus? Status { get; set; }
}