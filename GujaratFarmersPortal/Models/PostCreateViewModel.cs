using System.ComponentModel.DataAnnotations;
using GujaratFarmersPortal.Services;
using Microsoft.AspNetCore.Http;

namespace GujaratFarmersPortal.Models
{
    public class PostCreateViewModel : IValidatableObject
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
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "વર્ણન આવશ્યક છે")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "વર્ણન 10 થી 2000 અક્ષર સુધીનું હોવું જોઈએ")]
        [Display(Name = "વર્ણન")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "કિંમત")]
        [Range(0, 999999999, ErrorMessage = "કિંમત 0 થી 99,99,99,999 સુધી હોવી જોઈએ")]
        public decimal? Price { get; set; }

        [Display(Name = "હાલત")]
        public string Condition { get; set; } = string.Empty;

        [Display(Name = "બ્રાંડ")]
        [StringLength(50, ErrorMessage = "બ્રાંડ નામ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Brand { get; set; } = string.Empty;

        [Display(Name = "મોડલ")]
        [StringLength(50, ErrorMessage = "મોડલ નામ 50 અક્ષરથી વધુ હોવું જોઈએ નહીં")]
        public string Model { get; set; } = string.Empty;

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
        public string Address { get; set; } = string.Empty;

        [Display(Name = "તાત્કાલિક")]
        public bool IsUrgent { get; set; }

        // For image uploads
        [Display(Name = "ફોટો")]
        public List<IFormFile> Images { get; set; } = new List<IFormFile>();

        // For existing images (used in edit mode)
        public List<string> ExistingImages { get; set; } = new List<string>();

        // For images to be deleted (used in edit mode)
        public List<string> ImagesToDelete { get; set; } = new List<string>();

        // Dropdown data - Auto-populated by the model itself
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

        // Auto-population methods - These will be called automatically
        public async Task PopulateDropdownDataAsync(IUserService userService)
        {
            try
            {
                // Load all dropdown data in parallel for better performance
                var categoriesTask = userService.GetCategoriesAsync();
                var statesTask = userService.GetStatesAsync();
                var subCategoriesTask = CategoryID.HasValue ?
                    userService.GetSubCategoriesAsync(CategoryID.Value) :
                    Task.FromResult(new List<SubCategory>());
                var districtsTask = StateID.HasValue ?
                    userService.GetDistrictsAsync(StateID.Value) :
                    Task.FromResult(new List<District>());
                var talukasTask = DistrictID.HasValue ?
                    userService.GetTalukasAsync(DistrictID.Value) :
                    Task.FromResult(new List<Taluka>());
                var villagesTask = TalukaID.HasValue ?
                    userService.GetVillagesAsync(TalukaID.Value) :
                    Task.FromResult(new List<Village>());

                await Task.WhenAll(categoriesTask, statesTask, subCategoriesTask,
                                 districtsTask, talukasTask, villagesTask);

                Categories = await categoriesTask;
                States = await statesTask;
                SubCategories = await subCategoriesTask;
                Districts = await districtsTask;
                Talukas = await talukasTask;
                Villages = await villagesTask;

                // If editing existing post, also load related data
                if (PostID.HasValue)
                {
                    await LoadExistingPostDataAsync(userService);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - graceful degradation
                Console.WriteLine($"Error populating dropdown data: {ex.Message}");

                // Initialize empty lists to prevent null reference errors
                Categories = Categories ?? new List<Category>();
                States = States ?? new List<State>();
                SubCategories = SubCategories ?? new List<SubCategory>();
                Districts = Districts ?? new List<District>();
                Talukas = Talukas ?? new List<Taluka>();
                Villages = Villages ?? new List<Village>();
            }
        }

        private async Task LoadExistingPostDataAsync(IUserService userService)
        {
            if (PostID.HasValue)
            {
                try
                {
                    var existingPost = await userService.GetPostByIdAsync(PostID.Value);
                    if (existingPost != null)
                    {
                        // Populate form fields with existing data
                        CategoryID = existingPost.CategoryID;
                        SubCategoryID = existingPost.SubCategoryID;
                        Title = existingPost.Title;
                        Description = existingPost.Description;
                        Price = existingPost.Price;
                        Condition = existingPost.Condition;
                        Brand = existingPost.Brand;
                        Model = existingPost.Model;
                        Year = existingPost.Year;
                        StateID = existingPost.StateID;
                        DistrictID = existingPost.DistrictID;
                        TalukaID = existingPost.TalukaID;
                        VillageID = existingPost.VillageID;
                        Address = existingPost.Address;
                        IsUrgent = existingPost.IsUrgent;

                        // Load existing images
                        ExistingImages = existingPost.ImageUrls ?? new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading existing post data: {ex.Message}");
                }
            }
        }

        // Static factory method to create and populate the model
        public static async Task<PostCreateViewModel> CreateAsync(IUserService userService, int? postId = null)
        {
            var model = new PostCreateViewModel();
            if (postId.HasValue)
            {
                model.PostID = postId;
            }

            await model.PopulateDropdownDataAsync(userService);
            return model;
        }

        // Method to refresh specific dropdown data (for AJAX calls)
        public async Task RefreshSubCategoriesAsync(IUserService userService, int categoryId)
        {
            try
            {
                CategoryID = categoryId;
                SubCategories = await userService.GetSubCategoriesByCategoryAsync(categoryId);
                SubCategoryID = null; // Reset selection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing subcategories: {ex.Message}");
                SubCategories = new List<SubCategory>();
            }
        }

        public async Task RefreshDistrictsAsync(IUserService userService, int stateId)
        {
            try
            {
                StateID = stateId;
                Districts = await userService.GetDistrictsByStateAsync(stateId);
                DistrictID = null; // Reset selection
                Talukas = new List<Taluka>(); // Clear dependent data
                Villages = new List<Village>();
                TalukaID = null;
                VillageID = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing districts: {ex.Message}");
                Districts = new List<District>();
            }
        }

        public async Task RefreshTalukasAsync(IUserService userService, int districtId)
        {
            try
            {
                DistrictID = districtId;
                Talukas = await userService.GetTalukasByDistrictAsync(districtId);
                TalukaID = null; // Reset selection
                Villages = new List<Village>(); // Clear dependent data
                VillageID = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing talukas: {ex.Message}");
                Talukas = new List<Taluka>();
            }
        }

        public async Task RefreshVillagesAsync(IUserService userService, int talukaId)
        {
            try
            {
                TalukaID = talukaId;
                Villages = await userService.GetVillagesByTalukaAsync(talukaId);
                VillageID = null; // Reset selection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing villages: {ex.Message}");
                Villages = new List<Village>();
            }
        }

        // Helper methods (unchanged)
        public string GetFullLocation()
        {
            var locationParts = new List<string>();

            if (!string.IsNullOrEmpty(Address))
                locationParts.Add(Address);

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

        // Get available condition options
        public List<SelectListItem> GetConditionOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "હાલત પસંદ કરો" },
                new SelectListItem { Value = "નવું", Text = "નવું" },
                new SelectListItem { Value = "સારું", Text = "સારું" },
                new SelectListItem { Value = "સરેરાશ", Text = "સરેરાશ" },
                new SelectListItem { Value = "જૂનું", Text = "જૂનું" }
            };
        }

        // Get year options
        public List<SelectListItem> GetYearOptions()
        {
            var years = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "વર્ષ પસંદ કરો" }
            };

            for (int year = DateTime.Now.Year; year >= 1990; year--)
            {
                years.Add(new SelectListItem
                {
                    Value = year.ToString(),
                    Text = year.ToString(),
                    Selected = Year == year
                });
            }

            return years;
        }

        // Get category select list
        public List<SelectListItem> GetCategorySelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "કેટેગરી પસંદ કરો" }
            };

            items.AddRange(Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryNameGuj,
                Selected = CategoryID == c.CategoryID
            }));

            return items;
        }

        // Get subcategory select list
        public List<SelectListItem> GetSubCategorySelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "સબ-કેટેગરી પસંદ કરો" }
            };

            items.AddRange(SubCategories.Select(sc => new SelectListItem
            {
                Value = sc.SubCategoryID.ToString(),
                Text = sc.SubCategoryNameGuj,
                Selected = SubCategoryID == sc.SubCategoryID
            }));

            return items;
        }

        // Get state select list
        public List<SelectListItem> GetStateSelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "રાજ્ય પસંદ કરો" }
            };

            items.AddRange(States.Select(s => new SelectListItem
            {
                Value = s.StateID.ToString(),
                Text = s.StateName,
                Selected = StateID == s.StateID
            }));

            return items;
        }

        // Get district select list
        public List<SelectListItem> GetDistrictSelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = StateID.HasValue ? "જિલ્લો પસંદ કરો" : "પહેલા રાજ્ય પસંદ કરો" }
            };

            items.AddRange(Districts.Select(d => new SelectListItem
            {
                Value = d.DistrictID.ToString(),
                Text = d.DistrictName,
                Selected = DistrictID == d.DistrictID
            }));

            return items;
        }

        // Get taluka select list
        public List<SelectListItem> GetTalukaSelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = DistrictID.HasValue ? "તાલુકો પસંદ કરો" : "પહેલા જિલ્લો પસંદ કરો" }
            };

            items.AddRange(Talukas.Select(t => new SelectListItem
            {
                Value = t.TalukaID.ToString(),
                Text = t.TalukaName,
                Selected = TalukaID == t.TalukaID
            }));

            return items;
        }

        // Get village select list
        public List<SelectListItem> GetVillageSelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = TalukaID.HasValue ? "ગામ પસંદ કરો" : "પહેલા તાલુકો પસંદ કરો" }
            };

            items.AddRange(Villages.Select(v => new SelectListItem
            {
                Value = v.VillageID.ToString(),
                Text = v.VillageName,
                Selected = VillageID == v.VillageID
            }));

            return items;
        }

        // Custom validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Custom validation for images
            if (!HasAnyImages && PostID == null)
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
            if (Year.HasValue && Year.Value > DateTime.Now.Year && Condition == "જૂનું")
            {
                results.Add(new ValidationResult(
                    "જૂની વસ્તુ માટે ભવિષ્યનું વર્ષ હોઈ શકે નહીં",
                    new[] { nameof(Year) }));
            }

            // Validate district belongs to selected state
            if (StateID.HasValue && DistrictID.HasValue)
            {
                var district = Districts?.FirstOrDefault(d => d.DistrictID == DistrictID);
                if (district != null && district.StateID != StateID)
                {
                    results.Add(new ValidationResult(
                        "પસંદ કરેલ જિલ્લો પસંદ કરેલ રાજ્યમાં નથી",
                        new[] { nameof(DistrictID) }));
                }
            }

            // Validate taluka belongs to selected district
            if (DistrictID.HasValue && TalukaID.HasValue)
            {
                var taluka = Talukas?.FirstOrDefault(t => t.TalukaID == TalukaID);
                if (taluka != null && taluka.DistrictID != DistrictID)
                {
                    results.Add(new ValidationResult(
                        "પસંદ કરેલ તાલુકો પસંદ કરેલ જિલ્લામાં નથી",
                        new[] { nameof(TalukaID) }));
                }
            }

            // Validate village belongs to selected taluka
            if (TalukaID.HasValue && VillageID.HasValue)
            {
                var village = Villages?.FirstOrDefault(v => v.VillageID == VillageID);
                if (village != null && village.TalukaID != TalukaID)
                {
                    results.Add(new ValidationResult(
                        "પસંદ કરેલ ગામ પસંદ કરેલ તાલુકામાં નથી",
                        new[] { nameof(VillageID) }));
                }
            }

            return results;
        }
    }

    // Helper class for Select List Items
    public class SelectListItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}