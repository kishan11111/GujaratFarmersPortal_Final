using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GujaratFarmersPortal.Services;
using GujaratFarmersPortal.Models;
using GujaratFarmersPortal.Data;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace GujaratFarmersPortal.Controllers.API
{
    [ApiController]
    [Route("api/account")]
    public class AccountApiController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountApiController> _logger;

        public AccountApiController(IAccountService accountService, ILogger<AccountApiController> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        private int GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private string GetClientIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString();
        }

        #region Authentication APIs

        /// <summary>
        /// User login API
        /// </summary>
        /// <param name="model">Login credentials</param>
        /// <returns>Login result with user details</returns>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResult>>> Login(LoginApiModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<LoginResult>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var clientIP = GetClientIP();
                var userAgent = GetUserAgent();

                var result = await _accountService.LoginAsync(model.UserName, model.Password, clientIP, userAgent);

                if (result != null && result.Result == 1)
                {
                    return Ok(new ApiResponse<LoginResult>
                    {
                        Success = true,
                        Message = "Login successful",
                        Data = result
                    });
                }
                else
                {
                    return Unauthorized(new ApiResponse<LoginResult>
                    {
                        Success = false,
                        Message = result?.Message ?? "Invalid credentials",
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API login for user {UserName}", model.UserName);
                return StatusCode(500, new ApiResponse<LoginResult>
                {
                    Success = false,
                    Message = "લૉગિન કરવામાં તકનીકી સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// User logout API
        /// </summary>
        /// <param name="model">Session token</param>
        /// <returns>Logout result</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> Logout(LogoutApiModel model)
        {
            try
            {
                var result = await _accountService.LogoutAsync(model.SessionToken);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API logout");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "લૉગઆઉટ કરવામાં સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Validate session token
        /// </summary>
        /// <param name="sessionToken">Session token</param>
        /// <returns>Session validation result</returns>
        [HttpPost("validate-session")]
        public async Task<ActionResult<ApiResponse<string>>> ValidateSession([FromBody] string sessionToken)
        {
            try
            {
                var result = await _accountService.ValidateSessionAsync(sessionToken);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "સેશન વેલિડેશનમાં સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// User registration API
        /// </summary>
        /// <param name="model">Registration data</param>
        /// <returns>Registration result</returns>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<string>>> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _accountService.RegisterAsync(model);

                if (result.Success)
                {
                    return Ok(new ApiResponse<string>
                    {
                        Success = true,
                        Message = "User registered successfully",
                        Data = result.Data
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = result.Message,
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API registration for user {UserName}", model.UserName);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "નોંધણી કરવામાં તકનીકી સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Password Management APIs

        /// <summary>
        /// Forgot password API
        /// </summary>
        /// <param name="model">Email for password reset</param>
        /// <returns>Password reset result</returns>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(ForgotPasswordApiModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid email address",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _accountService.ForgotPasswordAsync(model.Email);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API forgot password for email {Email}", model.Email);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પાસવર્ડ રીસેટ કરવામાં તકનીકી સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Reset password API
        /// </summary>
        /// <param name="model">Reset token and new password</param>
        /// <returns>Password reset result</returns>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword(ResetPasswordApiModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _accountService.ResetPasswordAsync(model.Token, model.NewPassword);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API password reset");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પાસવર્ડ રીસેટ કરવામાં તકનીકી સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Change password API
        /// </summary>
        /// <param name="model">Current and new password</param>
        /// <returns>Password change result</returns>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> ChangePassword(ChangePasswordApiModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var userId = GetCurrentUserID();
                var result = await _accountService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API password change for user {UserId}", GetCurrentUserID());
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પાસવર્ડ બદલવામાં તકનીકી સમસ્યા આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Profile Management APIs

        /// <summary>
        /// Get user profile API
        /// </summary>
        /// <returns>User profile data</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<User>>> GetProfile()
        {
            try
            {
                var userId = GetCurrentUserID();
                var user = await _accountService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<User>
                    {
                        Success = false,
                        Message = "User not found",
                        Data = null
                    });
                }

                return Ok(new ApiResponse<User>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile via API for user {UserId}", GetCurrentUserID());
                return StatusCode(500, new ApiResponse<User>
                {
                    Success = false,
                    Message = "પ્રોફાઇલ લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user profile API
        /// </summary>
        /// <param name="model">Updated user data</param>
        /// <returns>Profile update result</returns>
        [HttpPut("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> UpdateProfile(User model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                model.UserID = GetCurrentUserID();
                var result = await _accountService.UpdateUserProfileAsync(model);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile via API for user {UserId}", GetCurrentUserID());
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પ્રોફાઇલ અપડેટ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Location APIs

        /// <summary>
        /// Get all states
        /// </summary>
        /// <returns>List of states</returns>
        [HttpGet("states")]
        public async Task<ActionResult<ApiResponse<List<State>>>> GetStates()
        {
            try
            {
                var states = await _accountService.GetStatesAsync();

                return Ok(new ApiResponse<List<State>>
                {
                    Success = true,
                    Message = "States retrieved successfully",
                    Data = states
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving states via API");
                return StatusCode(500, new ApiResponse<List<State>>
                {
                    Success = false,
                    Message = "રાજ્યોની યાદી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get districts by state ID
        /// </summary>
        /// <param name="stateId">State ID</param>
        /// <returns>List of districts</returns>
        [HttpGet("districts/{stateId}")]
        public async Task<ActionResult<ApiResponse<List<District>>>> GetDistricts(int stateId)
        {
            try
            {
                var districts = await _accountService.GetDistrictsAsync(stateId);

                return Ok(new ApiResponse<List<District>>
                {
                    Success = true,
                    Message = "Districts retrieved successfully",
                    Data = districts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving districts via API for state {StateId}", stateId);
                return StatusCode(500, new ApiResponse<List<District>>
                {
                    Success = false,
                    Message = "જિલ્લાઓની યાદી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get talukas by district ID
        /// </summary>
        /// <param name="districtId">District ID</param>
        /// <returns>List of talukas</returns>
        [HttpGet("talukas/{districtId}")]
        public async Task<ActionResult<ApiResponse<List<Taluka>>>> GetTalukas(int districtId)
        {
            try
            {
                var talukas = await _accountService.GetTalukasAsync(districtId);

                return Ok(new ApiResponse<List<Taluka>>
                {
                    Success = true,
                    Message = "Talukas retrieved successfully",
                    Data = talukas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving talukas via API for district {DistrictId}", districtId);
                return StatusCode(500, new ApiResponse<List<Taluka>>
                {
                    Success = false,
                    Message = "તાલુકાઓની યાદી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get villages by taluka ID
        /// </summary>
        /// <param name="talukaId">Taluka ID</param>
        /// <returns>List of villages</returns>
        [HttpGet("villages/{talukaId}")]
        public async Task<ActionResult<ApiResponse<List<Village>>>> GetVillages(int talukaId)
        {
            try
            {
                var villages = await _accountService.GetVillagesAsync(talukaId);

                return Ok(new ApiResponse<List<Village>>
                {
                    Success = true,
                    Message = "Villages retrieved successfully",
                    Data = villages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving villages via API for taluka {TalukaId}", talukaId);
                return StatusCode(500, new ApiResponse<List<Village>>
                {
                    Success = false,
                    Message = "ગામોની યાદી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Session Management APIs

        /// <summary>
        /// Get user sessions
        /// </summary>
        /// <returns>List of user sessions</returns>
        [HttpGet("sessions")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<UserSession>>>> GetSessions()
        {
            try
            {
                var userId = GetCurrentUserID();
                var sessions = await _accountService.GetUserSessionsAsync(userId);

                return Ok(new ApiResponse<List<UserSession>>
                {
                    Success = true,
                    Message = "Sessions retrieved successfully",
                    Data = sessions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sessions via API for user {UserId}", GetCurrentUserID());
                return StatusCode(500, new ApiResponse<List<UserSession>>
                {
                    Success = false,
                    Message = "સેશન્સ લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Invalidate a specific session
        /// </summary>
        /// <param name="sessionToken">Session token to invalidate</param>
        /// <returns>Invalidation result</returns>
        [HttpPost("invalidate-session")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> InvalidateSession([FromBody] string sessionToken)
        {
            try
            {
                var result = await _accountService.InvalidateSessionAsync(sessionToken);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating session via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "સેશન બંધ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Utility APIs

        /// <summary>
        /// Check if user exists
        /// </summary>
        /// <param name="userName">Username</param>
        /// <param name="email">Email</param>
        /// <param name="mobileNumber">Mobile number</param>
        /// <returns>User existence result</returns>
        [HttpGet("check-user-exists")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckUserExists(
            [FromQuery] string userName = null,
            [FromQuery] string email = null,
            [FromQuery] string mobileNumber = null)
        {
            try
            {
                var exists = await _accountService.CheckUserExistsAsync(userName, email, mobileNumber);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "User existence checked successfully",
                    Data = exists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence via API");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "વપરાશકર્તા તપાસવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Generate OTP for mobile verification
        /// </summary>
        /// <param name="model">Mobile number</param>
        /// <returns>OTP generation result</returns>
        [HttpPost("generate-otp")]
        public async Task<ActionResult<ApiResponse<string>>> GenerateOTP(GenerateOTPApiModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid mobile number",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _accountService.GenerateOTPAsync(model.MobileNumber);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP via API for mobile {MobileNumber}", model.MobileNumber);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "OTP જનરેટ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Verify OTP for mobile verification
        /// </summary>
        /// <param name="model">Mobile number and OTP</param>
        /// <returns>OTP verification result</returns>
        [HttpPost("verify-otp")]
        public async Task<ActionResult<ApiResponse<string>>> VerifyOTP(VerifyOTPApiModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Invalid input data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                    });
                }

                var result = await _accountService.VerifyOTPAsync(model.MobileNumber, model.OTP);

                return Ok(new ApiResponse<string>
                {
                    Success = result.Success,
                    Message = result.Message,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP via API for mobile {MobileNumber}", model.MobileNumber);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "OTP વેરિફાઇ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Cache Management APIs

        /// <summary>
        /// Clear account cache (Admin only)
        /// </summary>
        /// <returns>Cache clear result</returns>
        [HttpPost("clear-cache")]
        [Authorize(Roles = "Admin")]
        public IActionResult ClearCache()
        {
            try
            {
                _accountService.ClearCache();

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Cache cleared successfully",
                    Data = "All account cache cleared"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેશ ક્લીયર કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Clear cache by pattern (Admin only)
        /// </summary>
        /// <param name="pattern">Cache pattern to clear</param>
        /// <returns>Cache clear result</returns>
        [HttpPost("clear-cache/{pattern}")]
        [Authorize(Roles = "Admin")]
        public IActionResult ClearCacheByPattern(string pattern)
        {
            try
            {
                _accountService.ClearCacheByPattern(pattern);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = $"Cache cleared successfully for pattern: {pattern}",
                    Data = $"Cache pattern '{pattern}' cleared"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache by pattern via API: {Pattern}", pattern);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેશ ક્લીયર કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Health Check APIs

        /// <summary>
        /// API health check
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            try
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Account API is healthy",
                    Data = new
                    {
                        Status = "Healthy",
                        Timestamp = DateTime.UtcNow,
                        Version = "1.0.0",
                        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in health check");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Account API is unhealthy",
                    Errors = ex.Message
                });
            }
        }

        #endregion
    }

    #region API Models

    public class LoginApiModel
    {
        [Required(ErrorMessage = "યુઝરનેમ આવશ્યક છે")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "પાસવર્ડ આવશ્યક છે")]
        public string Password { get; set; }
    }

    public class LogoutApiModel
    {
        [Required(ErrorMessage = "સેશન ટોકન આવશ્યક છે")]
        public string SessionToken { get; set; }
    }

    public class ForgotPasswordApiModel
    {
        [Required(ErrorMessage = "ઈમેઈલ આવશ્યક છે")]
        [EmailAddress(ErrorMessage = "માન્ય ઈમેઈલ એડ્રેસ દાખલ કરો")]
        public string Email { get; set; }
    }

    public class ResetPasswordApiModel
    {
        [Required(ErrorMessage = "રીસેટ ટોકન આવશ્યક છે")]
        public string Token { get; set; }

        [Required(ErrorMessage = "નવો પાસવર્ડ આવશ્યક છે")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ")]
        public string NewPassword { get; set; }
    }

    public class ChangePasswordApiModel
    {
        [Required(ErrorMessage = "વર્તમાન પાસવર્ડ આવશ્યક છે")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "નવો પાસવર્ડ આવશ્યક છે")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ")]
        public string NewPassword { get; set; }
    }

    public class GenerateOTPApiModel
    {
        [Required(ErrorMessage = "મોબાઈલ નંબર આવશ્યક છે")]
        [Phone(ErrorMessage = "માન્ય મોબાઈલ નંબર દાખલ કરો")]
        public string MobileNumber { get; set; }
    }

    public class VerifyOTPApiModel
    {
        [Required(ErrorMessage = "મોબાઈલ નંબર આવશ્યક છે")]
        [Phone(ErrorMessage = "માન્ય મોબાઈલ નંબર દાખલ કરો")]
        public string MobileNumber { get; set; }

        [Required(ErrorMessage = "OTP આવશ્યક છે")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP 6 અંકનો હોવો જોઈએ")]
        public string OTP { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
    }

    public class UserStatsApiModel
    {
        public int UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public string ProfileImage { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsMobileVerified { get; set; }
        public decimal Rating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
        public string LocationInfo { get; set; }
    }

    public class UpdateUserStatusApiModel
    {
        [Required(ErrorMessage = "વપરાશકર્તા ID આવશ્યક છે")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "સ્થિતિ આવશ્યક છે")]
        public bool IsActive { get; set; }
    }

    #endregion
}