//namespace GujaratFarmersPortal.Services
//{
//    public class AdminService
//    {
//    }
//}

using GujaratFarmersPortal.Data;
using GujaratFarmersPortal.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GujaratFarmersPortal.Services
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardDataAsync();
        Task<PagedResult<User>> GetUsersAsync(string searchKeyword = null, int pageNumber = 1, int pageSize = 20);
        Task<User> GetUserByIdAsync(int userID);
        Task<ApiResponse<string>> ActivateUserAsync(int userID, int adminUserID);
        Task<ApiResponse<string>> DeactivateUserAsync(int userID, int adminUserID);
        Task<ApiResponse<string>> BanUserAsync(int userID, int adminUserID);
        Task<ApiResponse<string>> UnbanUserAsync(int userID, int adminUserID);
        Task<ApiResponse<string>> BulkUserOperationAsync(string operation, List<int> userIDs, int adminUserID);

        Task<PagedResult<Post>> GetPostsAsync(string filter = "all", string searchKeyword = null, int? categoryID = null, int? stateID = null, int? districtID = null, string status = null, int pageNumber = 1, int pageSize = 20);
        Task<Post> GetPostByIdAsync(int postID);
        Task<ApiResponse<string>> ApprovePostAsync(int postID, int adminUserID);
        Task<ApiResponse<string>> RejectPostAsync(int postID, int adminUserID, string rejectionReason);
        Task<ApiResponse<string>> FeaturePostAsync(int postID, int adminUserID);
        Task<ApiResponse<string>> UnfeaturePostAsync(int postID, int adminUserID);
        Task<ApiResponse<string>> DeletePostAsync(int postID, int adminUserID);
        Task<ApiResponse<string>> BulkPostOperationAsync(string operation, List<int> postIDs, int adminUserID);

        Task<List<Category>> GetCategoriesAsync(bool useCache = true);
        Task<Category> GetCategoryByIdAsync(int categoryID);
        Task<ApiResponse<string>> CreateCategoryAsync(Category category, int adminUserID);
        Task<ApiResponse<string>> UpdateCategoryAsync(Category category, int adminUserID);
        Task<ApiResponse<string>> DeleteCategoryAsync(int categoryID, int adminUserID);

        Task<List<SubCategory>> GetSubCategoriesAsync(int? categoryID = null, bool useCache = true);
        Task<SubCategory> GetSubCategoryByIdAsync(int subCategoryID);
        Task<ApiResponse<string>> CreateSubCategoryAsync(SubCategory subCategory, int adminUserID);
        Task<ApiResponse<string>> UpdateSubCategoryAsync(SubCategory subCategory, int adminUserID);
        Task<ApiResponse<string>> DeleteSubCategoryAsync(int subCategoryID, int adminUserID);

        Task<List<object>> GetPendingReportsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponse<string>> ReviewReportAsync(int reportID, int adminUserID, string action, string reviewNotes = null);

        Task<object> GetAnalyticsDataAsync();
        Task<List<object>> GetSystemLogsAsync(int pageNumber = 1, int pageSize = 50);
        Task<object> GetSystemInfoAsync();

        void ClearCache();
        void ClearCacheByPattern(string pattern);
    }

    public class AdminService : IAdminService
    {
        private readonly IAdminDataAccess _adminDataAccess;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AdminService> _logger;
        private readonly IConfiguration _configuration;

        private const string CACHE_KEY_CATEGORIES = "admin_categories";
        private const string CACHE_KEY_SUBCATEGORIES = "admin_subcategories";
        private const string CACHE_KEY_DASHBOARD = "admin_dashboard";
        private const int CACHE_EXPIRY_MINUTES = 30;

        public AdminService(IAdminDataAccess adminDataAccess, IMemoryCache memoryCache, ILogger<AdminService> logger, IConfiguration configuration)
        {
            _adminDataAccess = adminDataAccess;
            _memoryCache = memoryCache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
        {
            try
            {
                // Check cache first
                if (_memoryCache.TryGetValue(CACHE_KEY_DASHBOARD, out AdminDashboardViewModel cachedDashboard))
                {
                    return cachedDashboard;
                }

                var dashboard = await _adminDataAccess.GetDashboardStatsAsync();

                // Cache for 5 minutes for dashboard data
                _memoryCache.Set(CACHE_KEY_DASHBOARD, dashboard, TimeSpan.FromMinutes(5));

                _logger.LogInformation("Dashboard data retrieved successfully");
                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                throw;
            }
        }

        public async Task<PagedResult<User>> GetUsersAsync(string searchKeyword = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                return await _adminDataAccess.GetUsersAsync(searchKeyword, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users with search: {SearchKeyword}", searchKeyword);
                throw;
            }
        }

        public async Task<User> GetUserByIdAsync(int userID)
        {
            try
            {
                var result = await _adminDataAccess.GetUsersAsync(null, 1, 1);
                return result.Items.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserID}", userID);
                throw;
            }
        }

        public async Task<ApiResponse<string>> ActivateUserAsync(int userID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageUserAsync("ACTIVATE", userID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} activated by admin {AdminUserID}", userID, adminUserID);
                    ClearCacheByPattern("users");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserID}", userID);
                return new ApiResponse<string> { Success = false, Message = "તકનીકી ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> DeactivateUserAsync(int userID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageUserAsync("DELETE", userID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} deactivated by admin {AdminUserID}", userID, adminUserID);
                    ClearCacheByPattern("users");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserID}", userID);
                return new ApiResponse<string> { Success = false, Message = "તકનીકી ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> BanUserAsync(int userID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageUserAsync("BAN", userID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} banned by admin {AdminUserID}", userID, adminUserID);
                    ClearCacheByPattern("users");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user {UserID}", userID);
                return new ApiResponse<string> { Success = false, Message = "તકનીકી ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> UnbanUserAsync(int userID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageUserAsync("UNBAN", userID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("User {UserID} unbanned by admin {AdminUserID}", userID, adminUserID);
                    ClearCacheByPattern("users");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning user {UserID}", userID);
                return new ApiResponse<string> { Success = false, Message = "તકનીકી ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> BulkUserOperationAsync(string operation, List<int> userIDs, int adminUserID)
        {
            try
            {
                string mode = operation.ToUpper() switch
                {
                    "activate" => "BULK_ACTIVATE",
                    "deactivate" => "BULK_DEACTIVATE",
                    "ban" => "BULK_BAN",
                    "unban" => "BULK_UNBAN",
                    _ => throw new ArgumentException("Invalid operation")
                };

                var userIDsString = string.Join(",", userIDs);
                var result = await _adminDataAccess.BulkUserOperationAsync(mode, userIDsString, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Bulk user operation {Operation} performed on {UserCount} users by admin {AdminUserID}",
                        operation, userIDs.Count, adminUserID);
                    ClearCacheByPattern("users");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk user operation {Operation}", operation);
                return new ApiResponse<string> { Success = false, Message = "બલ્ક ઓપરેશનમાં ભૂલ આવી છે" };
            }
        }

        public async Task<PagedResult<Post>> GetPostsAsync(string filter = "all", string searchKeyword = null, int? categoryID = null, int? stateID = null, int? districtID = null, string status = null, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                string mode = filter.ToLower() switch
                {
                    "pending" => "GET_PENDING",
                    "all" => "GET_ALL",
                    _ => "GET_ALL"
                };

                return await _adminDataAccess.GetPostsAsync(mode, searchKeyword, categoryID, stateID, districtID, status, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts with filter: {Filter}", filter);
                throw;
            }
        }

        public async Task<Post> GetPostByIdAsync(int postID)
        {
            try
            {
                var result = await _adminDataAccess.GetPostsAsync("GET_ALL", null, null, null, null, null, 1, 1);
                return result.Items.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post by ID: {PostID}", postID);
                throw;
            }
        }

        public async Task<ApiResponse<string>> ApprovePostAsync(int postID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManagePostAsync("APPROVE", postID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Post {PostID} approved by admin {AdminUserID}", postID, adminUserID);
                    ClearCacheByPattern("posts");
                    ClearCacheByPattern("dashboard");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving post {PostID}", postID);
                return new ApiResponse<string> { Success = false, Message = "પોસ્ટ મંજૂર કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> RejectPostAsync(int postID, int adminUserID, string rejectionReason)
        {
            try
            {
                var result = await _adminDataAccess.ManagePostAsync("REJECT", postID, adminUserID, "Rejected", rejectionReason);

                if (result.Success)
                {
                    _logger.LogInformation("Post {PostID} rejected by admin {AdminUserID} with reason: {Reason}",
                        postID, adminUserID, rejectionReason);
                    ClearCacheByPattern("posts");
                    ClearCacheByPattern("dashboard");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting post {PostID}", postID);
                return new ApiResponse<string> { Success = false, Message = "પોસ્ટ નકારવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> FeaturePostAsync(int postID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManagePostAsync("FEATURE", postID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Post {PostID} featured by admin {AdminUserID}", postID, adminUserID);
                    ClearCacheByPattern("posts");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error featuring post {PostID}", postID);
                return new ApiResponse<string> { Success = false, Message = "પોસ્ટ ફીચર કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> UnfeaturePostAsync(int postID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManagePostAsync("UNFEATURE", postID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Post {PostID} unfeatured by admin {AdminUserID}", postID, adminUserID);
                    ClearCacheByPattern("posts");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfeaturing post {PostID}", postID);
                return new ApiResponse<string> { Success = false, Message = "પોસ્ટ અનફીચર કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> DeletePostAsync(int postID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManagePostAsync("DELETE", postID, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Post {PostID} deleted by admin {AdminUserID}", postID, adminUserID);
                    ClearCacheByPattern("posts");
                    ClearCacheByPattern("dashboard");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostID}", postID);
                return new ApiResponse<string> { Success = false, Message = "પોસ્ટ ડિલીટ કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> BulkPostOperationAsync(string operation, List<int> postIDs, int adminUserID)
        {
            try
            {
                string mode = operation.ToUpper() switch
                {
                    "approve" => "BULK_APPROVE",
                    "reject" => "BULK_REJECT",
                    "feature" => "BULK_FEATURE",
                    "unfeature" => "BULK_UNFEATURE",
                    "delete" => "BULK_DELETE",
                    _ => throw new ArgumentException("Invalid operation")
                };

                var postIDsString = string.Join(",", postIDs);
                var result = await _adminDataAccess.BulkPostOperationAsync(mode, postIDsString, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Bulk post operation {Operation} performed on {PostCount} posts by admin {AdminUserID}",
                        operation, postIDs.Count, adminUserID);
                    ClearCacheByPattern("posts");
                    ClearCacheByPattern("dashboard");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk post operation {Operation}", operation);
                return new ApiResponse<string> { Success = false, Message = "બલ્ક પોસ્ટ ઓપરેશનમાં ભૂલ આવી છે" };
            }
        }

        public async Task<List<Category>> GetCategoriesAsync(bool useCache = true)
        {
            try
            {
                if (useCache && _memoryCache.TryGetValue(CACHE_KEY_CATEGORIES, out List<Category> cachedCategories))
                {
                    return cachedCategories;
                }

                var categories = await _adminDataAccess.GetCategoriesAsync();

                if (useCache)
                {
                    _memoryCache.Set(CACHE_KEY_CATEGORIES, categories, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                throw;
            }
        }

        public async Task<Category> GetCategoryByIdAsync(int categoryID)
        {
            try
            {
                var categories = await GetCategoriesAsync();
                return categories.FirstOrDefault(c => c.CategoryID == categoryID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category by ID: {CategoryID}", categoryID);
                throw;
            }
        }

        public async Task<ApiResponse<string>> CreateCategoryAsync(Category category, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageCategoryAsync(
                    "INSERT", null, category.CategoryName, category.CategoryNameGuj,
                    category.CategoryIcon, category.CategoryImage, category.ParentCategoryID,
                    category.SortOrder, category.IsActive, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Category {CategoryName} created by admin {AdminUserID}",
                        category.CategoryName, adminUserID);
                    ClearCacheByPattern("categories");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category {CategoryName}", category.CategoryName);
                return new ApiResponse<string> { Success = false, Message = "કેટેગરી બનાવવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> UpdateCategoryAsync(Category category, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageCategoryAsync(
                    "UPDATE", category.CategoryID, category.CategoryName, category.CategoryNameGuj,
                    category.CategoryIcon, category.CategoryImage, category.ParentCategoryID,
                    category.SortOrder, category.IsActive, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Category {CategoryID} updated by admin {AdminUserID}",
                        category.CategoryID, adminUserID);
                    ClearCacheByPattern("categories");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryID}", category.CategoryID);
                return new ApiResponse<string> { Success = false, Message = "કેટેગરી અપડેટ કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> DeleteCategoryAsync(int categoryID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageCategoryAsync("DELETE", categoryID, createdBy: adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("Category {CategoryID} deleted by admin {AdminUserID}",
                        categoryID, adminUserID);
                    ClearCacheByPattern("categories");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryID}", categoryID);
                return new ApiResponse<string> { Success = false, Message = "કેટેગરી ડિલીટ કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<List<SubCategory>> GetSubCategoriesAsync(int? categoryID = null, bool useCache = true)
        {
            try
            {
                string cacheKey = $"{CACHE_KEY_SUBCATEGORIES}_{categoryID}";

                if (useCache && _memoryCache.TryGetValue(cacheKey, out List<SubCategory> cachedSubCategories))
                {
                    return cachedSubCategories;
                }

                var subCategories = await _adminDataAccess.GetSubCategoriesAsync(categoryID);

                if (useCache)
                {
                    _memoryCache.Set(cacheKey, subCategories, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }

                return subCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategories for category: {CategoryID}", categoryID);
                throw;
            }
        }

        public async Task<SubCategory> GetSubCategoryByIdAsync(int subCategoryID)
        {
            try
            {
                var subCategories = await GetSubCategoriesAsync();
                return subCategories.FirstOrDefault(sc => sc.SubCategoryID == subCategoryID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategory by ID: {SubCategoryID}", subCategoryID);
                throw;
            }
        }

        public async Task<ApiResponse<string>> CreateSubCategoryAsync(SubCategory subCategory, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageSubCategoryAsync(
                    "INSERT", null, subCategory.CategoryID, subCategory.SubCategoryName,
                    subCategory.SubCategoryNameGuj, subCategory.SubCategoryIcon,
                    subCategory.SubCategoryImage, subCategory.SortOrder, subCategory.IsActive, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("SubCategory {SubCategoryName} created by admin {AdminUserID}",
                        subCategory.SubCategoryName, adminUserID);
                    ClearCacheByPattern("subcategories");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subcategory {SubCategoryName}", subCategory.SubCategoryName);
                return new ApiResponse<string> { Success = false, Message = "સબ કેટેગરી બનાવવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> UpdateSubCategoryAsync(SubCategory subCategory, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageSubCategoryAsync(
                    "UPDATE", subCategory.SubCategoryID, subCategory.CategoryID, subCategory.SubCategoryName,
                    subCategory.SubCategoryNameGuj, subCategory.SubCategoryIcon,
                    subCategory.SubCategoryImage, subCategory.SortOrder, subCategory.IsActive, adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("SubCategory {SubCategoryID} updated by admin {AdminUserID}",
                        subCategory.SubCategoryID, adminUserID);
                    ClearCacheByPattern("subcategories");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subcategory {SubCategoryID}", subCategory.SubCategoryID);
                return new ApiResponse<string> { Success = false, Message = "સબ કેટેગરી અપડેટ કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<ApiResponse<string>> DeleteSubCategoryAsync(int subCategoryID, int adminUserID)
        {
            try
            {
                var result = await _adminDataAccess.ManageSubCategoryAsync("DELETE", subCategoryID, createdBy: adminUserID);

                if (result.Success)
                {
                    _logger.LogInformation("SubCategory {SubCategoryID} deleted by admin {AdminUserID}",
                        subCategoryID, adminUserID);
                    ClearCacheByPattern("subcategories");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcategory {SubCategoryID}", subCategoryID);
                return new ApiResponse<string> { Success = false, Message = "સબ કેટેગરી ડિલીટ કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<List<object>> GetPendingReportsAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                return await _adminDataAccess.GetPendingReportsAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending reports");
                throw;
            }
        }

        public async Task<ApiResponse<string>> ReviewReportAsync(int reportID, int adminUserID, string action, string reviewNotes = null)
        {
            try
            {
                string mode = action.ToLower() switch
                {
                    "review" => "REVIEW",
                    "resolve" => "RESOLVE",
                    _ => "REVIEW"
                };

                var result = await _adminDataAccess.ManageReportAsync(mode, reportID, adminUserID, reviewNotes);

                if (result.Success)
                {
                    _logger.LogInformation("Report {ReportID} {Action} by admin {AdminUserID}",
                        reportID, action, adminUserID);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing report {ReportID}", reportID);
                return new ApiResponse<string> { Success = false, Message = "રિપોર્ટ રિવ્યુ કરવામાં ભૂલ આવી છે" };
            }
        }

        public async Task<object> GetAnalyticsDataAsync()
        {
            try
            {
                return await _adminDataAccess.GetAnalyticsDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics data");
                throw;
            }
        }

        public async Task<List<object>> GetSystemLogsAsync(int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                return await _adminDataAccess.GetSystemLogsAsync(pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system logs");
                throw;
            }
        }

        public async Task<object> GetSystemInfoAsync()
        {
            try
            {
                return new
                {
                    ServerTime = DateTime.Now,
                    Environment = Environment.MachineName,
                    Framework = Environment.Version.ToString(),
                    WorkingSet = Environment.WorkingSet,
                    ProcessorCount = Environment.ProcessorCount,
                    OSVersion = Environment.OSVersion.ToString(),
                    ApplicationVersion = "1.0.0",
                    DatabaseConnection = _configuration.GetConnectionString("DefaultConnection") != null ? "Connected" : "Disconnected",
                    CacheStatus = "Active",
                    UptimeMinutes = Environment.TickCount / (1000 * 60)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system info");
                throw;
            }
        }

        public void ClearCache()
        {
            try
            {
                _memoryCache.Remove(CACHE_KEY_CATEGORIES);
                _memoryCache.Remove(CACHE_KEY_SUBCATEGORIES);
                _memoryCache.Remove(CACHE_KEY_DASHBOARD);

                _logger.LogInformation("All admin cache cleared");
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
                // Since IMemoryCache doesn't support pattern removal, we'll remove specific keys
                switch (pattern.ToLower())
                {
                    case "categories":
                        _memoryCache.Remove(CACHE_KEY_CATEGORIES);
                        break;
                    case "subcategories":
                        _memoryCache.Remove(CACHE_KEY_SUBCATEGORIES);
                        break;
                    case "dashboard":
                        _memoryCache.Remove(CACHE_KEY_DASHBOARD);
                        break;
                    case "posts":
                        // Clear dashboard cache as it contains post stats
                        _memoryCache.Remove(CACHE_KEY_DASHBOARD);
                        break;
                    case "users":
                        // Clear dashboard cache as it contains user stats
                        _memoryCache.Remove(CACHE_KEY_DASHBOARD);
                        break;
                }

                _logger.LogInformation("Cache cleared for pattern: {Pattern}", pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}
