using Microsoft.AspNetCore.Mvc.Rendering;

namespace CarSales.Models.ViewModels
{
    public class RoleManagerVM
    {
        public ApplicationUser ApplicationUser { get; set; }
        public IEnumerable<SelectListItem> RoleList { get; set; }
        public IEnumerable<SelectListItem> CompanyList { get; set; }

    }
}
