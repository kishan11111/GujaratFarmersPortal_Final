using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GujaratFarmersPortal.Models
{
    public class PostCreateViewModel
    {
        public int? PostID { get; set; }

        [Required(ErrorMessage = "કેટેગરી પસંદ કરવી આવશ્યક છે")]
        [Display(Name = "કેટેગરી")]
        public int? CategoryID { get; set; }

        [Display(Name = "સબ-કેટેગરી")]
        public int? SubCategoryID { get; set; }

        [Required(ErrorMessage = "શીર્ષક આવશ્યક છે")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "શીર્ષક 5 થી 100 અક્ષર સુધીનું હોવું જોઈએ")]
        [Display(Name = "શીર્ષક")]
        public string Title { get; set; }

        [Required(ErrorMessage = "વર્ણન આવશ્યક છે")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "વર્ણન 10 થી 2000 અક્ષર સુધીનું હોવું જોઈએ")]
        [Display(Name = "વર્ણન")]
        public string Description { get; set; }

        [Display(Name = "કિંમત")]
        [Range(0, 999999999, ErrorMessage = "કિંમત 0 થી 99,99,99,999 સુધી હોવી જોઈએ")]
        public decimal? Price { get; set; }

        [Display(Name = "હાલત")]
        public string Condition { get; set; }

        [Display(Name = "બ્રાંડ")]
        [StringLength(50, ErrorMessage = "બ્રાંડ નામ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Brand { get; set; }

        [Display(Name = "મોડલ")]
        [StringLength(50, ErrorMessage = "મોડલ નામ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Model { get; set; }

        [Display(Name = "વર્ષ")]
        [Range(1990, 2030, ErrorMessage = "વર્ષ 1990 થી 2030 વચ્ચે હોવું જોઈએ")]
        public int? Year { get; set; }

        [Required(ErrorMessage = "રાજ્ય પસંદ કરવું આવશ્યક છે")]
        [Display(Name = "રાજ્ય")]
        public int? StateID { get; set; }

        [Required(ErrorMessage = "જિલ્લો પસંદ કરવો આવશ્યક છે")]
        [Display(Name = "જિલ્લો")]
        public int? DistrictID { get; set; }

        [Display(Name = "તાલુકો")]
        public int? TalukaID { get; set; }

        [Display(Name = "ગામ")]
        public int? VillageID { get; set; }

        [Display(Name = "સરનામું")]
        [StringLength(500, ErrorMessage = "સરનામું 500 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Address { get; set; }

        [Display(Name = "તાત્કાલિક")]
        public bool IsUrgent { get; set; }

        // For image uploads
        [Display(Name = "ફોટો")]
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();

        // For existing images (used in edit mode)
        public List<string> ExistingImages { get; set; } = new List<string>();

        // For images to be deleted (used in edit mode)
        public List<string> ImagesToDelete { get; set; } = new List<string>();

        // Dropdown data - populated by controller
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        public List<State> States { get; set; } = new List<State>();
        public List<District> Districts { get; set; } = new List<District>();
        public List<Taluka> Talukas { get; set; } = new List<Taluka>();
        public List<Village> Villages { get; set; } = new List<Village>();

        // Additional properties for business logic
        public DateTime? ExpiryDate { get; set; }
        public bool AutoApprove { get; set; } = false;

        // Validation properties
        public bool HasValidImages => Images != null && Images.Any(i => i != null && i.Length > 0);
        public bool HasExistingImages => ExistingImages != null && ExistingImages.Any();
        public bool HasAnyImages => HasValidImages || HasExistingImages;

        // Helper methods
        public string GetFullLocation()
        {
            var locationParts = new List<string>();

            if (!string.IsNullOrEmpty(Address))
                locationParts.Add(Address);

            // These would be populated from dropdown selections
            var village = Villages?.FirstOrDefault(v => v.VillageID == VillageID)?.VillageName;
            var taluka = Talukas?.FirstOrDefault(t => t.TalukaID == TalukaID)?.TalukaName;
            var district = Districts?.FirstOrDefault(d => d.DistrictID == DistrictID)?.DistrictName;
            var state = States?.FirstOrDefault(s => s.StateID == StateID)?.StateName;

            if (!string.IsNullOrEmpty(village)) locationParts.Add(village);
            if (!string.IsNullOrEmpty(taluka)) locationParts.Add(taluka);
            if (!string.IsNullOrEmpty(district)) locationParts.Add(district);
            if (!string.IsNullOrEmpty(state)) locationParts.Add(state);

            return string.Join(", ", locationParts);
        }

        public string GetCategoryName()
        {
            return Categories?.FirstOrDefault(c => c.CategoryID == CategoryID)?.CategoryNameGuj ?? "";
        }

        public string GetSubCategoryName()
        {
            return SubCategories?.FirstOrDefault(sc => sc.SubCategoryID == SubCategoryID)?.SubCategoryNameGuj ?? "";
        }

        public string GetFormattedPrice()
        {
            if (Price.HasValue)
            {
                return "₹" + Price.Value.ToString("N0");
            }
            return "કિંમત નથી આપી";
        }

        public int GetCompletionPercentage()
        {
            int totalFields = 10;
            int completedFields = 0;

            if (CategoryID.HasValue) completedFields++;
            if (!string.IsNullOrEmpty(Title)) completedFields++;
            if (!string.IsNullOrEmpty(Description)) completedFields++;
            if (HasAnyImages) completedFields++;
            if (Price.HasValue) completedFields++;
            if (!string.IsNullOrEmpty(Condition)) completedFields++;
            if (StateID.HasValue) completedFields++;
            if (DistrictID.HasValue) completedFields++;
            if (!string.IsNullOrEmpty(Brand)) completedFields++;
            if (Year.HasValue) completedFields++;

            return (completedFields * 100) / totalFields;
        }

        // Custom validation attributes can be added here
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Custom validation for images
            if (!HasAnyImages && PostID == null) // Only require images for new posts
            {
                results.Add(new ValidationResult(
                    "ઓછામાં ઓછો 1 ફોટો આવશ્યક છે",
                    new[] { nameof(Images) }));
            }

            // Validate image file types and sizes
            if (Images != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var maxSizeBytes = 10 * 1024 * 1024; // 10MB

                foreach (var image in Images.Where(i => i != null))
                {
                    var extension = Path.GetExtension(image.FileName)?.ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        results.Add(new ValidationResult(
                            $"અમાન્ય ફાઇલ પ્રકાર: {image.FileName}. માત્ર JPG, PNG, GIF, WEBP ફાઇલો સ્વીકારવામાં આવે છે.",
                            new[] { nameof(Images) }));
                    }

                    if (image.Length > maxSizeBytes)
                    {
                        results.Add(new ValidationResult(
                            $"ફાઇલ કદ ખૂબ મોટું છે: {image.FileName}. મહત્તમ 10MB સ્વીકારવામાં આવે છે.",
                            new[] { nameof(Images) }));
                    }
                }
            }

            // Validate price for certain categories that typically require price
            var priceRequiredCategories = new[] { 1, 2, 3, 4, 5 }; // Adjust based on your category IDs
            if (CategoryID.HasValue && priceRequiredCategories.Contains(CategoryID.Value) && !Price.HasValue)
            {
                results.Add(new ValidationResult(
                    "આ કેટેગરી માટે કિંમત આવશ્યક છે",
                    new[] { nameof(Price) }));
            }

            // Validate year is not in future for used items
            if (Year.HasValue && Year.Value > DateTime.Now.Year && Condition == "બ્યુઝ્ડ")
            {
                results.Add(new ValidationResult(
                    "બ્યુઝ્ડ વસ્તુ માટે ભવિષ્યનું વર્ષ હોઈ શકે નહીં",
                    new[] { nameof(Year) }));
            }

            return results;
        }
    }

    // Supporting classes for dropdown data
  
}