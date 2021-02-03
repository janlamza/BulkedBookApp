﻿using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitofWork;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;

        public ShoppingCartVm ShoppingCartVm { get; set; }
        public CartController(IUnitOfWork unitofWork, IEmailSender emailSender, UserManager<IdentityUser> userManager)
        {
            _unitofWork = unitofWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVm = new ShoppingCartVm()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties:"Product")
            };
            ShoppingCartVm.OrderHeader.OrderTotal = 0;
            ShoppingCartVm.OrderHeader.ApplicationUser = _unitofWork.ApplicationUser
                .GetFirstOrDefault(u =>u.Id == claim.Value, includeProperties:"Company");

            foreach(var list in ShoppingCartVm.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price,
                        list.Product.Price50, list.Product.Price100);
                ShoppingCartVm.OrderHeader.OrderTotal += (list.Price * list.Count);
                list.Product.Description = SD.ConvertToRawHtml(list.Product.Description);
                if (list.Product.Description.Length > 100)
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";
                }
            }
            return View(ShoppingCartVm);
        }

        [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _unitofWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if(user == null)
            {
                ModelState.AddModelError(string.Empty, "Verification email is empty!");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here to pay with credit card hihi</a>.");

            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
            return RedirectToAction("Index");

        }



        public IActionResult plus(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId, includeProperties:"Product");
            cart.Count += 1;
            cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
            _unitofWork.Save();
            return RedirectToAction(nameof(Index));
        }




        public IActionResult minus(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");

            if(cart.Count == 1)
            {
                var count = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
                _unitofWork.ShoppingCart.Remove(cart);
                _unitofWork.Save();
                HttpContext.Session.SetInt32(SD.ssShopingCart, count - 1);
            }
            else
            {
                cart.Count -= 1;
                cart.Price = SD.GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50, cart.Product.Price100);
                _unitofWork.Save();
            }
            return RedirectToAction(nameof(Index));
        }





        public IActionResult remove(int cartId)
        {
            var cart = _unitofWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId, includeProperties: "Product");


                var count = _unitofWork.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
                _unitofWork.ShoppingCart.Remove(cart);
                _unitofWork.Save();
                HttpContext.Session.SetInt32(SD.ssShopingCart, count - 1);
           
           

            return RedirectToAction(nameof(Index));
        }


    }
}
