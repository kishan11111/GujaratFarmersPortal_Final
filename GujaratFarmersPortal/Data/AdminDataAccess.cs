//namespace GujaratFarmersPortal.Data
//{
//    public class AdminDataAccess
//    {
//    }
//}


using System.Data;
using System.Data.SqlClient;
using Dapper;
using GujaratFarmersPortal.Models;
//using GujaratFarmersPortal.Models;

namespace GujaratFarmersPortal.Data
{
    public interface IAdminDataAccess
    {
        Task<AdminDashboardViewModel> GetDashboardStatsAsync();
        Task<PagedResult<User>> GetUsersAsync(string searchKeyword = null, int pageNumber = 1, int pageSize = 20);
        Task<ApiResponse<string>> ManageUserAsync(string mode, int userID, int? adminUserID = null);
        Task<PagedResult<Post>> GetPostsAsync(string mode = "GET_ALL", string searchKeyword = null, int? categoryID = null, int? stateID = null, int? districtID = null, string status = null, int pageNumber = 1, int pageSize = 20);
        Task<ApiResponse<string>> ManagePostAsync(string mode, int postID, int adminUserID, string status = null, string rejectionReason = null);
        Task<List<object>> GetPendingReportsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponse<string>> ManageReportAsync(string mode, int reportID, int reviewedBy, string reviewNotes = null);
        Task<List<Category>> GetCategoriesAsync();
        Task<ApiResponse<string>> ManageCategoryAsync(string mode, int? categoryID = null, string categoryName = null, string categoryNameGuj = null, string categoryIcon = null, string categoryImage = null, int? parentCategoryID = null, int sortOrder = 0, bool isActive = true, int? createdBy = null);
        Task<List<SubCategory>> GetSubCategoriesAsync(int? categoryID = null);
        Task<ApiResponse<string>> ManageSubCategoryAsync(string mode, int? subCategoryID = null, int? categoryID = null, string subCategoryName = null, string subCategoryNameGuj = null, string subCategoryIcon = null, string subCategoryImage = null, int sortOrder = 0, bool isActive = true, int? createdBy = null);
        Task<object> GetAnalyticsDataAsync();
        Task<List<object>> GetSystemLogsAsync(int pageNumber = 1, int pageSize = 50);
        Task<ApiResponse<string>> BulkUserOperationAsync(string mode, string userIDs, int adminUserID);
        Task<ApiResponse<string>> BulkPostOperationAsync(string mode, string postIDs, int adminUserID);
    }

    public class AdminDataAccess : IAdminDataAccess
    {
        private readonly IDbConnection _connection;
        private readonly IConfiguration _configuration;

        public AdminDataAccess(IDbConnection connection, IConfiguration configuration)
        {
            _connection = connection;
            _configuration = configuration;
        }

        public async Task<AdminDashboardViewModel> GetDashboardStatsAsync()
        {
            try
            {
                using var multi = await _connection.QueryMultipleAsync("sp_AdminDashboardStats",
                    commandType: CommandType.StoredProcedure);

                // Basic statistics
                var basicStats = await multi.ReadFirstOrDefaultAsync();

                // Monthly user registration stats
                var monthlyStats = await multi.ReadAsync();

                // Category wise post count
                var categoryStats = await multi.ReadAsync();

                // Top districts by posts
                var districtStats = await multi.ReadAsync();

                var dashboardData = new AdminDashboardViewModel
                {
                    TotalUsers = basicStats?.TotalUsers ?? 0,
                    TodayUsers = basicStats?.TodayUsers ?? 0,
                    TotalPosts = basicStats?.TotalPosts ?? 0,
                    TodayPosts = basicStats?.TodayPosts ?? 0,
                    PendingPosts = basicStats?.PendingPosts ?? 0,
                    PendingReports = basicStats?.PendingReports ?? 0,
                    TotalCategories = basicStats?.TotalCategories ?? 0,
                    TodayMessages = basicStats?.TodayMessages ?? 0,
                    MonthlyUserStats = monthlyStats.ToList(),
                    CategoryPostStats = categoryStats.ToList(),
                    TopDistricts = districtStats.ToList()
                };

                // Get recent posts
                var recentPosts = await _connection.QueryAsync<Post>(
                    "sp_PostOperations",
                    new { Mode = "GET_RECENT" },
                    commandType: CommandType.StoredProcedure
                );
                dashboardData.RecentPosts = recentPosts.ToList();

                // Get recent users
                var recentUsers = await _connection.QueryAsync<User>(
                    "sp_UserOperations",
                    new { Mode = "GET_ALL", PageNumber = 1, PageSize = 5 },
                    commandType: CommandType.StoredProcedure
                );
                dashboardData.RecentUsers = recentUsers.ToList();

                return dashboardData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting dashboard stats: {ex.Message}", ex);
            }
        }

