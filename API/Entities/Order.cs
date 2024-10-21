namespace API.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public bool Returned { get; set; }
        public DateTime? ReturnDate { get; set; } = DateTime.UtcNow;
        public int FinePaid { get; set; }
        public User? User { get; set; }
        public Book? Book { get; set; }

    }
}
