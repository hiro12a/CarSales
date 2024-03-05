using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using CarSales.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarSales.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        // Call IUnitOfWork, IUnitOfWork has all of the different repository in it
        // Allowing us to have an easier time to access them instead of having to call each and 
        // Every one of them 
        private readonly IUnitOfWork _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // Front page for company where it will display all of the data
        public IActionResult Index()
        {
            return View();
        }

        // Check whether to create or update by id
        public IActionResult Upsert(int? id)
        {
            // If id is null or 0, then create 
            if(id == null || id == 0)
            {
                return View(new Company());
            }
            else
            {
                // Find company by id
                Company companyObj = _unitOfWork.Company.Get(u => u.Id == id);
                // retrieve the company and display
                return View(companyObj);
            }
        }
        
        [HttpPost] // Lets asp.net knows that the code below is used for data manipulation
        public IActionResult Upsert(Company companyObj)
        {
            // Check if state is valid or not
            if(ModelState.IsValid)
            {
                if(companyObj.Id == 0) // Check if company already exist or not
                {
                    _unitOfWork.Company.Add(companyObj); // Create a new company
                }
                else
                {
                    _unitOfWork.Company.Update(companyObj); // Update current company
                }

                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return View(companyObj);
            }
        }

        #region API Call
        [HttpGet]
        public IActionResult GetAll()
        {
            // Get a list of the company 
            List<Company> objCompanyList = _unitOfWork.Company.GetAll().ToList();

            // Display the company list
            return Json(new { data = objCompanyList });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            // Find the company by Id
            Company companyToDelete = _unitOfWork.Company.Get(u => u.Id == id);
            // If company doesn't exist
            if(companyToDelete == null)
            {
                return Json(new { sucess = false, message = "Error while deleting" });
            }

            // Delete the company
            _unitOfWork.Company.Remove(companyToDelete);
            _unitOfWork.Save();

            // Return the data
            return Json(new { sucess = false, message = "Delete successful"});
        }
        #endregion
    }
}
