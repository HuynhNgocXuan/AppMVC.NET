using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webMVC.Models;
using webMVC.Models.Contact;

namespace webMVC.Areas.Contact.Controllers
{
    [Area("Contact")]
    [Route("Contacts/{action=Index}")]
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;

        [TempData]
        public string? StatusMessage { get; set; }



        public ContactController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Contacts!.ToListAsync());
        }

        // GET: Contact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactModel = await _context.Contacts!
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contactModel == null)
            {
                return NotFound();
            }

            return View(contactModel);
        }

        // GET: Contact/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Contact/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Email,Message,Phone")] ContactModel contactModel)
        {
            if (ModelState.IsValid)
            {
                contactModel.DateTimeSend = DateTime.Now;
                _context.Add(contactModel);
                await _context.SaveChangesAsync();
                StatusMessage = "Gửi thành công";
                return RedirectToAction(nameof(Index), "Home");
            }
            return View(contactModel);
        }




        // GET: Contact/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactModel = await _context.Contacts!
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contactModel == null)
            {
                return NotFound();
            }

            return View(contactModel);
        }

        // POST: Contact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contactModel = await _context.Contacts!.FindAsync(id);
            if (contactModel != null)
            {
                _context.Contacts.Remove(contactModel);
                StatusMessage = "Xóa thành công";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // private bool ContactModelExists(int id)
        // {
        //     return _context.Contacts.Any(e => e.Id == id);
        // }
    }
}
