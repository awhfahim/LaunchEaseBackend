// using Acm.Api.DTOs.Requests;
// using Acm.Application.Interfaces;
// using Acm.Application.Services;
// using Acm.Domain.Entities;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
//
// namespace Acm.Api.Controllers;
//
// /// <summary>
// /// Example controller demonstrating the Unit of Work pattern usage
// /// </summary>
// [ApiController]
// [Route("api/[controller]")]
// public class ExampleUsersController : ControllerBase
// {
//     private readonly IAcmUnitOfWork _unitOfWork;
//     private readonly UserManagementService _userManagementService;
//     private readonly ILogger<ExampleUsersController> _logger;
//
//     public ExampleUsersController(
//         IAcmUnitOfWork unitOfWork,
//         UserManagementService userManagementService,
//         ILogger<ExampleUsersController> logger)
//     {
//         _unitOfWork = unitOfWork;
//         _userManagementService = userManagementService;
//         _logger = logger;
//     }
//
//     /// <summary>
//     /// Get user by ID - demonstrates simple repository usage
//     /// </summary>
//     [HttpGet("{id:guid}")]
//     public async Task<ActionResult<User>> GetUser(Guid id, CancellationToken cancellationToken)
//     {
//         try
//         {
//             var user = await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
//             
//             if (user == null)
//             {
//                 return NotFound($"User with ID {id} not found");
//             }
//
//             return Ok(user);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error retrieving user {UserId}", id);
//             return StatusCode(500, "Internal server error");
//         }
//     }
//
//     /// <summary>
//     /// Create user with tenant assignment - demonstrates transaction usage
//     /// </summary>
//     [HttpPost]
//     public async Task<IActionResult> CreateUser(
//         [FromBody] CreateUserRequest request, 
//         CancellationToken cancellationToken)
//     {
//         try
//         {
//             var user = new User
//             {
//                 Id = Guid.NewGuid(),
//                 Email = request.Email,
//                 FirstName = request.FirstName,
//                 LastName = request.LastName,
//                 PasswordHash = request.Password,
//                 SecurityStamp = Guid.NewGuid().ToString(),
//                 IsEmailConfirmed = false,
//                 IsGloballyLocked = false,
//                 GlobalAccessFailedCount = 0
//             };
//
//             var userId = await _userManagementService.CreateUserWithTenantAsync(
//                 user, 
//                 request.TenantId, 
//                 request.RoleId, 
//                 cancellationToken);
//
//             return CreatedAtAction(
//                 nameof(GetUser), 
//                 new { id = userId }, 
//                 new CreateUserResponse { UserId = userId });
//         }
//         catch (InvalidOperationException ex)
//         {
//             return BadRequest(ex.Message);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error creating user {Email}", request.Email);
//             return StatusCode(500, "Internal server error");
//         }
//     }
//
//     /// <summary>
//     /// Update user security information - demonstrates transaction usage
//     /// </summary>
//     [HttpPut("{id:guid}/security")]
//     public async Task<ActionResult> UpdateUserSecurity(
//         Guid id,
//         [FromBody] UpdateUserSecurityRequest request,
//         CancellationToken cancellationToken)
//     {
//         try
//         {
//             await _userManagementService.UpdateUserSecurityInfoAsync(
//                 id, 
//                 request.PasswordHash, 
//                 request.SecurityStamp, 
//                 cancellationToken);
//
//             return NoContent();
//         }
//         catch (InvalidOperationException ex)
//         {
//             return NotFound(ex.Message);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error updating security for user {UserId}", id);
//             return StatusCode(500, "Internal server error");
//         }
//     }
//
//     /// <summary>
//     /// Get user with tenants - demonstrates complex read operations
//     /// </summary>
//     [HttpGet("{id:guid}/tenants")]
//     public async Task<ActionResult<UserWithTenantsDto>> GetUserWithTenants(
//         Guid id, 
//         CancellationToken cancellationToken)
//     {
//         try
//         {
//             var result = await _userManagementService.GetUserWithTenantsAsync(id, cancellationToken);
//             return Ok(result);
//         }
//         catch (InvalidOperationException ex)
//         {
//             return NotFound(ex.Message);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error retrieving user {UserId} with tenants", id);
//             return StatusCode(500, "Internal server error");
//         }
//     }
//
//     /// <summary>
//     /// Check if email exists - demonstrates simple query operations
//     /// </summary>
//     [HttpGet("email-exists")]
//     public async Task<ActionResult<EmailExistsResponse>> CheckEmailExists(
//         [FromQuery] string email,
//         CancellationToken cancellationToken)
//     {
//         try
//         {
//             if (string.IsNullOrWhiteSpace(email))
//             {
//                 return BadRequest("Email is required");
//             }
//
//             var exists = await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken);
//             
//             return Ok(new EmailExistsResponse { Exists = exists });
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error checking email existence for {Email}", email);
//             return StatusCode(500, "Internal server error");
//         }
//     }
// }
//
// #region DTOs
//
// // public class CreateUserRequest
// // {
// //     public required string Email { get; set; }
// //     public required string FirstName { get; set; }
// //     public required string LastName { get; set; }
// //     public required string Password { get; set; }
// //     public required Guid TenantId { get; set; }
// //     public required Guid RoleId { get; set; }
// // }
//
// // public class CreateUserResponse
// // {
// //     public Guid UserId { get; set; }
// // }
// //
// // public class UpdateUserSecurityRequest
// // {
// //     public required string PasswordHash { get; set; }
// //     public required string SecurityStamp { get; set; }
// // }
// //
// // public class EmailExistsResponse
// // {
// //     public bool Exists { get; set; }
// // }
//
// #endregion
