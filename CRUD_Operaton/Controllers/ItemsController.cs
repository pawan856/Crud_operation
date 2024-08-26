using CRUD_Operaton.Data;
using CRUD_Operaton.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rotativa.AspNetCore;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using iText.Commons.Actions.Contexts;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout;


namespace CRUD_Operaton.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConverter _converter;
        public ItemsController(ApplicationDbContext context, IConverter converter)
        {
            _context = context;
            _converter = converter;
        }

        // GET: Items
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            // ViewData to store current sort order and search string for persistence in the view
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CurrentFilter"] = searchString;

            // If the search string changes, reset to the first page
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            // Fetch the items from the database
            var items = from i in _context.Items select i;

            // Search functionality
            if (!string.IsNullOrEmpty(searchString))
            {
                items = items.Where(i => i.Name.Contains(searchString) || i.Description.Contains(searchString));
            }

            // Sorting functionality
            switch (sortOrder)
            {
                case "name_desc":
                    items = items.OrderByDescending(i => i.Name);
                    break;
                default:
                    items = items.OrderBy(i => i.Name);
                    break;
            }

            // Define the page size (you can change it as needed)
            int pageSize = 5;

            // Return the paginated list of items to the view
            return View(await PaginatedList<Item>.CreateAsync(items.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Items/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Items/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] Item item)
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Items/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Items/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Item item)
        {
            if (id != item.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }
        public IActionResult ExportCSV()
        {
            var items = _context.Items.ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,Description"); // CSV Header

            foreach (var item in items)
            {
                csv.AppendLine($"{item.Id},{item.Name},{item.Description}");
            }

            byte[] buffer = Encoding.ASCII.GetBytes(csv.ToString());
            return File(buffer, "text/csv", "Items.csv");
        }

        //public IActionResult ExportPDF()
        //{
        //    var items = _context.Items.ToList();
        //    return new ViewAsPdf("Index", items)
        //    {
        //        FileName = "Items.pdf"
        //    };
        //}


        // GET: Items/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Items
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Items/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }

        public IActionResult ExportPDF()
        {

            var items = _context.Items.ToList();

            var pdfDocument = new HtmlToPdfDocument
            {
                GlobalSettings = {
                DocumentTitle = "Items Report",
                PaperSize = PaperKind.A4
            },
                Objects = {
                new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = GenerateHtmlContent(items),
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
            };

            var pdfBytes = _converter.Convert(pdfDocument);
            return File(pdfBytes, "application/pdf", "Items.pdf");
        }

        private string GenerateHtmlContent(IEnumerable<Item> items)
        {
            var sb = new StringBuilder();
            sb.Append("<html><body>");
            sb.Append("<h1>Items Report</h1>");
            sb.Append("<table border='1'><tr><th>Id</th><th>Name</th><th>Description</th></tr>");

            foreach (var item in items)
            {
                sb.Append($"<tr><td>{item.Id}</td><td>{item.Name}</td><td>{item.Description}</td></tr>");
            }

            sb.Append("</table></body></html>");
            return sb.ToString();
        }
    }
}