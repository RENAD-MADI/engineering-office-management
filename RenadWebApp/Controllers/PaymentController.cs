using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;
using RenadWebApp.Models.DataModel;
using RenadWebApp.Services;

namespace RenadWebApp.Controllers
{
    /// <summary>Payments received against a contract.</summary>
    public class PaymentController : AppController
    {
        private readonly ApplicationDbContext _db;
        private readonly ContractCalculator _calculator;

        public PaymentController(ApplicationDbContext db, ContractCalculator calculator)
        {
            _db = db;
            _calculator = calculator;
        }

        public async Task<ActionResult> Index(int id)
        {
            var payments = await _db.ContractPayment
                .Where(x => x.ContractId == id)
                .Select(x => x.Payment)
                .AsNoTracking()
                .ToListAsync();

            return View(payments);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PaymentModel paymentModel)
        {
            // The route id is the contract being paid.
            var contract = await _db.Contracts.FindAsync(paymentModel.Id);
            if (contract == null)
            {
                return ShowMessage("العرض غير موجود");
            }
            if (contract.Approved != ContractCalculator.Yes)
            {
                return ShowMessage("يجب الموافقة على العرض قبل تسجيل دفعة");
            }
            if (contract.RestOfContractShow < paymentModel.Payments)
            {
                return ShowMessage("قيمة الدفعة أكبر من المتبقي في العرض");
            }

            var payment = new PaymentModel
            {
                PaymentNumber = paymentModel.PaymentNumber,
                Payments = paymentModel.Payments,
                DateForPayment = paymentModel.DateForPayment,
                DueDate = paymentModel.DueDate,
                LateTime1 = DaysLate(paymentModel.DateForPayment, paymentModel.DueDate),
                RatioForEng = ContractCalculator.EngineerShareOf(paymentModel.Payments, contract.RatioForEng),
                DueEngRatio = ContractCalculator.No,
                PaymentTax = ContractCalculator.TaxOf(paymentModel.Payments),
                DueTax = ContractCalculator.No,
                PayMentValue = paymentModel.PayMentValue,
                Difference = paymentModel.PayMentValue - paymentModel.Payments,
            };

            _db.Payment.Add(payment);
            await _db.SaveChangesAsync();

            _db.ContractPayment.Add(new ContractPayment { ContractId = contract.Id, PaymentId = payment.Id });
            await _db.SaveChangesAsync();

            await _calculator.RecalculateContractAsync(contract.Id);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _db.Payment.FindAsync(id);
            if (payment == null)
            {
                return ShowMessage("الدفعة غير موجودة");
            }
            return View(payment);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PaymentModel paymentModel)
        {
            var payment = await _db.Payment.FindAsync(paymentModel.Id);
            if (payment == null)
            {
                return ShowMessage("الدفعة غير موجودة");
            }

            var contract = await _db.ContractPayment
                .Where(x => x.PaymentId == payment.Id)
                .Select(x => x.Contract)
                .FirstAsync();

            payment.Payments = paymentModel.Payments;
            payment.DueTax = ContractCalculator.NormalizeYesNo(paymentModel.DueTax);
            payment.DueEngRatio = ContractCalculator.NormalizeYesNo(paymentModel.DueEngRatio);
            payment.PayMentValue = paymentModel.PayMentValue;
            payment.DateForPayment = paymentModel.DateForPayment;
            payment.DueDate = paymentModel.DueDate;
            payment.Difference = paymentModel.PayMentValue - paymentModel.Payments;
            payment.LateTime1 = DaysLate(paymentModel.DateForPayment, paymentModel.DueDate);
            payment.PaymentTax = ContractCalculator.TaxOf(paymentModel.Payments);
            payment.RatioForEng = ContractCalculator.EngineerShareOf(paymentModel.Payments, contract.RatioForEng);

            await _db.SaveChangesAsync();

            // Editing a payment previously left the contract and client balances stale.
            await _calculator.RecalculateContractAsync(contract.Id);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        public async Task<ActionResult> ClientPayment(int id)
        {
            var payments = await _db.ClientContract
                .Where(x => x.ClientId == id)
                .SelectMany(x => x.Contract.ContractPayment)
                .Select(x => new PayContractModel
                {
                    ContractPay = x.Contract.ContractNumber!,
                    Id = x.Payment.Id,
                    PaymentNumber = x.Payment.PaymentNumber,
                    Payments = x.Payment.Payments,
                    PaymentTax = x.Payment.PaymentTax,
                    DateForPayment = x.Payment.DateForPayment,
                    DueDate = x.Payment.DueDate,
                    LateTime1 = x.Payment.LateTime1,
                    RatioForEng = x.Payment.RatioForEng,
                    DueEngRatio = x.Payment.DueEngRatio,
                    DueTax = x.Payment.DueTax,
                    PayMentValue = x.Payment.PayMentValue,
                    Difference = x.Payment.Difference,
                })
                .ToListAsync();

            return View(payments);
        }

        // Whole days between the due date and the actual payment (the old code subtracted day-of-month only).
        private static int? DaysLate(DateTime? paidOn, DateTime? dueOn) =>
            paidOn.HasValue && dueOn.HasValue ? (paidOn.Value.Date - dueOn.Value.Date).Days : null;
    }
}
