using System.ComponentModel.DataAnnotations;

namespace TestAPI.Models;

public record CreateStudentRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; init; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; init; }
}

public record UpdateStudentRequest
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    public string LastName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; init; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20)]
    public string PhoneNumber { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; init; }

    public bool IsActive { get; init; } = true;
}

public record StudentResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Address { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsActive { get; init; }
}

public record LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string>? Errors { get; init; }
}
