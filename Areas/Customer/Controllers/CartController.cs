using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using CarSales.Models.ViewModels;
using CarSales.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System.Security.Claims;

namespace CarSales.Web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            // Find the userId by using claimsIdentity
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Populate ShoppingCartVM
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.UserId == userId, 
                includeProperties: "Product"),
                OrderHeader = new()
            };

            // Get the image
            IEnumerable<Image> cartImages = _unitOfWork.Image.GetAll();

            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
				// Get the image
				cart.Product.Images = cartImages.Where(u => u.ProductId == cart.Product.Id).ToList(); 
                // Get the price and add them all
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Product.Price;
            }

            return View(ShoppingCartVM);
        }

        // This is used for placing order
        public IActionResult Summary()
        {
            // Find the userId by using claimsIdentity
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Populate ShoppingCartVM
            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.UserId == userId,
                            includeProperties: "Product"),
                OrderHeader = new()
            };

            // Get the values from the applicationUser
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            // Assign the values to orderheader from applicationuser
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Product.Price;
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")] // Let the script knows that it is the same as summary but with a diff name
        public IActionResult SummaryPost()
        {
            // Get Session User
            var claimsIdentity = (ClaimsIdentity)User.Identity; // Find the user
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value; // Get the value of claimsIdentity

            // Get items in the shoppingCart
            ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.UserId == userId,
                includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now; // Set the order date
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId; // Set the userId

            // Get the user from Application User
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u=>u.Id == userId);


            // Foreach item
            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
                // Get the total price
                ShoppingCartVM.OrderHeader.OrderTotal += cart.Product.Price;
            }

            // Check whether to pay now or later
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // User is not a company user or admin so we want them to immediately pay
                // But we can approve the order status
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            else
            {
                // It is a company user so they can pay later
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            // Get info for OrderDetails
            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
                // We want to set the order detail
                OrderDetails orderDetails = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price, 
                };

                _unitOfWork.OrderDetails.Add(orderDetails);
                _unitOfWork.Save();
            }

            // Stripe Payment Section
            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Regular customer account so we need to immediately capture payment
                // Stripe Logic
                var domain = Request.Scheme + "://" + Request.Host.Value + "/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}", // Get where to go after payment is complete
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions // For teh price 
                        {
                            UnitAmount = (long)(item.Product.Price * 100), // $20.50 => 2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Year + " " + item.Product.Make + " " + item.Product.Model
                            }
                        },
                        Quantity = 1 // Set it to 1 since we only want to buy 1 of each item
                    };
                    options.LineItems.Add(sessionLineItem);
                }


                // Go to stripe
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Create(options);
                // Get payment information 
                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);

            }

            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            // We want details from the orderheader and user details too 
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            
            // Check the payment status 
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                // This is an order from the customer
                var service = new Stripe.Checkout.SessionService();
                Stripe.Checkout.Session session = service.Get(orderHeader.SessionId);

                // Check if stripe says its paid
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    // Update these things if its paid
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }

                // Clear the session
                HttpContext.Session.Clear();
                _unitOfWork.Save();
            }

            // Get the items in the cart 
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(u => u.UserId == orderHeader.ApplicationUserId).ToList();
            // Clear the cart
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
        }

        // Removing the item from the cart
        public IActionResult Remove(int cartId)
        {
            // Get the item
            var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);

            // Remove it
            _unitOfWork.ShoppingCart.Remove(cartFromDb);

            // Used for displaying the cart number to let users know that the item has been removed
            // Substract the number in the cart icon
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(
                u => u.UserId == cartFromDb.UserId).Count() - 1);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
    }
}
