using GujaratFarmersPortal.Data;
using GujaratFarmersPortal.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace GujaratFarmersPortal.Services
{
    public interface IAccountService
    {
        Task<LoginResult> LoginAsync(string userName, string password, string ipAddress, string deviceInfo);
        Task<ApiResponse<string>> LogoutAsync(string sessionToken);
        Task<ApiResponse<string>> ValidateSessionAsync(string sessionToken);
        Task<ApiResponse<string>> RegisterAsync(RegisterViewModel model);
        Task<ApiResponse<string>> ForgotPasswordAsync(string email);
        Task<ApiResponse<string>> ResetPasswordAsync(string resetToken, string newPassword);
        Task<ApiResponse<string>> GenerateOTPAsync(string mobileNumber);
        Task<ApiResponse<string>> VerifyOTPAsync(string mobileNumber, string otp);
        Task<bool> CheckUserExistsAsync(string userName = null, string email = null, string mobileNumber = null);
        Task<List<State>> GetStatesAsync(bool useCache = true);
        Task<List<District>> GetDistrictsAsync(int stateId, bool useCache = true);
        Task<List<Taluka>> GetTalukasAsync(int districtId, bool useCache = true);
        Task<List<Village>> GetVillagesAsync(int talukaId, bool useCache = true);
        Task<User> GetUserByIdAsync(int userId);
        Task<ApiResponse<string>> UpdateUserProfileAsync(User user);
        Task<ApiResponse<string>> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<ApiResponse<string>> UpdateUserStatusAsync(int userId, bool isActive);
        Task<List<UserSession>> GetUserSessionsAsync(int userId);
        Task<ApiResponse<string>> InvalidateSessionAsync(string sessionToken);
        void ClearCache();
        void ClearCacheByPattern(string pattern);
    }

    public class AccountService : IAccountService
    {
        private readonly IAccountDataAccess _accountDataAccess;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AccountService> _logger;
        private readonly IConfiguration _configuration;

        private const string CACHE_KEY_STATES = "account_states";
        private const string CACHE_KEY_DISTRICTS = "account_districts_";
        private const string CACHE_KEY_TALUKAS = "account_talukas_";
        private const string CACHE_KEY_VILLAGES = "account_villages_";
        private const string CACHE_KEY_USER = "account_user_";
        private const int CACHE_EXPIRY_MINUTES = 60; // Location data cached for 1 hour

        public AccountService(IAccountDataAccess accountDataAccess, IMemoryCache memoryCache, ILogger<AccountService> logger, IConfiguration configuration)
        {
            _accountDataAccess = accountDataAccess;
            _memoryCache = memoryCache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<LoginResult> LoginAsync(string userName, string password, string ipAddress, string deviceInfo)
        {
            try
            {
                // Hash the password
                var hashedPassword = HashPassword(password);

                // Call data access layer
                var result = await _accountDataAccess.UserLoginAsync(userName, hashedPassword, ipAddress, deviceInfo);

                if (result != null && result.Result == 1)
                {
                    _logger.LogInformation("User {UserName} logged in successfully from IP {IPAddress}", userName, ipAddress);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user {UserName} from IP {IPAddress}", userName, ipAddress);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {UserName}", userName);
                throw;
            }
        }

        public async Task<ApiResponse<string>> LogoutAsync(string sessionToken)
        {
            try
            {
                var result = await _accountDataAccess.UserLogoutAsync(sessionToken);

                if (result.Success)
                {
                    _logger.LogInformation("User logged out successfully");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        public async Task<ApiResponse<string>> ValidateSessionAsync(string sessionToken)
        {
            try
            {
                return await _accountDataAccess.ValidateSessionAsync(sessionToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session");
                throw;
            }
        }

        public async Task<ApiResponse<string>> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                // Validate business rules
                var validationResult = ValidateRegistrationData(model);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Check if user already exists
                var userExists = await _accountDataAccess.CheckUserExistsAsync(model.UserName, model.Email, model.MobileNumber);
                if (userExists)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "યુઝરનેમ, ઈમેઈલ અથવા મોબાઈલ નંબર પહેલાથી અસ્તિત્વમાં છે.",
                        Data = null
                    };
                }

                // Hash the password
                model.Password = HashPassword(model.Password);

                // Register user
                var result = await _accountDataAccess.UserRegistrationAsync(model);

                if (result.Success)
                {
                    _logger.LogInformation("New user registered successfully: {UserName}", model.UserName);
                }
                else
                {
                    _logger.LogWarning("User registration failed for: {UserName}", model.UserName);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {UserName}", model.UserName);
                throw;
            }
        }

        public async Task<ApiResponse<string>> ForgotPasswordAsync(string email)
        {
            try
            {
                // Generate reset token
                var resetToken = GenerateResetToken();

                var result = await _accountDataAccess.UserPasswordResetAsync("FORGOT_PASSWORD", email, resetToken: resetToken);

                if (result.Success)
                {
                    _logger.LogInformation("Password reset requested for email: {Email}", email);
                    // TODO: Send email with reset link
                    // await SendPasswordResetEmail(email, resetToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password for email: {Email}", email);
                throw;
            }
        }

        public async Task<ApiResponse<string>> ResetPasswordAsync(string resetToken, string newPassword)
        {
            try
            {
                // Hash the new password
                var hashedPassword = HashPassword(newPassword);

                var result = await _accountDataAccess.UserPasswordResetAsync("RESET_PASSWORD", resetToken: resetToken, newPassword: hashedPassword);

                if (result.Success)
                {
                    _logger.LogInformation("Password reset successfully");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                throw;
            }
        }

        public async Task<ApiResponse<string>> GenerateOTPAsync(string mobileNumber)
        {
            try
            {
                var result = await _accountDataAccess.UserPasswordResetAsync("GENERATE_OTP", mobileNumber: mobileNumber);

                if (result.Success)
                {
                    _logger.LogInformation("OTP generated for mobile number: {MobileNumber}", mobileNumber);
                    // TODO: Send SMS with OTP
                    // await SendOTPSMS(mobileNumber, result.Data);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for mobile: {MobileNumber}", mobileNumber);
                throw;
            }
        }

        public async Task<ApiResponse<string>> VerifyOTPAsync(string mobileNumber, string otp)
        {
            try
            {
                var result = await _accountDataAccess.UserPasswordResetAsync("VERIFY_OTP", mobileNumber: mobileNumber, otp: otp);

                if (result.Success)
                {
                    _logger.LogInformation("OTP verified successfully for mobile: {MobileNumber}", mobileNumber);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for mobile: {MobileNumber}", mobileNumber);
                throw;
            }
        }

        public async Task<bool> CheckUserExistsAsync(string userName = null, string email = null, string mobileNumber = null)
        {
            try
            {
                return await _accountDataAccess.CheckUserExistsAsync(userName, email, mobileNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence");
                throw;
            }
        }

        public async Task<List<State>> GetStatesAsync(bool useCache = true)
        {
            try
            {
                if (useCache && _memoryCache.TryGetValue(CACHE_KEY_STATES, out List<State> cachedStates))
                {
                    return cachedStates;
                }

                var states = await _accountDataAccess.GetStatesAsync();

                if (useCache)
                {
                    _memoryCache.Set(CACHE_KEY_STATES, states, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }

                return states;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting states");
                throw;
            }
        }

        public async Task<List<District>> GetDistrictsAsync(int stateId, bool useCache = true)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_DISTRICTS}{stateId}";

                if (useCache && _memoryCache.TryGetValue(cacheKey, out List<District> cachedDistricts))
                {
                    return cachedDistricts;
                }

                var districts = await _accountDataAccess.GetDistrictsAsync(stateId);

                if (useCache)
                {
                    _memoryCache.Set(cacheKey, districts, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }

                return districts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting districts for state: {StateId}", stateId);
                throw;
            }
        }

        public async Task<List<Taluka>> GetTalukasAsync(int districtId, bool useCache = true)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_TALUKAS}{districtId}";

                if (useCache && _memoryCache.TryGetValue(cacheKey, out List<Taluka> cachedTalukas))
                {
                    return cachedTalukas;
                }

                var talukas = await _accountDataAccess.GetTalukasAsync(districtId);

                if (useCache)
                {
                    _memoryCache.Set(cacheKey, talukas, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }

                return talukas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting talukas for district: {DistrictId}", districtId);
                throw;
            }
        }

        public async Task<List<Village>> GetVillagesAsync(int talukaId, bool useCache = true)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_VILLAGES}{talukaId}";

                if (useCache && _memoryCache.TryGetValue(cacheKey, out List<Village> cachedVillages))
                {
                    return cachedVillages;
                }

                var villages = await _accountDataAccess.GetVillagesAsync(talukaId);

                if (useCache)
                {
                    _memoryCache.Set(cacheKey, villages, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }

                return villages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting villages for taluka: {TalukaId}", talukaId);
                throw;
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_USER}{userId}";

                if (_memoryCache.TryGetValue(cacheKey, out User cachedUser))
                {
                    return cachedUser;
                }

                var user = await _accountDataAccess.GetUserByIdAsync(userId);

                if (user != null)
                {
                    _memoryCache.Set(cacheKey, user, TimeSpan.FromMinutes(30)); // Cache user for 30 minutes
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<string>> UpdateUserProfileAsync(User user)
        {
            try
            {
                var result = await _accountDataAccess.UpdateUserProfileAsync(user);

                if (result.Success)
                {
                    _logger.LogInformation("User profile updated successfully for UserID: {UserId}", user.UserID);

                    // Clear user cache
                    _memoryCache.Remove($"{CACHE_KEY_USER}{user.UserID}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for UserID: {UserId}", user.UserID);
                throw;
            }
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                // Hash passwords
                var hashedCurrentPassword = HashPassword(currentPassword);
                var hashedNewPassword = HashPassword(newPassword);

                var result = await _accountDataAccess.ChangePasswordAsync(userId, hashedCurrentPassword, hashedNewPassword);

                if (result.Success)
                {
                    _logger.LogInformation("Password changed successfully for UserID: {UserId}", userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task<ApiResponse<string>> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                var result = await _accountDataAccess.UpdateUserStatusAsync(userId, isActive);

                if (result.Success)
                {
                    _logger.LogInformation("User status updated successfully for UserID: {UserId} to {Status}", userId, isActive ? "Active" : "Inactive");

                    // Clear user cache
                    _memoryCache.Remove($"{CACHE_KEY_USER}{userId}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<UserSession>> GetUserSessionsAsync(int userId)
        {
            try
            {
                return await _accountDataAccess.GetUserSessionsAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sessions for UserID: {UserId}", userId);
                throw;
            }
        }

        public async Task< ApiResponse<string>> InvalidateSessionAsync(string sessionToken)
        {
            try
            {
                var result = await _accountDataAccess.InvalidateSessionAsync(sessionToken);

                if (result.Success)
                {
                    _logger.LogInformation("Session invalidated successfully");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating session");
                throw;
            }
        }

        public void ClearCache()
        {
            try
            {
                _memoryCache.Remove(CACHE_KEY_STATES);

                // Clear districts, talukas, and villages cache (pattern-based removal)
                var cacheKeys = new List<string>();

                // Clear location cache keys (assuming max 50 states/districts/talukas)
                for (int i = 1; i <= 50; i++)
                {
                    cacheKeys.Add($"{CACHE_KEY_DISTRICTS}{i}");
                    cacheKeys.Add($"{CACHE_KEY_TALUKAS}{i}");
                    cacheKeys.Add($"{CACHE_KEY_VILLAGES}{i}");
                    cacheKeys.Add($"{CACHE_KEY_USER}{i}");
                }

                foreach (var key in cacheKeys)
                {
                    _memoryCache.Remove(key);
                }

                _logger.LogInformation("All account cache cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        public void ClearCacheByPattern(string pattern)
        {
            try
            {
                switch (pattern.ToLower())
                {
                    case "states":
                        _memoryCache.Remove(CACHE_KEY_STATES);
                        break;
                    case "districts":
                        // Clear all districts cache
                        for (int i = 1; i <= 50; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_DISTRICTS}{i}");
                        }
                        break;
                    case "talukas":
                        // Clear all talukas cache
                        for (int i = 1; i <= 500; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_TALUKAS}{i}");
                        }
                        break;
                    case "villages":
                        // Clear all villages cache
                        for (int i = 1; i <= 5000; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_VILLAGES}{i}");
                        }
                        break;
                    case "users":
                        // Clear all user cache
                        for (int i = 1; i <= 10000; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_USER}{i}");
                        }
                        break;
                    case "locations":
                        // Clear all location cache
                        _memoryCache.Remove(CACHE_KEY_STATES);
                        for (int i = 1; i <= 50; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_DISTRICTS}{i}");
                        }
                        for (int i = 1; i <= 500; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_TALUKAS}{i}");
                        }
                        for (int i = 1; i <= 5000; i++)
                        {
                            _memoryCache.Remove($"{CACHE_KEY_VILLAGES}{i}");
                        }
                        break;
                }

                _logger.LogInformation("Cache cleared for pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
            }
        }

        #region Private Helper Methods

        private string HashPassword(string password)
        {
            // Using SHA256 for now - In production, use BCrypt or Argon2
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private string GenerateResetToken()
        {
            return Guid.NewGuid().ToString("N");
        }

        private ApiResponse<string> ValidateRegistrationData(RegisterViewModel model)
        {
            var errors = new List<string>();

            // Basic validation
            if (string.IsNullOrWhiteSpace(model.FirstName))
                errors.Add("પ્રથમ નામ આવશ્યક છે");

            if (string.IsNullOrWhiteSpace(model.LastName))
                errors.Add("છેલ્લું નામ આવશ્યક છે");

            if (string.IsNullOrWhiteSpace(model.UserName))
                errors.Add("યુઝરનેમ આવશ્યક છે");

            if (string.IsNullOrWhiteSpace(model.Email))
                errors.Add("ઈમેઈલ આવશ્યક છે");

            if (string.IsNullOrWhiteSpace(model.MobileNumber))
                errors.Add("મોબાઈલ નંબર આવશ્યક છે");

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 6)
                errors.Add("પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ");

            // Email validation
            if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
                errors.Add("માન્ય ઈમેઈલ એડ્રેસ દાખલ કરો");

            // Mobile number validation
            if (!string.IsNullOrWhiteSpace(model.MobileNumber) && !IsValidMobileNumber(model.MobileNumber))
                errors.Add("માન્ય મોબાઈલ નંબર દાખલ કરો");

            // Username validation
            if (!string.IsNullOrWhiteSpace(model.UserName) && !IsValidUserName(model.UserName))
                errors.Add("યુઝરનેમમાં માત્ર અક્ષરો, નંબરો અને અંડરસ્કોર (_) નો ઉપયોગ કરો");

            if (errors.Any())
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = string.Join(", ", errors),
                    Data = null
                };
            }

            return new ApiResponse<string>
            {
                Success = true,
                Message = "Validation successful",
                Data = null
            };
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidMobileNumber(string mobileNumber)
        {
            // Basic Indian mobile number validation
            if (string.IsNullOrWhiteSpace(mobileNumber))
                return false;

            // Remove spaces and special characters
            string cleanNumber = mobileNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Check if it's a 10-digit number starting with 6, 7, 8, or 9
            if (cleanNumber.Length == 10 && "6789".Contains(cleanNumber[0]))
                return true;

            // Check if it's a 10-digit number with +91 prefix
            if (cleanNumber.Length == 13 && cleanNumber.StartsWith("+91") && "6789".Contains(cleanNumber[3]))
                return true;

            // Check if it's a 10-digit number with 91 prefix
            if (cleanNumber.Length == 12 && cleanNumber.StartsWith("91") && "6789".Contains(cleanNumber[2]))
                return true;

            return false;
        }

        private bool IsValidUserName(string userName)
        {
            // Username should contain only alphanumeric characters and underscores
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            return userName.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        #endregion
    }
}