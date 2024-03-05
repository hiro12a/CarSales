using CarSales.Models;
using CarSales.Models.ViewModels;
using CarSales.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CarSales.DataAccess.Repository.IRepository;

namespace CarSales.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController : Controller
    {
        // Call from 
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _signInManager = signInManager;
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult Index()
        {
            return View();
        }

        // Demo Users
        public async Task<IActionResult> DemoCustomer(LoginVM loginVM)
        {
            await _signInManager.PasswordSignInAsync("customer@carsales.com", "Customer@123", loginVM.RememberMe, lockoutOnFailure: false);

            return RedirectToAction("Index", "Home", new { Area = "Customer" });
        }
        public async Task<IActionResult> DemoCompany(LoginVM loginVM)
        {
            await _signInManager.PasswordSignInAsync("company@carsales.com", "Company@123", loginVM.RememberMe, lockoutOnFailure: false);

            return RedirectToAction("Index", "Home", new { Area = "Customer" });
        }
        public async Task<IActionResult> DemoAdmin(LoginVM loginVM)
        {
            await _signInManager.PasswordSignInAsync("admin@carsales.com", "Adminuser@123", loginVM.RememberMe, lockoutOnFailure: false);

            return RedirectToAction("Index", "Home", new { Area = "Customer" });
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home", new { Area = "Customer"});
        }
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            LoginVM loginVM = new()
            {
                RedirectUrl = returnUrl
            };

            return View(loginVM);
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(loginVM.Email, loginVM.Password, loginVM.RememberMe, lockoutOnFailure:false);


                if (result.Succeeded)
                {
                    if (string.IsNullOrEmpty(loginVM.RedirectUrl))
                    {
                        return RedirectToAction("Index", "Home", new { Area = "Customer" });
                    }
                    else
                    {
                        // User Localredirect so it doesn't redirect to a malicious page
                        return LocalRedirect(loginVM.RedirectUrl);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid Login Attempt");
                }
            }

            return View(loginVM);
        }

        public IActionResult Register()
        {
            // .Wait is a simplified version of await async
            if (!_roleManager.RoleExistsAsync(SD.Role_Admin).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).Wait();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).Wait();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).Wait();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).Wait();
            }

            RegisterVM register = new()
            {
                RoleList = _roleManager.Roles.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Name
                }), 
                CompanyList = _unitOfWork.Company.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name, 
                    Value = x.Id.ToString()
                })
            };

            return View(register);
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM registerVM)
        {
            // Get User Data
            ApplicationUser user = new()
            {
                Name = registerVM.Name,
                Email = registerVM.Email,
                StreetAddress = registerVM.StreetAddress,
                PhoneNumber = registerVM.PhoneNumber,
                City = registerVM.City,
                State = registerVM.State,
                PostalCode = registerVM.PostalCode,
                UserName = registerVM.Email,
                NormalizedEmail = registerVM.Email.ToUpper(),
                EmailConfirmed = true
            };

            // Create the user
            var result = await _userManager.CreateAsync(user, registerVM.Password);
            if (result.Succeeded)
            {
                // Assign role to user
                if (!string.IsNullOrEmpty(registerVM.Role))
                {
                    await _userManager.AddToRoleAsync(user, registerVM.Role);
                }
                else
                {
                    // Assign Customer Role to User if they don't select it
                    await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                }

                // Sign in the User automatically
                await _signInManager.SignInAsync(user, isPersistent: false);

                if (string.IsNullOrEmpty(registerVM.RedirectUrl))
                {
                    return RedirectToAction("Index", "Home", new { Area = "Customer" });
                }
                else
                {
                    // User Localredirect so it doesn't redirect to a malicious page
                    return LocalRedirect(registerVM.RedirectUrl);
                }              
            }

            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            registerVM.RoleList = _roleManager.Roles.Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Name
            });

            return View(registerVM);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult RoleManagment(string userId)
        {
            // Populate RoleVM
            RoleManagerVM RoleVM = new RoleManagerVM()
            {
                // Get the user list along with the company list 
                ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId, includeProperties: "Company"),
                RoleList = _roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _unitOfWork.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };

            // Assign the role to the user
            // GetRolesAsync will get roles assigned to a user
            RoleVM.ApplicationUser.Role = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == userId))
                    .GetAwaiter().GetResult().FirstOrDefault();
            return View(RoleVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)] // Only admins or employees can work on this
        public IActionResult RoleManagment(RoleManagerVM RoleVM)
        {
            // Get the old role
            string oldRole = _userManager.GetRolesAsync(_unitOfWork.ApplicationUser.Get(u => u.Id == RoleVM.ApplicationUser.Id))
                    .GetAwaiter().GetResult().FirstOrDefault();

            // Get the user by userId
            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == RoleVM.ApplicationUser.Id);

            // Check if the new role is not the same as the old role
            if (!(RoleVM.ApplicationUser.Role == oldRole))
            {
                // If user clicks on the company role, assign the user to a company role and let them pick a company
                if (RoleVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = RoleVM.ApplicationUser.CompanyId;
                }
                // If user is already in the company role, don't update the role
                if (oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();

                // This removes the old role 
                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                // This adds the new role to the user
                _userManager.AddToRoleAsync(applicationUser, RoleVM.ApplicationUser.Role).GetAwaiter().GetResult();

            }
            // This is for updating the company the user belogns to
            // If user role is already a company role but the company they select is different, then update the company
            else if (oldRole == SD.Role_Company && applicationUser.CompanyId != RoleVM.ApplicationUser.CompanyId)
            {
                applicationUser.CompanyId = RoleVM.ApplicationUser.CompanyId; // Change the company

                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();
            }
            // Update everything else
            else{
                applicationUser.Name = RoleVM.ApplicationUser.Name;
                applicationUser.PhoneNumber = RoleVM.ApplicationUser.PhoneNumber;
                applicationUser.StreetAddress = RoleVM.ApplicationUser.StreetAddress;
                applicationUser.City = RoleVM.ApplicationUser.City;
                applicationUser.State = RoleVM.ApplicationUser.State;
                applicationUser.PostalCode = RoleVM.ApplicationUser.PostalCode;
                _unitOfWork.ApplicationUser.Update(applicationUser);
                _unitOfWork.Save();
            }

            return RedirectToAction("Index");
        }


        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            // Get a list of all the users
            List<ApplicationUser> objUserList = _unitOfWork.ApplicationUser.GetAll(includeProperties: "Company").ToList();

            // Check what type of user it is
            foreach(var user in objUserList)
            {
                // Get the user Role
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
                
                // If user not a company user, just set the name to nothing
                if(user.Company == null)
                {
                    user.Company = new() { Name = "" };
                }
            }

            // return a list of users
            return Json(new { data = objUserList });
        }

        // Locking and unlocking accoutns
        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id) // FromBody gets the input from the web
        {

            // Gets the user
            var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);

            // If user is not valid
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                // user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _unitOfWork.ApplicationUser.Update(objFromDb);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Operation Successful" });
        }

        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if(user == null)
            {
                return Json(new { success = false, message = "User Not Found" });
            }
            else
            {
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                return View("Index");
            }
        }
        #endregion
    }
}
