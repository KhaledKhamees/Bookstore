using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Models;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(UserManager<ApplicationUser> userManager,
                              RoleManager<ApplicationRole> roleManager,
                              IConfiguration configuration,
                              ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
            {
                _logger.LogWarning("Registration failed: User {Username} already exists", model.Username);
                return BadRequest(new { Status = "Error", Message = "User already exists!" });
            }

            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };

            // Validate password using proper validator
            var passwordValidator = new PasswordValidator<ApplicationUser>();
            var passwordValidation = await passwordValidator.ValidateAsync(_userManager, user, model.Password);

            if (!passwordValidation.Succeeded)
            {
                _logger.LogWarning("Password validation failed for user {Username}", model.Username);
                return BadRequest(new
                {
                    Status = "Error",
                    Message = "Password does not meet requirements.",
                    Errors = passwordValidation.Errors
                });
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                _logger.LogError("User creation failed for {Username}: {Errors}", model.Username, result.Errors);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Status = "Error",
                    Message = "User creation failed! Please check user details and try again.",
                    Errors = result.Errors
                });
            }

            // Assign role to user
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole(model.Role));
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Role creation failed for {Role}: {Errors}", model.Role, roleResult.Errors);
                }
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addToRoleResult.Succeeded)
            {
                _logger.LogWarning("Failed to add role {Role} to user {Username}", model.Role, model.Username);
            }

            _logger.LogInformation("User {Username} registered successfully with role {Role}", model.Username, model.Role);
            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                    // Use username as identifier instead of database ID for security
                    new System.Security.Claims.Claim("sub", user.UserName)
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaims);

                _logger.LogInformation("User {Username} logged in successfully", model.Username);
                return Ok(new
                {
                    token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    username = user.UserName,
                    email = user.Email,
                    roles = userRoles
                });
            }

            _logger.LogWarning("Failed login attempt for user {Username}", model.Username);
            return Unauthorized(new { Message = "Invalid username or password" });
        }

        [HttpPost("add-role")]
        public async Task<IActionResult> AddRole([FromBody] RoleDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate role name format
            if (string.IsNullOrWhiteSpace(model.RoleName) || model.RoleName.Length < 2)
            {
                return BadRequest(new { Message = "Role name must be at least 2 characters long" });
            }

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
            {
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole(model.RoleName));
                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", model.RoleName, roleResult.Errors);
                    return StatusCode(500, new { Message = "Failed to create role" });
                }
                _logger.LogInformation("Role {RoleName} created successfully", model.RoleName);
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                _logger.LogWarning("User {UserName} not found for role assignment", model.UserName);
                return NotFound(new { Message = "User not found" });
            }

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to add role {RoleName} to user {UserName}: {Errors}",
                    model.RoleName, model.UserName, result.Errors);
                return StatusCode(500, new { Message = "Failed to add role to user" });
            }

            _logger.LogInformation("Role {RoleName} added to user {UserName} successfully", model.RoleName, model.UserName);
            return Ok(new { Message = $"Role {model.RoleName} added to {model.UserName}" });
        }

        private System.IdentityModel.Tokens.Jwt.JwtSecurityToken GetToken(List<System.Security.Claims.Claim> authClaims)
        {
            var authSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var expiryInMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "180");

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.Now.AddMinutes(expiryInMinutes),
                claims: authClaims,
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    authSigningKey,
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
            );
            return token;
        }
    }
}