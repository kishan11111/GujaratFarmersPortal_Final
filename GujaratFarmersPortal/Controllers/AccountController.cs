using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using GujaratFarmersPortal.Models;
using GujaratFarmersPortal.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace GujaratFarmersPortal.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger, IConfiguration configuration)
        {
            _accountService = accountService;
            _logger = logger;
            _configuration = configuration;
        }

        #region Helper Methods

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

        private IActionResult RedirectToUserDashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else
            {
                return RedirectToAction("Index", "Home"); // Redirect to user dashboard when created
            }
        }

        private void SetBreadcrumb(string pageName, List<dynamic> breadcrumbItems = null)
        {
            ViewBag.ActivePage = pageName;
            ViewBag.BreadcrumbItems = breadcrumbItems;
        }

        #endregion

        #region Login

        [HttpGet]
        [HttpGet("Login")]
        public IActionResult Login(string returnUrl = null)
        {
            // If user is already logged in, redirect to appropriate dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }

            ViewBag.ReturnUrl = returnUrl;
            SetBreadcrumb("Login");
            return View();
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            SetBreadcrumb("Login");

            if (ModelState.IsValid)
            {
                try
                {
                    // Get client information
                    var clientIP = GetClientIP();
                    var userAgent = GetUserAgent();

                    // Call service layer for login
                    var result = await _accountService.LoginAsync(model.UserName, model.Password, clientIP, userAgent);

                    if (result != null && result.Result == 1)
                    {
                        // Create authentication claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, result.UserName),
                            new Claim(ClaimTypes.Email, result.Email ?? ""),
                            new Claim(ClaimTypes.MobilePhone, result.MobileNumber ?? ""),
                            new Claim("UserID", result.UserID.ToString()),
                            new Claim("FirstName", result.FirstName ?? ""),
                            new Claim("LastName", result.LastName ?? ""),
                            new Claim("UserName", result.UserName),
                            new Claim("ProfileImage", result.ProfileImage ?? ""),
                            new Claim("SessionToken", result.SessionToken ?? ""),
                            new Claim("StateName", result.StateName ?? ""),
                            new Claim("DistrictName", result.DistrictName ?? ""),
                            new Claim("TalukaName", result.TalukaName ?? ""),
                            new Claim("VillageName", result.VillageName ?? "")
                        };

                        // Add role claim based on UserTypeID
                        if (result.UserTypeID == 1)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "User"));
                        }

                        // Create claims identity
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        // Create authentication properties
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = model.RememberMe ?
                                DateTimeOffset.UtcNow.AddDays(30) :
                                DateTimeOffset.UtcNow.AddMinutes(_configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 30))
                        };

                        // Sign in the user
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity), authProperties);

                        // Store session token in session
                        HttpContext.Session.SetString("SessionToken", result.SessionToken);

                        // Log successful login
                        _logger.LogInformation("User {UserName} logged in successfully from IP {IPAddress}",
                            result.UserName, clientIP);

                        TempData["SuccessMessage"] = "સફળતાપૂર્વક લૉગિન થયા";

                        // Redirect to return URL or dashboard
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToUserDashboard();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", result?.Message ?? "લૉગિન નિષ્ફળ. કૃપા કરીને તમારી વિગતો તપાસો.");
                        _logger.LogWarning("Failed login attempt for user {UserName} from IP {IPAddress}",
                            model.UserName, clientIP);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during login for user {UserName}", model.UserName);
                    ModelState.AddModelError("", "લૉગિન કરવામાં તકનીકી સમસ્યા આવી છે. કૃપા કરીને પછીથી પ્રયાસ કરો.");
                }
            }

            return View(model);
        }

        #endregion

        #region Register

        [HttpGet("Register")]
        public async Task<IActionResult> Register()
        {
            // If user is already logged in, redirect to dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }

            try
            {
                SetBreadcrumb("Register");

                // Load states for dropdown
                ViewBag.States = await _accountService.GetStatesAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading registration page");
                TempData["ErrorMessage"] = "નોંધણી પૃષ્ઠ લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Login");
            }
        }

        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                SetBreadcrumb("Register");

                // Load states for dropdown (in case of validation errors)
                ViewBag.States = await _accountService.GetStatesAsync();

                if (ModelState.IsValid)
                {
                    // Call service layer for registration
                    var result = await _accountService.RegisterAsync(model);

                    if (result.Success)
                    {
                        _logger.LogInformation("New user registered successfully: {UserName}", model.UserName);
                        TempData["SuccessMessage"] = "નોંધણી સફળ થઈ! કૃપા કરીને લૉગિન કરો.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", result.Message);
                        _logger.LogWarning("User registration failed for: {UserName}. Reason: {Message}",
                            model.UserName, result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {UserName}", model.UserName);
                ModelState.AddModelError("", "નોંધણી કરવામાં તકનીકી સમસ્યા આવી છે. કૃપા કરીને પછીથી પ્રયાસ કરો.");
                ViewBag.States = await _accountService.GetStatesAsync();
            }

            return View(model);
        }

        #endregion

        #region Logout

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get session token
                var sessionToken = HttpContext.Session.GetString("SessionToken");

                if (!string.IsNullOrEmpty(sessionToken))
                {
                    // Call service layer for logout
                    await _accountService.LogoutAsync(sessionToken);
                }

                // Clear session
                HttpContext.Session.Clear();

                // Sign out
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation("User logged out successfully");
                TempData["SuccessMessage"] = "તમે સફળતાપૂર્વક લૉગઆઉટ થઈ ગયા છો.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                TempData["ErrorMessage"] = "લૉગઆઉટ કરવામાં સમસ્યા આવી છે.";
            }

            return RedirectToAction("Login");
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> LogoutGet()
        {
            return await Logout();
        }

        #endregion

        #region Access Denied

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            ViewBag.Title = "પ્રવેશ નકારાયો";
            SetBreadcrumb("AccessDenied");
            return View();
        }

        #endregion

        #region Forgot Password

        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToUserDashboard();
            }

            SetBreadcrumb("ForgotPassword");
            return View();
        }

        [HttpPost("ForgotPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            SetBreadcrumb("ForgotPassword");

            if (ModelState.IsValid)
            {
                try
                {
                    // Call service layer for password reset
                    var result = await _accountService.ForgotPasswordAsync(model.Email);

                    if (result.Success)
                    {
                        _logger.LogInformation("Password reset requested for email: {Email}", model.Email);
                        TempData["SuccessMessage"] = "પાસવર્ડ રીસેટ લિંક તમારા ઈમેઈલ પર મોકલવામાં આવી છે.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during forgot password for {Email}", model.Email);
                    ModelState.AddModelError("", "તકનીકી સમસ્યા આવી છે. કૃપા કરીને પછીથી પ્રયાસ કરો.");
                }
            }

            return View(model);
        }

        #endregion

        #region Reset Password

        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "અમાન્ય રીસેટ લિંક";
                return RedirectToAction("Login");
            }

            SetBreadcrumb("ResetPassword");
            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            SetBreadcrumb("ResetPassword");

            if (ModelState.IsValid)
            {
                try
                {
                    // Call service layer for password reset
                    var result = await _accountService.ResetPasswordAsync(model.Token, model.NewPassword);

                    if (result.Success)
                    {
                        _logger.LogInformation("Password reset successfully");
                        TempData["SuccessMessage"] = "પાસવર્ડ સફળતાપૂર્વક રીસેટ થયો છે. કૃપા કરીને નવા પાસવર્ડ સાથે લૉગિન કરો.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during password reset");
                    ModelState.AddModelError("", "તકનીકી સમસ્યા આવી છે. કૃપા કરીને પછીથી પ્રયાસ કરો.");
                }
            }

            return View(model);
        }

        #endregion

        #region Profile Management

        [HttpGet("Profile")]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                SetBreadcrumb("Profile", new List<dynamic>
                {
                    new { Title = "મારી પ્રોફાઇલ", Url = "/Account/Profile", IsActive = true }
                });

                var userId = GetCurrentUserID();
                var user = await _accountService.GetUserByIdAsync(userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "પ્રોફાઇલ મળી નથી";
                    return RedirectToAction("Login");
                }

                ViewBag.States = await _accountService.GetStatesAsync();

                if (user.StateID.HasValue)
                {
                    ViewBag.Districts = await _accountService.GetDistrictsAsync(user.StateID.Value);
                }

                if (user.DistrictID.HasValue)
                {
                    ViewBag.Talukas = await _accountService.GetTalukasAsync(user.DistrictID.Value);
                }

                if (user.TalukaID.HasValue)
                {
                    ViewBag.Villages = await _accountService.GetVillagesAsync(user.TalukaID.Value);
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["ErrorMessage"] = "પ્રોફાઇલ લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToUserDashboard();
            }
        }

        [HttpPost("Profile")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User model)
        {
            try
            {
                SetBreadcrumb("Profile", new List<dynamic>
                {
                    new { Title = "મારી પ્રોફાઇલ", Url = "/Account/Profile", IsActive = true }
                });

                if (ModelState.IsValid)
                {
                    model.UserID = GetCurrentUserID();

                    var result = await _accountService.UpdateUserProfileAsync(model);

                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = "પ્રોફાઇલ સફળતાપૂર્વક અપડેટ થઈ";
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        ModelState.AddModelError("", result.Message);
                    }
                }

                // Reload dropdown data
                ViewBag.States = await _accountService.GetStatesAsync();

                if (model.StateID.HasValue)
                {
                    ViewBag.Districts = await _accountService.GetDistrictsAsync(model.StateID.Value);
                }

                if (model.DistrictID.HasValue)
                {
                    ViewBag.Talukas = await _accountService.GetTalukasAsync(model.DistrictID.Value);
                }

                if (model.TalukaID.HasValue)
                {
                    ViewBag.Villages = await _accountService.GetVillagesAsync(model.TalukaID.Value);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                TempData["ErrorMessage"] = "પ્રોફાઇલ અપડેટ કરવામાં ભૂલ આવી છે";
                return View(model);
            }
        }

        #endregion

        #region Change Password

        [HttpGet("ChangePassword")]
        [Authorize]
        public IActionResult ChangePassword()
        {
            SetBreadcrumb("ChangePassword", new List<dynamic>
            {
                new { Title = "પાસવર્ડ બદલો", Url = "/Account/ChangePassword", IsActive = true }
            });

            return View();
        }

        [HttpPost("ChangePassword")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            SetBreadcrumb("ChangePassword", new List<dynamic>
            {
                new { Title = "પાસવર્ડ બદલો", Url = "/Account/ChangePassword", IsActive = true }
            });

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = GetCurrentUserID();
                    var result = await _accountService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

                    if (result.Success)
                    {
                        TempData["SuccessMessage"] = "પાસવર્ડ સફળતાપૂર્વક બદલાઈ ગયો";
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        ModelState.AddModelError("", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error changing password for user {UserId}", GetCurrentUserID());
                    ModelState.AddModelError("", "પાસવર્ડ બદલવામાં તકનીકી સમસ્યા આવી છે");
                }
            }

            return View(model);
        }

        #endregion

        #region User Sessions

        [HttpGet("Sessions")]
        [Authorize]
        public async Task<IActionResult> Sessions()
        {
            try
            {
                SetBreadcrumb("Sessions", new List<dynamic>
                {
                    new { Title = "મારા સેશન્સ", Url = "/Account/Sessions", IsActive = true }
                });

                var userId = GetCurrentUserID();
                var sessions = await _accountService.GetUserSessionsAsync(userId);

                return View(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user sessions");
                TempData["ErrorMessage"] = "સેશન્સ લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Profile");
            }
        }

        [HttpPost("InvalidateSession")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InvalidateSession(string sessionToken)
        {
            try
            {
                var result = await _accountService.InvalidateSessionAsync(sessionToken);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "સેશન સફળતાપૂર્વક બંધ કરવામાં આવ્યો";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating session");
                TempData["ErrorMessage"] = "સેશન બંધ કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Sessions");
        }

        #endregion

        #region AJAX Methods for Location

        [HttpGet("GetDistricts")]
        public async Task<JsonResult> GetDistricts(int stateId)
        {
            try
            {
                var districts = await _accountService.GetDistrictsAsync(stateId);
                return Json(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading districts for state {StateId}", stateId);
                return Json(new List<District>());
            }
        }

        [HttpGet("GetTalukas")]
        public async Task<JsonResult> GetTalukas(int districtId)
        {
            try
            {
                var talukas = await _accountService.GetTalukasAsync(districtId);
                return Json(talukas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading talukas for district {DistrictId}", districtId);
                return Json(new List<Taluka>());
            }
        }

        [HttpGet("GetVillages")]
        public async Task<JsonResult> GetVillages(int talukaId)
        {
            try
            {
                var villages = await _accountService.GetVillagesAsync(talukaId);
                return Json(villages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading villages for taluka {TalukaId}", talukaId);
                return Json(new List<Village>());
            }
        }

        #endregion

        #region Check User Existence (AJAX)

        [HttpPost("CheckUserExists")]
        public async Task<JsonResult> CheckUserExists(string userName = null, string email = null, string mobileNumber = null)
        {
            try
            {
                var exists = await _accountService.CheckUserExistsAsync(userName, email, mobileNumber);
                return Json(new { exists = exists });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence");
                return Json(new { exists = false });
            }
        }

        #endregion

        #region Error Handling

        [HttpGet("Error")]
        public IActionResult Error()
        {
            SetBreadcrumb("Error");
            return View();
        }

        #endregion
    }

    #region Helper ViewModels

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "ઈમેઈલ આવશ્યક છે")]
        [EmailAddress(ErrorMessage = "માન્ય ઈમેઈલ એડ્રેસ દાખલ કરો")]
        [Display(Name = "ઈમેઈલ એડ્રેસ")]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "નવો પાસવર્ડ આવશ્યક છે")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ")]
        [DataType(DataType.Password)]
        [Display(Name = "નવો પાસવર્ડ")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "પાસવર્ડની પુષ્ટિ આવશ્યક છે")]
        [DataType(DataType.Password)]
        [Display(Name = "પાસવર્ડની પુષ્ટિ")]
        [Compare("NewPassword", ErrorMessage = "પાસવર્ડ અને પુષ્ટિ મેળ ખાતા નથી")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "વર્તમાન પાસવર્ડ આવશ્યક છે")]
        [DataType(DataType.Password)]
        [Display(Name = "વર્તમાન પાસવર્ડ")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "નવો પાસવર્ડ આવશ્યક છે")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ")]
        [DataType(DataType.Password)]
        [Display(Name = "નવો પાસવર્ડ")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "પાસવર્ડની પુષ્ટિ આવશ્યક છે")]
        [DataType(DataType.Password)]
        [Display(Name = "નવા પાસવર્ડની પુષ્ટિ")]
        [Compare("NewPassword", ErrorMessage = "પાસવર્ડ અને પુષ્ટિ મેળ ખાતા નથી")]
        public string ConfirmNewPassword { get; set; }
    }
}
    #endregion