using BackendAPI.Contexts;
using BackendAPI.DTO.User;
using BackendAPI.Models;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using BackendAPI.DTO.Task;

namespace BackendAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        // Diccionario para almacenar los tokens de restablecimiento de contraseña
        private static readonly ConcurrentDictionary<string, (string token, DateTime expiration)> passwordResetTokens = new ConcurrentDictionary<string, (string token, DateTime expiration)>();

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("email")]
        public async Task<IActionResult> FindByEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email no valido" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return NotFound(new { message = "No se encontró usuario por email" });
            }

            return Ok(user);

        }

        [HttpGet("username")]
        public async Task<IActionResult> FindByUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest(new { message = "Nombre de usuario no válido" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

            if (user == null)
            {
                return NotFound(new { message = "No se encontró nombre de usuario" });
            }

            // Genera solo un nombre de usuario
            var suggestions = await generateUserNameSuggestions(username, 3);

            return Ok(new { user, suggestionsMessage = suggestions });
        }

        [HttpGet("dni")]
        public async Task<IActionResult> FindByDni(int dni)
        {
            if (dni == 0)
            {
                return BadRequest(new { message = "DNI no valido" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Dni == dni);

            if (user == null)
            {
                return NotFound(new { message = "No se encontró usuario por DNI" });
            }

            return Ok(user);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Error en registro." });
            }

            // Validación del formato de email
            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Email, emailRegex))
            {
                return BadRequest(new { Message = "El formato del correo electrónico no es válido." });
            }

            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            var existingUserName = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);

            var existingDni = await _context.Users.FirstOrDefaultAsync(u => u.Dni == model.Dni);

            if (existingEmail != null)
            {
                return Conflict(new { message = "Correo ya registrado." });
            }

            if (existingUserName != null)
            {
                // Llama al método para generar [3] sugerencias de usuario
                var suggestions = await generateUserNameSuggestions(model.UserName, 3);

                return Conflict(new
                {
                    message = "Nombre de usuario ya registrado.",

                    // Mensaje que muestra las sugerencias de usuario
                    suggestionsMessage = suggestions
                });
            }

            if (existingDni != null)
            {
                return Conflict(new { message = "DNI ya registrado." });
            }

            UserModel userRegister;

            if (model.Password != null)
            {
                // Regexp para la contraseña (entre 6-12 caracteres, al menos una letra, un número y opcionalmente, símbolos)
                var passwordRegex = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d!@#\$%\^&\*\(\)_\+\[\]{}|;:,.<>?\^()\[\]|\-]{6,12}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Password, passwordRegex))
                {
                    return BadRequest(new { message = "La contraseña debe tener entre 6 y 12 caracteres, y contener al menos una letra y un número." });
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

                userRegister = new UserModel
                {
                    Name = model.Name,
                    UserName = model.UserName,
                    Dni = model.Dni,
                    Email = model.Email,
                    Password = hashedPassword,
                    RoleId = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                return Conflict(new { message = "Ingrese una contraseña." });
            }

            try
            {
                _context.Users.Add(userRegister);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Usuario registrado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Método que genera sugerencias de usuario
        private async Task<List<string>> generateUserNameSuggestions(string? userNameBase, int? maxSuggestions)
        {
            var suggestions = new List<string>();

            // Busca según cantidad de sugerencias deseadas
            for (int i = 1; suggestions.Count < maxSuggestions; i++)
            {
                // Concatena el usuario con 3 digitos aleatorios 
                string suggestion = $"{userNameBase}{new Random().Next(100, 999)}";

                //Verifica si el usuario sugerido existe
                bool existUserName = await _context.Users.AnyAsync(u => u.UserName == suggestion);

                if (!existUserName)
                {
                    suggestions.Add(suggestion);
                }
            }
            return suggestions;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Identifier) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { message = "Datos de inicio de sesión incompletos o incorrectos." });
            }

            // Intentar convertir el Identifier a un número (Dni)
            bool isDni = int.TryParse(model.Identifier, out int dni);

            // Buscar al usuario dependiendo si el Identifier es Dni o no
            var user = await _context.Users
                                      .Include(u => u.Role) // Incluir la relación con el rol
                                      .FirstOrDefaultAsync(u =>
                                           (u.Email == model.Identifier || u.UserName == model.Identifier) ||
                                           (isDni && u.Dni == dni));

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
            {
                return Unauthorized(new { message = "Correo electrónico, nombre de usuario o contraseña incorrectos." });
            }

            return Ok(new
            {
                message = "Inicio de sesión exitoso",
                user = new
                {
                    user.UserId,
                    user.Name,
                    user.Dni,
                    user.Email,
                    user.UserName,
                    RoleName = user?.Role?.Name
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Usuario no encontrado." });
            }

            var token = GenerateResetToken();
            var expiration = DateTime.UtcNow.AddHours(2);
            var resetLink = $"http://integrador.somee.com/reset-password?email={user.Email}&token={WebUtility.UrlEncode(token)}";

            // Almacena o actualiza el token para el usuario
            passwordResetTokens[user.Email] = (token, expiration);

            await SendResetPasswordEmail(user.Email, resetLink);

            return Ok(new { message = "Correo enviado con éxito." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!passwordResetTokens.TryGetValue(request.Email, out var tokenInfo) || tokenInfo.token != request.Token || tokenInfo.expiration < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Token inválido o expirado." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Usuario no encontrado." });
            }

            // Regexp para la contraseña (entre 6-12 caracteres, al menos una letra, un número y opcionalmente, símbolos)
            var passwordRegex = @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d!@#\$%\^&\*\(\)_\+\[\]{}|;:,.<>?\^()\[\]|\-]{6,12}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.NewPassword, passwordRegex))
            {
                return BadRequest(new { message = "La contraseña debe tener entre 6 y 12 caracteres, y contener al menos una letra y un número." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.Password = hashedPassword;

            // Elimina el token después de usarlo
            passwordResetTokens.TryRemove(request.Email, out _);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña restablecida correctamente." });
        }

        private string GenerateResetToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private async Task SendResetPasswordEmail(string email, string resetLink)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = int.Parse(_configuration["Email:Port"]),
                Credentials = new NetworkCredential(_configuration["Email:Username"], _configuration["Email:Password"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration["Email:Username"]),
                // Cuerpo del correo
                Subject = "Restablecer Contraseña",
                // Body = $"Para restablecer tu contraseña, haz clic en el siguiente enlace: {resetLink}",
                Body = GetEmailBody(email, resetLink),
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }

        private string GetEmailBody(string userEmail, string resetLink)
        {
            return $@"
    <!DOCTYPE html>
    <html xmlns='http://www.w3.org/1999/xhtml'>
    <head>
      <title></title>
      <meta http-equiv='X-UA-Compatible' content='IE=edge'>
      <meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
      <meta name='viewport' content='width=device-width, initial-scale=1.0'>
      <style type='text/css'>
        #outlook a {{ padding: 0; }}
        .ReadMsgBody {{ width: 100%; }}
        .ExternalClass {{ width: 100%; }}
        .ExternalClass * {{ line-height: 100%; }}
        body {{ margin: 0; padding: 0; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; }}
        table, td {{ border-collapse: collapse; mso-table-lspace: 0pt; mso-table-rspace: 0pt; }}
      </style>
      <style type='text/css'>
        @media only screen and (max-width:480px) {{
          @-ms-viewport {{ width: 320px; }}
          @viewport {{ width: 320px; }}
        }}
      </style>
      <link href='https://fonts.googleapis.com/css2?family=Open+Sans:wght@400;600&display=swap' rel='stylesheet' type='text/css'>
      <style type='text/css'>
        @import url('https://fonts.googleapis.com/css2?family=Open+Sans:wght@400;600&display=swap');
      </style>
      <style type='text/css'>
        @media only screen and (max-width:595px) {{
          .container {{ width: 100% !important; }}
          .button {{ display: block !important; width: auto !important; }}
        }}
      </style>
    </head>
    <body style='font-family: Open Sans, sans-serif; background: #E5E5E5;'>
      <table width='100%' cellspacing='0' cellpadding='0' border='0' align='center' bgcolor='#F6FAFB'>
        <tbody>
          <tr>
            <td valign='top' align='center'>
              <table class='container' width='600' cellspacing='0' cellpadding='0' border='0'>
                <tbody>
                  <tr>
                    <td style='padding:48px 0 30px 0; text-align: center; font-size: 14px; color: #4C83EE;'></td>
                  </tr>
                  <tr>
                    <td class='main-content' style='padding: 48px 30px 40px; color: #000000;' bgcolor='#ffffff'>
                      <table width='100%' cellspacing='0' cellpadding='0' border='0'>
                        <tbody>
                          <tr>
                            <td style='padding: 0 0 24px 0; font-size: 18px; line-height: 150%; font-weight: bold; color: #000000; letter-spacing: 0.01em;'>
                              ¡Hola! ¿Olvidaste tu contraseña?
                            </td>
                          </tr>
                          <tr>
                            <td style='padding: 0 0 10px 0; font-size: 14px; line-height: 150%; font-weight: 400; color: #000000; letter-spacing: 0.01em;'>
                              Recibimos una solicitud de restablecimiento de contraseña para su cuenta: <span style='color: #4C83EE;'>{userEmail}</span>.
                            </td>
                          </tr>
                          <tr>
                            <td style='padding: 0 0 16px 0; font-size: 14px; line-height: 150%; font-weight: 700; color: #000000; letter-spacing: 0.01em;'>
                              Haga clic en el botón de abajo para continuar.
                            </td>
                          </tr>
                          <tr>
                            <td style='padding: 0 0 24px 0;'>
                              <a class='button' href='{resetLink}' title='Reset Password' style='width: 100%; background: #e84c3d; text-decoration: none; display: inline-block; padding: 10px 0; color: #fff; font-size: 14px; line-height: 21px; text-align: center; font-weight: bold; border-radius: 7px;'>Restablecer contraseña</a>
                            </td>
                          </tr>
                          <tr>
                            <td style='padding: 0 0 10px 0; font-size: 14px; line-height: 150%; font-weight: 400; color: #000000; letter-spacing: 0.01em;'>
                              El enlace para restablecer contraseña solo es válido durante las próximas 2 horas.
                            </td>
                          </tr>
                          <tr>
                            <td style='padding: 0 0 60px 0; font-size: 14px; line-height: 150%; font-weight: 400; color: #000000; letter-spacing: 0.01em;'>
                              Si no solicitó el restablecimiento de contraseña, ignore este mensaje o comuníquese con nuestro soporte al <a href='mailto:todoist.cibertec@gmail.com'>support_todoist</a>.
                            </td>
                          </tr>
                          <tr>
                            <td style='padding: 0 0 16px;'>
                              <span style='display: block; width: 117px; border-bottom: 1px solid #8B949F;'></span>
                            </td>
                          </tr>
                          <tr>
                            <td style='font-size: 14px; line-height: 170%; font-weight: 400; color: #000000; letter-spacing: 0.01em;'>
                              Atentamente, <br><strong>TodoIst Support</strong>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </td>
                  </tr>
                    <td style=""padding: 24px 0 48px; font-size: 0px;"">
                  <!--[if mso | IE]>      <table role=""presentation"" border=""0"" cellpadding=""0"" cellspacing=""0"">        <tr>          <td style=""vertical-align:top;width:300px;"">      <![endif]-->
                  
                  <!--[if mso | IE]>      </td></tr></table>      <![endif]-->
                </td>
                </tbody>
              </table>
            </td>
          </tr>
        </tbody>
      </table>
    </body>
    </html>
    ";
        }

        [HttpPut("actualizar-nombres-usuario")]
        public async Task<IActionResult> ActualizarNombresUsuario([FromBody] UpdateUserDTO model)
        {
            var user = await _context.Users.FindAsync(model.UserId);

            if (user == null)
            {
                return NotFound(new { Message = "Usuario no encontrado." });
            }

            bool passwordMatches = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);

            if (!passwordMatches)
            {
                return Unauthorized(new { Message = "La contraseña proporcionada no es correcta." });
            }

            if (!string.IsNullOrEmpty(model.Name))
            {
                user.Name = model.Name;
            }

            try
            {
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Usuario actualizado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar el usuario: {ex.Message}");
            }
        }

        [HttpPost("verify-password")]
        public async Task<IActionResult> VerifyPassword([FromBody] VerifyPassword verifyPassword)
        {
            if (verifyPassword == null || string.IsNullOrEmpty(verifyPassword.Password))
            {
                return BadRequest(new { message = "Datos de verificación incompletos o incorrectos." });
            }

            var user = await _context.Users
                                     .FirstOrDefaultAsync(u => u.UserId == verifyPassword.UserId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(verifyPassword.Password, user.Password))
            {
                return Unauthorized(new { message = "Contraseña incorrecta." });
            }

            return Ok(new { message = "Verificación exitosa." });
        }
    }
}