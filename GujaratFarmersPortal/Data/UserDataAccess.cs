using System.Data;
using System.Data.SqlClient;
using Dapper;
using GujaratFarmersPortal.Models;
using GujaratFarmersPortal.Services;

namespace GujaratFarmersPortal.Data
{
    public class UserDataAccess : IUserDataAccess
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<UserDataAccess> _logger;

        public UserDataAccess(IDbConnection connection, ILogger<UserDataAccess> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        #region Dashboard & Feed

        public async Task<UserDashboardViewModel> GetUserDashboardDataAsync(int userID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_DASHBOARD");
                parameters.Add("@UserID", userID);

                using var multi = await _connection.QueryMultipleAsync("sp_UserDashboard", parameters, commandType: CommandType.StoredProcedure);

                var dashboard = await multi.ReadFirstOrDefaultAsync<UserDashboardViewModel>();
                dashboard.RecentPosts = (await multi.ReadAsync<UserPost>()).ToList();
                dashboard.TrendingPosts = (await multi.ReadAsync<UserPost>()).ToList();
                dashboard.PopularCategories = (await multi.ReadAsync<Category>()).ToList();

                return dashboard ?? new UserDashboardViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user dashboard data");
                return new UserDashboardViewModel();
            }
        }

        public async Task<PagedResult<UserPost>> GetFeedPostsAsync(int userID, int? categoryID = null,
            string location = null, string sortBy = "recent", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_FEED");
                parameters.Add("@UserID", userID);
                parameters.Add("@CategoryID", categoryID);
                parameters.Add("@Location", location);
                parameters.Add("@SortBy", sortBy);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                var posts = (await multi.ReadAsync<UserPost>()).ToList();
                var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<UserPost>
                {
                    Items = posts,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting feed posts");
                return new PagedResult<UserPost>();
            }
        }

        public async Task<List<UserPost>> GetFeaturedPostsAsync(int userID, int count = 5)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_FEATURED");
                parameters.Add("@UserID", userID);
                parameters.Add("@PageSize", count);

