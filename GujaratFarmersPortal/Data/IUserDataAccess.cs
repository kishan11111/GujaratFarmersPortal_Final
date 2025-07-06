using GujaratFarmersPortal.Models;
using GujaratFarmersPortal.Services;

namespace GujaratFarmersPortal.Data
{
    public interface IUserDataAccess
    {
        // Dashboard & Feed
        Task<UserDashboardViewModel> GetUserDashboardDataAsync(int userID);
        Task<PagedResult<UserPost>> GetFeedPostsAsync(int userID, int? categoryID = null,
            string location = null, string sortBy = "recent", int pageNumber = 1, int pageSize = 10);
        Task<List<UserPost>> GetFeaturedPostsAsync(int userID, int count = 5);
        Task<List<UserPost>> GetUrgentPostsAsync(int userID, int count = 5);
        Task<List<CategoryWithCount>> GetCategoriesWithCountAsync();
        Task<Category> GetCategoryByIdAsync(int categoryID);
        Task<PagedResult<UserPost>> GetPostsByCategoryAsync(int categoryID, int userID,
            string sortBy = "recent", int pageNumber = 1, int pageSize = 12);

        // Post Management
        Task<ApiResponse<string>> CreatePostAsync(PostCreateViewModel model, int userID, List<string> imageUrls);
        Task<PagedResult<UserPost>> GetUserPostsAsync(int userID, string filter = "all",
            int pageNumber = 1, int pageSize = 12);
        Task<PostDetailsViewModel> GetPostDetailsAsync(int postID, int currentUserID);
        Task<Post> GetPostByIdAsync(int postID);
        Task<ApiResponse<string>> UpdatePostAsync(PostCreateViewModel model, int userID, List<string> newImageUrls);
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
        Task<PagedResult<UserPost>> SearchPostsAsync(string searchQuery, int? categoryID = null,
            string location = null, decimal? minPrice = null, decimal? maxPrice = null,
            string sortBy = "recent", int userID = 0, int pageNumber = 1, int pageSize = 12);

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
}