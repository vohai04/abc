using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.RazorPages;

using BookInfoFinder.Service;

using System.Collections.Generic;

using System.Threading.Tasks;

using System.Linq;

using BookInfoFinder.Models;

using Microsoft.AspNetCore.Http;



namespace BookInfoFinder.Pages

{

    public class FavoritesModel : PageModel

    {

        private readonly IFavoriteService _favoriteService;

        public string? UserName { get; set; }

        public const int PageSize = 6;



        public FavoritesModel(IFavoriteService favoriteService)

        {

            _favoriteService = favoriteService;

        }



        public void OnGet()

        {

            UserName = HttpContext.Session.GetString("UserName");

        }



        // ✅ FIX: Thêm logging và validation tốt hơn

        public async Task<JsonResult> OnGetAjaxFavoritesAsync(int page = 1, int pageSize = 6)

{

    try

    {

        var userIdStr = HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))

        {

            return new JsonResult(new {

                success = false,

                books = new List<object>(),

                totalPages = 1,

                message = "User not logged in"

            });

        }



        // ✅ Validation cho page và pageSize

        page = Math.Max(1, page);

        pageSize = Math.Max(1, Math.Min(50, pageSize)); // Giới hạn pageSize tối đa 50



        var totalBooks = await _favoriteService.GetFavoritesCountAsync(userId);

        var totalPages = totalBooks > 0 ? (int)Math.Ceiling(totalBooks / (double)pageSize) : 1;

        var currentPage = Math.Min(page, totalPages); // ✅ Đảm bảo currentPage không vượt quá totalPages



        var books = await _favoriteService.GetFavoritesByUserPagedAsync(userId, currentPage, pageSize);



        var result = books.Select(b => new

        {

            bookId = b.BookId,

            title = b.Title ?? "Không có tiêu đề",

            imageBase64 = b.ImageBase64,

            author = new { name = b.Author?.Name ?? "Không rõ" },

            category = new { name = b.Category?.Name ?? "Không rõ" },

            // ✅ FIX: Kiểm tra null cho BookTags

            tags = b.BookTags?.Where(bt => bt.Tag != null)

                             .Select(bt => bt.Tag.Name)

                             .Where(name => !string.IsNullOrEmpty(name))

                             .ToList() ?? new List<string>()

        }).ToList();



        return new JsonResult(new {

            success = true,

            books = result,

            totalPages = totalPages,

            currentPage = currentPage,

            totalBooks = totalBooks

        });

    }

    catch (Exception ex)

    {

        // ✅ Error handling

        return new JsonResult(new {

            success = false,

            books = new List<object>(),

            totalPages = 1,

            message = "Có lỗi xảy ra khi tải danh sách yêu thích"

        });

    }

}



        // ✅ FIX: Cải thiện error handling

        public async Task<JsonResult> OnPostRemoveFavoriteAsync([FromBody] FavoriteRequest req)

        {

            try

            {

                if (req == null || req.BookId <= 0)

                {

                    return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ!" });

                }



                var userIdStr = HttpContext.Session.GetString("UserId");

                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))

                {

                    return new JsonResult(new { success = false, message = "Bạn cần đăng nhập!" });

                }



                await _favoriteService.RemoveFromFavoritesAsync(userId, req.BookId);

                return new JsonResult(new { success = true, message = "Đã xóa khỏi yêu thích!" });

            }

            catch (Exception ex)

            {

                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi xóa!" });

            }

        }

    }



    public class FavoriteRequest

    {

        public int BookId { get; set; }

    }

}