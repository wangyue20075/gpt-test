namespace Oc.BinGrid.Domain.Entities
{
    public class AssetBalance
    {
        public string Asset { get; set; }

        public decimal Free { get; set; }

        public decimal Locked { get; set; }
    }
}
