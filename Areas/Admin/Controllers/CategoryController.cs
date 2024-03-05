using CarSales.Models;
using CarSales.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CarSales.DataAccess.Repository.IRepository;

namespace CarSales.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private IUnitOfWork _unitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        // Create and Edit 
        public IActionResult Upsert(int? id)
        {          
            // Check whether to create or update
            if(id == null || id == 0)
            {
                // Create a new category
                return View(new Category());
            }
            else
            {
                // Finds the category by id and display it
                Category category = _unitOfWork.Category.Get(u => u.Id == id);
                return View(category);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Category category)
        {
            // Check if the modelstate is valid
            // Modelstate is for the validation
            // EX: Since we put name as required, if name field is not input, the code won't run
            if (ModelState.IsValid) 
            {
                // Check whether to create or update
                if(category.Id == 0)
                {
                    // Create 
                    _unitOfWork.Category.Add(category);
                }
                else
                {
                    // Update
                    _unitOfWork.Category.Update(category);
                }

                // Save it
                _unitOfWork.Save();
            }
            // Return back to 
            return RedirectToAction(nameof(Index));
        }


        #region API Calls
        [HttpGet] 
        public IActionResult GetALL()
        {
            // Get the list of categories
            IEnumerable<Category> categories = _unitOfWork.Category.GetAll();
            return Json(new {data = categories});
        }

        // Delete the category
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            // Find category by Id
            var categoryToDelete = _unitOfWork.Category.Get(u=>u.Id == id);
            // If unable to find the category by id
            if(categoryToDelete == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Category.Remove(categoryToDelete);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
