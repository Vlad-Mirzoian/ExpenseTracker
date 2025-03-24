using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpenseTracker.Data;
using ExpenseTracker.Data.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using ExpenseTracker.API.Interface;
using Org.BouncyCastle.Crypto.Generators;

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
        public string Login { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
    }
    public class LoginRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
    // Регистрация пользователя
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userRepository.GetUserByLoginAsync(request.Login);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Користувач із таким логіном вже існує" });
        }

        // Хешируем пароль перед сохранением
        var newUser = new User
        {
            Login = request.Login,
            Token = request.Token,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(newUser);

        return Ok(new { message = "Користувач зареєстрований успішно!" });
    }

    // Авторизация пользователя
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
    // Генерация JWT
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
            expires: DateTime.Now.AddDays(1), // Токен будет действовать 1 день
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
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
            // Додайте інші необхідні поля
        });
    }

    // Выход (просто на клиенте удаляем токен)
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Тут ничего не сохраняем, токен просто удаляется на клиенте
        return Ok(new { message = "Выход выполнен успешно" });
    }
}
