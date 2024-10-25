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

            if(Context.Users.Any(user => user.Email.Equals(email) && user.Password.Equals(password)))
            {
                var user = Context.Users.Single(user => user.Email.Equals(email) && user.Password.Equals(password));

                if(user.AccountStatus == AccountStatus.UNAPROOVED)
                {
                    return Ok("Unapproved");
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
                return Ok (Context.Books.Include(book => book.BookCategory).ToList());
            }

            return NotFound();
        }


    }
}
