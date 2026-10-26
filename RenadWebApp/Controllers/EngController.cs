using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;
using RenadWebApp.Models.DataModel;
using RenadWebApp.Models.ViewModels;

namespace RenadWebApp.Controllers
{
    /// <summary>Engineers and the share they are owed from each contract.</summary>
    public class EngController : AppController
    {
        private readonly ApplicationDbContext _db;

        public EngController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ActionResult> Index()
        {
            // Totals are per engineer (previously every card showed the office-wide sum).
            var engineers = await _db.Eng
                .Select(x => new EngSummaryViewModel
                {
                    Id = x.Id,
                    EngName = x.EngName,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    Offices = x.Offices,
                    TotalRatioForEng = x.EngContract.Sum(c => c.Contract.TotalRatioForEng ?? 0),
                    RestOfEngShow = x.EngContract.Sum(c => c.Contract.RestOfEngShow ?? 0),
                })
                .ToListAsync();

            return View(engineers);
        }

        public async Task<ActionResult> Details(int id)
        {
            var contracts = await _db.EngContract
                .Where(x => x.EngId == id)
                .Select(x => new EngContractViewModel
                {
                    EngName = x.Eng.EngName,
                    ContractNumber = x.Contract.ContractNumber,
                    ProjectName = x.Contract.ProjectName,
                    TotalOfContract = x.Contract.TotalOfContract,
                    TotalAmount = x.Contract.TotalAmount,
                    RestOfContractShow = x.Contract.RestOfContractShow,
                    RatioForEng = x.Contract.RatioForEng,
                    TotalRatioForEng = x.Contract.TotalRatioForEng,
                    RestOfEngShow = x.Contract.RestOfEngShow,
                })
                .ToListAsync();

            return View(contracts);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EngModel eng)
        {
            if (await _db.Eng.AnyAsync(x => x.EngName == eng.EngName))
            {
                return ShowMessage("اسم المهندس موجود مسبقاً");
            }

            var engModel = new EngModel
            {
                EngName = eng.EngName,
                Email = eng.Email,
                PhoneNumber = eng.PhoneNumber,
                CompOrFree = eng.CompOrFree,
                Offices = eng.Offices,
                Connection = eng.Connection,
                Meeting = eng.Meeting,
                TimeOfMeeting = eng.TimeOfMeeting,
                LastCommunication = eng.LastCommunication,
                Created = eng.Created ?? DateTime.Today,
            };

            _db.Eng.Add(engModel);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int? id)
        {
            var eng = await _db.Eng.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (eng == null)
            {
                return ShowMessage("المهندس غير موجود");
            }
            return View(eng);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, EngModel engModel)
        {
            var eng = await _db.Eng.FindAsync(engModel.Id);
            if (eng == null)
            {
                return ShowMessage("المهندس غير موجود");
            }
            // The old code ran this check after saving, so every successful edit showed "already exists".
            if (await _db.Eng.AnyAsync(x => x.EngName == engModel.EngName && x.Id != eng.Id))
            {
                return ShowMessage("اسم المهندس موجود مسبقاً");
            }

            // Contracts reference the engineer by name, so keep them in sync on rename.
            if (eng.EngName != engModel.EngName)
            {
                var contracts = await _db.EngContract.Where(x => x.EngId == eng.Id).Select(x => x.Contract).ToListAsync();
                contracts.ForEach(c => c.ProjectBy = engModel.EngName);
            }

            eng.EngName = engModel.EngName;
            eng.PhoneNumber = engModel.PhoneNumber;
            eng.Offices = engModel.Offices;
            eng.Connection = engModel.Connection;
            eng.Meeting = engModel.Meeting;
            eng.TimeOfMeeting = engModel.TimeOfMeeting;
            eng.LastCommunication = engModel.LastCommunication;
            eng.CompOrFree = engModel.CompOrFree;
            eng.Email = engModel.Email;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var eng = await _db.Eng.FindAsync(id);
            if (eng == null)
            {
                return ShowMessage("المهندس غير موجود");
            }
            if (await _db.EngContract.AnyAsync(x => x.EngId == id))
            {
                return ShowMessage("لا يمكن حذف مهندس مرتبط بعقود");
            }

            _db.Eng.Remove(eng);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
