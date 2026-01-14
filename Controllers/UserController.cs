using SDA_Project.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace SDA_Project.Controllers
{
    public class UserController : Controller
    {
        private readonly dbemarketEntities db = new dbemarketEntities();

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(tbl_user user)
        {
            if (ModelState.IsValid)
            {
                if (db.tbl_user.Any(u => u.u_name == user.u_name || u.u_contact == user.u_contact))
                {
                    ViewBag.ErrorMessage = "Username or Contact already exists.";
                    return View();
                }

                db.tbl_user.Add(user);
                db.SaveChanges();
                ViewBag.SuccessMessage = "Registration successful!";
                return RedirectToAction("UserLogin");
            }

            ViewBag.ErrorMessage = "Please fill in all required fields.";
            return View();
        }

        [HttpGet]
        public ActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UserLogin(tbl_user user)
        {
            var loggedInUser = db.tbl_user
                .FirstOrDefault(u => u.u_name == user.u_name && u.u_password == user.u_password);

            if (loggedInUser != null)
            {
                Session["user_id"] = loggedInUser.u_id.ToString();
                Session["user_name"] = loggedInUser.u_name;

                return RedirectToAction("ManageProducts");
            }

            ViewBag.ErrorMessage = "Invalid username or password.";
            return View();
        }

        [HttpGet]
        public ActionResult ManageProducts()
        {
            if (Session["user_id"] == null)
                return RedirectToAction("UserLogin");

            int userId = int.Parse(Session["user_id"].ToString());
            ViewBag.Categories = db.tbl_category.ToList();

            var products = db.tbl_product
                .Where(p => p.pro_fk_user == userId)
                .Include(p => p.tbl_category)
                .ToList();

            return View(products);
        }

        [HttpPost]
        public ActionResult ManageProducts(string actionType, int? pro_id, string pro_name, int? pro_price, int? pro_fk_cat, HttpPostedFileBase pro_image, string pro_description)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("UserLogin");

            int userId = int.Parse(Session["user_id"].ToString());
            ViewBag.Categories = db.tbl_category.ToList();

            try
            {
                if (actionType == "SaveProduct")
                {
                    if (string.IsNullOrEmpty(pro_name) || pro_price <= 0 || pro_fk_cat == null || string.IsNullOrEmpty(pro_description))
                    {
                        ViewBag.ErrorMessage = "All fields, including description, are required.";
                        return View(db.tbl_product.Where(p => p.pro_fk_user == userId).ToList());
                    }

                    tbl_product product;
                    if (pro_id.HasValue) 
                    {
                        product = db.tbl_product.FirstOrDefault(p => p.pro_id == pro_id && p.pro_fk_user == userId);
                        if (product == null)
                        {
                            ViewBag.ErrorMessage = "Product not found.";
                            return View(db.tbl_product.Where(p => p.pro_fk_user == userId).ToList());
                        }
                    }
                    else
                    {
                        product = new tbl_product { pro_fk_user = userId };
                        db.tbl_product.Add(product);
                    }

                    product.pro_name = pro_name;
                    product.pro_price = pro_price.Value;
                    product.pro_fk_cat = pro_fk_cat.Value;
                    product.pro_des = pro_description;

                    if (pro_image != null && pro_image.ContentLength > 0)
                    {
                        string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                        string fileExtension = Path.GetExtension(pro_image.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ViewBag.ErrorMessage = "Only JPG, JPEG, and PNG images are allowed.";
                            return View(db.tbl_product.Where(p => p.pro_fk_user == userId).ToList());
                        }

                        string path = Server.MapPath("~/Uploads/");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);

                        string fileName = Guid.NewGuid() + fileExtension;
                        string fullPath = Path.Combine(path, fileName);
                        pro_image.SaveAs(fullPath);

                        product.pro_image = "~/Uploads/" + fileName;
                    }

                    db.SaveChanges();
                    ViewBag.SuccessMessage = "Product saved successfully!";
                }
                else if (actionType == "DeleteProduct" && pro_id.HasValue) 
                {
                    var product = db.tbl_product.FirstOrDefault(p => p.pro_id == pro_id && p.pro_fk_user == userId);
                    if (product != null)
                    {
                        db.tbl_product.Remove(product);
                        db.SaveChanges();
                        ViewBag.SuccessMessage = "Product deleted successfully!";
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "Product not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
            }
            var products = db.tbl_product.Where(p => p.pro_fk_user == userId).ToList();
            return View(products);
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("UserLogin");
        }
        [HttpGet]
        public ActionResult EditUser()
        {
            if (Session["user_id"] == null)
                return RedirectToAction("UserLogin");

            int userId = int.Parse(Session["user_id"].ToString());
            var user = db.tbl_user.Find(userId);

            if (user == null)
                return RedirectToAction("UserLogin");

            return View(user);
        }

        [HttpPost]
        public ActionResult EditUser(tbl_user updatedUser)
        {
            if (Session["user_id"] == null)
                return RedirectToAction("UserLogin");

            int userId = int.Parse(Session["user_id"].ToString());
            var user = db.tbl_user.Find(userId);

            if (user == null)
            {
                ViewBag.ErrorMessage = "User not found.";
                return RedirectToAction("UserLogin");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    user.u_name = updatedUser.u_name;
                    user.u_email = updatedUser.u_email;
                    user.u_contact = updatedUser.u_contact;
                    user.u_password = updatedUser.u_password; 

                    db.SaveChanges();
                    ViewBag.SuccessMessage = "Details updated successfully!";
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                }
            }

            return View(user); 
        }

    }
}
