namespace Order.API.Models
{
    public class ProductOrder
    {
        public int Id { get; set; }
        public string Product { get; set; } = "";
        public decimal Price { get; set; }
    }
}