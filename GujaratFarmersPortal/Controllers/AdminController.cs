using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GujaratFarmersPortal.Services;
using GujaratFarmersPortal.Models;
using System.Security.Claims;

namespace GujaratFarmersPortal.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        private int GetCurrentUserID()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private void SetBreadcrumb(string pageName, List<dynamic> breadcrumbItems = null)
        {
            ViewBag.ActivePage = pageName;
            ViewBag.BreadcrumbItems = breadcrumbItems;
        }

        #region Dashboard

        [HttpGet]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Index()
        {
            try
            {
                SetBreadcrumb("Dashboard");
                var dashboardData = await _adminService.GetDashboardDataAsync();

                ViewBag.UserFullName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
                ViewBag.UserProfileImage = User.FindFirst("ProfileImage")?.Value;

                return View("Dashboard", dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "ડેશબોર્ડ લોડ કરવામાં ભૂલ આવી છે";
                return View("Dashboard", new AdminDashboardViewModel());
            }
        }

        #endregion

        #region User Management

        [HttpGet("Users")]
        public async Task<IActionResult> Users(string search = null, int page = 1, int size = 20)
        {
            try
            {
                SetBreadcrumb("Users", new List<dynamic>
                {
                    new { Title = "વપરાશકર્તા મેનેજમેન્ટ", Url = "/Admin/Users", IsActive = true }
                });

                var users = await _adminService.GetUsersAsync(search, page, size);

                ViewBag.SearchQuery = search;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = size;

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users page");
                TempData["ErrorMessage"] = "વપરાશકર્તાઓની માહિતી લોડ કરવામાં ભૂલ આવી છે";
                return View(new PagedResult<User>());
            }
        }

        [HttpGet("Users/{id}")]
        public async Task<IActionResult> UserDetails(int id)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "વપરાશકર્તા મળ્યો નથી";
                    return RedirectToAction("Users");
                }

                SetBreadcrumb("Users", new List<dynamic>
                {
                    new { Title = "વપરાશકર્તાઓ", Url = "/Admin/Users", IsActive = false },
                    new { Title = user.FullName, Url = "", IsActive = true }
                });

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "વપરાશકર્તાની વિગતો લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Users");
            }
        }

        [HttpPost("Users/Activate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var result = await _adminService.ActivateUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "વપરાશકર્તા સફળતાપૂર્વક સક્રિય કરવામાં આવ્યો";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user: {UserId}", id);
                TempData["ErrorMessage"] = "વપરાશકર્તા સક્રિય કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Users");
        }

        [HttpPost("Users/Deactivate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var result = await _adminService.DeactivateUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "વપરાશકર્તા સફળતાપૂર્વક નિષ્ક્રિય કરવામાં આવ્યો";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user: {UserId}", id);
                TempData["ErrorMessage"] = "વપરાશકર્તા નિષ્ક્રિય કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Users");
        }

        [HttpPost("Users/Ban/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(int id)
        {
            try
            {
                var result = await _adminService.BanUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "વપરાશકર્તા સફળતાપૂર્વક બેન કરવામાં આવ્યો";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning user: {UserId}", id);
                TempData["ErrorMessage"] = "વપરાશકર્તા બેન કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Users");
        }

        [HttpPost("Users/Unban/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(int id)
        {
            try
            {
                var result = await _adminService.UnbanUserAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "વપરાશકર્તા સફળતાપૂર્વક અનબેન કરવામાં આવ્યો";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning user: {UserId}", id);
                TempData["ErrorMessage"] = "વપરાશકર્તા અનબેન કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Users");
        }

        #endregion

        #region Post Management

        [HttpGet("Posts")]
        public async Task<IActionResult> Posts(string filter = "all", string search = null, int? categoryId = null, int? stateId = null, int? districtId = null, string status = null, int page = 1, int size = 20)
        {
            try
            {
                SetBreadcrumb("Posts", new List<dynamic>
                {
                    new { Title = "પોસ્ટ મેનેજમેન્ટ", Url = "/Admin/Posts", IsActive = true }
                });

                var posts = await _adminService.GetPostsAsync(filter, search, categoryId, stateId, districtId, status, page, size);
                var categories = await _adminService.GetCategoriesAsync();

                ViewBag.Filter = filter;
                ViewBag.SearchQuery = search;
                ViewBag.CategoryId = categoryId;
                ViewBag.StateId = stateId;
                ViewBag.DistrictId = districtId;
                ViewBag.Status = status;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = size;
                ViewBag.Categories = categories;

                return View(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading posts page");
                TempData["ErrorMessage"] = "પોસ્ટ્સની માહિતી લોડ કરવામાં ભૂલ આવી છે";
                return View(new PagedResult<Post>());
            }
        }

        [HttpGet("Posts/Pending")]
        public async Task<IActionResult> PendingPosts(int page = 1, int size = 20)
        {
            try
            {
                SetBreadcrumb("Posts", new List<dynamic>
                {
                    new { Title = "પોસ્ટ્સ", Url = "/Admin/Posts", IsActive = false },
                    new { Title = "મંજૂરી બાકી", Url = "", IsActive = true }
                });

                var posts = await _adminService.GetPostsAsync("pending", null, null, null, null, null, page, size);

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = size;

                return View("PendingPosts", posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending posts");
                TempData["ErrorMessage"] = "મંજૂરી બાકી પોસ્ટ્સ લોડ કરવામાં ભૂલ આવી છે";
                return View("PendingPosts", new PagedResult<Post>());
            }
        }

        [HttpGet("Posts/{id}")]
        public async Task<IActionResult> PostDetails(int id)
        {
            try
            {
                var post = await _adminService.GetPostByIdAsync(id);
                if (post == null)
                {
                    TempData["ErrorMessage"] = "પોસ્ટ મળી નથી";
                    return RedirectToAction("Posts");
                }

                SetBreadcrumb("Posts", new List<dynamic>
                {
                    new { Title = "પોસ્ટ્સ", Url = "/Admin/Posts", IsActive = false },
                    new { Title = post.Title, Url = "", IsActive = true }
                });

                return View(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading post details for ID: {PostId}", id);
                TempData["ErrorMessage"] = "પોસ્ટની વિગતો લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Posts");
            }
        }

        [HttpPost("Posts/Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePost(int id)
        {
            try
            {
                var result = await _adminService.ApprovePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "પોસ્ટ સફળતાપૂર્વક મંજૂર કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving post: {PostId}", id);
                TempData["ErrorMessage"] = "પોસ્ટ મંજૂર કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Posts");
        }

        [HttpPost("Posts/Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPost(int id, string rejectionReason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["ErrorMessage"] = "રિજેક્ટ કરવાનું કારણ આવશ્યક છે";
                    return RedirectToAction("Posts");
                }

                var result = await _adminService.RejectPostAsync(id, GetCurrentUserID(), rejectionReason);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "પોસ્ટ સફળતાપૂર્વક રિજેક્ટ કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting post: {PostId}", id);
                TempData["ErrorMessage"] = "પોસ્ટ રિજેક્ટ કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Posts");
        }

        [HttpPost("Posts/Feature/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FeaturePost(int id)
        {
            try
            {
                var result = await _adminService.FeaturePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "પોસ્ટ સફળતાપૂર્વક ફીચર કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error featuring post: {PostId}", id);
                TempData["ErrorMessage"] = "પોસ્ટ ફીચર કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Posts");
        }

        [HttpPost("Posts/Unfeature/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnfeaturePost(int id)
        {
            try
            {
                var result = await _adminService.UnfeaturePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "પોસ્ટ સફળતાપૂર્વક અનફીચર કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfeaturing post: {PostId}", id);
                TempData["ErrorMessage"] = "પોસ્ટ અનફીચર કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Posts");
        }

        [HttpPost("Posts/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var result = await _adminService.DeletePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "પોસ્ટ સફળતાપૂર્વક ડિલીટ કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post: {PostId}", id);
                TempData["ErrorMessage"] = "પોસ્ટ ડિલીટ કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Posts");
        }

        #endregion

        #region Category Management

        [HttpGet("Categories")]
        public async Task<IActionResult> Categories()
        {
            try
            {
                SetBreadcrumb("Categories", new List<dynamic>
                {
                    new { Title = "કેટેગરી મેનેજમેન્ટ", Url = "/Admin/Categories", IsActive = true }
                });

                var categories = await _adminService.GetCategoriesAsync();

                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                TempData["ErrorMessage"] = "કેટેગરીની માહિતી લોડ કરવામાં ભૂલ આવી છે";
                return View(new List<Category>());
            }
        }

        [HttpGet("Categories/Create")]
        public async Task<IActionResult> CreateCategory()
        {
            try
            {
                SetBreadcrumb("Categories", new List<dynamic>
                {
                    new { Title = "કેટેગરીઓ", Url = "/Admin/Categories", IsActive = false },
                    new { Title = "નવી કેટેગરી", Url = "", IsActive = true }
                });

                var categories = await _adminService.GetCategoriesAsync();
                ViewBag.ParentCategories = categories;

                return View(new Category());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create category page");
                return RedirectToAction("Categories");
            }
        }

        [HttpPost("Categories/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(Category category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var categories = await _adminService.GetCategoriesAsync();
                    ViewBag.ParentCategories = categories;
                    return View(category);
                }

                var result = await _adminService.CreateCategoryAsync(category, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "કેટેગરી સફળતાપૂર્વક બનાવવામાં આવી";
                    return RedirectToAction("Categories");
                }
                else
                {
                    ModelState.AddModelError("", result.Message);
                    var categories = await _adminService.GetCategoriesAsync();
                    ViewBag.ParentCategories = categories;
                    return View(category);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["ErrorMessage"] = "કેટેગરી બનાવવામાં ભૂલ આવી છે";
                return RedirectToAction("Categories");
            }
        }

        [HttpGet("Categories/Edit/{id}")]
        public async Task<IActionResult> EditCategory(int id)
        {
            try
            {
                var category = await _adminService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "કેટેગરી મળી નથી";
                    return RedirectToAction("Categories");
                }

                SetBreadcrumb("Categories", new List<dynamic>
                {
                    new { Title = "કેટેગરીઓ", Url = "/Admin/Categories", IsActive = false },
                    new { Title = "સંપાદન", Url = "", IsActive = true }
                });

                var categories = await _adminService.GetCategoriesAsync();
                ViewBag.ParentCategories = categories.Where(c => c.CategoryID != id).ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit category page for ID: {CategoryId}", id);
                return RedirectToAction("Categories");
            }
        }

        [HttpPost("Categories/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category category)
        {
            try
            {
                category.CategoryID = id;

                if (!ModelState.IsValid)
                {
                    var categories = await _adminService.GetCategoriesAsync();
                    ViewBag.ParentCategories = categories.Where(c => c.CategoryID != id).ToList();
                    return View(category);
                }

                var result = await _adminService.UpdateCategoryAsync(category, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "કેટેગરી સફળતાપૂર્વક અપડેટ કરવામાં આવી";
                    return RedirectToAction("Categories");
                }
                else
                {
                    ModelState.AddModelError("", result.Message);
                    var categories = await _adminService.GetCategoriesAsync();
                    ViewBag.ParentCategories = categories.Where(c => c.CategoryID != id).ToList();
                    return View(category);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {CategoryId}", id);
                TempData["ErrorMessage"] = "કેટેગરી અપડેટ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Categories");
            }
        }

        [HttpPost("Categories/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var result = await _adminService.DeleteCategoryAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "કેટેગરી સફળતાપૂર્વક ડિલીટ કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                TempData["ErrorMessage"] = "કેટેગરી ડિલીટ કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Categories");
        }

        #endregion

        #region Reports Management

        [HttpGet("Reports")]
        public async Task<IActionResult> Reports(int page = 1, int size = 10)
        {
            try
            {
                SetBreadcrumb("Reports", new List<dynamic>
                {
                    new { Title = "રિપોર્ટ મેનેજમેન્ટ", Url = "/Admin/Reports", IsActive = true }
                });

                var reports = await _adminService.GetPendingReportsAsync(page, size);

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = size;

                return View(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports page");
                TempData["ErrorMessage"] = "રિપોર્ટ્સની માહિતી લોડ કરવામાં ભૂલ આવી છે";
                return View(new List<object>());
            }
        }

        [HttpPost("Reports/Review/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewReport(int id, string action, string reviewNotes = null)
        {
            try
            {
                var result = await _adminService.ReviewReportAsync(id, GetCurrentUserID(), action, reviewNotes);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"રિપોર્ટ સફળતાપૂર્વક {(action == "resolve" ? "રિઝોલ્વ" : "રિવ્યુ")} કરવામાં આવી";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing report: {ReportId}", id);
                TempData["ErrorMessage"] = "રિપોર્ટ રિવ્યુ કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Reports");
        }

        #endregion

        #region Analytics

        [HttpGet("Analytics")]
        public async Task<IActionResult> Analytics()
        {
            try
            {
                SetBreadcrumb("Analytics", new List<dynamic>
                {
                    new { Title = "એનાલિટિક્સ", Url = "/Admin/Analytics", IsActive = true }
                });

                var analyticsData = await _adminService.GetAnalyticsDataAsync();

                return View(analyticsData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics page");
                TempData["ErrorMessage"] = "એનાલિટિક્સ ડેટા લોડ કરવામાં ભૂલ આવી છે";
                return View(new object());
            }
        }

        #endregion

        #region System Settings

        [HttpGet("Settings")]
        public async Task<IActionResult> Settings()
        {
            try
            {
                SetBreadcrumb("Settings", new List<dynamic>
                {
                    new { Title = "સિસ્ટમ સેટિંગ્સ", Url = "/Admin/Settings", IsActive = true }
                });

                var systemInfo = await _adminService.GetSystemInfoAsync();

                return View(systemInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings page");
                TempData["ErrorMessage"] = "સેટિંગ્સ લોડ કરવામાં ભૂલ આવી છે";
                return View(new object());
            }
        }

        [HttpPost("Settings/ClearCache")]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCache()
        {
            try
            {
                _adminService.ClearCache();
                TempData["SuccessMessage"] = "કેશ સફળતાપૂર્વક ક્લિયર કરવામાં આવ્યો";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                TempData["ErrorMessage"] = "કેશ ક્લિયર કરવામાં ભૂલ આવી છે";
            }

            return RedirectToAction("Settings");
        }

        #endregion

        #region System Logs

        [HttpGet("Logs")]
        public async Task<IActionResult> Logs(int page = 1, int size = 50)
        {
            try
            {
                SetBreadcrumb("Logs", new List<dynamic>
                {
                    new { Title = "સિસ્ટમ લોગ્સ", Url = "/Admin/Logs", IsActive = true }
                });

                var logs = await _adminService.GetSystemLogsAsync(page, size);

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = size;

                return View(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading logs page");
                TempData["ErrorMessage"] = "લોગ્સ લોડ કરવામાં ભૂલ આવી છે";
                return View(new List<object>());
            }
        }

        #endregion

        #region Profile Management

        [HttpGet("Profile")]
        public IActionResult Profile()
        {
            try
            {
                SetBreadcrumb("Profile", new List<dynamic>
                {
                    new { Title = "મારી પ્રોફાઇલ", Url = "/Admin/Profile", IsActive = true }
                });

                var user = new User
                {
                    UserID = GetCurrentUserID(),
                    FirstName = User.FindFirst("FirstName")?.Value,
                    LastName = User.FindFirst("LastName")?.Value,
                    UserName = User.FindFirst("UserName")?.Value ?? User.Identity.Name,
                    Email = User.FindFirst("Email")?.Value,
                    MobileNumber = User.FindFirst("MobileNumber")?.Value,
                    ProfileImage = User.FindFirst("ProfileImage")?.Value
                };

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin profile");
                TempData["ErrorMessage"] = "પ્રોફાઇલ લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Error Handling

        [HttpGet("Error")]
        public IActionResult Error()
        {
            return View();
        }

        #endregion
    }
}