using CarSales.DataAccess.Repository.IRepository;
using CarSales.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarSales.Web.ViewComponents
{
	// This whole script is used for displaying the number on the cart icon
	public class ShoppingCartViewComponent : ViewComponent 
	{
		private readonly IUnitOfWork _unitOfWork;
		public ShoppingCartViewComponent(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			// Get the logged in user
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

			// If user is logged in 
			if (claim != null)
			{

				if (HttpContext.Session.GetInt32(SD.SessionCart) == null)
				{
					HttpContext.Session.SetInt32(SD.SessionCart,
					_unitOfWork.ShoppingCart.GetAll(u => u.UserId == claim.Value).Count());
				}

				return View(HttpContext.Session.GetInt32(SD.SessionCart));
			}
			else // Clear the cart if user is not logged in
			{
				HttpContext.Session.Clear();
				return View(0);
			}
		}
	}
}
