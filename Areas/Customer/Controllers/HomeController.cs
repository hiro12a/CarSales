using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using CarSales.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace CarSales.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // Get a list of products
            IEnumerable<Product> product = _unitOfWork.Product.GetAll(includeProperties: "Category,Images");

            // Display the six products that has been added
            return View(product.OrderByDescending(u => u.Id).Take(6));
        }

        // The productId is connected to the asp-route-productId in the cshtml
        public IActionResult Details(int productId)
        {
            // Get the product and productid
            ShoppingCart cart = new()
            {
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "Category,Images"),
                ProductId = productId
            };
            return View(cart);
        }

        // For adding the item into the shopping cart
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            // Find the userId and assign the userId to the ApplicationUserId
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.UserId = userId;

            // We want to get the user and product
            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.UserId == userId &&
            u.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                // There are items in the cart, so just update and add more items to the cart
                _unitOfWork.ShoppingCart.Update(cartFromDb);
                _unitOfWork.Save();
            }
            else
            {
                // There is nothing in the cart so add the items
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();

                // Used fo displaying the number in the cart               
                HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.UserId == userId).Count());
            }
            TempData["success"] = "Cart updated Successfully";

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Shop(string type)
        {
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties: "Category,Images");

            if (!string.IsNullOrEmpty(type))
            {
                if (type == "all")
                {
                    return View(products);
                }
                else
                {
                    // First make sure all the products are uppercase so that it will be easier to find them
                    products = products.Where(u => u.Year.ToString() == type
                    || u.Make.ToUpper().Contains(type.ToUpper())
                    || u.Model.ToUpper().Contains(type.ToUpper())
                    || u.Category.Name.ToUpper().Contains(type.ToUpper()));
                    return View(products);
                }
            }

            return View(products);
        }
        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region API Keys
        [HttpGet]
        public IActionResult GetAll(string type)
        {
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties: "Category");


            if (!string.IsNullOrEmpty(type))
            {
                products = products.Where(u=>u.Category.Name.Contains(type));
            }
            return View(products.ToList());
        }

        [HttpGet]
        public IActionResult Search(string type)
        {
            IEnumerable<Product> products = _unitOfWork.Product.GetAll(includeProperties: "Category,Images");

            if (!string.IsNullOrEmpty(type))
            {
                products = products.Where(u=>u.Year.Equals(type) || u.Make.Contains(type));
            }

            return RedirectToAction(nameof(Shop));
        }
        #endregion
    }
}