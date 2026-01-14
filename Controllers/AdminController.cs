using SDA_Project.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace SDA_Project.Controllers
{
    public class AdminController : Controller
    {
        private dbemarketEntities db = new dbemarketEntities();

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(tbl_admin avm)
        {
            var admin = db.tbl_admin
                          .FirstOrDefault(x => x.ad_username == avm.ad_username && x.ad_password == avm.ad_password);

            if (admin != null)
            {
                Session["ad_id"] = admin.ad_id.ToString();
                return RedirectToAction("ManageCategories");
            }

            ViewBag.ErrorMessage = "Invalid Username or Password.";
            return View();
        }

        public ActionResult ManageCategories()
        {
            if (Session["ad_id"] == null)
                return RedirectToAction("Login");

            int adminId = int.Parse(Session["ad_id"].ToString());
            var categories = db.tbl_category.Where(c => c.cat_fk_ad == adminId).ToList();

            return View(categories);
        }

        [HttpPost]
        public ActionResult ManageCategories(string cat_name, HttpPostedFileBase cat_image)
        {
            if (Session["ad_id"] == null)
                return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(cat_name))
            {
                ModelState.AddModelError("", "Category name is required.");
            }
            else if (cat_image == null || cat_image.ContentLength == 0)
            {
                ModelState.AddModelError("", "Please upload a valid image file.");
            }
            else
            {
                try
                {
                    string savedPath = SaveImage(cat_image);

                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        var newCategory = new tbl_category
                        {
                            cat_name = cat_name,
                            cat_image = savedPath,
                            cat_fk_ad = int.Parse(Session["ad_id"].ToString())
                        };

                        db.tbl_category.Add(newCategory);
                        db.SaveChanges();

                        ViewBag.SuccessMessage = "Category added successfully!";
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to save the image. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }

            int adminId = int.Parse(Session["ad_id"].ToString());
            var categories = db.tbl_category.Where(c => c.cat_fk_ad == adminId).ToList();

            return View(categories);
        }

        [HttpPost]
        public ActionResult DeleteCategory(int id)
        {
            if (Session["ad_id"] == null)
                return RedirectToAction("Login");
            try
            {
                var category = db.tbl_category.Find(id);
                if (category != null)
                {
                    string imagePath = Server.MapPath(category.cat_image);
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);

                    db.tbl_category.Remove(category);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Category deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Category not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            return RedirectToAction("ManageCategories");
        }
        private string SaveImage(HttpPostedFileBase file)
        {
            try
            {
                string uploadFolder = Server.MapPath("~/Uploads/");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);
                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string fullPath = Path.Combine(uploadFolder, uniqueFileName);
                file.SaveAs(fullPath);
                return "~/Uploads/" + uniqueFileName; 
            }
            catch
            {
                return null; 
            }
        }

        public ActionResult ManageUserProducts()
        {
            if (Session["ad_id"] == null)
                return RedirectToAction("Login");

            var productsWithUsers = db.tbl_product
                .Include("tbl_user") 
                .Select(p => new ProductWithUserViewModel
                {
                    Product = p,
                    User = p.tbl_user
                })
                .ToList();
            ViewBag.Categories = db.tbl_category.ToList();
            return View(productsWithUsers);
        }

        [HttpPost]
        public ActionResult AssignCategoryToProduct(int pro_id, int pro_fk_cat)
        {
            if (Session["ad_id"] == null)
                return RedirectToAction("Login");

            try
            {
                var product = db.tbl_product.Find(pro_id);
                if (product != null)
                {
                    product.pro_fk_cat = pro_fk_cat;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Product category updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("ManageUserProducts");
        }
        public class ProductWithUserViewModel
        {
            public tbl_product Product { get; set; }
            public tbl_user User { get; set; }
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
