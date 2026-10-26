using Microsoft.EntityFrameworkCore;
using RenadWebApp.Data;

namespace RenadWebApp.Services
{
    /// <summary>
    /// Central place for the office's money rules: 15% VAT is included in every amount,
    /// and the engineer's share is a percentage on top of the net (pre-VAT) amount.
    /// </summary>
    public class ContractCalculator
    {
        public const string Yes = "yes";
        public const string No = "no";
        private const int VatPercent = 15;

        private readonly ApplicationDbContext _db;

        public ContractCalculator(ApplicationDbContext db)
        {
            _db = db;
        }

        public static int NetOf(int? gross) => (gross ?? 0) * 100 / (100 + VatPercent);

        public static int TaxOf(int? gross) => (gross ?? 0) - NetOf(gross);

        public static int EngineerShareOf(int? gross, int? ratio)
        {
            var net = NetOf(gross);
            return net - net * 100 / ((ratio ?? 0) + 100);
        }

        public static string NormalizeYesNo(string? value) =>
            value?.Trim().ToLower() is "yes" or "نعم" ? Yes : No;

        /// <summary>Recomputes a contract's remaining balances from its payments.</summary>
        public async Task RecalculateContractAsync(int contractId)
        {
            var contract = await _db.Contracts.FindAsync(contractId);
            if (contract == null) return;

            var payments = await _db.ContractPayment
                .Where(x => x.ContractId == contractId)
                .Select(x => x.Payment)
                .ToListAsync();

            contract.RestOfContractShow = contract.TotalOfContract - payments.Sum(p => p.Payments ?? 0);
            contract.RestOfEngShow = contract.TotalRatioForEng - payments.Where(p => p.DueEngRatio == Yes).Sum(p => p.RatioForEng ?? 0);
            contract.RestOfTAxShow = contract.TotalTax - payments.Where(p => p.DueTax == Yes).Sum(p => p.PaymentTax ?? 0);
            contract.AmountLate = payments.Sum(p => p.Difference ?? 0);

            await _db.SaveChangesAsync();

            var clientId = await _db.ClientContract
                .Where(x => x.ContractId == contractId)
                .Select(x => (int?)x.ClientId)
                .FirstOrDefaultAsync();
            if (clientId != null)
            {
                await RecalculateClientAsync(clientId.Value);
            }
        }

        /// <summary>Rolls the totals of all approved contracts up to the client.</summary>
        public async Task RecalculateClientAsync(int clientId)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null) return;

            var contracts = await _db.ClientContract
                .Where(x => x.ClientId == clientId)
                .Select(x => x.Contract)
                .ToListAsync();
            var approved = contracts.Where(c => c.Approved == Yes).ToList();

            client.TotalAmount = approved.Sum(c => c.TotalOfContract ?? 0);
            client.RestFromAmount = approved.Sum(c => c.RestOfContractShow ?? 0);
            client.AmountLate = contracts.Sum(c => c.AmountLate ?? 0);

            await _db.SaveChangesAsync();
        }
    }
}
