using System.ComponentModel.DataAnnotations;

namespace GujaratFarmersPortal.Models
{
    public class ViewModels
    {
    }
    public class LoginViewModel
    {
        [Required(ErrorMessage = "યુઝરનેમ આવશ્યક છે")]
        [Display(Name = "યુઝરનેમ/ઈમેઈલ/મોબાઈલ")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "પાસવર્ડ આવશ્યક છે")]
        [DataType(DataType.Password)]
        [Display(Name = "પાસવર્ડ")]
        public string Password { get; set; }

        [Display(Name = "મને યાદ રાખો")]
        public bool RememberMe { get; set; }

        //public string ReturnUrl { get; set; }
    }

    // Register View Model
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "પ્રથમ નામ આવશ્યક છે")]
        [StringLength(50, ErrorMessage = "પ્રથમ નામ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "પ્રથમ નામ")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "છેલ્લું નામ આવશ્યક છે")]
        [StringLength(50, ErrorMessage = "છેલ્લું નામ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "છેલ્લું નામ")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "યુઝરનેમ આવશ્યક છે")]
        [StringLength(50, ErrorMessage = "યુઝરનેમ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "યુઝરનેમ")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "ઈમેઈલ આવશ્યક છે")]
        [EmailAddress(ErrorMessage = "માન્ય ઈમેઈલ એડ્રેસ દાખલ કરો")]
        [Display(Name = "ઈમેઈલ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "મોબાઈલ નંબર આવશ્યક છે")]
        [Phone(ErrorMessage = "માન્ય મોબાઈલ નંબર દાખલ કરો")]
        [StringLength(15, ErrorMessage = "મોબાઈલ નંબર 15 અંકથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "મોબાઈલ નંબર")]
        public string MobileNumber { get; set; }

        [Required(ErrorMessage = "પાસવર્ડ આવશ્યક છે")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ")]
        [DataType(DataType.Password)]
        [Display(Name = "પાસવર્ડ")]
        public string Password { get; set; }

        [Required(ErrorMessage = "પાસવર્ડની પુષ્ટિ આવશ્યક છે")]
        [DataType(DataType.Password)]
        [Display(Name = "પાસવર્ડની પુષ્ટિ")]
        [Compare("Password", ErrorMessage = "પાસવર્ડ અને પુષ્ટિ મેળ ખાતા નથી")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "લિંગ")]
        public string Gender { get; set; }

        [Display(Name = "રાજ્ય")]
        public int? StateID { get; set; }

        [Display(Name = "જિલ્લો")]
        public int? DistrictID { get; set; }

        [Display(Name = "તાલુકો")]
        public int? TalukaID { get; set; }

        [Display(Name = "ગામ")]
        public int? VillageID { get; set; }

        [Display(Name = "સરનામું")]
        [StringLength(500, ErrorMessage = "સરનામું 500 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Address { get; set; }

        [Display(Name = "પિનકોડ")]
        [StringLength(10, ErrorMessage = "પિનકોડ 10 અંકથી વધુ હોવું જોઈએ નહીં")]
        public string Pincode { get; set; }

        [Required(ErrorMessage = "નિયમો અને શરતોની સ્વીકૃતિ આવશ્યક છે")]
        [Display(Name = "હું નિયમો અને શરતો સ્વીકારું છું")]
        public bool AcceptTerms { get; set; }
    }

    // Dashboard View Model
    public class DashboardViewModel
    {
        public string UserFullName { get; set; }
        public string UserRole { get; set; }
        public int TotalPosts { get; set; }
        public int ActivePosts { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int UnreadMessages { get; set; }
        public int UnreadNotifications { get; set; }
        public List<Post> RecentPosts { get; set; } = new List<Post>();
        public List<Category> Categories { get; set; } = new List<Category>();

        // Admin specific
        public int TotalUsers { get; set; }
        public int TodayUsers { get; set; }
        public int PendingPosts { get; set; }
        public int PendingReports { get; set; }
        public List<object> RecentActivity { get; set; } = new List<object>();
    }

    // Home Page View Model
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Post> FeaturedPosts { get; set; } = new List<Post>();
        public List<Post> RecentPosts { get; set; } = new List<Post>();
        public List<Post> UrgentPosts { get; set; } = new List<Post>();
        public int TotalPosts { get; set; }
        public int TotalUsers { get; set; }
        public int TotalCategories { get; set; }
    }

    // Profile View Model
    public class ProfileViewModel
    {
        public User User { get; set; }
        public List<Post> UserPosts { get; set; } = new List<Post>();
        public List<State> States { get; set; } = new List<State>();
        public List<District> Districts { get; set; } = new List<District>();
        public List<Taluka> Talukas { get; set; } = new List<Taluka>();
        public List<Village> Villages { get; set; } = new List<Village>();
        public int TotalPosts { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalFavorites { get; set; }
        public decimal AverageRating { get; set; }
        public int ProfileCompletionPercentage { get; set; }
    }

    // Search/Filter View Model
    public class SearchViewModel
    {
        public string SearchQuery { get; set; }
        public int? CategoryID { get; set; }
        public int? SubCategoryID { get; set; }
        public int? StateID { get; set; }
        public int? DistrictID { get; set; }
        public int? TalukaID { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Condition { get; set; }
        public string SortBy { get; set; } = "CreatedDate";
        public string SortOrder { get; set; } = "DESC";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Results
        public List<Post> Posts { get; set; } = new List<Post>();
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        // Filter Options
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public List<State> States { get; set; } = new List<State>();
        public List<District> Districts { get; set; } = new List<District>();
        public List<Taluka> Talukas { get; set; } = new List<Taluka>();
    }

    // Post Create/Edit View Model
    public class PostViewModel
    {
        public int PostID { get; set; }

        [Required(ErrorMessage = "શીર્ષક આવશ્યક છે")]
        [StringLength(200, ErrorMessage = "શીર્ષક 200 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "શીર્ષક")]
        public string Title { get; set; }

        [Required(ErrorMessage = "વર્ણન આવશ્યક છે")]
        [StringLength(2000, ErrorMessage = "વર્ણન 2000 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "વર્ણન")]
        public string Description { get; set; }

        [Required(ErrorMessage = "કેટેગરી પસંદ કરો")]
        [Display(Name = "કેટેગરી")]
        public int CategoryID { get; set; }

        [Display(Name = "સબ કેટેગરી")]
        public int? SubCategoryID { get; set; }

        [Display(Name = "કિંમત")]
        [Range(0, 999999999, ErrorMessage = "માન્ય કિંમત દાખલ કરો")]
        public decimal? Price { get; set; }

        [Display(Name = "કિંમતનો પ્રકાર")]
        public string PriceType { get; set; } = "Fixed";

        [Required(ErrorMessage = "સંપર્ક નામ આવશ્યક છે")]
        [Display(Name = "સંપર્ક નામ")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "સંપર્ક નંબર આવશ્યક છે")]
        [Phone(ErrorMessage = "માન્ય મોબાઈલ નંબર દાખલ કરો")]
        [Display(Name = "સંપર્ક નંબર")]
        public string ContactNumber { get; set; }

        [Phone(ErrorMessage = "માન્ય વ્હોટ્સએપ નંબર દાખલ કરો")]
        [Display(Name = "વ્હોટ્સએપ નંબર")]
        public string WhatsAppNumber { get; set; }

        [Display(Name = "રાજ્ય")]
        public int? StateID { get; set; }

        [Display(Name = "જિલ્લો")]
        public int? DistrictID { get; set; }

        [Display(Name = "તાલુકો")]
        public int? TalukaID { get; set; }

        [Display(Name = "ગામ")]
        public int? VillageID { get; set; }

        [Display(Name = "સરનામું")]
        [StringLength(500, ErrorMessage = "સરનામું 500 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Address { get; set; }

        [Display(Name = "હાલત")]
        public string Condition { get; set; }

        [Display(Name = "બ્રાન્ડ")]
        [StringLength(50, ErrorMessage = "બ્રાન્ડ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Brand { get; set; }

        [Display(Name = "મોડેલ")]
        [StringLength(50, ErrorMessage = "મોડેલ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Model { get; set; }

        [Display(Name = "વર્ષ")]
        [Range(1900, 2030, ErrorMessage = "માન્ય વર્ષ દાખલ કરો")]
        public int? Year { get; set; }

        [Display(Name = "તાત્કાલિક")]
        public bool IsUrgent { get; set; }

        // For image uploads
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
        public List<string> ExistingImages { get; set; } = new List<string>();
        public List<string> ImagesToDelete { get; set; } = new List<string>();

        // Dropdown data
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public List<State> States { get; set; } = new List<State>();
        public List<District> Districts { get; set; } = new List<District>();
        public List<Taluka> Talukas { get; set; } = new List<Taluka>();
        public List<Village> Villages { get; set; } = new List<Village>();
    }

    // Post Details View Model
    public class PostDetailsViewModel
    {
        public Post Post { get; set; }
        public User PostOwner { get; set; }
        public List<Post> RelatedPosts { get; set; } = new List<Post>();
        public List<Post> OwnerOtherPosts { get; set; } = new List<Post>();
        public bool IsLiked { get; set; }
        public bool IsFavorite { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanContact { get; set; }
    }

    // API Response Models
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
    }

    // Notification Models
    public class Notification
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; }
        public int? ReferenceID { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public string TimeAgo { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
    }

    // Message Models
    public class Message
    {
        public int MessageID { get; set; }
        public int PostID { get; set; }
        public int SenderUserID { get; set; }
        public int ReceiverUserID { get; set; }
        public string MessageText { get; set; }
        public string MessageType { get; set; }
        public string AttachmentPath { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ReadDate { get; set; }

        // Navigation Properties
        public string SenderName { get; set; }
        public string SenderProfileImage { get; set; }
        public string PostTitle { get; set; }
        public string TimeAgo { get; set; }
    }

    // Contact Form View Model
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "નામ આવશ્યક છે")]
        [Display(Name = "તમારું નામ")]
        public string Name { get; set; }

        [Required(ErrorMessage = "ઈમેઈલ આવશ્યક છે")]
        [EmailAddress(ErrorMessage = "માન્ય ઈમેઈલ દાખલ કરો")]
        [Display(Name = "ઈમેઈલ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "મોબાઈલ નંબર આવશ્યક છે")]
        [Phone(ErrorMessage = "માન્ય મોબાઈલ નંબર દાખલ કરો")]
        [Display(Name = "મોબાઈલ નંબર")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "વિષય આવશ્યક છે")]
        [Display(Name = "વિષય")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "સંદેશ આવશ્યક છે")]
        [StringLength(1000, ErrorMessage = "સંદેશ 1000 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        [Display(Name = "સંદેશ")]
        public string MessageText { get; set; }

        public int? PostID { get; set; }
        public string PostTitle { get; set; }
    }

    // Settings View Model
    public class SettingsViewModel
    {
        // Profile Settings
        [Display(Name = "પ્રોફાઈલ ફોટો")]
        public IFormFile ProfileImage { get; set; }
        public string CurrentProfileImage { get; set; }

        // Notification Settings
        [Display(Name = "ઈમેઈલ નોટિફિકેશન")]
        public bool EmailNotifications { get; set; }

        [Display(Name = "SMS નોટિફિકેશન")]
        public bool SMSNotifications { get; set; }

        [Display(Name = "પુશ નોટિફિકેશન")]
        public bool PushNotifications { get; set; }

        // Privacy Settings
        [Display(Name = "પ્રોફાઈલ સાર્વજનિક")]
        public bool PublicProfile { get; set; }

        [Display(Name = "સંપર્ક માહિતી બતાવો")]
        public bool ShowContactInfo { get; set; }

        [Display(Name = "ઓનલાઈન સ્ટેટસ બતાવો")]
        public bool ShowOnlineStatus { get; set; }

        // Language and Theme
        [Display(Name = "ભાષા")]
        public string PreferredLanguage { get; set; }

        [Display(Name = "થીમ")]
        public string Theme { get; set; }

        // Security Settings
        [Display(Name = "વર્તમાન પાસવર્ડ")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Display(Name = "નવો પાસવર્ડ")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "પાસવર્ડ ઓછામાં ઓછો 6 અક્ષરનો હોવો જોઈએ")]
        public string NewPassword { get; set; }

        [Display(Name = "નવા પાસવર્ડની પુષ્ટિ")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "પાસવર્ડ અને પુષ્ટિ મેળ ખાતા નથી")]
        public string ConfirmNewPassword { get; set; }

        // Account Management
        [Display(Name = "એકાઉન્ટ ડિએક્ટિવેટ કરો")]
        public bool DeactivateAccount { get; set; }

        [Display(Name = "એકાઉન્ટ ડિલીટ કરો")]
        public bool DeleteAccount { get; set; }
    }

    // Admin View Models
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TodayUsers { get; set; }
        public int TotalPosts { get; set; }
        public int TodayPosts { get; set; }
        public int PendingPosts { get; set; }
        public int PendingReports { get; set; }
        public int TotalCategories { get; set; }
        public int TodayMessages { get; set; }

        public List<object> MonthlyUserStats { get; set; } = new List<object>();
        public List<object> CategoryPostStats { get; set; } = new List<object>();
        public List<object> TopDistricts { get; set; } = new List<object>();
        public List<Post> RecentPosts { get; set; } = new List<Post>();
        public List<User> RecentUsers { get; set; } = new List<User>();
        public List<object> RecentActivity { get; set; } = new List<object>();
    }

    // Report View Model
    public class ReportViewModel
    {
        public int ReportID { get; set; }
        public int PostID { get; set; }
        public int ReportedByUserID { get; set; }

        [Required(ErrorMessage = "રિપોર્ટનું કારણ પસંદ કરો")]
        [Display(Name = "રિપોર્ટનું કારણ")]
        public string ReportReason { get; set; }

        [Display(Name = "વિગતવાર વર્ણન")]
        [StringLength(500, ErrorMessage = "વર્ણન 500 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string ReportDescription { get; set; }

        // For display
        public string PostTitle { get; set; }
        public string ReporterName { get; set; }
        public string PostOwnerName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
