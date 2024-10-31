using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LibraryController : ControllerBase
    {
        public LibraryController(Context context, EmailService emailService, JwtService jwtService)
        {
            Context = context;
            EmailService = emailService;
            JwtService = jwtService;
        }

        public Context Context { get; }
        public EmailService EmailService { get; }
        public JwtService JwtService { get; }

        [HttpPost("Register")]
        public ActionResult Register(User user)
        {
            user.AccountStatus = AccountStatus.UNAPROOVED;
            user.UserType = UserType.STUDENT;
            user.CreatedOn = DateTime.Now;

            Context.Users.Add(user);
            Context.SaveChanges();

            const string subject = "Account Created";
            var body =
                $"""
                    <html>
                        <body>
                            <h1>Hello {user.FirstName}{user.LastName} </h1>
                            <h2>
                                Your Account has been created and we have sent approval request to admin.
                                Once the request is approved by admin you will receive email, and you will be
                                able to login in to your account.
                            </h2>
                            <h3>Thanks</h3>
                        </body>
                    </html>
                 """;

            EmailService.SendEmail(user.Email, subject, body);

            return Ok(@"Thank you for registration
                        Your account has been sent for aprooval.
                        Once it is aprooved, you will get an email.");
        }

        [HttpPost("Login")]
        public ActionResult Login(string email, string password)
        {

            if (Context.Users.Any(u => u.Email.Equals(email) && u.Password.Equals(password)))
            {
                var user = Context.Users.Single(u => u.Email.Equals(email) && u.Password.Equals(password));

                if (user.AccountStatus == AccountStatus.UNAPROOVED)
                {
                    return Ok("Unapproved");
                }
                if (user.AccountStatus == AccountStatus.BLOCKED)
                {
                    return Ok("Blocked");
                }

                return Ok(JwtService.GenerateToken(user));
            }

            return Ok("Not Found");
        }

        [Authorize]
        [HttpGet("GetBooks")]
        public ActionResult GetBooks()
        {

            if (Context.Books.Any())
            {
                return Ok(Context.Books.Include(book => book.BookCategory).ToList());
            }

            return NotFound();
        }

        [Authorize]
        [HttpPost("OrderBook")]
        public ActionResult OrderBook(int userId, int bookId)
        {
            var canOrder = Context.Orders.Count(o => o.UserId == userId && !o.Returned) < 3;
            if (canOrder)
            {
                Context.Orders.Add(new()
                {
                    UserId = userId,
                    BookId = bookId,
                    OrderDate = DateTime.Now,
                    ReturnDate = null,
                    Returned = false,
                    FinePaid = 0
                });

                var book = Context.Books.Find(bookId);
                if (book is not null)
                {
                    book.Ordered = true;
                }

                Context.SaveChanges();
                return Ok("Ordered");
            }

            return Ok("Cannot Order");
        }

        [Authorize]
        [HttpGet("GetOrderofUser")]
        public ActionResult GetOrderofBook(int userId)
        {

            var orders = Context.Orders
                .Include(o => o.Book)
                .Include(o => o.User)
                .Where(o => o.UserId == userId)
                .ToList();
            if (orders.Any())
            {
                return Ok(orders);
            }
            else
            {
                return NotFound();
            }

        }

        [Authorize]
        [HttpPost("AddCategory")]
        public ActionResult AddCategory(BookCategory bookCategory)
        {
            var exist = Context.BookCategories.Any(bc => bc.Category == bookCategory.Category && bc.SubCategory == bookCategory.SubCategory);
            if (exist)
                return Ok("not inserted");

            else
            {
                Context.BookCategories.Add(new()
                {
                    Category = bookCategory.Category,
                    SubCategory = bookCategory.SubCategory
                });
                Context.SaveChanges();
                return Ok("Inserted");
            }
        }


        [Authorize]
        [HttpGet("GetCategory")]
        public ActionResult GetCategory()
        {
            var categories = Context.BookCategories.ToList();
            if (categories.Any())
            {
                return Ok(categories);
            }
            return Ok("Not Found");
        }

        [Authorize]
        [HttpPost("AddBook")]
        public ActionResult AddBook(Book book)
        {
            book.BookCategory = null; //karena pada frontend BookCategory ditetapkan nilai id: 0
                                      // hapus ini jika frontendnya menggunakan BookCategory id: null

            Context.Books.Add(book);
            Context.SaveChanges();

            return Ok("Inserted Book");
        }

        [Authorize]
        [HttpDelete("Delete Book")]
        public ActionResult DeleteBook(int id)
        {
            var exists = Context.Books.Any(o => o.Id == id);
            if (exists)
            {
                var book = Context.Books.Find(id);
                Context.Books.Remove(book!);
                Context.SaveChanges(true);
                return Ok("Deleted");
            }
            return NotFound();
        }

        [Authorize]
        [HttpGet("ReturnBook")]
        public ActionResult ReturnBook(int userId, int bookId, int fine)
        {
            var order = Context.Orders.SingleOrDefault(o => o.UserId == userId && o.BookId == bookId);
            if (order is not null)
            {
                order.Returned = true;
                order.ReturnDate = DateTime.Now;
                order.FinePaid = fine;

                var book = Context.Books.Single(b => b.Id == order.BookId);
                book.Ordered = false;

                Context.SaveChanges();

                return Ok("Returned");
            }
            return Ok("not returned");
        }


        [Authorize]
        [HttpGet("GetUsers")]
        public ActionResult GetUsers()
        {
            Context.Users.ToList();
            return Ok("Success");
        }

        [Authorize]
        [HttpGet("ApproveRequest")]
        public ActionResult ApproveRequest(int userId)
        {
            var user = Context.Users.Find(userId);

            if (user is not null)
            {
                if (user.AccountStatus == AccountStatus.UNAPROOVED)
                {
                    user.AccountStatus = AccountStatus.ACTIVE;
                    Context.SaveChanges();

                    EmailService.SendEmail(user.Email, "Account Approved", $"""
                        <html>
                            <body>
                                <h2>Hi, {user.FirstName} {user.LastName} </h2>
                                <h3>You Account has been approved by Admin.</h3>
                                <h3>Now you can login to your account.</h3>
                            </body>
                        </html>
                    """);
                    return Ok("Approved");
                }
            }
            return Ok("Not Approved");
        }

        [Authorize]
        [HttpGet("GetOrders")]
        public ActionResult GetOrders()
        {
            var orders = Context.Orders
                .Include(o => o.User)
                .Include(o => o.Book)
                .ToList();

            if (orders.Any())
            {
                return Ok(orders);
            }
            else
            {
                return NotFound();
            }
        }

        [Authorize]
        [HttpGet("SendEmailForPendingReturn")]
        public ActionResult SendEmailForPendingReturn()
        {
            var orders = Context.Orders.Where(o => !o.Returned).Include(o => o.User).ToList();

            var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
            emailsWithFine.ForEach(x => x.FinePaid = (DateTime.Now - x.OrderDate.AddDays(10)).Days * 50);

            var firstFineEmails = emailsWithFine.Where(x => x.FinePaid == 50).ToList();
            firstFineEmails.ForEach(x =>
            {
                var body =
            $""" 
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName} </h2>
                        <h4>Yesterday was your last day to return Book: "{x.Book?.Title}".</h4>
                        <h4>From today, every day a fine of 50Rs will be added for this book.</h4>
                        <h4>Please return it as soon as possible.</h4>
                        <h4>If your fine exceeds 500Rs, your account will be blocked.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
            """;

                EmailService.SendEmail(x.User!.Email, "Return Overdue", body);

            });

            var regularFineEmails = emailsWithFine.Where(x => x.FinePaid > 50 && x.FinePaid <= 500).ToList();
            regularFineEmails.ForEach(x =>
            {
                var regularFineEmailsBody = $""" 
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>You have {x.FinePaid}Rs fine on Book: "{x.Book?.Title}"</h4>
                        <h4>Please pay it as soon as possible.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                EmailService.SendEmail(x.User?.Email!, "Fine To Pay", regularFineEmailsBody);
            });

            var overdueFineEmails = emailsWithFine.Where(x => x.FinePaid > 500).ToList();
            overdueFineEmails.ForEach(x =>
            {
                var overduefineEmailsBody = $""" 
                <html>
                    <body>
                        <h2>Hi, {x.User?.FirstName} {x.User?.LastName}</h2>
                        <h4>You have {x.FinePaid}Rs fine on Book: "{x.Book?.Title}"</h4>
                        <h4>Your account is BLOCKED.</h4>
                        <h4>Please pay it as soon as possible to UNBLOCK your account.</h4>
                        <h4>Thanks</h4>
                    </body>
                </html>
                """;

                EmailService.SendEmail(x.User?.Email!, "Fine Overdue", overduefineEmailsBody);
            });

            return Ok("sent");
        }

        [Authorize]
        [HttpGet("BlockFineOverdueUsers")]
        public ActionResult BlockFineOverdueUsers()
        {
            var orders = Context.Orders
                .Include(o => o.User)
                .Include(o => o.Book)
                .Where(o => o!.Returned)
                .ToList();

            var emailsWithFine = orders.Where(o => DateTime.Now > o.OrderDate.AddDays(10)).ToList();
            emailsWithFine.ForEach(x => x.FinePaid = (DateTime.Now - x.OrderDate.AddDays(10)).Days * 50);

            var users = emailsWithFine.Where(x => x.FinePaid > 500).Select(x => x.User).Distinct().ToList();

            if (users is not null && users.Any())
            {
                foreach (var user in users)
                {
                    user!.AccountStatus = AccountStatus.BLOCKED;
                }
                Context.SaveChanges();

                return Ok("blocked");
            }
            else
            {
                return Ok("not blocked");
            }
        }

        [Authorize]
        [HttpGet("Unblock")]
        public ActionResult Unblock(int userId)
        {
            var user = Context.Users.Find(userId);
                if (user is not null)
            {
                user.AccountStatus = AccountStatus.ACTIVE;
                Context.SaveChanges();
                return Ok("unblocked");
            }
            return Ok("not unblocked");
        }


    }
}
