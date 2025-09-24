using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.RazorPages;

using BookInfoFinder.Models;

using BookInfoFinder.Services;

using System.Collections.Generic;

using System.Text;

using System.Threading.Tasks;



namespace BookInfoFinder.Pages.Admin

{

    public class ManageBookModel : PageModel

    {

        private readonly IBookService _bookService;

        private readonly IAuthorService _authorService;

        private readonly ICategoryService _categoryService;

        private readonly IPublisherService _publisherService;

        private readonly IUserService _userService;

        private readonly ITagService _tagService;

        private readonly IBookTagService _bookTagService;



        public ManageBookModel(

            IBookService bookService,

            IAuthorService authorService,

            ICategoryService categoryService,

            IPublisherService publisherService,

            IUserService userService,

            ITagService tagService,

            IBookTagService bookTagService)

        {

            _bookService = bookService;

            _authorService = authorService;

            _categoryService = categoryService;

            _publisherService = publisherService;

            _userService = userService;

            _tagService = tagService;

            _bookTagService = bookTagService;

        }



        public List<Book> Books { get; set; } = new();

        public List<Category> Categories { get; set; } = new();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public int TotalBooks { get; set; }



        // ✅ FIX: Bind search và category parameters

        [BindProperty(SupportsGet = true)]

        public string? Search { get; set; }

        

        [BindProperty(SupportsGet = true)]

        public int? Category { get; set; }



        private async Task LoadAllAsync()

        {

            Categories = await _categoryService.GetAllCategoriesAsync();

        }



        public async Task OnGetAsync()

        {

            await LoadAllAsync();



            // ✅ FIX: Lấy parameters từ query string

            var query = Request.Query;

            int page = 1, pageSize = 6;

            

            if (int.TryParse(query["page"], out var p)) page = p;

            if (int.TryParse(query["pageSize"], out var ps)) pageSize = ps;

            if (page < 1) page = 1;

            if (pageSize < 1) pageSize = 6;



            // ✅ FIX: Lấy search và category từ query nếu không có trong BindProperty

            if (string.IsNullOrEmpty(Search))

                Search = query["search"].ToString();

            

            if (!Category.HasValue && int.TryParse(query["category"], out var catId))

                Category = catId;



            Page = page;

            PageSize = pageSize;



            List<Book> books;

            int totalCount;



            // ✅ FIX: Sử dụng method có sẵn từ BookService

            if (!string.IsNullOrEmpty(Search) || Category.HasValue)

            {

                // Tìm kiếm theo title hoặc category

                string? categoryName = null;

                if (Category.HasValue)

                {

                    var selectedCategory = Categories.FirstOrDefault(c => c.CategoryId == Category.Value);

                    categoryName = selectedCategory?.Name;

                }



                // Sử dụng SearchBooksPagedAsync với parameters phù hợp

                books = await _bookService.SearchBooksPagedAsync(

                    title: Search,        // Search theo title

                    author: null,         // Không search theo author

                    category: categoryName, // Search theo category name

                    date: null,           // Không search theo date  

                    page: page,

                    pageSize: pageSize,

                    tag: null             // Không search theo tag

                );



                // Đếm tổng số kết quả search

                totalCount = await _bookService.CountSearchBooksAsync(

                    title: Search,

                    author: null,

                    category: categoryName,

                    date: null,

                    tag: null

                );

            }

            else

            {

                // Lấy tất cả sách với phân trang

                (books, totalCount) = await _bookService.GetAllBooksPagedAsync(page, pageSize);

            }



            TotalBooks = totalCount;

            TotalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

            Books = books ?? new List<Book>();



            // Load tags cho từng sách

            foreach (var book in Books)

            {

                book.BookTags = await _bookTagService.GetTagsByBookIdAsync(book.BookId);

            }

        }



        public async Task<IActionResult> OnPostDeleteAsync(int id)

        {

            await _bookTagService.RemoveTagsByBookAsync(id);

            await _bookService.DeleteBookAsync(id);



            // ✅ FIX: Preserve all query parameters

            var routeValues = new Dictionary<string, object?>();

            

            if (int.TryParse(Request.Query["page"], out var p)) 

                routeValues["page"] = p;

            

            if (!string.IsNullOrEmpty(Request.Query["search"]))

                routeValues["search"] = Request.Query["search"].ToString();

                

            if (int.TryParse(Request.Query["category"], out var catId))

                routeValues["category"] = catId;



            return RedirectToPage(routeValues);

        }



        public async Task<IActionResult> OnPostExportCsvAsync()

        {

            await LoadAllAsync();



            List<Book> books;



            // ✅ FIX: Export dựa trên filter hiện tại

            if (!string.IsNullOrEmpty(Search) || Category.HasValue)

            {

                string? categoryName = null;

                if (Category.HasValue)

                {

                    var selectedCategory = Categories.FirstOrDefault(c => c.CategoryId == Category.Value);

                    categoryName = selectedCategory?.Name;

                }



                books = await _bookService.SearchBooksPagedAsync(

                    title: Search,

                    author: null,

                    category: categoryName,

                    date: null,

                    page: 1,

                    pageSize: int.MaxValue, // Lấy tất cả

                    tag: null

                );

            }

            else

            {

                (books, _) = await _bookService.GetAllBooksPagedAsync(1, int.MaxValue);

            }



            foreach (var book in books)

                book.BookTags = await _bookTagService.GetTagsByBookIdAsync(book.BookId);



            string CleanCsv(string input) => (input ?? "").Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");



            var csv = new StringBuilder();

            csv.AppendLine("Tiêu đề,ISBN,Tác giả,Thể loại,NXB,Năm XB,Mô tả,Tóm tắt,Tag");



            foreach (var book in books)

            {

                var tags = string.Join(";", book.BookTags?.Select(bt => bt.Tag?.Name ?? "") ?? new List<string>());

                var year = book.PublicationDate != default ? book.PublicationDate.Year.ToString() : "";

                csv.AppendLine(

                    $"\"{CleanCsv(book.Title)}\",\"{CleanCsv(book.ISBN)}\",\"{CleanCsv(book.Author?.Name)}\",\"{CleanCsv(book.Category?.Name)}\",\"{CleanCsv(book.Publisher?.Name)}\",\"{year}\",\"{CleanCsv(book.Description)}\",\"{CleanCsv(book.Abstract)}\",\"{CleanCsv(tags)}\"");

            }



            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            var fileName = !string.IsNullOrEmpty(Search) || Category.HasValue 

                ? $"DanhSachSach_Filtered_{DateTime.Now:yyyyMMdd}.csv"

                : $"DanhSachSach_All_{DateTime.Now:yyyyMMdd}.csv";

                

            return File(bytes, "text/csv", fileName);

        }

    }

}