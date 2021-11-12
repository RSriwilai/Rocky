using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rocky.Data;
using Rocky.Models;
using Rocky.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }


        [HttpGet]
        public IActionResult Index()
        {
            IEnumerable<Product> objList = _db.Product.Include(u => u.Category).Include(u => u.ApplicationType);

            //foreach(var obj in objList)
            //{
            //    obj.Category = _db.Category.FirstOrDefault(u => u.Id == obj.CategoryId);
            //    obj.ApplicationType = _db.ApplicationType.FirstOrDefault(u => u.Id == obj.ApplicationTypeId);
            //}

            return View(objList);
        }

        //GET - Upsert
        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            //IEnumerable<SelectListItem> CategoryDropDown = _db.Category.Select(i => new SelectListItem
            //{
            //    Text = i.Name,
            //    Value =i.Id.ToString()
            //});

            //ViewBag.CategoryDropDown = CategoryDropDown;

            //Product product = new Product();

            ProductViewModel productViewModel = new ProductViewModel()

            {
                Product = new Product(),
                CategorySelectList = _db.Category.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                })
            };

            if (id == null)
            {
                //this is for create
                return View(productViewModel);
            }
            else
            {
                //this is for edit
                productViewModel.Product = _db.Product.Find(id);
                if(productViewModel.Product == null)
                {
                    return NotFound();
                }
                return View(productViewModel);
            }
        }

        //POST - Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel productViewModel)
        {
            if(ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;

                if(productViewModel.Product.Id == 0)
                {
                    //Creating
                    string upload = webRootPath + WC.ImagePath;
                    string filename = Guid.NewGuid().ToString();
                    string extension = Path.GetExtension(files[0].FileName);

                    using (var filestream = new FileStream(Path.Combine(upload, filename + extension), FileMode.Create))
                    {
                        files[0].CopyTo(filestream);
                    }
                    productViewModel.Product.Image = filename + extension;

                    _db.Product.Add(productViewModel.Product);
                    
                } 
                else
                {
                    //Updating - Edit
                    var objFromDb = _db.Product.AsNoTracking().FirstOrDefault(u => u.Id == productViewModel.Product.Id);

                    if(files.Count > 0)
                    {
                        string upload = webRootPath + WC.ImagePath;
                        string filename = Guid.NewGuid().ToString();
                        string extension = Path.GetExtension(files[0].FileName);

                        var oldFile = Path.Combine(upload, objFromDb.Image);

                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }

                        using (var filestream = new FileStream(Path.Combine(upload, filename + extension), FileMode.Create))
                        {
                            files[0].CopyTo(filestream);
                        }

                        productViewModel.Product.Image = filename + extension;
                    }
                    else
                    {
                        productViewModel.Product.Image = objFromDb.Image;
                    }
                    _db.Product.Update(productViewModel.Product);
                }
                _db.SaveChanges();
                return RedirectToAction("Index");

            }
            productViewModel.CategorySelectList = _db.Category.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });
            productViewModel.ApplicationTypeSelectList = _db.ApplicationType.Select(i => new SelectListItem
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });
            return View(productViewModel);
        }



        //GET -DELETE
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product product = _db.Product.Include(u => u.Category).Include(u => u.ApplicationType).FirstOrDefault(u => u.Id == id);
            //product.Category = _db.Category.Find(product.CategoryId);
           
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        //POST - DELETE
        [HttpPost,ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int? id)
        {
            var obj = _db.Product.Find(id);
            if(obj == null)
            {
                return NotFound();
            }

            string upload = _webHostEnvironment.WebRootPath + WC.ImagePath;

            var oldFile = Path.Combine(upload, obj.Image);

            if (System.IO.File.Exists(oldFile))
            {
                System.IO.File.Delete(oldFile);
            }
            _db.Product.Remove(obj);
            _db.SaveChanges();
            return RedirectToAction("Index");

        }


    }
}
