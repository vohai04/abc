// using Microsoft.AspNetCore.Mvc;

// using Microsoft.AspNetCore.Mvc.RazorPages;

// using BookInfoFinder.Models;

// using BookInfoFinder.Services;

// using BookInfoFinder.Service;

// using System.Threading.Tasks;

// using System.Security.Claims;



// namespace BookInfoFinder.Pages

// {

//     public class IndexModel : PageModel

//     {

//         private readonly IBookService _bookService;

//         private readonly ICategoryService _categoryService;

//         private readonly ITagService _tagService;

//         private readonly IFavoriteService _favoriteService;



//         public IndexModel(

//             IBookService bookService,

//             ICategoryService categoryService,

//             ITagService tagService,

//             IFavoriteService favoriteService)

//         {

//             _bookService = bookService;

//             _categoryService = categoryService;

//             _tagService = tagService;

//             _favoriteService = favoriteService;

//         }



//         public List<Category> Categories { get; set; } = new();

//         public List<Tag> Tags { get; set; } = new();



//         // Sửa thành bất đồng bộ

//         public async Task OnGetAsync()

//         {

//             Categories = await _categoryService.GetAllCategoriesAsync();

//             Tags = await _tagService.GetAllTagsAsync();

//         }



//         // Nếu các hàm tìm kiếm của BookService đã có async, bạn nên chuyển sang async:

//         public async Task<JsonResult> OnGetAjaxSearchAsync()

//         {

//             var query = Request.Query;



//             string? title = query["title"];

//             string? author = query["author"];

//             string? tag = query["tag"];

//             int.TryParse(query["categoryId"], out int categoryId);

//             int.TryParse(query["year"], out int year);

//             int.TryParse(query["page"], out int page);

//             int.TryParse(query["pageSize"], out int pageSize);



//             page = page <= 0 ? 1 : page;

//             pageSize = pageSize <= 0 ? 6 : pageSize;



//             DateTime? publicationDate = year > 0 ? new DateTime(year, 1, 1) : (DateTime?)null;



//             // Giả sử BookService có các hàm async tương ứng

//             var totalCount = await _bookService.CountSearchBooksAsync(

//                 title,

//                 author,

//                 categoryId > 0 ? categoryId : null,

//                 publicationDate,

//                 tag

//             );



//             var pagedBooks = await _bookService.SearchBooksPagedAsync(

//                 title,

//                 author,

//                 categoryId > 0 ? categoryId : null,

//                 publicationDate,

//                 page,

//                 pageSize,

//                 tag

//             );



//             var result = pagedBooks.Select(b => new

//             {

//                 b.BookId,

//                 b.Title,

//                 b.Description,

//                 b.Abstract,

//                 ImageBase64 = b.ImageBase64,

//                 PublicationYear = b.PublicationDate.Year,

//                 Author = new { Name = b.Author?.Name ?? "Không rõ" },

//                 Category = new { Name = b.Category?.Name ?? "Không rõ" },

//                 Publisher = new { Name = b.Publisher?.Name ?? "Không rõ" },

//                 Tags = b.BookTags.Select(bt => bt.Tag.Name).ToList()

//             });



//             var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);



//             return new JsonResult(new { books = result, totalPages });

//         }



//        // CHỈ DÙNG SESSION - KHÔNG DÙNG CLAIMS HAY COOKIES



// public async Task<IActionResult> OnGetCheckLoginAsync()

//        {

//            Console.WriteLine("=== OnGetCheckLoginAsync CALLED ===");

//            // Đảm bảo session được load

//            await HttpContext.Session.LoadAsync();

//            var userIdStr = HttpContext.Session.GetString("UserId");

//            var userName = HttpContext.Session.GetString("UserName");

//            var role = HttpContext.Session.GetString("Role");

//            bool isLoggedIn = !string.IsNullOrEmpty(userIdStr);

//            Console.WriteLine($"Session ID: {HttpContext.Session.Id}");

//            Console.WriteLine($"Session Available: {HttpContext.Session.IsAvailable}");

//            Console.WriteLine($"UserId: '{userIdStr}'");

//            Console.WriteLine($"UserName: '{userName}'");

//            Console.WriteLine($"Role: '{role}'");

//            Console.WriteLine($"IsLoggedIn: {isLoggedIn}");

//            var result = new

//            {

//                isLoggedIn = isLoggedIn,

//                sessionId = HttpContext.Session.Id,

//                userId = userIdStr,

//                userName = userName,

//                role = role,

//                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

//                allSessionKeys = HttpContext.Session.Keys.ToArray()

//            };

//            Console.WriteLine($"Returning JSON: {System.Text.Json.JsonSerializer.Serialize(result)}");

//            return new JsonResult(result);

//        }

//        public async Task<IActionResult> OnPostAddFavoriteAsync()

//        {

//            Console.WriteLine("=== OnPostAddFavoriteAsync CALLED ===");

//            // Đảm bảo session được load

//            await HttpContext.Session.LoadAsync();

//            Console.WriteLine($"Session ID: {HttpContext.Session.Id}");

//            Console.WriteLine($"Request Form Keys: {string.Join(", ", Request.Form.Keys)}");

//            if (!int.TryParse(Request.Form["bookId"], out int bookId))

//            {

//                Console.WriteLine("ERROR: Invalid bookId");

//                return new JsonResult(new { success = false, message = "Thiếu bookId" });

//            }

//            var userIdStr = HttpContext.Session.GetString("UserId");

//            Console.WriteLine($"UserId from session: '{userIdStr}'");

//            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))

//            {

//                Console.WriteLine("ERROR: User not logged in");

//                return new JsonResult(new { success = false, message = "Chưa đăng nhập" });

//            }

//            try

//            {

//                var bookExists = await _favoriteService.BookExistsAsync(bookId);

//                if (!bookExists)

//                {

//                    return new JsonResult(new { success = false, message = "Sách không tồn tại." });

//                }

//                var isFavorite = await _favoriteService.IsFavoriteAsync(userId, bookId);

//                if (isFavorite)

//                {

//                    return new JsonResult(new { success = false, message = "Sách đã có trong yêu thích." });

//                }

//                await _favoriteService.AddToFavoritesAsync(userId, bookId);

//                Console.WriteLine("SUCCESS: Added to favorites");

//                return new JsonResult(new { success = true, message = "Đã thêm vào danh sách yêu thích!" });

//            }

//            catch (Exception ex)

//            {

//                Console.WriteLine($"EXCEPTION: {ex.Message}");

//                return new JsonResult(new { success = false, message = "Có lỗi xảy ra." });

//            }

//        }



//         public class FavoriteRequest

//         {

//             public int BookId { get; set; }

//         }

//     }

// }