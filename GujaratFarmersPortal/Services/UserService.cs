using GujaratFarmersPortal.Data;
using GujaratFarmersPortal.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GujaratFarmersPortal.Services
{
    public class UserService : IUserService
    {
        private readonly IUserDataAccess _userDataAccess;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserService> _logger;
        private readonly IConfiguration _configuration;

        public UserService(IUserDataAccess userDataAccess, IMemoryCache cache,
            ILogger<UserService> logger, IConfiguration configuration)
        {
            _userDataAccess = userDataAccess;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        #region Dashboard & Feed

        public async Task<UserDashboardViewModel> GetUserDashboardAsync(int userID)
        {
            try
            {
                var dashboardData = await _userDataAccess.GetUserDashboardDataAsync(userID);
                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user dashboard data for user: {UserID}", userID);
                return new UserDashboardViewModel();
            }
        }

        public async Task<UserFeedViewModel> GetUserFeedAsync(int userID, int? categoryID = null,
            string location = null, string sortBy = "recent", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var feedData = new UserFeedViewModel();

                // Get posts for feed
                feedData.Posts = await _userDataAccess.GetFeedPostsAsync(userID, categoryID,
                    location, sortBy, pageNumber, pageSize);

                // Get featured posts (cache for 30 minutes)
                var cacheKey = "featured_posts";
                if (!_cache.TryGetValue(cacheKey, out List<UserPost> featuredPosts))
                {
                    featuredPosts = await _userDataAccess.GetFeaturedPostsAsync(userID, 5);
                    _cache.Set(cacheKey, featuredPosts, TimeSpan.FromMinutes(30));
                }
                feedData.FeaturedPosts = featuredPosts;

                // Get urgent posts
                feedData.UrgentPosts = await _userDataAccess.GetUrgentPostsAsync(userID, 5);

                // Set filter values
                feedData.SelectedCategoryID = categoryID;
                feedData.SelectedLocation = location;
                feedData.SortBy = sortBy;

                return feedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user feed for user: {UserID}", userID);
                return new UserFeedViewModel();
            }
        }

        public async Task<List<CategoryWithCount>> GetCategoriesWithCountAsync()
        {
            try
            {
                var cacheKey = "categories_with_count";
                if (!_cache.TryGetValue(cacheKey, out List<CategoryWithCount> categories))
                {
                    categories = await _userDataAccess.GetCategoriesWithCountAsync();
                    _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(15));
                }
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories with count");
                return new List<CategoryWithCount>();
            }
        }

        public async Task<Category> GetCategoryByIdAsync(int categoryID)
        {
            try
            {
                return await _userDataAccess.GetCategoryByIdAsync(categoryID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID: {CategoryID}", categoryID);
                return null;
            }
        }

        public async Task<PagedResult<UserPost>> GetPostsByCategoryAsync(int categoryID, int userID,
            string sortBy = "recent", int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                return await _userDataAccess.GetPostsByCategoryAsync(categoryID, userID,
                    sortBy, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts by category: {CategoryID}", categoryID);
                return new PagedResult<UserPost>();
            }
        }

        #endregion

        #region Post Management

        public async Task<ApiResponse<string>> CreatePostAsync(PostCreateViewModel model, int userID)
        {
            try
            {
                // Validate business rules
                var validationResult = ValidatePostModel(model);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Handle image uploads
                var imageUrls = new List<string>();
                if (model.Images != null && model.Images.Any())
                {
                    foreach (var image in model.Images)
                    {
                        var imageUrl = await SavePostImageAsync(image, userID);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            imageUrls.Add(imageUrl);
                        }
                    }
                }

                // Create post in database
                var result = await _userDataAccess.CreatePostAsync(model, userID, imageUrls);

                if (result.Success)
                {
                    // Clear relevant caches
                    ClearPostRelatedCaches();

                    // Log activity
                    await LogUserActivityAsync(userID, "POST_CREATED", int.Parse(result.Data));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post for user: {UserID}", userID);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "જાહેરાત બનાવવામાં તકનીકી ભૂલ આવી છે"
                };
            }
        }

        public async Task<PagedResult<UserPost>> GetUserPostsAsync(int userID, string filter = "all",
            int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                return await _userDataAccess.GetUserPostsAsync(userID, filter, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user posts for user: {UserID}", userID);
                return new PagedResult<UserPost>();
            }
        }

        public async Task<PostDetailsViewModel> GetPostDetailsAsync(int postID, int currentUserID)
        {
            try
            {
                return await _userDataAccess.GetPostDetailsAsync(postID, currentUserID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post details for post: {PostID}", postID);
                return null;
            }
        }

        public async Task<ApiResponse<string>> UpdatePostAsync(PostCreateViewModel model, int userID)
        {
            try
            {
                // Validate ownership
                var post = await _userDataAccess.GetPostByIdAsync(model.PostID.Value);
                if (post == null || post.UserID != userID)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "તમને આ જાહેરાત સંપાદિત કરવાની મંજૂરી નથી"
                    };
                }

                // Validate business rules
                var validationResult = ValidatePostModel(model);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // Handle new image uploads
                var newImageUrls = new List<string>();
                if (model.Images != null && model.Images.Any())
                {
                    foreach (var image in model.Images)
                    {
                        var imageUrl = await SavePostImageAsync(image, userID);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            newImageUrls.Add(imageUrl);
                        }
                    }
                }

                var result = await _userDataAccess.UpdatePostAsync(model, userID, newImageUrls);

                if (result.Success)
                {
                    ClearPostRelatedCaches();
                    await LogUserActivityAsync(userID, "POST_UPDATED", model.PostID.Value);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post: {PostID}", model.PostID);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "જાહેરાત અપડેટ કરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<Post> GetPostByIdAsync(int postID)
        {
            try
            {
                var result = await _userDataAccess.GetPostsAsync("GET_ALL", null, null, null, null, null, 1, 1);
                return result.Items.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving post by ID: {PostID}", postID);
                throw;
            }
        }

        public async Task<ApiResponse<string>> DeletePostAsync(int postID, int userID)
        {
            try
            {
                // Validate ownership
                var post = await _userDataAccess.GetPostByIdAsync(postID);
                if (post == null || post.UserID != userID)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "તમને આ જાહેરાત ડિલીટ કરવાની મંજૂરી નથી"
                    };
                }

                var result = await _userDataAccess.DeletePostAsync(postID, userID);

                if (result.Success)
                {
                    ClearPostRelatedCaches();
                    await LogUserActivityAsync(userID, "POST_DELETED", postID);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post: {PostID}", postID);
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "જાહેરાત ડિલીટ કરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<ApiResponse<string>> IncrementPostViewAsync(int postID, int userID)
        {
            try
            {
                return await _userDataAccess.IncrementPostViewAsync(postID, userID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing post view: {PostID}", postID);
                return new ApiResponse<string> { Success = false };
            }
        }

        #endregion

        #region Post Interactions

        public async Task<ApiResponse<bool>> TogglePostLikeAsync(int postID, int userID)
        {
            try
            {
                var result = await _userDataAccess.TogglePostLikeAsync(postID, userID);

                if (result.Success)
                {
                    var activityType = result.Data ? "POST_LIKED" : "POST_UNLIKED";
                    await LogUserActivityAsync(userID, activityType, postID);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling post like: {PostID}", postID);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "લાઈક કરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<ApiResponse<bool>> TogglePostFavoriteAsync(int postID, int userID)
        {
            try
            {
                var result = await _userDataAccess.TogglePostFavoriteAsync(postID, userID);

                if (result.Success)
                {
                    var activityType = result.Data ? "POST_FAVORITED" : "POST_UNFAVORITED";
                    await LogUserActivityAsync(userID, activityType, postID);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling post favorite: {PostID}", postID);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = "પસંદીદામાં ઉમેરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<ApiResponse<Comment>> AddCommentAsync(int postID, int userID, string commentText)
        {
            try
            {
                // Validate comment
                if (string.IsNullOrWhiteSpace(commentText) || commentText.Length > 1000)
                {
                    return new ApiResponse<Comment>
                    {
                        Success = false,
                        Message = "કમેન્ટ 1 થી 1000 અક્ષર સુધીનો હોવો જોઈએ"
                    };
                }

                var result = await _userDataAccess.AddCommentAsync(postID, userID, commentText);

                if (result.Success)
                {
                    await LogUserActivityAsync(userID, "COMMENT_ADDED", postID);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to post: {PostID}", postID);
                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "કમેન્ટ ઉમેરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<PagedResult<Comment>> GetPostCommentsAsync(int postID, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                return await _userDataAccess.GetPostCommentsAsync(postID, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for post: {PostID}", postID);
                return new PagedResult<Comment>();
            }
        }

        #endregion

        #region User Profile & Favorites

        public async Task<UserProfileViewModel> GetUserProfileAsync(int userID)
        {
            try
            {
                return await _userDataAccess.GetUserProfileAsync(userID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile: {UserID}", userID);
                return new UserProfileViewModel();
            }
        }

        public async Task<PagedResult<UserPost>> GetUserFavoritesAsync(int userID, int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                return await _userDataAccess.GetUserFavoritesAsync(userID, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user favorites: {UserID}", userID);
                return new PagedResult<UserPost>();
            }
        }

        #endregion

        #region Search

        public async Task<PagedResult<UserPost>> SearchPostsAsync(string searchQuery, int? categoryID = null,
            string location = null, decimal? minPrice = null, decimal? maxPrice = null,
            string sortBy = "recent", int userID = 0, int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                return await _userDataAccess.SearchPostsAsync(searchQuery, categoryID, location,
                    minPrice, maxPrice, sortBy, userID, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching posts");
                return new PagedResult<UserPost>();
            }
        }

        #endregion

        #region Dropdown Data

        public async Task<List<Category>> GetCategoriesAsync()
        {
            try
            {
                var cacheKey = "categories";
                if (!_cache.TryGetValue(cacheKey, out List<Category> categories))
                {
                    categories = await _userDataAccess.GetCategoriesAsync();
                    _cache.Set(cacheKey, categories, TimeSpan.FromHours(1));
                }
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return new List<Category>();
            }
        }

        public async Task<List<SubCategory>> GetSubCategoriesAsync(int categoryID)
        {
            try
            {
                var cacheKey = $"subcategories_{categoryID}";
                if (!_cache.TryGetValue(cacheKey, out List<SubCategory> subCategories))
                {
                    subCategories = await _userDataAccess.GetSubCategoriesAsync(categoryID);
                    _cache.Set(cacheKey, subCategories, TimeSpan.FromHours(1));
                }
                return subCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subcategories for category: {CategoryID}", categoryID);
                return new List<SubCategory>();
            }
        }

        public async Task<List<State>> GetStatesAsync()
        {
            try
            {
                var cacheKey = "states";
                if (!_cache.TryGetValue(cacheKey, out List<State> states))
                {
                    states = await _userDataAccess.GetStatesAsync();
                    _cache.Set(cacheKey, states, TimeSpan.FromHours(24));
                }
                return states;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting states");
                return new List<State>();
            }
        }

        public async Task<List<District>> GetDistrictsAsync(int stateID)
        {
            try
            {
                var cacheKey = $"districts_{stateID}";
                if (!_cache.TryGetValue(cacheKey, out List<District> districts))
                {
                    districts = await _userDataAccess.GetDistrictsAsync(stateID);
                    _cache.Set(cacheKey, districts, TimeSpan.FromHours(24));
                }
                return districts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting districts for state: {StateID}", stateID);
                return new List<District>();
            }
        }

        public async Task<List<Taluka>> GetTalukasAsync(int districtID)
        {
            try
            {
                var cacheKey = $"talukas_{districtID}";
                if (!_cache.TryGetValue(cacheKey, out List<Taluka> talukas))
                {
                    talukas = await _userDataAccess.GetTalukasAsync(districtID);
                    _cache.Set(cacheKey, talukas, TimeSpan.FromHours(24));
                }
                return talukas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting talukas for district: {DistrictID}", districtID);
                return new List<Taluka>();
            }
        }

        public async Task<List<Village>> GetVillagesAsync(int talukaID)
        {
            try
            {
                var cacheKey = $"villages_{talukaID}";
                if (!_cache.TryGetValue(cacheKey, out List<Village> villages))
                {
                    villages = await _userDataAccess.GetVillagesAsync(talukaID);
                    _cache.Set(cacheKey, villages, TimeSpan.FromHours(24));
                }
                return villages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting villages for taluka: {TalukaID}", talukaID);
                return new List<Village>();
            }
        }

        #endregion

        #region Analytics

        public async Task<ApiResponse<string>> LogUserActivityAsync(int userID, string activity, int? referenceID = null)
        {
            try
            {
                return await _userDataAccess.LogUserActivityAsync(userID, activity, referenceID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user activity");
                return new ApiResponse<string> { Success = false };
            }
        }

        #endregion

        #region Private Helper Methods

        private ApiResponse<string> ValidatePostModel(PostCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title) || model.Title.Length < 5)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "શીર્ષક ઓછામાં ઓછું 5 અક્ષરનું હોવું જોઈએ"
                };
            }

            if (string.IsNullOrWhiteSpace(model.Description) || model.Description.Length < 10)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "વર્ણન ઓછામાં ઓછું 10 અક્ષરનું હોવું જોઈએ"
                };
            }

            if (!model.CategoryID.HasValue)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "કેટેગરી પસંદ કરવી આવશ્યક છે"
                };
            }

            if (model.Price.HasValue && model.Price < 0)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "કિંમત 0 કરતાં ઓછી હોઈ શકે નહીં"
                };
            }

            return new ApiResponse<string> { Success = true };
        }

        private async Task<string> SavePostImageAsync(IFormFile image, int userID)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return null;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return null;

                var maxSizeBytes = _configuration.GetValue<int>("AppSettings:MaxImageSizeMB", 10) * 1024 * 1024;
                if (image.Length > maxSizeBytes)
                    return null;

                var fileName = $"{Guid.NewGuid()}{extension}";
                var relativePath = $"uploads/posts/{DateTime.Now:yyyy/MM}/{userID}";
                var fullPath = Path.Combine("wwwroot", relativePath);

                Directory.CreateDirectory(fullPath);

                var filePath = Path.Combine(fullPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                return $"/{relativePath}/{fileName}".Replace("\\", "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving post image");
                return null;
            }
        }

        private void ClearPostRelatedCaches()
        {
            var keysToRemove = new[]
            {
                "featured_posts",
                "categories_with_count"
            };

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }

        #endregion
    }
}