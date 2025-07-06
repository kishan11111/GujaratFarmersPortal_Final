using GujaratFarmersPortal.Services;

namespace GujaratFarmersPortal.Models
{
    public class UserFeedViewModel
    {
        public PagedResult<UserPost> Posts { get; set; } = new PagedResult<UserPost>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<UserPost> FeaturedPosts { get; set; } = new List<UserPost>();
        public List<UserPost> UrgentPosts { get; set; } = new List<UserPost>();
        public string SelectedLocation { get; set; }
        public int? SelectedCategoryID { get; set; }
        public string SortBy { get; set; }
    }
}
