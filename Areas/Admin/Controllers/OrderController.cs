using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using CarSales.Models.ViewModels;
using CarSales.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace CarSales.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            // OrderVM contains a combination of OrderHeader and OrderDetails scripts
            // Lets us call both script and display it 
            OrderVM = new()
            {
                // We want to get the orderId and who the user is 
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
               
                // We want to get everything and also stuff from the products
                OrderDetails = _unitOfWork.OrderDetails.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin+","+SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult UpdateOrderDetail()
        {
            // Get details from orderHeader 
            var orderHeaderObj = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            // Update the details after user input it 
            orderHeaderObj.Name = OrderVM.OrderHeader.Name;
            orderHeaderObj.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderObj.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderObj.City = OrderVM.OrderHeader.City;
            orderHeaderObj.State = OrderVM.OrderHeader.State;
            orderHeaderObj.PostalCode = OrderVM.OrderHeader.PostalCode;

            // Check to see if these are empty
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderObj.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderObj.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeaderObj);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";

            // Basically refreshs the page with the details from orderHeader
            // orderId is case sensitive because its used to find the obj 
            return RedirectToAction(nameof(Details), new { orderId = orderHeaderObj.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult StartProcessing()
        {
            // Just change the status by finding what the id is and switching its status
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["succes"] = "Order Details updated sucessfully";

            // Basically refreshs the page with the details from orderHeader
            // orderId is case sensitive because its used to find the obj 
            return RedirectToAction(nameof(Details), new {orderId = OrderVM.OrderHeader.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult ShipOrder()
        {
            // Get the orderHeader item by its ID
            var orderHeaderObj = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            // Update these when user inputs it
            orderHeaderObj.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderObj.Carrier = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderObj.OrderStatus = SD.StatusShipped; // Change the status 
            orderHeaderObj.ShippingDate = DateTime.Now;

            // For company users to pay, they have 30 days to pay
            if(orderHeaderObj.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeaderObj.PaymentDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeaderObj);
            _unitOfWork.Save();

            TempData["success"] = "Order Shipped Successfully";

            // Basically refreshs the page with the details from orderHeader
            // orderId is case sensitive because its used to find the obj 
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult CancelOrder()
        {
            // Get orderHeaderObj by its Id
            var orderHeaderObj = _unitOfWork.OrderHeader.Get(u=>u.Id == OrderVM.OrderHeader.Id);

            // Check payment status
            if(orderHeaderObj.PaymentStatus == SD.PaymentStatusApproved)
            {
                // Refund the user since they already paid
                // Get refund details 
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderObj.PaymentIntentId
                };

                // Refund the user
                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderObj.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                // Just cancel the payment process and the order
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderObj.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["success"] = "Order Canceled Successfully";

            // Basically refreshs the page with the details from orderHeader
            // orderId is case sensitive because its used to find the obj 
            return RedirectToAction(nameof(Details), new {orderId = OrderVM.OrderHeader.Id});
        }

        #region Company Payment 
        [ActionName("Details")]
        [HttpPost]
        public IActionResult PAY_NOW()
        {
            // We want to get details from OrderHeader, as well as the user
            OrderVM.OrderHeader = _unitOfWork.OrderHeader.
                Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            // We want to get the orderDetails items and include the products too
            OrderVM.OrderDetails = _unitOfWork.OrderDetails.
                GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            // Stripe Logic 
            var domain = Request.Scheme + "://" + Request.Host.Value + "/"; // Find the domain 
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}", // If successfull, go to this url
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}", // If user cancels, go back to previous page
                LineItems = new List<SessionLineItemOptions>(), // Just the lsit of items
                Mode = "payment", // The mode type
            };

            // What to display in the stripe cart
            foreach (var item in OrderVM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions()
                {
                    PriceData = new SessionLineItemPriceDataOptions()
                    {
                        UnitAmount = (long)(item.Product.Price * 100), // to make it display as $20.50 instead of 2050
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Year + " " + item.Product.Make + " " + item.Product.Model
                        }
                    }, 
                    Quantity = 1 // Set the quantity to 1 since we only want one of each cars
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            // Get orderHeader details by its Id
            OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u=>u.Id == orderHeaderId);

            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                // This is an order by a company 
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            return View(orderHeaderId);
        }
        #endregion

        #region API Calls
        [HttpGet] // Don't forget about [HttpGet], it won't work if you do
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> objOrderHeaders;

            // Check the user role
            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                // Retrieve all the data from orderheader 
                // We want to include the user who made the order
                objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                // Get the user
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Get the orders the user ordered, not all orders
                objOrderHeaders = _unitOfWork.OrderHeader
                    .GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }

            // For filtering buttons
            switch (status)
            {
                case "pending":
                    objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }

            return Json(new { data = objOrderHeaders });
        }
        #endregion
    }
}
