namespace RenadWebApp.Models.DataModel
{
    public class EngContract
    {
        public int ContractId { get; set; }
        public virtual ContractModel Contract { get; set; } = null!;

        public int EngId { get; set; }
        public virtual EngModel Eng { get; set; } = null!;
    }
}
