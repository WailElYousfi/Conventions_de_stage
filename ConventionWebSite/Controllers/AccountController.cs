using ConventionWebSite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace ConventionWebSite.Controllers
{
    public class AccountController : Controller
    {
        private DataContext db = new DataContext();

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Conventions");
            }
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(new LoginModel());
            }
            else
                return RedirectToAction("Index", "Conventions");

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            // For simplicity, this example uses forms authentication with credentials stored in web.config.
            // Your application can use any authentication method you choose (eg Active Directory, custom database etc).
            // There are no restrictions on the method of authentication.
            User user = db.users.Where(u => u.Email == model.UserName).SingleOrDefault();
            if (ModelState.IsValid && user != null && user.Password == model.Password)
            {
                FormsAuthentication.SetAuthCookie(model.UserName, false);   // false :cad que l’authentiﬁcation n’aura qu’une duree de vie limitee a la session
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError("", "Informations invalides.");
            return View(model);
        }

        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/Account/Login");
        }

        
    }
}