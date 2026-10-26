using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;
using RenadWebApp.Models.DataModel;
using RenadWebApp.Services;

namespace RenadWebApp.Controllers
{
    /// <summary>Clients and their contracts (quotations).</summary>
    public class HomeController : AppController
    {
        private readonly ApplicationDbContext _db;
        private readonly ContractCalculator _calculator;

        public HomeController(ApplicationDbContext db, ContractCalculator calculator)
        {
            _db = db;
            _calculator = calculator;
        }

        public ActionResult Index(string? search)
        {
            return View(_db.Clients.Where(x => search == null || x.Name!.StartsWith(search)).ToList());
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ClientModels client)
        {
            if (await _db.Clients.AnyAsync(x => x.Name == client.Name))
            {
                return ShowMessage("اسم العميل موجود مسبقاً");
            }

            var clientModel = new ClientModels
            {
                Name = client.Name,
                Email = client.Email,
                PhoneNumber = client.PhoneNumber,
                NumberOfProject = client.NumberOfProject,
                Approved = ContractCalculator.No,
                CreatedDate = (client.CreatedDate ?? DateTime.Today).Date,
            };

            _db.Clients.Add(clientModel);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> Edit(int? id)
        {
            var client = await _db.Clients.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (client == null)
            {
                return ShowMessage("العميل غير موجود");
            }
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, ClientModels clientModel)
        {
            var client = await _db.Clients.FindAsync(clientModel.Id);
            if (client == null)
            {
                return ShowMessage("العميل غير موجود");
            }

            client.Approved = ContractCalculator.NormalizeYesNo(clientModel.Approved);
            client.Name = clientModel.Name;
            client.Email = clientModel.Email;
            client.PhoneNumber = clientModel.PhoneNumber;
            client.NumberOfProject = clientModel.NumberOfProject;
            client.CreatedDate = clientModel.CreatedDate;
            await _db.SaveChangesAsync();

            await _calculator.RecalculateClientAsync(client.Id);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var client = await _db.Clients.FindAsync(id);
            if (client == null)
            {
                return ShowMessage("العميل غير موجود");
            }
            if (await _db.ClientContract.AnyAsync(x => x.ClientId == id))
            {
                return ShowMessage("لا يمكن حذف عميل لديه عروض مسجلة");
            }

            _db.Clients.Remove(client);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> IndexContract(int id)
        {
            var contracts = await _db.ClientContract
                .Where(x => x.ClientId == id)
                .Select(x => x.Contract)
                .AsNoTracking()
                .ToListAsync();

            return View(contracts);
        }

        public ActionResult CreateContract()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateContract(ContractModel contractModel)
        {
            // The route id is the client this contract belongs to.
            var client = await _db.Clients.FindAsync(contractModel.Id);
            if (client == null)
            {
                return ShowMessage("العميل غير موجود");
            }
            if (client.Approved != ContractCalculator.Yes)
            {
                return ShowMessage("يجب الموافقة على العميل قبل إضافة عرض");
            }
            if (await _db.Contracts.AnyAsync(x => x.ContractNumber == contractModel.ContractNumber))
            {
                return ShowMessage("رقم العرض مستخدم مسبقاً، الرجاء تغييره");
            }

            var eng = await _db.Eng.FirstOrDefaultAsync(x => x.EngName == contractModel.ProjectBy);
            if (eng == null)
            {
                return ShowMessage("اسم المهندس غير موجود");
            }

            var createdDate = contractModel.CreatedDate ?? DateTime.Today;
            var contract = new ContractModel
            {
                Approved = ContractCalculator.No,
                ProjectName = contractModel.ProjectName,
                ContractNumber = contractModel.ContractNumber,
                CreatedDate = createdDate,
                Duration = contractModel.Duration,
                DeliveryDate = createdDate.AddMonths(contractModel.Duration ?? 0),
                ProjectBy = contractModel.ProjectBy,
                RatioForEng = contractModel.RatioForEng,
                TotalOfContract = contractModel.TotalOfContract,
                TotalAmount = ContractCalculator.NetOf(contractModel.TotalOfContract),
                TotalTax = ContractCalculator.TaxOf(contractModel.TotalOfContract),
                TotalRatioForEng = ContractCalculator.EngineerShareOf(contractModel.TotalOfContract, contractModel.RatioForEng),
            };
            contract.RestOfContractShow = contract.TotalOfContract;
            contract.RestOfEngShow = contract.TotalRatioForEng;
            contract.RestOfTAxShow = contract.TotalTax;

            _db.Contracts.Add(contract);
            await _db.SaveChangesAsync();

            _db.EngContract.Add(new EngContract { ContractId = contract.Id, EngId = eng.Id });
            _db.ClientContract.Add(new ClientContract { ContractId = contract.Id, ClientId = client.Id });
            await _db.SaveChangesAsync();

            await _calculator.RecalculateClientAsync(client.Id);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditContract(int? id)
        {
            var contract = await _db.Contracts.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (contract == null)
            {
                return ShowMessage("العرض غير موجود");
            }
            return View(contract);
        }

        [HttpPost, ActionName("EditContract")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContract(int id, ContractModel contractModel)
        {
            var contract = await _db.Contracts.FindAsync(id);
            if (contract == null)
            {
                return ShowMessage("العرض غير موجود");
            }
            if (await _db.Contracts.AnyAsync(x => x.ContractNumber == contractModel.ContractNumber && x.Id != id))
            {
                return ShowMessage("رقم العرض مستخدم مسبقاً، الرجاء تغييره");
            }

            contract.Approved = ContractCalculator.NormalizeYesNo(contractModel.Approved);
            contract.ProjectName = contractModel.ProjectName;
            contract.ContractNumber = contractModel.ContractNumber;
            contract.CreatedDate = contractModel.CreatedDate;
            contract.Duration = contractModel.Duration;
            contract.RatioForEng = contractModel.RatioForEng;
            contract.ProjectBy = contractModel.ProjectBy;
            contract.DeliveryDate = contractModel.CreatedDate?.AddMonths(contractModel.Duration ?? 0);
            contract.TotalOfContract = contractModel.TotalOfContract;
            contract.TotalAmount = ContractCalculator.NetOf(contractModel.TotalOfContract);
            contract.TotalTax = ContractCalculator.TaxOf(contractModel.TotalOfContract);
            contract.TotalRatioForEng = ContractCalculator.EngineerShareOf(contractModel.TotalOfContract, contractModel.RatioForEng);
            await _db.SaveChangesAsync();

            await _calculator.RecalculateContractAsync(id);

            return RedirectToAction(nameof(Index));
        }

        public async Task<ActionResult> DetailsContract(int id)
        {
            var contract = await _db.Contracts.SingleOrDefaultAsync(c => c.Id == id);
            if (contract == null)
            {
                return ShowMessage("العرض غير موجود");
            }
            return View(contract);
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return ShowMessage("حدث خطأ غير متوقع، الرجاء المحاولة مرة أخرى");
        }
    }
}
