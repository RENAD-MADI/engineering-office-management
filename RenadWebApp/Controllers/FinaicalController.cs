using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;
using RenadWebApp.Models.DataModel.FinaicalModels;

namespace RenadWebApp.Controllers
{
    /// <summary>Financial requests (claims) raised against a contract.</summary>
    public class FinaicalController : AppController
    {
        private readonly ApplicationDbContext _db;

        public FinaicalController(ApplicationDbContext db)
        {
            _db = db;
        }

        public ActionResult Index(string? search)
        {
            return View(_db.FinaicalRequests.Where(x => search == null || x.ContractNumber!.StartsWith(search)).ToList());
        }

        public async Task<ActionResult> IndexClient(int id)
        {
            var requests = await _db.ClientContract
                .Where(x => x.ClientId == id)
                .SelectMany(x => x.Contract.ContractFinaical)
                .Select(x => x.Finaical)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        public async Task<ActionResult> IndexContract(int id)
        {
            var requests = await _db.ContractFinaical
                .Where(x => x.ContractId == id)
                .Select(x => x.Finaical)
                .AsNoTracking()
                .ToListAsync();

            return View(requests);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(FinaicalRequest fina)
        {
            // The route id is the contract this request belongs to.
            var contract = await _db.Contracts.FindAsync(fina.Id);
            if (contract == null)
            {
                return ShowMessage("العرض غير موجود");
            }

            var request = new FinaicalRequest
            {
                FinaicalNumber = fina.FinaicalNumber,
                Titel = fina.Titel,
                ContractNumber = contract.ContractNumber,
                ClientName = fina.ClientName,
                Description = fina.Description,
                CreatedDate = (fina.CreatedDate ?? DateTime.Today).Date,
            };

            _db.FinaicalRequests.Add(request);
            await _db.SaveChangesAsync();

            _db.ContractFinaical.Add(new ContractFinaical { ContractId = contract.Id, FinaicalId = request.Id });
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int? id)
        {
            var request = await _db.FinaicalRequests.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (request == null)
            {
                return ShowMessage("المطالبة غير موجودة");
            }
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, FinaicalRequest finaical)
        {
            var request = await _db.FinaicalRequests.FindAsync(finaical.Id);
            if (request == null)
            {
                return ShowMessage("المطالبة غير موجودة");
            }

            request.Titel = finaical.Titel;
            request.ClientName = finaical.ClientName;
            request.Description = finaical.Description;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var request = await _db.FinaicalRequests.FindAsync(id);
            if (request == null)
            {
                return ShowMessage("المطالبة غير موجودة");
            }

            _db.ContractFinaical.RemoveRange(_db.ContractFinaical.Where(x => x.FinaicalId == id));
            _db.FinaicalRequests.Remove(request);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
