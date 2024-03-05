using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using CarSales.Models.ViewModels;
using CarSales.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CarSales.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {   
        // Call IUnitOfWork
        private readonly IUnitOfWork _unitOfWork;

        // Allows us to interact with the wwwroot folder
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Check whether to create or update
        public IActionResult Upsert(int? id)
        {
            // Need to create a productVM becuase we want to get data from
            // the Category obj and be able to edit it too
            ProductVM productVM = new()
            {
                // Get the category list and put it in a selectlist
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),

                // Declare that this is a new product 
                Product = new Product()
            };

            if(id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                // Retrieve the products by id into a list
                // IncludeProperties will include images too
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id, includeProperties: "Images");
                return View(productVM);
            }
        }

        // List<IformFile> allows us to get a list of files, ex: the image
        // We need to use list since we want to have multiple images
        // We are using the ProductVM because we want to include Category
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, List<IFormFile> files)
        {
            // Check ModelState Validation
            if (ModelState.IsValid)
            {
                // Check wether to add or update by the productId
                if(productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                _unitOfWork.Save();

                // Get the imagePath 
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if(files != null) // Check if user has uploaded an iamge or not
                {
                    // Go through every file/image that has been uploaded
                    foreach(IFormFile file in files)
                    {
                        // Rename the image to a random name
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        // go to the products folder under image and create a new folder for each individual product
                        string productPath = @"images\products\product-" + productVM.Product.Id;
                        // Combine the paths
                        string finalPath = Path.Combine(wwwRootPath, productPath);

                        // If the obj does not exist, create it 
                        if (!Directory.Exists(finalPath))
                        {
                            Directory.CreateDirectory(finalPath);
                        }

                        // Upload the image and copy it into the folder
                        using (var fileStrem = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStrem);
                        }

                        // Find where the image is located and its productId
                        Image image = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                        // If images don't exist
                        if(productVM.Product.Images == null)
                        {
                            // add all the images
                            productVM.Product.Images = new List<Image>();
                        }

                        // Add the images
                        productVM.Product.Images.Add(image);
                    }

                    // Update and save
                    _unitOfWork.Product.Update(productVM.Product);
                    _unitOfWork.Save();
                }

                // Return back to main product page
                TempData["sucess"] = "Product Created/Updated Successfully";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

                return View(productVM);
            }
        }


        // Remove Image
        // imageId refers to the asp-route-imageId that we have in the html script
        public IActionResult DeleteImage(int imageId)
        {
            // Get the image by id
            var imageToDelete = _unitOfWork.Image.Get(u=>u.Id == imageId);
            // Get the productId that is in the Image Script
            int productId = imageToDelete.ProductId;

            // Check if image exist
            if(imageToDelete != null)
            {
                // Check for the imageUrl to see if it exists
                if(!string.IsNullOrEmpty(imageToDelete.ImageUrl))
                {
                    // Get the old image path 
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        imageToDelete.ImageUrl.TrimStart('\\'));

                    // Check if the file exists and delete it
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Delete the iamge
                _unitOfWork.Image.Remove(imageToDelete);
                _unitOfWork.Save();

                TempData["success"] = "Image Deleted Successfully";
            }

            // Refresh the page 
            // Need the new {id = productId} to tell the server to refresh the page
            // With this id
            TempData["success"] = "Image Deleted Successfully";
            return RedirectToAction(nameof(Upsert), new { id = productId });
        }


        #region API Calls
        public IActionResult GetAll(int? id)
        {
            // Retrieve the products into a list
            // IncludeProperties will include objects from the category script
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList(); 
            return Json(new { data = products });
        }

        // Delete the product
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            // Find the product
            var prodToDelete = _unitOfWork.Product.Get(u => u.Id == id);
            // If there is no product to delete
            if(prodToDelete == null)
            {
                // Return this
                return Json(new { success = false, message = "Error while deleting" });
            }

            // find the images that is associated with the product
            string imagePath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath);

            // Check if the folder exist
            if(Directory.Exists(finalPath))
            {
                // Get all of the images
                string[] filePaths = Directory.GetFiles(finalPath);
                // Delete all of the images
                foreach(string filePath in filePaths) 
                {
                    System.IO.File.Delete(filePath);
                }

                Directory.Delete(finalPath);
            }

            _unitOfWork.Product.Remove(prodToDelete);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }
        #endregion
    }
}
