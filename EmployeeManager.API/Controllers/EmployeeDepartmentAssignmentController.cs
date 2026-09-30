using EmployeeManager.Application.Dtos;
using EmployeeManager.Application.Repositories;
using EmployeeManager.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManager.API.Controllers;

[Route("api/assignment")]
[ApiController]
public class EmployeeDepartmentAssignmentController : ControllerBase
{
    private readonly ILogger<EmployeeDepartmentAssignmentController> _logger;
    private readonly IEmployeeDepartmentAssignmentRepository _assignmentRepository;

    public EmployeeDepartmentAssignmentController(
        ILogger<EmployeeDepartmentAssignmentController> logger,
        IEmployeeDepartmentAssignmentRepository assignmentRepository)
    {
        _logger = logger;
        _assignmentRepository = assignmentRepository;
    }

    // get assignment by id
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetAssignmentById(
        int id,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository
            .GetAssignmentById(id, cancellationToken);

        if (assignment is null)
            return NotFound();

        return Ok(ToResponse(assignment));
    }

    // create new assignment
    [HttpPost]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateAssignment(
        [FromBody] CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        // check employee exists
        var employee = await _assignmentRepository
            .GetEmployeeById(request.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return BadRequest(CreateProblem(
                "Employee does not exist.",
                StatusCodes.Status400BadRequest));
        }

        // check department exists
        if (!await _assignmentRepository
            .DepartmentExists(request.DepartmentId, cancellationToken))
        {
            return BadRequest(CreateProblem(
                "Department does not exist.",
                StatusCodes.Status400BadRequest));
        }

        // BR-02 - date cannot be more than 31 days in future
        if (request.AssignmentDate!.Value.Date >
            DateTime.UtcNow.Date.AddDays(31))
        {
            return BadRequest(CreateProblem(
                "Assignment date cannot be more than 31 days in the future.",
                StatusCodes.Status400BadRequest));
        }

        // BR-05 - employee cannot be assigned to permanent department
        if (employee.DepartmentId == request.DepartmentId)
        {
            return BadRequest(CreateProblem(
                "Employee cannot be assigned to their permanent department.",
                StatusCodes.Status400BadRequest));
        }

        // BR-03 - every new assignment starts as Scheduled
        var assignment = new EmployeeDepartmentAssignment
        {
            EmployeeId = request.EmployeeId,
            DepartmentId = request.DepartmentId,
            AssignmentDate = request.AssignmentDate.Value,
            Status = AssignmentStatus.Scheduled
        };

        var created = await _assignmentRepository
            .CreateAssignment(assignment, cancellationToken);

        return CreatedAtAction(
            nameof(GetAssignmentById),
            new { id = created.AssignmentId },
            ToResponse(created));
    }

    // update assignment date and status
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateAssignment(
        int id,
        [FromBody] UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        // first check assignment exists
        var existing = await _assignmentRepository
            .GetAssignmentById(id, cancellationToken);

        if (existing is null)
            return NotFound();

        // BR-02 - check assignment date
        if (request.AssignmentDate!.Value.Date >
            DateTime.UtcNow.Date.AddDays(31))
        {
            return BadRequest(CreateProblem(
                "Assignment date cannot be more than 31 days in the future.",
                StatusCodes.Status400BadRequest));
        }

        var newStatus = request.Status!.Value;

        // BR-04 - check if status change is allowed
        if (!IsValidStatusTransition(existing.Status, newStatus))
        {
            return BadRequest(CreateProblem(
                "Invalid assignment status transition.",
                StatusCodes.Status400BadRequest));
        }

        // BR-01 - only one Active assignment for employee
        if (newStatus == AssignmentStatus.Active &&
            await _assignmentRepository.HasOtherActiveAssignment(
                existing.EmployeeId,
                existing.AssignmentId,
                cancellationToken))
        {
            return Conflict(CreateProblem(
                "Employee already has an active assignment.",
                StatusCodes.Status409Conflict));
        }

        var updated = await _assignmentRepository.UpdateAssignment(
            id,
            request.AssignmentDate.Value,
            newStatus,
            cancellationToken);

        if (updated is null)
            return NotFound();

        return Ok(ToResponse(updated));
    }

    // delete assignment
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAssignment(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await _assignmentRepository
            .DeleteAssignmentIfExists(id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // check allowed status transitions
    private static bool IsValidStatusTransition(
        AssignmentStatus currentStatus,
        AssignmentStatus newStatus)
    {
        // same status is allowed
        if (currentStatus == newStatus)
            return true;

        return currentStatus switch
        {
            AssignmentStatus.Scheduled =>
                newStatus == AssignmentStatus.Active ||
                newStatus == AssignmentStatus.Cancelled,

            AssignmentStatus.Active =>
                newStatus == AssignmentStatus.Completed ||
                newStatus == AssignmentStatus.Cancelled,

            AssignmentStatus.Completed => false,

            AssignmentStatus.Cancelled => false,

            _ => false
        };
    }

    // create RFC 7807 error response
    private static ProblemDetails CreateProblem(
        string detail,
        int status)
    {
        return new ProblemDetails
        {
            Detail = detail,
            Status = status
        };
    }

    // convert entity to response dto
    private static AssignmentResponse ToResponse(
        EmployeeDepartmentAssignment assignment)
    {
        return new AssignmentResponse(
            assignment.AssignmentId,
            assignment.EmployeeId,
            assignment.DepartmentId,
            assignment.AssignmentDate,
            assignment.Status);
    }
}