using System.Net;
using System.Net.Http.Json;
using EmployeeManager.Application.Dtos;
using EmployeeManager.Core.Models;
using EmployeeManagerApi.IntegrationTests.Urls;
using FluentAssertions;

namespace EmployeeManagerApi.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class EmployeeDepartmentAssignmentControllerTests
{
    private readonly HttpClient _client;

    public EmployeeDepartmentAssignmentControllerTests(ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }
    [Fact]
    public async Task CreateAssignment_WhenStatusIsSent_ShouldStillCreateAsScheduled()
    {
        // arrange
        var request = new
        {
            employeeId = 1,
            departmentId = 2,
            assignmentDate = DateTime.UtcNow,
            status = "Active"
        };

        // act
        var response = await _client.PostAsJsonAsync(
            "/api/assignment",
            request);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        created.Should().NotBeNull();
        created!.Status.Should().Be(AssignmentStatus.Scheduled);
    }

    [Fact]
    public async Task CreateAssignment_WithDateMoreThan31Days_ShouldReturnBadRequest()
    {
        // arrange
        var request = new CreateAssignmentRequest
        {
            EmployeeId = 2,
            DepartmentId = 3,
            AssignmentDate = DateTime.UtcNow.AddDays(32)
        };

        // act
        var response = await _client.PostAsJsonAsync(
            "/api/assignment",
            request);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAssignment_ToPermanentDepartment_ShouldReturnBadRequest()
    {
        // arrange
        var request = new CreateAssignmentRequest
        {
            EmployeeId = 3,
            DepartmentId = 3,
            AssignmentDate = DateTime.UtcNow
        };

        // act
        var response = await _client.PostAsJsonAsync(
            "/api/assignment",
            request);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAssignment_FromScheduledToActive_ShouldReturnOk()
    {
        // arrange - first create a scheduled assignment
        var createRequest = new CreateAssignmentRequest
        {
            EmployeeId = 4,
            DepartmentId = 2,
            AssignmentDate = DateTime.UtcNow
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/assignment",
            createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        created.Should().NotBeNull();

        // prepare update to Active
        var updateRequest = new UpdateAssignmentRequest
        {
            AssignmentDate = created!.AssignmentDate,
            Status = AssignmentStatus.Active
        };

        // act
        var response = await _client.PutAsJsonAsync(
            $"/api/assignment/{created.AssignmentId}",
            updateRequest);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(AssignmentStatus.Active);
    }

    [Fact]
    public async Task UpdateAssignment_FromScheduledToCompleted_ShouldReturnBadRequest()
    {
        // arrange - create a new assignment
        var createRequest = new CreateAssignmentRequest
        {
            EmployeeId = 5,
            DepartmentId = 2,
            AssignmentDate = DateTime.UtcNow
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/assignment",
            createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        created.Should().NotBeNull();

        // try to change directly from Scheduled to Completed
        var updateRequest = new UpdateAssignmentRequest
        {
            AssignmentDate = created!.AssignmentDate,
            Status = AssignmentStatus.Completed
        };

        // act
        var response = await _client.PutAsJsonAsync(
            $"/api/assignment/{created.AssignmentId}",
            updateRequest);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAssignment_FromActiveToCompleted_ShouldReturnOk()
    {
        // arrange - create a new assignment
        var createRequest = new CreateAssignmentRequest
        {
            EmployeeId = 6,
            DepartmentId = 2,
            AssignmentDate = DateTime.UtcNow
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/assignment",
            createRequest);

        var created = await createResponse.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        created.Should().NotBeNull();

        // first update: Scheduled -> Active
        var activeRequest = new UpdateAssignmentRequest
        {
            AssignmentDate = created!.AssignmentDate,
            Status = AssignmentStatus.Active
        };

        var activeResponse = await _client.PutAsJsonAsync(
            $"/api/assignment/{created.AssignmentId}",
            activeRequest);

        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // second update: Active -> Completed
        var completedRequest = new UpdateAssignmentRequest
        {
            AssignmentDate = created.AssignmentDate,
            Status = AssignmentStatus.Completed
        };

        var completedResponse = await _client.PutAsJsonAsync(
            $"/api/assignment/{created.AssignmentId}",
            completedRequest);

        // assert
        completedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var completed = await completedResponse.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        completed.Should().NotBeNull();
        completed!.Status.Should().Be(AssignmentStatus.Completed);
    }

    [Fact]
    public async Task UpdateAssignment_WhenAnotherActiveAssignmentExists_ShouldReturnConflict()
    {
        // arrange - create first assignment
        var firstRequest = new CreateAssignmentRequest
        {
            EmployeeId = 1,
            DepartmentId = 2,
            AssignmentDate = DateTime.UtcNow
        };

        var firstResponse = await _client.PostAsJsonAsync(
            "/api/assignment",
            firstRequest);

        var firstAssignment = await firstResponse.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        firstAssignment.Should().NotBeNull();

        // make first assignment Active
        var firstUpdate = new UpdateAssignmentRequest
        {
            AssignmentDate = firstAssignment!.AssignmentDate,
            Status = AssignmentStatus.Active
        };

        var firstActiveResponse = await _client.PutAsJsonAsync(
            $"/api/assignment/{firstAssignment.AssignmentId}",
            firstUpdate);

        firstActiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // create second assignment for same employee
        var secondRequest = new CreateAssignmentRequest
        {
            EmployeeId = 1,
            DepartmentId = 3,
            AssignmentDate = DateTime.UtcNow
        };

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/assignment",
            secondRequest);

        var secondAssignment = await secondResponse.Content
            .ReadFromJsonAsync<AssignmentResponse>(ApiTestFixture.JsonOptions);

        secondAssignment.Should().NotBeNull();

        // try to make second assignment Active too
        var secondUpdate = new UpdateAssignmentRequest
        {
            AssignmentDate = secondAssignment!.AssignmentDate,
            Status = AssignmentStatus.Active
        };

        // act
        var response = await _client.PutAsJsonAsync(
            $"/api/assignment/{secondAssignment.AssignmentId}",
            secondUpdate);

        // assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}