using System.Data;
using System.Data.SqlClient;
using Dapper;
using GujaratFarmersPortal.Models;

namespace GujaratFarmersPortal.Data
{
    public interface IAccountDataAccess
    {
        Task<LoginResult> UserLoginAsync(string userName, string password, string ipAddress, string deviceInfo);
        Task<ApiResponse<string>> UserLogoutAsync(string sessionToken);
        Task<ApiResponse<string>> ValidateSessionAsync(string sessionToken);
        Task<ApiResponse<string>> UserRegistrationAsync(RegisterViewModel model);
        Task<ApiResponse<string>> UserPasswordResetAsync(string mode, string email = null, string mobileNumber = null, string resetToken = null, string newPassword = null, string otp = null);
        Task<bool> CheckUserExistsAsync(string userName = null, string email = null, string mobileNumber = null);
        Task<List<State>> GetStatesAsync();
        Task<List<District>> GetDistrictsAsync(int stateId);
        Task<List<Taluka>> GetTalukasAsync(int districtId);
        Task<List<Village>> GetVillagesAsync(int talukaId);
        Task<User> GetUserByIdAsync(int userId);
        Task<ApiResponse<string>> UpdateUserProfileAsync(User user);
        Task<ApiResponse<string>> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<ApiResponse<string>> UpdateUserStatusAsync(int userId, bool isActive);
        Task<List<UserSession>> GetUserSessionsAsync(int userId);
        Task<ApiResponse<string>> InvalidateSessionAsync(string sessionToken);
    }

    public class AccountDataAccess : IAccountDataAccess
    {
        private readonly IDbConnection _connection;
        private readonly IConfiguration _configuration;

        public AccountDataAccess(IDbConnection connection, IConfiguration configuration)
        {
            _connection = connection;
            _configuration = configuration;
        }

