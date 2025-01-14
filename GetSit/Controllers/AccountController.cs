﻿using GetSit.Data.ViewModels;
using GetSit.Data.enums;
using GetSit.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using GetSit.Data;
using Microsoft.Win32;
using GetSit.Data.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using GetSit.Common;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Routing;
using GetSit.Data.Services;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace GetSit.Controllers
{
    public class AccountController : Controller
    {
        const string TokenSession = "TokenId";
        #region Dependencies
        AppDBcontext _context;
        private readonly IUserManager _userManager;
        private readonly ICustomerService _customerService;
        private readonly ISpaceEmployeeService _spaceEmployeeService;
        private readonly ISystemAdminService _adminSerivce;
        /* check if the entered password while logging in matches the stored password in database*/
        bool VerifyPassword(string encodedPassword, string password)
        {

            return (PasswordHashing.Decode (encodedPassword) == password);
        }
        bool PresirvedEmail(string email)
        {
            return (_customerService.GetByEmail(email) != null ||
                    _spaceEmployeeService.GetByEmail(email) != null ||
                    _adminSerivce.GetByEmail(email) != null
                    );
        }
        public AccountController(AppDBcontext context, IUserManager userManager,ICustomerService customerService, ISpaceEmployeeService spaceEmployeeService, ISystemAdminService adminSerivce)
        {
            _context = context;
            _userManager = userManager;
            _customerService = customerService;
            _spaceEmployeeService = spaceEmployeeService;
            _adminSerivce = adminSerivce;
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> LoginAsync(LoginVM login)
        {
            if (!ModelState.IsValid)
            {
                return View(login);
            }

            if(!PresirvedEmail(login.Email))
            {
                ModelState.AddModelError("Email", "Invalid email");
                return View(login);
            }
            
            switch (login.Role)//Which user role?
            {
                case UserRole.Admin:
                    var admin = _adminSerivce.GetByEmail(login.Email);
                    if (admin == null)
                    {
                        // Email not found in database
                        ModelState.AddModelError("Role", "Wrong Role");
                        return RedirectToAction("Index", "SystemAdmin");
                    }
                    if (admin.Registerd != true)
                    {
                        ModelState.AddModelError("Email", "Your account has not been varifed, please use the link sent to your email.");
                        return View(login);
                    }
                    if (!VerifyPassword(admin.Password, login.Password))
                    {
                        // Password is incorrect
                        ModelState.AddModelError("Password", "Invalid login attempt, incorrect password.");
                        return View(login);
                    }
                    _userManager.SignIn(HttpContext, admin);
                    return RedirectToAction("AdminProfile", "Account");
                    break;
                case UserRole.Provider:
                    var provider = _spaceEmployeeService.GetByEmail(login.Email);
                    if (provider == null)
                    {
                        // Email not found in database
                        ModelState.AddModelError("Role", "Wrong Role");
                        return View(login);
                    }
                    if (provider.Registerd != true)
                    {
                        ModelState.AddModelError("Email", "Your account has not been varifed, please use the link sent to your email.");
                        return View(login);
                    }
                    if (!VerifyPassword(provider.Password, login.Password))
                    {
                        // Password is incorrect
                        ModelState.AddModelError("Password", "Invalid login attempt, incorrect password.");
                        return View(login);
                    }
                    _userManager.SignIn(HttpContext, provider);
                    return RedirectToAction("Index", "SpaceManagement");
                    break;
                case UserRole.Customer:
        
                    var customer = _customerService.GetByEmail(login.Email);
                    if (customer == null)
                    {
                        // Email not found in database
                        ModelState.AddModelError("Role", "Wrong Role");
                            return View(login);
                        
                    }
                    if (!VerifyPassword(customer.Password, login.Password))
                    {
                        // Password is incorrect
                        ModelState.AddModelError("Password", "Invalid login attempt, incorrect password.");
                            return View(login);
                        
                    }
                    await _userManager.SignIn(HttpContext, customer);
                    return RedirectToAction("Index", "Customer");
                    break;
                default:
                    break;
            }
            return View();
        }
        [HttpGet]
        public IActionResult logout()
        {

            _userManager.SignOut(HttpContext);
            HttpContext.Response.Cookies.Delete("SpaceId");
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Register(RegisterVM? vm)
        {
            ModelState.Clear();
            if (vm.UserId != null)
            {
                return View(vm);
            }
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> RegisterProvider(int UID, UserRole Role, string token)
        {
            if (_context.Token.Where(t => t.Id == UID).FirstOrDefault() == null || Common.JwtTokenHelper.ValidateToken(token) == null)
            {
                return NotFound();
            }
            var UserIdStr = Common.JwtTokenHelper.ValidateToken(token);
            var UserId = 0;
            int.TryParse(UserIdStr, out UserId);
            var SpaceEmployee = await _spaceEmployeeService.GetByIdAsync(UserId);
            Common.SessoinHelper.saveObject(HttpContext, TokenSession, new { id = UID });
            return RedirectToAction("Register", new RegisterVM()
            {
                UserId = SpaceEmployee.Id,
                FirstName = SpaceEmployee.FirstName,
                LastName = SpaceEmployee.LastName,
                Email = SpaceEmployee.Email,
                PhoneNumber = SpaceEmployee.PhoneNumber,
                Birthdate = SpaceEmployee.Birthdate,
                Role = Role
            });
        }
        public async Task<IActionResult> RegisterAdmin(int UID, UserRole Role, string token)
        {
            if (_context.Token.Where(t => t.Id == UID).FirstOrDefault() == null || Common.JwtTokenHelper.ValidateToken(token) == null)
            {
                return NotFound();
            }
            var UserIdStr = Common.JwtTokenHelper.ValidateToken(token);
            var UserId = 0;
            int.TryParse(UserIdStr, out UserId);
            var SpaceAdmin = await _adminSerivce.GetByIdAsync(UserId);
            Common.SessoinHelper.saveObject(HttpContext, TokenSession, new { id = UID });
            return RedirectToAction("Register", new RegisterVM()
            {
                UserId = SpaceAdmin.Id,
                FirstName = SpaceAdmin.FirstName,
                LastName = SpaceAdmin.LastName,
                Email = SpaceAdmin.Email,
                PhoneNumber = SpaceAdmin.PhoneNumber,
                Birthdate = SpaceAdmin.Birthdate,
                Role = Role
            });
        }
        [HttpPost,ActionName("Register")]
        public async Task<IActionResult> RegisterPost(RegisterVM register)
        {
            if (!ModelState.IsValid)
            {
                return View(register);
            }

            /*check if the entered email in register is already in database*/
            if (PresirvedEmail(register.Email))
            {
                if (register.Role != UserRole.Admin && register.Role != UserRole.Provider)
                {
                    ModelState.AddModelError("Email", "This email already has an account.");
                    return View(register);
                }
            }
            HttpContext.Session.SetString("RegisterModel", JsonConvert.SerializeObject(register));
            var otpVm = new OTPVM();
            otpVm.Email = register.Email;
            otpVm.Phone = register.PhoneNumber;
            return RedirectToAction("EmailOTP", otpVm);//skip phone otp 
        }
        #region verification
        [HttpGet]
        public IActionResult PhoneOTP(OTPVM? otpVm)
        {
            if (otpVm is null)
                RedirectToAction("Register");
            OTPServices.SendPhoneOTP(HttpContext,otpVm.Phone);
            return View(otpVm);
        }
        [HttpPost]
        public async Task<IActionResult> PhoneOTPAsync(OTPVM otp)
        {
            if (!ModelState.IsValid)
            {
                return View(otp);
            }
            if (OTPServices.VerifyOTP(HttpContext, otp) == false)
            {
                ModelState.AddModelError("OTP", "InValid Code");
                return View(otp); 
            }
            
            return RedirectToAction("EmailOTP",otp);
        }
        [HttpGet]
        public IActionResult EmailOTP(OTPVM? otpVm)
        {
            if (otpVm is null)
                RedirectToAction("Register");
            OTPServices.SendEmailOTP(HttpContext, otpVm.Email);
            return View(otpVm);
        }
        [HttpPost]
        public async Task<IActionResult> EmailOTPAsync(OTPVM otp)
        {
            if (!ModelState.IsValid)
            {
                return View(otp);
            }
            if (OTPServices.VerifyOTP(HttpContext, otp) == false)
            {
                ModelState.AddModelError("OTP", "InValid Code");
                return View(otp);
            }
            /*Get User model from session*/
            var stringUser = HttpContext.Session.GetString("RegisterModel");
            var register = JsonConvert.DeserializeObject<RegisterVM>(stringUser) as RegisterVM;
            if (register is null)
                RedirectToAction("Register");
            if (register.Role == null)
                register.Role = UserRole.Customer;

            switch (register.Role)
            {
                case UserRole.Admin:
                    var admin = await _adminSerivce.GetByIdAsync((int)register.UserId);
                    admin.Id = (int)register.UserId;
                    admin.FirstName = register.FirstName;
                    admin.LastName = register.LastName;
                    admin.Email = register.Email;
                    admin.PhoneNumber = register.PhoneNumber;
                    admin.Birthdate = register.Birthdate;
                    admin.Password = PasswordHashing.Encode (register.Password);/*Here password should be hashed*/
                    admin.ProfilePictureUrl = Consts.userProfilePhotoHolder;
                    admin.Registerd = true;
                    
                    try
                    {
                        await _adminSerivce.UpdateAsync(admin.Id,admin);
                        //Expire Token
                        var tokenString = Common.SessoinHelper.getObject<Token>(HttpContext, TokenSession);
                        var token = _context.Token.Find(tokenString.Id);
                        if (token != null)
                        {
                            _context.Token.Remove(token);
                        }
                        _context.SaveChanges();

                       await _userManager.SignIn(HttpContext, admin);
                       return RedirectToAction("AdminProfile", "Account");
                    }
                    catch (Exception error)
                    {
                        return View(register);
                    }
                    break;
                case UserRole.Provider:
                    var provider = await _spaceEmployeeService.GetByIdAsync((int)register.UserId);
                    provider.FirstName = register.FirstName;
                    provider.LastName = register.LastName;
                    provider.Email = register.Email;
                    provider.PhoneNumber = register.PhoneNumber;
                    provider.Birthdate = register.Birthdate;
                    provider.Password = PasswordHashing.Encode(register.Password);/*Here password should be hashed*/
                    provider.ProfilePictureUrl = Consts.userProfilePhotoHolder;
                    provider.Registerd = true;
                    try
                    {
                        await _spaceEmployeeService.UpdateAsync(provider.Id,provider);
                        //Expire Token
                        var tokenString = Common.SessoinHelper.getObject<Token>(HttpContext, TokenSession);
                        var token = _context.Token.Find(tokenString.Id);
                        if(token != null)
                        {
                            _context.Token.Remove(token);
                        }
                        _context.SaveChanges();

                        await _userManager.SignIn(HttpContext, provider);
                        return RedirectToAction("Index", "SpaceManagement");
                    }
                    catch (Exception error)
                    {
                        return RedirectToAction("Register");
                    }
                    break;
                case UserRole.Customer:
                    var customer = new Customer()
                    {
                        FirstName = register.FirstName,
                        LastName = register.LastName,
                        Email = register.Email,
                        PhoneNumber = register.PhoneNumber,
                        CustomerType=CustomerType.Registered,
                        Birthdate=register.Birthdate,
                        Password = PasswordHashing.Encode(register.Password),/*Here password should be hashed*/
                        ProfilePictureUrl= Consts.userProfilePhotoHolder
            };

                    try
                    {
                        await _customerService.AddAsync(customer);
                        await _userManager.SignIn(HttpContext, customer);
                        return RedirectToAction("CustomerProfile","Account");
                    }
                    catch (Exception error)
                    {
                        return View(register);
                    }
                    break;
                default:
                    return View(register);
                    break;
            }
            return RedirectToAction("Register");
        }
        #endregion

        [Authorize(Roles ="Customer , Provider , Admin")]
        public async Task<IActionResult> ProfileAsync()
        {
            var userRole = _userManager.GetUserRole(HttpContext);
            if (userRole == null)
                return NotFound();

            switch (userRole)
            {
                case "Customer":
                    return RedirectToAction("Index", "Customer");
                    break;
                case "Admin":
                    return RedirectToAction("Index", "SystemAdmin");
                    break;
                case "Provider":
                    return RedirectToAction("Index", "SpaceManagement");
                    break;
                default:
                    return NotFound();
            }
            return RedirectToAction("Index", "SystemAdmin");
        }
        [Authorize(Roles = "Admin")]//error enum must be used
        public async Task<IActionResult> AdminProfileAsync()
        {
            return RedirectToAction("Index", "SystemAdmin");
        }
        [Authorize(Roles = "Customer")]//error enum must be used
        public async Task<IActionResult> CustomerProfileAsync()
        {
            return RedirectToAction("Index", "Customer");
        }
        [Authorize(Roles = "Provider")]//error enum must be used
        public async Task<IActionResult> ProviderProfileAsync()
        {
            var user =(SpaceEmployee) await _userManager.GetCurrentUserAsync(HttpContext);
            return View(user);
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        
    }
}