        public async Task<PagedResult<User>> GetUsersAsync(string searchKeyword = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var parameters = new
                {
                    Mode = "GET_ALL",
                    SearchKeyword = searchKeyword,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                using var multi = await _connection.QueryMultipleAsync("sp_UserOperations", parameters,
                    commandType: CommandType.StoredProcedure);

                var users = await multi.ReadAsync<User>();
                var totalRecords = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<User>
                {
                    Items = users.ToList(),
                    TotalRecords = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < Math.Ceiling((double)totalRecords / pageSize)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting users: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> ManageUserAsync(string mode, int userID, int? adminUserID = null)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    UserID = userID,
                    CreatedBy = adminUserID
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_UserOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Operation completed",
                    Data = result?.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error managing user: {ex.Message}",
                    Errors = ex
                };
            }
        }

        public async Task<PagedResult<Post>> GetPostsAsync(string mode = "GET_ALL", string searchKeyword = null, int? categoryID = null, int? stateID = null, int? districtID = null, string status = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    SearchKeyword = searchKeyword,
                    CategoryID = categoryID,
                    StateID = stateID,
                    DistrictID = districtID,
                    Status = status,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                using var multi = await _connection.QueryMultipleAsync("sp_AdminPostManagement", parameters,
                    commandType: CommandType.StoredProcedure);

                var posts = await multi.ReadAsync<Post>();
                var totalRecords = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<Post>
                {
                    Items = posts.ToList(),
                    TotalRecords = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < Math.Ceiling((double)totalRecords / pageSize)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting posts: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> ManagePostAsync(string mode, int postID, int adminUserID, string status = null, string rejectionReason = null)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    PostID = postID,
                    AdminUserID = adminUserID,
                    Status = status,
                    RejectionReason = rejectionReason
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_AdminPostManagement", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Operation completed",
                    Data = result?.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error managing post: {ex.Message}",
                    Errors = ex
                };
            }
        }

