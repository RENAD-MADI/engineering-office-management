
namespace RenadWebApp.Models.DataModel
{
    public class ContractPayment
    {
        public int ContractId { get; set; }
        public virtual ContractModel Contract { get; set; } = null!;

        public int PaymentId { get; set; }
        public virtual PaymentModel Payment { get; set; } = null!;
    }
}