        public async Task<LoginResult> UserLoginAsync(string userName, string password, string ipAddress, string deviceInfo)
        {
            try
            {
                var parameters = new
                {
                    UserName = userName,
                    Password = password,
                    IPAddress = ipAddress,
                    DeviceInfo = deviceInfo
                };

                var result = await _connection.QueryFirstOrDefaultAsync<LoginResult>(
                    "sp_UserLogin", parameters, commandType: CommandType.StoredProcedure);

                return result ?? new LoginResult { Result = 0, Message = "Invalid login credentials" };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error during login: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> UserLogoutAsync(string sessionToken)
        {
            try
            {
                var parameters = new { SessionToken = sessionToken };

                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_UserLogout", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Logout completed",
                    Data = result?.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error during logout: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> ValidateSessionAsync(string sessionToken)
        {
            try
            {
                var parameters = new { SessionToken = sessionToken };

                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_ValidateSession", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result != null,
                    Message = result != null ? "Session is valid" : "Session is invalid",
                    Data = result?.UserID?.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error validating session: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> UserRegistrationAsync(RegisterViewModel model)
        {
            try
            {
                var parameters = new
                {
                    UserTypeID = 2, // Regular user
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserName = model.UserName,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    Password = model.Password, // Should be hashed in service layer
                    Gender = model.Gender,
                    StateID = model.StateID,
                    DistrictID = model.DistrictID,
                    TalukaID = model.TalukaID,
                    VillageID = model.VillageID,
                    Address = model.Address,
                    Pincode = model.Pincode,
                    CreatedDate = DateTime.Now
                };

                var sql = @"
                    INSERT INTO tbl_Users (
                        UserTypeID, FirstName, LastName, UserName, Email, MobileNumber, Password, 
                        Gender, StateID, DistrictID, TalukaID, VillageID, Address, Pincode, CreatedDate
                    ) VALUES (
                        @UserTypeID, @FirstName, @LastName, @UserName, @Email, @MobileNumber, @Password, 
                        @Gender, @StateID, @DistrictID, @TalukaID, @VillageID, @Address, @Pincode, @CreatedDate
                    );
                    SELECT SCOPE_IDENTITY();";

                var newUserId = await _connection.QuerySingleAsync<int>(sql, parameters);

                return new ApiResponse<string>
                {
                    Success = newUserId > 0,
                    Message = newUserId > 0 ? "User registered successfully" : "Registration failed",
                    Data = newUserId.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error during registration: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> UserPasswordResetAsync(string mode, string email = null, string mobileNumber = null, string resetToken = null, string newPassword = null, string otp = null)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    Email = email,
                    MobileNumber = mobileNumber,
                    ResetToken = resetToken,
                    NewPassword = newPassword,
                    OTP = otp
                };

                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_UserPasswordReset", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Operation completed",
                    Data = result?.OTP?.ToString() ?? result?.UserID?.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error in password reset: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<bool> CheckUserExistsAsync(string userName = null, string email = null, string mobileNumber = null)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) FROM tbl_Users 
                    WHERE (@UserName IS NULL OR UserName = @UserName) 
                       OR (@Email IS NULL OR Email = @Email) 
                       OR (@MobileNumber IS NULL OR MobileNumber = @MobileNumber)";

                var count = await _connection.QuerySingleAsync<int>(sql, new { UserName = userName, Email = email, MobileNumber = mobileNumber });
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking user existence: {ex.Message}", ex);
            }
        }

        public async Task<List<State>> GetStatesAsync()
        {
            try
            {
                var states = await _connection.QueryAsync<State>(
                    "SELECT StateID, StateName, StateCode FROM tbl_States WHERE IsActive = 1 ORDER BY StateName");
                return states.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting states: {ex.Message}", ex);
            }
        }

        public async Task<List<District>> GetDistrictsAsync(int stateId)
        {
            try
            {
                var districts = await _connection.QueryAsync<District>(
                    "SELECT DistrictID, DistrictName, DistrictCode FROM tbl_Districts WHERE StateID = @StateID AND IsActive = 1 ORDER BY DistrictName",
                    new { StateID = stateId });
                return districts.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting districts: {ex.Message}", ex);
            }
        }

        public async Task<List<Taluka>> GetTalukasAsync(int districtId)
        {
            try
            {
                var talukas = await _connection.QueryAsync<Taluka>(
                    "SELECT TalukaID, TalukaName, TalukaCode FROM tbl_Talukas WHERE DistrictID = @DistrictID AND IsActive = 1 ORDER BY TalukaName",
                    new { DistrictID = districtId });
                return talukas.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting talukas: {ex.Message}", ex);
            }
        }

        public async Task<List<Village>> GetVillagesAsync(int talukaId)
        {
            try
            {
                var villages = await _connection.QueryAsync<Village>(
                    "SELECT VillageID, VillageName, VillageCode FROM tbl_Villages WHERE TalukaID = @TalukaID AND IsActive = 1 ORDER BY VillageName",
                    new { TalukaID = talukaId });
                return villages.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting villages: {ex.Message}", ex);
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                var sql = @"
                    SELECT u.*, s.StateName, d.DistrictName, t.TalukaName, v.VillageName
                    FROM tbl_Users u
                    LEFT JOIN tbl_States s ON u.StateID = s.StateID
                    LEFT JOIN tbl_Districts d ON u.DistrictID = d.DistrictID
                    LEFT JOIN tbl_Talukas t ON u.TalukaID = t.TalukaID
                    LEFT JOIN tbl_Villages v ON u.VillageID = v.VillageID
                    WHERE u.UserID = @UserID";

                var user = await _connection.QueryFirstOrDefaultAsync<User>(sql, new { UserID = userId });
                return user;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting user by ID: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> UpdateUserProfileAsync(User user)
        {
            try
            {
                var sql = @"
                    UPDATE tbl_Users SET 
                        FirstName = @FirstName,
                        LastName = @LastName,
                        Email = @Email,
                        MobileNumber = @MobileNumber,
                        Gender = @Gender,
                        StateID = @StateID,
                        DistrictID = @DistrictID,
                        TalukaID = @TalukaID,
                        VillageID = @VillageID,
                        Address = @Address,
                        Pincode = @Pincode,
                        ProfileImage = @ProfileImage,
                        ModifiedDate = GETDATE()
                    WHERE UserID = @UserID";

                var affectedRows = await _connection.ExecuteAsync(sql, user);

                return new ApiResponse<string>
                {
                    Success = affectedRows > 0,
                    Message = affectedRows > 0 ? "Profile updated successfully" : "Profile update failed",
                    Data = user.UserID.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error updating profile: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var sql = @"
                    UPDATE tbl_Users SET 
                        Password = @NewPassword,
                        ModifiedDate = GETDATE()
                    WHERE UserID = @UserID AND Password = @CurrentPassword";

                var affectedRows = await _connection.ExecuteAsync(sql, new
                {
                    UserID = userId,
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                });

                return new ApiResponse<string>
                {
                    Success = affectedRows > 0,
                    Message = affectedRows > 0 ? "Password changed successfully" : "Current password is incorrect",
                    Data = userId.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error changing password: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<ApiResponse<string>> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                var sql = @"
                    UPDATE tbl_Users SET 
                        IsActive = @IsActive,
                        ModifiedDate = GETDATE()
                    WHERE UserID = @UserID";

                var affectedRows = await _connection.ExecuteAsync(sql, new
                {
                    UserID = userId,
                    IsActive = isActive
                });

                return new ApiResponse<string>
                {
                    Success = affectedRows > 0,
                    Message = affectedRows > 0 ? "User status updated successfully" : "User status update failed",
                    Data = userId.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error updating user status: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }

        public async Task<List<UserSession>> GetUserSessionsAsync(int userId)
        {
            try
            {
                var sql = @"
                    SELECT SessionID, UserID, SessionToken, DeviceInfo, IPAddress, 
                           LoginDate, LastActivity, IsActive
                    FROM tbl_UserSessions 
                    WHERE UserID = @UserID 
                    ORDER BY LoginDate DESC";

                var sessions = await _connection.QueryAsync<UserSession>(sql, new { UserID = userId });
                return sessions.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting user sessions: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> InvalidateSessionAsync(string sessionToken)
        {
            try
            {
                var sql = @"
                    UPDATE tbl_UserSessions SET 
                        IsActive = 0,
                        LastActivity = GETDATE()
                    WHERE SessionToken = @SessionToken";

                var affectedRows = await _connection.ExecuteAsync(sql, new { SessionToken = sessionToken });

                return new ApiResponse<string>
                {
                    Success = affectedRows > 0,
                    Message = affectedRows > 0 ? "Session invalidated successfully" : "Session not found",
                    Data = sessionToken
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error invalidating session: {ex.Message}",
                    Errors = ex.Message
                };
            }
        }
    }

    #region Helper Models

    public class LoginResult
    {
        public int Result { get; set; }
        public string Message { get; set; }
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public string ProfileImage { get; set; }
        public int UserTypeID { get; set; }
        public string SessionToken { get; set; }
        public string StateName { get; set; }
        public string DistrictName { get; set; }
        public string TalukaName { get; set; }
        public string VillageName { get; set; }
    }

    public class UserSession
    {
        public int SessionID { get; set; }
        public int UserID { get; set; }
        public string SessionToken { get; set; }
        public string DeviceInfo { get; set; }
        public string IPAddress { get; set; }
        public DateTime LoginDate { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
    }

    //public class ApiResponse<T>
    //{
    //    public bool Success { get; set; }
    //    public string Message { get; set; }
    //    public T Data { get; set; }
    //    public object Errors { get; set; }
    //}

    #endregion
}