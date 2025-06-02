using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseTracker.Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.API
{
    [Route("api/auth")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _config;

        public UserController(IUserRepository userRepository, IConfiguration config)
        {
            _userRepository = userRepository;
            _config = config;
        }
        public class RegisterRequest
        {
            [Required(ErrorMessage = "Логін обов'язковий")]
            [MinLength(4, ErrorMessage = "Логін повинен містити щонайменше 4 символи")]
            public string Login { get; set; }

            [Required(ErrorMessage = "Пароль обов'язковий")]
            [MinLength(8, ErrorMessage = "Пароль повинен містити щонайменше 8 символів")]
            public string Password { get; set; }
            public string Token { get; set; }
        }
        public class LoginRequest
        {
            public string Login { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _userRepository.GetUserByLoginAsync(request.Login);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Користувач із таким логіном вже існує" });
            }

            var newUser = new User
            {
                Login = request.Login,
                Token = request.Token,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _userRepository.AddAsync(newUser);

            return Ok(new { message = "Користувач зареєстрований успішно!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var existingUser = await _userRepository.GetUserByLoginAsync(request.Login);

            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(request.Password, existingUser.PasswordHash))
            {
                return Unauthorized(new { message = "Невірний логін або пароль" });
            }

            var token = GenerateJwtToken(existingUser);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"])); // SecretKey из конфигурации
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized(new { message = "Користувач не авторизований" });
            }

            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));

            if (user == null)
            {
                return NotFound(new { message = "Користувача не знайдено" });
            }

            return Ok(new
            {
                user.Id,
                user.Login,
                user.Token
            });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Выход выполнен успешно" });
        }
    }
}
