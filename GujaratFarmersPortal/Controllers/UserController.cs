using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GujaratFarmersPortal.Services;
using GujaratFarmersPortal.Models;
using System.Security.Claims;

namespace GujaratFarmersPortal.Controllers
{
    //[Authorize(Roles = "User")]
    [Route("User")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;

        public UserController(IUserService userService, ILogger<UserController> logger, IConfiguration configuration)
        {
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("Test")]
        [AllowAnonymous]  // This bypasses auth for testing
        public IActionResult Test()
        {
            return Json(new
            {
                Message = "UserController is working!",
                DateTime = DateTime.Now
            });
        }

        #region Helper Methods

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

        #endregion

        #region Dashboard & Feed

        [HttpGet]
        [HttpGet("Dashboard")]
        public async Task<IActionResult> Index()
        {
            try
            {
                SetBreadcrumb("Dashboard");

                var dashboardData = await _userService.GetUserDashboardAsync(GetCurrentUserID());

                ViewBag.UserFullName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";
                ViewBag.UserProfileImage = User.FindFirst("ProfileImage")?.Value;

                return View("Dashboard", dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dashboard");
                TempData["ErrorMessage"] = "ડેશબોર્ડ લોડ કરવામાં ભૂલ આવી છે";
                return View("Dashboard", new UserDashboardViewModel());
            }
        }

        [HttpGet("Feed")]
        public async Task<IActionResult> Feed(int? categoryId = null, string location = null, string sortBy = "recent", int page = 1)
        {
            try
            {
                SetBreadcrumb("Feed", new List<dynamic>
                {
                    new { Title = "પોસ્ટ ફીડ", Url = "/User/Feed", IsActive = true }
                });

                var feedData = await _userService.GetUserFeedAsync(GetCurrentUserID(), categoryId, location, sortBy, page, 10);
                var categories = await _userService.GetCategoriesAsync();

                ViewBag.Categories = categories;
                ViewBag.SelectedCategory = categoryId;
                ViewBag.Location = location;
                ViewBag.SortBy = sortBy;
                ViewBag.CurrentPage = page;

                return View(feedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user feed");
                TempData["ErrorMessage"] = "ફીડ લોડ કરવામાં ભૂલ આવી છે";
                return View(new UserFeedViewModel());
            }
        }

        [HttpGet("Categories")]
        public async Task<IActionResult> Categories()
        {
            try
            {
                SetBreadcrumb("Categories", new List<dynamic>
                {
                    new { Title = "કેટેગરીઓ", Url = "/User/Categories", IsActive = true }
                });

                var categories = await _userService.GetCategoriesWithCountAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["ErrorMessage"] = "કેટેગરીઓ લોડ કરવામાં ભૂલ આવી છે";
                return View(new List<CategoryWithCount>());
            }
        }

        [HttpGet("Category/{categoryId}")]
        public async Task<IActionResult> CategoryPosts(int categoryId, string sortBy = "recent", int page = 1)
        {
            try
            {
                var category = await _userService.GetCategoryByIdAsync(categoryId);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "કેટેગરી મળી નથી";
                    return RedirectToAction("Categories");
                }

                SetBreadcrumb("Categories", new List<dynamic>
                {
                    new { Title = "કેટેગરીઓ", Url = "/User/Categories", IsActive = false },
                    new { Title = category.CategoryNameGuj, Url = "", IsActive = true }
                });

                var posts = await _userService.GetPostsByCategoryAsync(categoryId, GetCurrentUserID(), sortBy, page, 12);

                ViewBag.Category = category;
                ViewBag.SortBy = sortBy;
                ViewBag.CurrentPage = page;

                return View("CategoryPosts", posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category posts for category: {CategoryId}", categoryId);
                TempData["ErrorMessage"] = "કેટેગરીની પોસ્ટ્સ લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Categories");
            }
        }

        #endregion

        #region Post Management

        [HttpGet("Posts/Create")]
        public async Task<IActionResult> CreatePost()
        {
            try
            {
                SetBreadcrumb("CreatePost", new List<dynamic>
                {
                    new { Title = "નવી જાહેરાત બનાવો", Url = "/User/Posts/Create", IsActive = true }
                });

                var model = new PostCreateViewModel();
                await PopulateDropdownsForPost(model);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create post page");
                TempData["ErrorMessage"] = "પેજ લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost("Posts/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(PostCreateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateDropdownsForPost(model);
                    return View(model);
                }

                var result = await _userService.CreatePostAsync(model, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "તમારી જાહેરાત સફળતાપૂર્વક બનાવવામાં આવી છે";
                    return RedirectToAction("MyPosts");
                }
                else
                {
                    ModelState.AddModelError("", result.Message);
                    await PopulateDropdownsForPost(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                TempData["ErrorMessage"] = "જાહેરાત બનાવવામાં ભૂલ આવી છે";
                await PopulateDropdownsForPost(model);
                return View(model);
            }
        }

        [HttpGet("Posts/My")]
        public async Task<IActionResult> MyPosts(string filter = "all", int page = 1)
        {
            try
            {
                SetBreadcrumb("MyPosts", new List<dynamic>
                {
                    new { Title = "મારી જાહેરાતો", Url = "/User/Posts/My", IsActive = true }
                });

                var posts = await _userService.GetUserPostsAsync(GetCurrentUserID(), filter, page, 12);

                ViewBag.Filter = filter;
                ViewBag.CurrentPage = page;

                return View(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user posts");
                TempData["ErrorMessage"] = "તમારી જાહેરાતો લોડ કરવામાં ભૂલ આવી છે";
                return View(new PagedResult<UserPost>());
            }
        }

        [HttpGet("Posts/{id}")]
        public async Task<IActionResult> PostDetails(int id)
        {
            try
            {
                var postDetails = await _userService.GetPostDetailsAsync(id, GetCurrentUserID());

                if (postDetails == null)
                {
                    TempData["ErrorMessage"] = "જાહેરાત મળી નથી";
                    return RedirectToAction("Feed");
                }

                // Increment view count
                await _userService.IncrementPostViewAsync(id, GetCurrentUserID());

                SetBreadcrumb("PostDetails", new List<dynamic>
                {
                    new { Title = "જાહેરાત", Url = "/User/Feed", IsActive = false },
                    new { Title = postDetails.Post.Title, Url = "", IsActive = true }
                });

                return View(postDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading post details for ID: {PostId}", id);
                TempData["ErrorMessage"] = "જાહેરાતની વિગતો લોડ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("Feed");
            }
        }

        [HttpPost("Posts/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, PostCreateViewModel model)
        {
            try
            {
                model.PostID = id;

                if (!ModelState.IsValid)
                {
                    await PopulateDropdownsForPost(model);
                    return View("EditPost", model);
                }

                var result = await _userService.UpdatePostAsync(model, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "જાહેરાત સફળતાપૂર્વક અપડેટ કરવામાં આવી છે";
                    return RedirectToAction("PostDetails", new { id = id });
                }
                else
                {
                    ModelState.AddModelError("", result.Message);
                    await PopulateDropdownsForPost(model);
                    return View("EditPost", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post: {PostId}", id);
                TempData["ErrorMessage"] = "જાહેરાત અપડેટ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("PostDetails", new { id = id });
            }
        }

        [HttpPost("Posts/Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            try
            {
                var result = await _userService.DeletePostAsync(id, GetCurrentUserID());

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "જાહેરાત સફળતાપૂર્વક ડિલીટ કરવામાં આવી છે";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                }

                return RedirectToAction("MyPosts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post: {PostId}", id);
                TempData["ErrorMessage"] = "જાહેરાત ડિલીટ કરવામાં ભૂલ આવી છે";
                return RedirectToAction("MyPosts");
            }
        }

        #endregion

        #region Post Interactions

        [HttpPost("Posts/{id}/Like")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> LikePost(int id)
        {
            try
            {
                var result = await _userService.TogglePostLikeAsync(id, GetCurrentUserID());
                return Json(new { success = result.Success, isLiked = result.Data, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post: {PostId}", id);
                return Json(new { success = false, message = "લાઈક કરવામાં ભૂલ આવી છે" });
            }
        }

        [HttpPost("Posts/{id}/Favorite")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> FavoritePost(int id)
        {
            try
            {
                var result = await _userService.TogglePostFavoriteAsync(id, GetCurrentUserID());
                return Json(new { success = result.Success, isFavorite = result.Data, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for post: {PostId}", id);
                return Json(new { success = false, message = "પસંદીદામાં ઉમેરવામાં ભૂલ આવી છે" });
            }
        }

        [HttpPost("Posts/{id}/Comment")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddComment(int id, string comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comment))
                {
                    return Json(new { success = false, message = "કમેન્ટ ખાલી હોઈ શકે નહીં" });
                }

                var result = await _userService.AddCommentAsync(id, GetCurrentUserID(), comment);
                return Json(new { success = result.Success, comment = result.Data, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to post: {PostId}", id);
                return Json(new { success = false, message = "કમેન્ટ ઉમેરવામાં ભૂળ આવી છે" });
            }
        }

        [HttpGet("Posts/{id}/Comments")]
        public async Task<JsonResult> GetComments(int id, int page = 1)
        {
            try
            {
                var comments = await _userService.GetPostCommentsAsync(id, page, 10);
                return Json(new { success = true, comments = comments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading comments for post: {PostId}", id);
                return Json(new { success = false, message = "કમેન્ટ્સ લોડ કરવામાં ભૂળ આવી છે" });
            }
        }

        #endregion

        #region User Profile & Favorites

        [HttpGet("Profile")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                SetBreadcrumb("Profile", new List<dynamic>
                {
                    new { Title = "મારી પ્રોફાઇલ", Url = "/User/Profile", IsActive = true }
                });

                var profileData = await _userService.GetUserProfileAsync(GetCurrentUserID());
                return View(profileData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["ErrorMessage"] = "પ્રોફાઇલ લોડ કરવામાં ભૂળ આવી છે";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet("Favorites")]
        public async Task<IActionResult> Favorites(int page = 1)
        {
            try
            {
                SetBreadcrumb("Favorites", new List<dynamic>
                {
                    new { Title = "પસંદીદા જાહેરાતો", Url = "/User/Favorites", IsActive = true }
                });

                var favorites = await _userService.GetUserFavoritesAsync(GetCurrentUserID(), page, 12);
                ViewBag.CurrentPage = page;

                return View(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user favorites");
                TempData["ErrorMessage"] = "પસંદીદા જાહેરાતો લોડ કરવામાં ભૂળ આવી છે";
                return View(new PagedResult<UserPost>());
            }
        }

        #endregion

        #region AJAX Helpers

        [HttpGet("GetSubCategories")]
        public async Task<JsonResult> GetSubCategories(int categoryId)
        {
            try
            {
                var subCategories = await _userService.GetSubCategoriesAsync(categoryId);
                return Json(subCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subcategories for category: {CategoryId}", categoryId);
                return Json(new List<SubCategory>());
            }
        }

        [HttpGet("GetDistricts")]
        public async Task<JsonResult> GetDistricts(int stateId)
        {
            try
            {
                var districts = await _userService.GetDistrictsAsync(stateId);
                return Json(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading districts for state: {StateId}", stateId);
                return Json(new List<District>());
            }
        }

        [HttpGet("GetTalukas")]
        public async Task<JsonResult> GetTalukas(int districtId)
        {
            try
            {
                var talukas = await _userService.GetTalukasAsync(districtId);
                return Json(talukas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading talukas for district: {DistrictId}", districtId);
                return Json(new List<Taluka>());
            }
        }

        [HttpGet("GetVillages")]
        public async Task<JsonResult> GetVillages(int talukaId)
        {
            try
            {
                var villages = await _userService.GetVillagesAsync(talukaId);
                return Json(villages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading villages for taluka: {TalukaId}", talukaId);
                return Json(new List<Village>());
            }
        }

        #endregion

        #region Search

        [HttpGet("Search")]
        public async Task<IActionResult> Search(string q, int? categoryId = null, string location = null,
            decimal? minPrice = null, decimal? maxPrice = null, string sortBy = "recent", int page = 1)
        {
            try
            {
                SetBreadcrumb("Search", new List<dynamic>
                {
                    new { Title = "શોધ પરિણામો", Url = "/User/Search", IsActive = true }
                });

                var searchResults = await _userService.SearchPostsAsync(q, categoryId, location,
                    minPrice, maxPrice, sortBy, GetCurrentUserID(), page, 12);

                var categories = await _userService.GetCategoriesAsync();

                ViewBag.Categories = categories;
                ViewBag.SearchQuery = q;
                ViewBag.SelectedCategory = categoryId;
                ViewBag.Location = location;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.SortBy = sortBy;
                ViewBag.CurrentPage = page;

                return View(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search");
                TempData["ErrorMessage"] = "શોધવામાં ભૂળ આવી છે";
                return View(new PagedResult<UserPost>());
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task PopulateDropdownsForPost(PostCreateViewModel model)
        {
            model.Categories = await _userService.GetCategoriesAsync();
            model.States = await _userService.GetStatesAsync();

            if (model.CategoryID.HasValue)
            {
                model.SubCategories = await _userService.GetSubCategoriesAsync(model.CategoryID.Value);
            }

            if (model.StateID.HasValue)
            {
                model.Districts = await _userService.GetDistrictsAsync(model.StateID.Value);
            }

            if (model.DistrictID.HasValue)
            {
                model.Talukas = await _userService.GetTalukasAsync(model.DistrictID.Value);
            }

            if (model.TalukaID.HasValue)
            {
                model.Villages = await _userService.GetVillagesAsync(model.TalukaID.Value);
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