                var posts = await _connection.QueryAsync<UserPost>("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);
                return posts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured posts");
                return new List<UserPost>();
            }
        }

        public async Task<List<UserPost>> GetUrgentPostsAsync(int userID, int count = 5)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_URGENT");
                parameters.Add("@UserID", userID);
                parameters.Add("@PageSize", count);

                var posts = await _connection.QueryAsync<UserPost>("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);
                return posts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting urgent posts");
                return new List<UserPost>();
            }
        }

        public async Task<List<CategoryWithCount>> GetCategoriesWithCountAsync()
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_CATEGORIES_WITH_COUNT");

                using var multi = await _connection.QueryMultipleAsync("sp_CategoryOperations", parameters, commandType: CommandType.StoredProcedure);

                var categories = (await multi.ReadAsync<CategoryWithCount>()).ToList();
                var subCategories = (await multi.ReadAsync<SubCategory>()).ToList();

                // Group subcategories by category
                foreach (var category in categories)
                {
                    category.SubCategories = subCategories.Where(sc => sc.CategoryID == category.CategoryID).ToList();
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_BY_ID");
                parameters.Add("@CategoryID", categoryID);

                var category = await _connection.QueryFirstOrDefaultAsync<Category>("sp_CategoryOperations", parameters, commandType: CommandType.StoredProcedure);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by ID");
                return null;
            }
        }

        public async Task<PagedResult<UserPost>> GetPostsByCategoryAsync(int categoryID, int userID,
            string sortBy = "recent", int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_BY_CATEGORY");
                parameters.Add("@CategoryID", categoryID);
                parameters.Add("@UserID", userID);
                parameters.Add("@SortBy", sortBy);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                var posts = (await multi.ReadAsync<UserPost>()).ToList();
                var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<UserPost>
                {
                    Items = posts,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts by category");
                return new PagedResult<UserPost>();
            }
        }

        #endregion

        #region Post Management

        public async Task<ApiResponse<string>> CreatePostAsync(PostCreateViewModel model, int userID, List<string> imageUrls)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "CREATE");
                parameters.Add("@UserID", userID);
                parameters.Add("@CategoryID", model.CategoryID);
                parameters.Add("@SubCategoryID", model.SubCategoryID);
                parameters.Add("@Title", model.Title);
                parameters.Add("@Description", model.Description);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Condition", model.Condition);
                parameters.Add("@Brand", model.Brand);
                parameters.Add("@Model", model.Model);
                parameters.Add("@Year", model.Year);
                parameters.Add("@StateID", model.StateID);
                parameters.Add("@DistrictID", model.DistrictID);
                parameters.Add("@TalukaID", model.TalukaID);
                parameters.Add("@VillageID", model.VillageID);
                parameters.Add("@Address", model.Address);
                parameters.Add("@IsUrgent", model.IsUrgent);
                parameters.Add("@ImageUrls", string.Join(",", imageUrls));
                parameters.Add("@PostID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);
                var postID = parameters.Get<int>("@PostID");

                if (postID > 0)
                {
                    return new ApiResponse<string>
                    {
                        Success = true,
                        Message = "જાહેરાત સફળતાપૂર્વક બનાવવામાં આવી છે",
                        Data = postID.ToString()
                    };
                }

                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "જાહેરાત બનાવવામાં ભૂલ આવી છે"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "તકનીકી ભૂલ આવી છે"
                };
            }
        }

        public async Task<PagedResult<UserPost>> GetUserPostsAsync(int userID, string filter = "all",
            int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_USER_POSTS");
                parameters.Add("@UserID", userID);
                parameters.Add("@Filter", filter);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                var posts = (await multi.ReadAsync<UserPost>()).ToList();
                var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<UserPost>
                {
                    Items = posts,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user posts");
                return new PagedResult<UserPost>();
            }
        }

        //public async Task<PostDetailsViewModel> GetPostDetailsAsync(int postID, int currentUserID)
        //{
        //    try
        //    {
        //        var parameters = new DynamicParameters();
        //        parameters.Add("@Mode", "GET_DETAILS");
        //        parameters.Add("@PostID", postID);
        //        parameters.Add("@UserID", currentUserID);

        //        using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

        //        var post = await multi.ReadFirstOrDefaultAsync<UserPost>();
        //        if (post == null) return null;

        //        var postDetails = new PostDetailsViewModel
        //        {
        //            Post = post,
        //            PostOwner = await multi.ReadFirstOrDefaultAsync<User>(),
        //            RelatedPosts = (await multi.ReadAsync<UserPost>()).ToList(),
        //            OwnerOtherPosts = (await multi.ReadAsync<UserPost>()).ToList(),
        //            IsLiked = post.IsLiked,
        //            IsFavorite = post.IsFavorite,
        //            CanEdit = post.UserID == currentUserID,
        //            CanDelete = post.UserID == currentUserID,
        //            CanContact = post.UserID != currentUserID
        //        };

        //        return postDetails;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting post details");
        //        return null;
        //    }
        //}

        public async Task<PostDetailsViewModel> GetPostDetailsAsync(int postID, int currentUserID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_DETAILS");
                parameters.Add("@PostID", postID);
                parameters.Add("@UserID", currentUserID);

                using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                var userPost = await multi.ReadFirstOrDefaultAsync<UserPost>();
                if (userPost == null) return null;

                // Convert UserPost to Post
                var post = new Post
                {
                    PostID = userPost.PostID,
                    UserID = userPost.UserID,
                    CategoryID = userPost.CategoryID,
                    Title = userPost.Title,
                    Description = userPost.Description,
                    Price = userPost.Price,
                    ViewCount = userPost.ViewCount,
                    LikeCount = userPost.LikeCount,
                    CommentCount = userPost.CommentCount,
                    IsActive = true, // Assuming active since it's being viewed
                    CreatedDate = userPost.CreatedDate,
                    ExpiryDate = userPost.ExpiryDate,
                    // Add other properties as needed
                };

                var postOwner = await multi.ReadFirstOrDefaultAsync<User>();
                var relatedUserPosts = (await multi.ReadAsync<UserPost>()).ToList();
                var ownerOtherUserPosts = (await multi.ReadAsync<UserPost>()).ToList();

                // Convert related posts from UserPost to Post
                var relatedPosts = relatedUserPosts.Select(up => new Post
                {
                    PostID = up.PostID,
                    UserID = up.UserID,
                    CategoryID = up.CategoryID,
                    Title = up.Title,
                    Description = up.Description,
                    Price = up.Price,
                    ViewCount = up.ViewCount,
                    LikeCount = up.LikeCount,
                    CommentCount = up.CommentCount,
                    IsActive = true,
                    CreatedDate = up.CreatedDate,
                    ExpiryDate = up.ExpiryDate,
                    // Add other properties as needed
                }).ToList();

                // Convert owner other posts from UserPost to Post
                var ownerOtherPosts = ownerOtherUserPosts.Select(up => new Post
                {
                    PostID = up.PostID,
                    UserID = up.UserID,
                    CategoryID = up.CategoryID,
                    Title = up.Title,
                    Description = up.Description,
                    Price = up.Price,
                    ViewCount = up.ViewCount,
                    LikeCount = up.LikeCount,
                    CommentCount = up.CommentCount,
                    IsActive = true,
                    CreatedDate = up.CreatedDate,
                    ExpiryDate = up.ExpiryDate,
                    // Add other properties as needed
                }).ToList();

                var postDetails = new PostDetailsViewModel
                {
                    Post = post,
                    PostOwner = postOwner,
                    RelatedPosts = relatedPosts,
                    OwnerOtherPosts = ownerOtherPosts,
                    IsLiked = userPost.IsLiked,
                    IsFavorite = userPost.IsFavorite,
                    CanEdit = userPost.UserID == currentUserID,
                    CanDelete = userPost.UserID == currentUserID,
                    CanContact = userPost.UserID != currentUserID
                };

                return postDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post details");
                return null;
            }
        }
        public async Task<Post> GetPostByIdAsync(int postID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_BY_ID");
                parameters.Add("@PostID", postID);

                var post = await _connection.QueryFirstOrDefaultAsync<Post>("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);
                return post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post by ID");
                return null;
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

        public async Task<ApiResponse<string>> UpdatePostAsync(PostCreateViewModel model, int userID, List<string> newImageUrls)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "UPDATE");
                parameters.Add("@PostID", model.PostID);
                parameters.Add("@UserID", userID);
                parameters.Add("@CategoryID", model.CategoryID);
                parameters.Add("@SubCategoryID", model.SubCategoryID);
                parameters.Add("@Title", model.Title);
                parameters.Add("@Description", model.Description);
                parameters.Add("@Price", model.Price);
                parameters.Add("@Condition", model.Condition);
                parameters.Add("@Brand", model.Brand);
                parameters.Add("@Model", model.Model);
                parameters.Add("@Year", model.Year);
                parameters.Add("@StateID", model.StateID);
                parameters.Add("@DistrictID", model.DistrictID);
                parameters.Add("@TalukaID", model.TalukaID);
                parameters.Add("@VillageID", model.VillageID);
                parameters.Add("@Address", model.Address);
                parameters.Add("@IsUrgent", model.IsUrgent);
                parameters.Add("@NewImageUrls", string.Join(",", newImageUrls));
                parameters.Add("@ImagesToDelete", string.Join(",", model.ImagesToDelete ?? new List<string>()));

                var result = await _connection.QueryFirstOrDefaultAsync<dynamic>("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "જાહેરાત સફળતાપૂર્વક અપડેટ કરવામાં આવી છે"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "અપડેટ કરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<ApiResponse<string>> DeletePostAsync(int postID, int userID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "DELETE");
                parameters.Add("@PostID", postID);
                parameters.Add("@UserID", userID);

                await _connection.ExecuteAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "જાહેરાત સફળતાપૂર્વક ડિલીટ કરવામાં આવી છે"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = "ડિલીટ કરવામાં ભૂલ આવી છે"
                };
            }
        }

        public async Task<ApiResponse<string>> IncrementPostViewAsync(int postID, int userID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "INCREMENT_VIEW");
                parameters.Add("@PostID", postID);
                parameters.Add("@UserID", userID);

                await _connection.ExecuteAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string> { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing post view");
                return new ApiResponse<string> { Success = false };
            }
        }

        #endregion

        #region Post Interactions

        public async Task<ApiResponse<bool>> TogglePostLikeAsync(int postID, int userID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "TOGGLE_LIKE");
                parameters.Add("@PostID", postID);
                parameters.Add("@UserID", userID);
                parameters.Add("@IsLiked", dbType: DbType.Boolean, direction: ParameterDirection.Output);

                await _connection.ExecuteAsync("sp_PostInteractions", parameters, commandType: CommandType.StoredProcedure);
                var isLiked = parameters.Get<bool>("@IsLiked");

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = isLiked,
                    Message = isLiked ? "લાઈક કર્યું" : "લાઈક કાઢ્યું"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling post like");
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "TOGGLE_FAVORITE");
                parameters.Add("@PostID", postID);
                parameters.Add("@UserID", userID);
                parameters.Add("@IsFavorite", dbType: DbType.Boolean, direction: ParameterDirection.Output);

                await _connection.ExecuteAsync("sp_PostInteractions", parameters, commandType: CommandType.StoredProcedure);
                var isFavorite = parameters.Get<bool>("@IsFavorite");

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = isFavorite,
                    Message = isFavorite ? "પસંદીદામાં ઉમેર્યું" : "પસંદીદામાંથી કાઢ્યું"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling post favorite");
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "ADD_COMMENT");
                parameters.Add("@PostID", postID);
                parameters.Add("@UserID", userID);
                parameters.Add("@CommentText", commentText);
                parameters.Add("@CommentID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _connection.ExecuteAsync("sp_PostInteractions", parameters, commandType: CommandType.StoredProcedure);
                var commentID = parameters.Get<int>("@CommentID");

                if (commentID > 0)
                {
                    // Get the created comment
                    var comment = await GetCommentByIdAsync(commentID);
                    return new ApiResponse<Comment>
                    {
                        Success = true,
                        Data = comment,
                        Message = "કમેન્ટ ઉમેરવામાં આવ્યો"
                    };
                }

                return new ApiResponse<Comment>
                {
                    Success = false,
                    Message = "કમેન્ટ ઉમેરવામાં ભૂલ આવી છે"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment");
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_COMMENTS");
                parameters.Add("@PostID", postID);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                using var multi = await _connection.QueryMultipleAsync("sp_PostInteractions", parameters, commandType: CommandType.StoredProcedure);

                var comments = (await multi.ReadAsync<Comment>()).ToList();
                var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<Comment>
                {
                    Items = comments,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post comments");
                return new PagedResult<Comment>();
            }
        }

        private async Task<Comment> GetCommentByIdAsync(int commentID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_COMMENT_BY_ID");
                parameters.Add("@CommentID", commentID);

                var comment = await _connection.QueryFirstOrDefaultAsync<Comment>("sp_PostInteractions", parameters, commandType: CommandType.StoredProcedure);
                return comment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment by ID");
                return null;
            }
        }

        #endregion

        #region User Profile & Favorites

        public async Task<UserProfileViewModel> GetUserProfileAsync(int userID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_PROFILE");
                parameters.Add("@UserID", userID);

                using var multi = await _connection.QueryMultipleAsync("sp_UserProfile", parameters, commandType: CommandType.StoredProcedure);

                var profile = await multi.ReadFirstOrDefaultAsync<UserProfileViewModel>();
                if (profile != null)
                {
                    profile.User = await multi.ReadFirstOrDefaultAsync<User>();
                    profile.RecentPosts = (await multi.ReadAsync<UserPost>()).ToList();
                    profile.PopularPosts = (await multi.ReadAsync<UserPost>()).ToList();
                }

                return profile ?? new UserProfileViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return new UserProfileViewModel();
            }
        }

        public async Task<PagedResult<UserPost>> GetUserFavoritesAsync(int userID, int pageNumber = 1, int pageSize = 12)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_FAVORITES");
                parameters.Add("@UserID", userID);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                var posts = (await multi.ReadAsync<UserPost>()).ToList();
                var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<UserPost>
                {
                    Items = posts,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user favorites");
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "SEARCH");
                parameters.Add("@SearchQuery", searchQuery);
                parameters.Add("@CategoryID", categoryID);
                parameters.Add("@Location", location);
                parameters.Add("@MinPrice", minPrice);
                parameters.Add("@MaxPrice", maxPrice);
                parameters.Add("@SortBy", sortBy);
                parameters.Add("@UserID", userID);
                parameters.Add("@PageNumber", pageNumber);
                parameters.Add("@PageSize", pageSize);

                using var multi = await _connection.QueryMultipleAsync("sp_UserPostOperations", parameters, commandType: CommandType.StoredProcedure);

                var posts = (await multi.ReadAsync<UserPost>()).ToList();
                var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

                return new PagedResult<UserPost>
                {
                    Items = posts,
                    TotalRecords = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = pageNumber > 1,
                    HasNext = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize)
                };
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_ALL");

                var categories = await _connection.QueryAsync<Category>("sp_CategoryOperations", parameters, commandType: CommandType.StoredProcedure);
                return categories.ToList();
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
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "GET_SUBCATEGORIES");
                parameters.Add("@CategoryID", categoryID);

                var subCategories = await _connection.QueryAsync<SubCategory>("sp_CategoryOperations", parameters, commandType: CommandType.StoredProcedure);
                return subCategories.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subcategories");
                return new List<SubCategory>();
            }
        }

        public async Task<List<State>> GetStatesAsync()
        {
            try
            {
                var sql = "SELECT StateID, StateName, StateNameGuj FROM tbl_States WHERE IsActive = 1 ORDER BY StateName";
                var states = await _connection.QueryAsync<State>(sql);
                return states.ToList();
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
                var sql = "SELECT DistrictID, StateID, DistrictName, DistrictNameGuj FROM tbl_Districts WHERE StateID = @StateID AND IsActive = 1 ORDER BY DistrictName";
                var districts = await _connection.QueryAsync<District>(sql, new { StateID = stateID });
                return districts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting districts");
                return new List<District>();
            }
        }

        public async Task<List<Taluka>> GetTalukasAsync(int districtID)
        {
            try
            {
                var sql = "SELECT TalukaID, DistrictID, TalukaName, TalukaNameGuj FROM tbl_Talukas WHERE DistrictID = @DistrictID AND IsActive = 1 ORDER BY TalukaName";
                var talukas = await _connection.QueryAsync<Taluka>(sql, new { DistrictID = districtID });
                return talukas.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting talukas");
                return new List<Taluka>();
            }
        }

        public async Task<List<Village>> GetVillagesAsync(int talukaID)
        {
            try
            {
                var sql = "SELECT VillageID, TalukaID, VillageName, VillageNameGuj FROM tbl_Villages WHERE TalukaID = @TalukaID AND IsActive = 1 ORDER BY VillageName";
                var villages = await _connection.QueryAsync<Village>(sql, new { TalukaID = talukaID });
                return villages.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting villages");
                return new List<Village>();
            }
        }

        #endregion

        #region Analytics

        public async Task<ApiResponse<string>> LogUserActivityAsync(int userID, string activity, int? referenceID = null)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", "LOG_ACTIVITY");
                parameters.Add("@UserID", userID);
                parameters.Add("@Activity", activity);
                parameters.Add("@ReferenceID", referenceID);

                await _connection.ExecuteAsync("sp_UserAnalytics", parameters, commandType: CommandType.StoredProcedure);

                return new ApiResponse<string> { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging user activity");
                return new ApiResponse<string> { Success = false };
            }
        }

        #endregion
    }
}