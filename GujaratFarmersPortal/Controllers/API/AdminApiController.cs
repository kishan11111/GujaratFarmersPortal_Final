using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GujaratFarmersPortal.Services;
using GujaratFarmersPortal.Models;
using System.Security.Claims;

namespace GujaratFarmersPortal.Controllers.API
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(IAdminService adminService, ILogger<AdminApiController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        private int GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        #region Dashboard APIs

        /// <summary>
        /// Get admin dashboard statistics
        /// </summary>
        /// <returns>Dashboard data</returns>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<AdminDashboardViewModel>>> GetDashboardStats()
        {
            try
            {
                var dashboardData = await _adminService.GetDashboardDataAsync();

                return Ok(new ApiResponse<AdminDashboardViewModel>
                {
                    Success = true,
                    Message = "Dashboard data retrieved successfully",
                    Data = dashboardData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data via API");
                return StatusCode(500, new ApiResponse<AdminDashboardViewModel>
                {
                    Success = false,
                    Message = "ડેશબોર્ડ ડેટા લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region User Management APIs

        /// <summary>
        /// Get paginated list of users
        /// </summary>
        /// <param name="search">Search keyword</param>
        /// <param name="page">Page number</param>
        /// <param name="size">Page size</param>
        /// <returns>Paginated user list</returns>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PagedResult<User>>>> GetUsers(
            [FromQuery] string search = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                var users = await _adminService.GetUsersAsync(search, page, size);

                return Ok(new ApiResponse<PagedResult<User>>
                {
                    Success = true,
                    Message = "Users retrieved successfully",
                    Data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users via API");
                return StatusCode(500, new ApiResponse<PagedResult<User>>
                {
                    Success = false,
                    Message = "વપરાશકર્તાઓની માહિતી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("users/{id}")]
        public async Task<ActionResult<ApiResponse<User>>> GetUser(int id)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new ApiResponse<User>
                    {
                        Success = false,
                        Message = "વપરાશકર્તા મળ્યો નથી"
                    });
                }

                return Ok(new ApiResponse<User>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId} via API", id);
                return StatusCode(500, new ApiResponse<User>
                {
                    Success = false,
                    Message = "વપરાશકર્તાની વિગતો લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Activate user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("users/{id}/activate")]
        public async Task<ActionResult<ApiResponse<string>>> ActivateUser(int id)
        {
            try
            {
                var result = await _adminService.ActivateUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "વપરાશકર્તા સક્રિય કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Deactivate user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("users/{id}/deactivate")]
        public async Task<ActionResult<ApiResponse<string>>> DeactivateUser(int id)
        {
            try
            {
                var result = await _adminService.DeactivateUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "વપરાશકર્તા નિષ્ક્રિય કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Ban user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("users/{id}/ban")]
        public async Task<ActionResult<ApiResponse<string>>> BanUser(int id)
        {
            try
            {
                var result = await _adminService.BanUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user {UserId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "વપરાશકર્તા બેન કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Unban user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("users/{id}/unban")]
        public async Task<ActionResult<ApiResponse<string>>> UnbanUser(int id)
        {
            try
            {
                var result = await _adminService.UnbanUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning user {UserId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "વપરાશકર્તા અનબેન કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Bulk user operations
        /// </summary>
        /// <param name="request">Bulk operation request</param>
        /// <returns>Operation result</returns>
        [HttpPost("users/bulk")]
        public async Task<ActionResult<ApiResponse<string>>> BulkUserOperation([FromBody] BulkOperationRequest request)
        {
            try
            {
                if (request?.UserIDs == null || !request.UserIDs.Any())
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "વપરાશકર્તા IDs આવશ્યક છે"
                    });
                }

                var result = await _adminService.BulkUserOperationAsync(request.Operation, request.UserIDs, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk user operation via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "બલ્ક ઓપરેશનમાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Post Management APIs

        /// <summary>
        /// Get paginated list of posts
        /// </summary>
        /// <param name="filter">Filter type (all, pending)</param>
        /// <param name="search">Search keyword</param>
        /// <param name="categoryId">Category ID</param>
        /// <param name="stateId">State ID</param>
        /// <param name="districtId">District ID</param>
        /// <param name="status">Post status</param>
        /// <param name="page">Page number</param>
        /// <param name="size">Page size</param>
        /// <returns>Paginated post list</returns>
        [HttpGet("posts")]
        public async Task<ActionResult<ApiResponse<PagedResult<Post>>>> GetPosts(
            [FromQuery] string filter = "all",
            [FromQuery] string search = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] int? stateId = null,
            [FromQuery] int? districtId = null,
            [FromQuery] string status = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                var posts = await _adminService.GetPostsAsync(filter, search, categoryId, stateId, districtId, status, page, size);

                return Ok(new ApiResponse<PagedResult<Post>>
                {
                    Success = true,
                    Message = "Posts retrieved successfully",
                    Data = posts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts via API");
                return StatusCode(500, new ApiResponse<PagedResult<Post>>
                {
                    Success = false,
                    Message = "પોસ્ટ્સની માહિતી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get post by ID
        /// </summary>
        /// <param name="id">Post ID</param>
        /// <returns>Post details</returns>
        [HttpGet("posts/{id}")]
        public async Task<ActionResult<ApiResponse<Post>>> GetPost(int id)
        {
            try
            {
                var post = await _adminService.GetPostByIdAsync(id);

                if (post == null)
                {
                    return NotFound(new ApiResponse<Post>
                    {
                        Success = false,
                        Message = "પોસ્ટ મળી નથી"
                    });
                }

                return Ok(new ApiResponse<Post>
                {
                    Success = true,
                    Message = "Post retrieved successfully",
                    Data = post
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post {PostId} via API", id);
                return StatusCode(500, new ApiResponse<Post>
                {
                    Success = false,
                    Message = "પોસ્ટની વિગતો લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Approve post
        /// </summary>
        /// <param name="id">Post ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("posts/{id}/approve")]
        public async Task<ActionResult<ApiResponse<string>>> ApprovePost(int id)
        {
            try
            {
                var result = await _adminService.ApprovePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving post {PostId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પોસ્ટ મંજૂર કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Reject post
        /// </summary>
        /// <param name="id">Post ID</param>
        /// <param name="request">Rejection request with reason</param>
        /// <returns>Operation result</returns>
        [HttpPost("posts/{id}/reject")]
        public async Task<ActionResult<ApiResponse<string>>> RejectPost(int id, [FromBody] RejectPostRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.RejectionReason))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "રિજેક્ટ કરવાનું કારણ આવશ્યક છે"
                    });
                }

                var result = await _adminService.RejectPostAsync(id, GetCurrentUserID(), request.RejectionReason);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting post {PostId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પોસ્ટ રિજેક્ટ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Feature post
        /// </summary>
        /// <param name="id">Post ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("posts/{id}/feature")]
        public async Task<ActionResult<ApiResponse<string>>> FeaturePost(int id)
        {
            try
            {
                var result = await _adminService.FeaturePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error featuring post {PostId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પોસ્ટ ફીચર કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Unfeature post
        /// </summary>
        /// <param name="id">Post ID</param>
        /// <returns>Operation result</returns>
        [HttpPost("posts/{id}/unfeature")]
        public async Task<ActionResult<ApiResponse<string>>> UnfeaturePost(int id)
        {
            try
            {
                var result = await _adminService.UnfeaturePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfeaturing post {PostId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પોસ્ટ અનફીચર કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete post
        /// </summary>
        /// <param name="id">Post ID</param>
        /// <returns>Operation result</returns>
        [HttpDelete("posts/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeletePost(int id)
        {
            try
            {
                var result = await _adminService.DeletePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "પોસ્ટ ડિલીટ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Bulk post operations
        /// </summary>
        /// <param name="request">Bulk operation request</param>
        /// <returns>Operation result</returns>
        [HttpPost("posts/bulk")]
        public async Task<ActionResult<ApiResponse<string>>> BulkPostOperation([FromBody] BulkPostOperationRequest request)
        {
            try
            {
                if (request?.PostIDs == null || !request.PostIDs.Any())
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "પોસ્ટ IDs આવશ્યક છે"
                    });
                }

                var result = await _adminService.BulkPostOperationAsync(request.Operation, request.PostIDs, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk post operation via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "બલ્ક પોસ્ટ ઓપરેશનમાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Category Management APIs

        /// <summary>
        /// Get all categories
        /// </summary>
        /// <returns>List of categories</returns>
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<List<Category>>>> GetCategories()
        {
            try
            {
                var categories = await _adminService.GetCategoriesAsync();

                return Ok(new ApiResponse<List<Category>>
                {
                    Success = true,
                    Message = "Categories retrieved successfully",
                    Data = categories
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories via API");
                return StatusCode(500, new ApiResponse<List<Category>>
                {
                    Success = false,
                    Message = "કેટેગરીની માહિતી લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category details</returns>
        [HttpGet("categories/{id}")]
        public async Task<ActionResult<ApiResponse<Category>>> GetCategory(int id)
        {
            try
            {
                var category = await _adminService.GetCategoryByIdAsync(id);

                if (category == null)
                {
                    return NotFound(new ApiResponse<Category>
                    {
                        Success = false,
                        Message = "કેટેગરી મળી નથી"
                    });
                }

                return Ok(new ApiResponse<Category>
                {
                    Success = true,
                    Message = "Category retrieved successfully",
                    Data = category
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category {CategoryId} via API", id);
                return StatusCode(500, new ApiResponse<Category>
                {
                    Success = false,
                    Message = "કેટેગરીની વિગતો લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Create new category
        /// </summary>
        /// <param name="category">Category data</param>
        /// <returns>Operation result</returns>
        [HttpPost("categories")]
        public async Task<ActionResult<ApiResponse<string>>> CreateCategory([FromBody] Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "અમાન્ય ડેટા",
                        Errors = ModelState
                    });
                }

                var result = await _adminService.CreateCategoryAsync(category, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેટેગરી બનાવવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Update category
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="category">Category data</param>
        /// <returns>Operation result</returns>
        [HttpPut("categories/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateCategory(int id, [FromBody] Category category)
        {
            try
            {
                category.CategoryID = id;

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "અમાન્ય ડેટા",
                        Errors = ModelState
                    });
                }

                var result = await _adminService.UpdateCategoryAsync(category, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેટેગરી અપડેટ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete category
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Operation result</returns>
        [HttpDelete("categories/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteCategory(int id)
        {
            try
            {
                var result = await _adminService.DeleteCategoryAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId} via API", id);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેટેગરી ડિલીટ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region Analytics APIs

        /// <summary>
        /// Get analytics data
        /// </summary>
        /// <returns>Analytics data</returns>
        [HttpGet("analytics")]
        public async Task<ActionResult<ApiResponse<object>>> GetAnalytics()
        {
            try
            {
                var analyticsData = await _adminService.GetAnalyticsDataAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Analytics data retrieved successfully",
                    Data = analyticsData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics data via API");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "એનાલિટિક્સ ડેટા લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion

        #region System APIs

        /// <summary>
        /// Get system information
        /// </summary>
        /// <returns>System info</returns>
        [HttpGet("system/info")]
        public async Task<ActionResult<ApiResponse<object>>> GetSystemInfo()
        {
            try
            {
                var systemInfo = await _adminService.GetSystemInfoAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "System info retrieved successfully",
                    Data = systemInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system info via API");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "સિસ્ટમ ઇન્ફો લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <returns>Operation result</returns>
        [HttpPost("system/clear-cache")]
        public ActionResult<ApiResponse<string>> ClearCache()
        {
            try
            {
                _adminService.ClearCache();

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "કેશ સફળતાપૂર્વક ક્લિયર કરવામાં આવ્યો",
                    Data = "Cache cleared successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache via API");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેશ ક્લિયર કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        /// <summary>
        /// Get system logs
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="size">Page size</param>
        /// <returns>System logs</returns>
        [HttpGet("system/logs")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetSystemLogs(
            [FromQuery] int page = 1,
            [FromQuery] int size = 50)
        {
            try
            {
                var logs = await _adminService.GetSystemLogsAsync(page, size);

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "System logs retrieved successfully",
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system logs via API");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "સિસ્ટમ લોગ્સ લોડ કરવામાં ભૂલ આવી છે",
                    Errors = ex.Message
                });
            }
        }

        #endregion
    }

    #region Request Models

    public class BulkOperationRequest
    {
        public string Operation { get; set; }
        public List<int> UserIDs { get; set; }
    }

    public class BulkPostOperationRequest
    {
        public string Operation { get; set; }
        public List<int> PostIDs { get; set; }
    }

    public class RejectPostRequest
    {
        public string RejectionReason { get; set; }
    }

    #endregion
}