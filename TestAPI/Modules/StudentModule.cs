using Carter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TestAPI.Data;
using TestAPI.Entities;
using TestAPI.Middleware;
using TestAPI.Models;

namespace TestAPI.Modules;

public class StudentModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students")
            .WithTags("Students")
            .RequireAuthorization();

        // GET: Get all students
        group.MapGet("/", GetAllStudents)
            .WithName("GetAllStudents")
            .WithOpenApi()
            .Produces<ApiResponse<List<StudentResponse>>>(StatusCodes.Status200OK);

        // GET: Get student by ID
        group.MapGet("/{id:guid}", GetStudentById)
            .WithName("GetStudentById")
            .WithOpenApi()
            .Produces<ApiResponse<StudentResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);

        // POST: Create new student
        group.MapPost("/", CreateStudent)
            .WithName("CreateStudent")
            .WithOpenApi()
            .Produces<ApiResponse<StudentResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        // PUT: Update student
        group.MapPut("/{id:guid}", UpdateStudent)
            .WithName("UpdateStudent")
            .WithOpenApi()
            .Produces<ApiResponse<StudentResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object>>(StatusCodes.Status400BadRequest);

        // DELETE: Delete student
        group.MapDelete("/{id:guid}", DeleteStudent)
            .WithName("DeleteStudent")
            .WithOpenApi()
            .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<object>>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllStudents(
        ApplicationDbContext dbContext,
        ILogger<StudentModule> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching all students");

        var students = await dbContext.Students
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentResponse
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                DateOfBirth = s.DateOfBirth,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} students", students.Count);

        var response = new ApiResponse<List<StudentResponse>>
        {
            Success = true,
            Data = students,
            Message = "Students retrieved successfully"
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetStudentById(
        Guid id,
        ApplicationDbContext dbContext,
        ILogger<StudentModule> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching student with ID: {StudentId}", id);

        var student = await dbContext.Students
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentResponse
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                Email = s.Email,
                DateOfBirth = s.DateOfBirth,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null)
        {
            logger.LogWarning("Student with ID {StudentId} not found", id);
            var notFoundResponse = new ApiResponse<object>
            {
                Success = false,
                Message = $"Student with ID {id} not found"
            };
            return Results.NotFound(notFoundResponse);
        }

        logger.LogInformation("Student with ID {StudentId} retrieved successfully", id);

        var response = new ApiResponse<StudentResponse>
        {
            Success = true,
            Data = student,
            Message = "Student retrieved successfully"
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateStudent(
        [FromBody] CreateStudentRequest request,
        ApplicationDbContext dbContext,
        ILogger<StudentModule> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating new student with email: {Email}", request.Email);

        // Check if email already exists
        var emailExists = await dbContext.Students
            .AnyAsync(s => s.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            logger.LogWarning("Student with email {Email} already exists", request.Email);
            throw new ValidationException($"Student with email {request.Email} already exists");
        }

        var student = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            DateOfBirth = request.DateOfBirth,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student created successfully with ID: {StudentId}", student.Id);

        var studentResponse = new StudentResponse
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            DateOfBirth = student.DateOfBirth,
            PhoneNumber = student.PhoneNumber,
            Address = student.Address,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt,
            IsActive = student.IsActive
        };

        var response = new ApiResponse<StudentResponse>
        {
            Success = true,
            Data = studentResponse,
            Message = "Student created successfully"
        };

        return Results.Created($"/api/students/{student.Id}", response);
    }

    private static async Task<IResult> UpdateStudent(
        Guid id,
        [FromBody] UpdateStudentRequest request,
        ApplicationDbContext dbContext,
        ILogger<StudentModule> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating student with ID: {StudentId}", id);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
        {
            logger.LogWarning("Student with ID {StudentId} not found", id);
            throw new KeyNotFoundException($"Student with ID {id} not found");
        }

        // Check if email already exists for another student
        var emailExists = await dbContext.Students
            .AnyAsync(s => s.Email == request.Email && s.Id != id, cancellationToken);

        if (emailExists)
        {
            logger.LogWarning("Email {Email} is already used by another student", request.Email);
            throw new ValidationException($"Email {request.Email} is already used by another student");
        }

        student.FirstName = request.FirstName;
        student.LastName = request.LastName;
        student.Email = request.Email;
        student.DateOfBirth = request.DateOfBirth;
        student.PhoneNumber = request.PhoneNumber;
        student.Address = request.Address;
        student.IsActive = request.IsActive;
        student.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student with ID {StudentId} updated successfully", id);

        var studentResponse = new StudentResponse
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            DateOfBirth = student.DateOfBirth,
            PhoneNumber = student.PhoneNumber,
            Address = student.Address,
            CreatedAt = student.CreatedAt,
            UpdatedAt = student.UpdatedAt,
            IsActive = student.IsActive
        };

        var response = new ApiResponse<StudentResponse>
        {
            Success = true,
            Data = studentResponse,
            Message = "Student updated successfully"
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteStudent(
        Guid id,
        ApplicationDbContext dbContext,
        ILogger<StudentModule> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting student with ID: {StudentId}", id);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (student is null)
        {
            logger.LogWarning("Student with ID {StudentId} not found", id);
            throw new KeyNotFoundException($"Student with ID {id} not found");
        }

        dbContext.Students.Remove(student);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Student with ID {StudentId} deleted successfully", id);

        var response = new ApiResponse<object>
        {
            Success = true,
            Message = "Student deleted successfully"
        };

        return Results.Ok(response);
    }
}