        public async Task<List<object>> GetPendingReportsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var parameters = new
                {
                    Mode = "GET_PENDING",
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var reports = await _connection.QueryAsync(
                    "sp_PostReportOperations", parameters, commandType: CommandType.StoredProcedure);

                return reports.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting pending reports: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> ManageReportAsync(string mode, int reportID, int reviewedBy, string reviewNotes = null)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    ReportID = reportID,
                    ReviewedBy = reviewedBy,
                    ReviewNotes = reviewNotes
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_PostReportOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Operation completed",
                    Data = result?.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error managing report: {ex.Message}",
                    Errors = ex
                };
            }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _connection.QueryAsync<Category>(
                    "sp_CategoryOperations",
                    new { Mode = "GET_ALL" },
                    commandType: CommandType.StoredProcedure
                );

                return categories.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting categories: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> ManageCategoryAsync(string mode, int? categoryID = null, string categoryName = null, string categoryNameGuj = null, string categoryIcon = null, string categoryImage = null, int? parentCategoryID = null, int sortOrder = 0, bool isActive = true, int? createdBy = null)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    CategoryID = categoryID,
                    CategoryName = categoryName,
                    CategoryNameGuj = categoryNameGuj,
                    CategoryIcon = categoryIcon,
                    CategoryImage = categoryImage,
                    ParentCategoryID = parentCategoryID,
                    SortOrder = sortOrder,
                    IsActive = isActive,
                    CreatedBy = createdBy
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_CategoryOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Operation completed",
                    Data = result?.CategoryID?.ToString() ?? result?.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error managing category: {ex.Message}",
                    Errors = ex
                };
            }
        }

        public async Task<List<SubCategory>> GetSubCategoriesAsync(int? categoryID = null)
        {
            try
            {
                var mode = categoryID.HasValue ? "GET_BY_CATEGORY" : "GET_ALL";
                var subCategories = await _connection.QueryAsync<SubCategory>(
                    "sp_SubCategoryOperations",
                    new { Mode = mode, CategoryID = categoryID },
                    commandType: CommandType.StoredProcedure
                );

                return subCategories.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting subcategories: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> ManageSubCategoryAsync(string mode, int? subCategoryID = null, int? categoryID = null, string subCategoryName = null, string subCategoryNameGuj = null, string subCategoryIcon = null, string subCategoryImage = null, int sortOrder = 0, bool isActive = true, int? createdBy = null)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    SubCategoryID = subCategoryID,
                    CategoryID = categoryID,
                    SubCategoryName = subCategoryName,
                    SubCategoryNameGuj = subCategoryNameGuj,
                    SubCategoryIcon = subCategoryIcon,
                    SubCategoryImage = subCategoryImage,
                    SortOrder = sortOrder,
                    IsActive = isActive,
                    CreatedBy = createdBy
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_SubCategoryOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.Result == 1,
                    Message = result?.Message ?? "Operation completed",
                    Data = result?.SubCategoryID?.ToString() ?? result?.Message
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error managing subcategory: {ex.Message}",
                    Errors = ex
                };
            }
        }

        public async Task<object> GetAnalyticsDataAsync()
        {
            try
            {
                // Get various analytics data
                var userAnalytics = await _connection.QueryAsync(
                    "sp_UserAnalytics",
                    new { Mode = "GET_MONTHLY_STATS", StartDate = DateTime.Now.AddMonths(-12), EndDate = DateTime.Now },
                    commandType: CommandType.StoredProcedure
                );

                var dashboardStats = await GetDashboardStatsAsync();

                return new
                {
                    UserAnalytics = userAnalytics,
                    DashboardStats = dashboardStats,
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting analytics data: {ex.Message}", ex);
            }
        }

        public async Task<List<object>> GetSystemLogsAsync(int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                var logs = await _connection.QueryAsync(
                    @"SELECT TOP (@PageSize) * FROM tbl_AuditLogs 
                      WHERE LogID NOT IN (SELECT TOP (@Offset) LogID FROM tbl_AuditLogs ORDER BY CreatedDate DESC)
                      ORDER BY CreatedDate DESC",
                    new { PageSize = pageSize, Offset = (pageNumber - 1) * pageSize }
                );

                return logs.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting system logs: {ex.Message}", ex);
            }
        }

        public async Task<ApiResponse<string>> BulkUserOperationAsync(string mode, string userIDs, int adminUserID)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    UserIDs = userIDs,
                    AdminUserID = adminUserID
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_BulkUserOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.AffectedRows > 0,
                    Message = result?.Message ?? "Bulk operation completed",
                    Data = result?.AffectedRows?.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error in bulk user operation: {ex.Message}",
                    Errors = ex
                };
            }
        }

        public async Task<ApiResponse<string>> BulkPostOperationAsync(string mode, string postIDs, int adminUserID)
        {
            try
            {
                var parameters = new
                {
                    Mode = mode,
                    PostIDs = postIDs,
                    AdminUserID = adminUserID
                };

                var result = await _connection.QueryFirstOrDefaultAsync(
                    "sp_BulkPostOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = result?.AffectedRows > 0,
                    Message = result?.Message ?? "Bulk operation completed",
                    Data = result?.AffectedRows?.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error in bulk post operation: {ex.Message}",
                    Errors = ex
                };
            }
        }
    }
}