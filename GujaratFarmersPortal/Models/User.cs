namespace GujaratFarmersPortal.Models
{
    // Basic User Model
    public class User
    {
        public int UserID { get; set; }
        public int UserTypeID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string MobileNumber { get; set; }
        public string Password { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string ProfileImage { get; set; }
        public int? StateID { get; set; }
        public int? DistrictID { get; set; }
        public int? TalukaID { get; set; }
        public int? VillageID { get; set; }
        public string Address { get; set; }
        public string Pincode { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsMobileVerified { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public decimal Rating { get; set; }
        public int TotalReviews { get; set; }
        public DateTime CreatedDate { get; set; }

        public string? DistrictName { get; set; }
        public string? StateName { get; set; }
        public int TotalPosts { get; set; }

        // Computed Properties
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => string.IsNullOrEmpty(FirstName) ? UserName : FullName;
    }

    // Location Models
    public class State
    {
        public int StateID { get; set; }
        public string StateName { get; set; }
        public string StateCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class District
    {
        public int DistrictID { get; set; }
        public int StateID { get; set; }
        public string DistrictName { get; set; }
        public string DistrictCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class Taluka
    {
        public int TalukaID { get; set; }
        public int DistrictID { get; set; }
        public string TalukaName { get; set; }
        public string TalukaCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class Village
    {
        public int VillageID { get; set; }
        public int TalukaID { get; set; }
        public string VillageName { get; set; }
        public string VillageCode { get; set; }
        public string Pincode { get; set; }
        public bool IsActive { get; set; }
    }

    // Category Models
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryNameGuj { get; set; }
        public string CategoryIcon { get; set; }
        public string CategoryImage { get; set; }
        public int? ParentCategoryID { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int PostCount { get; set; }
    }

    public class SubCategory
    {
        public int SubCategoryID { get; set; }
        public int CategoryID { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryNameGuj { get; set; }
        public string SubCategoryIcon { get; set; }
        public string SubCategoryImage { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int PostCount { get; set; }
    }

    // Post Model
    public class Post
    {
        public int PostID { get; set; }
        public int UserID { get; set; }
        public int CategoryID { get; set; }
        public int? SubCategoryID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public string PriceType { get; set; }
        public string ContactName { get; set; }
        public string ContactNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public int? StateID { get; set; }
        public int? DistrictID { get; set; }
        public int? TalukaID { get; set; }
        public int? VillageID { get; set; }
        public string Address { get; set; }
        public string Condition { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int? Year { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsTop { get; set; }
        public bool IsSold { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string PostImage { get; set; }
       
        public bool IsApproved { get; set; }

        // Navigation Properties
        public string CategoryName { get; set; }
        public string CategoryNameGuj { get; set; }
        public string SubCategoryName { get; set; }
        public string UserFullName { get; set; }
        public string StateName { get; set; }
        public string DistrictName { get; set; }
        public string PrimaryImage { get; set; }
        public List<string> Images { get; set; } = new List<string>();

        public string FirstName { get; set; }
        public string LastName { get; set; }
   
    }
}
