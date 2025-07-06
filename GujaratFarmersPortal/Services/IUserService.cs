using GujaratFarmersPortal.Models;

namespace GujaratFarmersPortal.Services
{
    public interface IUserService
    {
        // Dashboard & Feed
        Task<UserDashboardViewModel> GetUserDashboardAsync(int userID);
        Task<UserFeedViewModel> GetUserFeedAsync(int userID, int? categoryID = null, string location = null, string sortBy = "recent", int pageNumber = 1, int pageSize = 10);
        Task<List<CategoryWithCount>> GetCategoriesWithCountAsync();
        Task<Category> GetCategoryByIdAsync(int categoryID);
        Task<PagedResult<UserPost>> GetPostsByCategoryAsync(int categoryID, int userID, string sortBy = "recent", int pageNumber = 1, int pageSize = 12);

        // Post Management
        Task<ApiResponse<string>> CreatePostAsync(PostCreateViewModel model, int userID);
        Task<PagedResult<UserPost>> GetUserPostsAsync(int userID, string filter = "all", int pageNumber = 1, int pageSize = 12);
        Task<PostDetailsViewModel> GetPostDetailsAsync(int postID, int currentUserID);
        Task<ApiResponse<string>> UpdatePostAsync(PostCreateViewModel model, int userID);
        Task<ApiResponse<string>> DeletePostAsync(int postID, int userID);
        Task<ApiResponse<string>> IncrementPostViewAsync(int postID, int userID);

        // Post Interactions
        Task<ApiResponse<bool>> TogglePostLikeAsync(int postID, int userID);
        Task<ApiResponse<bool>> TogglePostFavoriteAsync(int postID, int userID);
        Task<ApiResponse<Comment>> AddCommentAsync(int postID, int userID, string commentText);
        Task<PagedResult<Comment>> GetPostCommentsAsync(int postID, int pageNumber = 1, int pageSize = 10);

        // User Profile & Favorites
        Task<UserProfileViewModel> GetUserProfileAsync(int userID);
        Task<PagedResult<UserPost>> GetUserFavoritesAsync(int userID, int pageNumber = 1, int pageSize = 12);

        // Search
        Task<PagedResult<UserPost>> SearchPostsAsync(string searchQuery, int? categoryID = null, string location = null,
            decimal? minPrice = null, decimal? maxPrice = null, string sortBy = "recent", int userID = 0, int pageNumber = 1, int pageSize = 12);

        // Dropdown Data
        Task<List<Category>> GetCategoriesAsync();
        Task<List<SubCategory>> GetSubCategoriesAsync(int categoryID);
        Task<List<State>> GetStatesAsync();
        Task<List<District>> GetDistrictsAsync(int stateID);
        Task<List<Taluka>> GetTalukasAsync(int districtID);
        Task<List<Village>> GetVillagesAsync(int talukaID);

        // Analytics
        Task<ApiResponse<string>> LogUserActivityAsync(int userID, string activity, int? referenceID = null);
    }

    // Supporting Models for User Service
    public class UserDashboardViewModel
    {
        public string UserFullName { get; set; }
        public string ProfileImage { get; set; }
        public int TotalPosts { get; set; }
        public int ActivePosts { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalFavorites { get; set; }
        public int UnreadMessages { get; set; }
        public List<UserPost> RecentPosts { get; set; } = new List<UserPost>();
        public List<UserPost> TrendingPosts { get; set; } = new List<UserPost>();
        public List<Category> PopularCategories { get; set; } = new List<Category>();
        public int ProfileCompletionPercentage { get; set; }
    }

    //public class UserFeedViewModel
    //{
    //    public PagedResult<UserPost> Posts { get; set; } = new PagedResult<UserPost>();
    //    public List<Category> Categories { get; set; } = new List<Category>();
    //    public List<UserPost> FeaturedPosts { get; set; } = new List<UserPost>();
    //    public List<UserPost> UrgentPosts { get; set; } = new List<UserPost>();
    //    public string SelectedLocation { get; set; }
    //    public int? SelectedCategoryID { get; set; }
    //    public string SortBy { get; set; }
    //}

    public class UserPost
    {
        public int PostID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public string MainImage { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        // User Info
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserProfileImage { get; set; }
        public decimal? UserRating { get; set; }

        // Category Info
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryNameGuj { get; set; }
        public string CategoryIcon { get; set; }

        // Location Info
        public string StateName { get; set; }
        public string DistrictName { get; set; }
        public string TalukaName { get; set; }
        public string VillageName { get; set; }

        // Calculated Properties
        public string TimeAgo => GetTimeAgo(CreatedDate);
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate < DateTime.Now;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Location => $"{VillageName}, {TalukaName}, {DistrictName}".Replace(", ,", ",").Trim(',', ' ');

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "હમણાં જ";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} મિનિટ પહેલા";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} કલાક પહેલા";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} દિવસ પહેલા";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} અઠવાડિયા પહેલા";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} મહિના પહેલા";

            return $"{(int)(timeSpan.TotalDays / 365)} વર્ષ પહેલા";
        }
    }

    public class CategoryWithCount
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryNameGuj { get; set; }
        public string CategoryIcon { get; set; }
        public string CategoryImage { get; set; }
        public int PostCount { get; set; }
        public int TodayPostCount { get; set; }
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }

    public class UserProfileViewModel
    {
        public User User { get; set; }
        public int TotalPosts { get; set; }
        public int ActivePosts { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalFavorites { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<UserPost> RecentPosts { get; set; } = new List<UserPost>();
        public List<UserPost> PopularPosts { get; set; } = new List<UserPost>();
        public int ProfileCompletionPercentage { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime LastActive { get; set; }
    }

    public class Comment
    {
        public int CommentID { get; set; }
        public int PostID { get; set; }
        public int UserID { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }

        // User Info
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserProfileImage { get; set; }

        // Calculated Properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string TimeAgo => GetTimeAgo(CreatedDate);

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "હમણાં જ";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} મિનિટ પહેલા";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} કલાક પહેલા";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} દિવસ પહેલા";

            return dateTime.ToString("dd/MM/yyyy");
        }
    }
}