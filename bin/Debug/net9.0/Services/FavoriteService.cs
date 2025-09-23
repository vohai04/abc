using BookInfoFinder.Data;
using BookInfoFinder.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookInfoFinder.Service
{
    public class FavoriteService : IFavoriteService
    {
        private readonly BookContext _context;

        public FavoriteService(BookContext context)
        {
            _context = context;
        }

        public async Task AddToFavoritesAsync(int userId, int bookId)
        {
            if (!await IsFavoriteAsync(userId, bookId) && await BookExistsAsync(bookId))
            {
                var favorite = new Favorite { UserId = userId, BookId = bookId };
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFavoriteAsync(int userId, int bookId)
        {
            return await _context.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == bookId);
        }

        public async Task RemoveFromFavoritesAsync(int userId, int bookId)
        {
            var favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);
            if (favorite != null)
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetFavoritesCountAsync(int userId)
        {
            return await _context.Favorites
                .Where(f => f.UserId == userId)
                .CountAsync();
        }

        public async Task<List<Book>> GetFavoritesByUserPagedAsync(int userId, int page, int pageSize)
        {
            // ✅ FIX: Thêm validation và đảm bảo thứ tự chính xác
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 6;

            var books = await _context.Favorites
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.FavoriteId) // Thứ tự mới nhất trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new { f.Book, f.FavoriteId }) // Lấy cả FavoriteId để debug
                .ToListAsync();

            // Load related data riêng để tránh multiple include
            var bookIds = books.Select(x => x.Book.BookId).ToList();
            
            var booksWithRelations = await _context.Books
                .Where(b => bookIds.Contains(b.BookId))
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)
                .ToListAsync();

            // Sắp xếp lại theo thứ tự của favorites
            var orderedBooks = books
                .Select(fb => booksWithRelations.FirstOrDefault(b => b.BookId == fb.Book.BookId))
                .Where(book => book != null)
                .ToList();

            return orderedBooks;
        }

        public async Task<bool> BookExistsAsync(int bookId)
        {
            return await _context.Books.AnyAsync(b => b.BookId == bookId);
        }

        public async Task<List<Favorite>> GetAllFavoritesAsync()
        {
            return await _context.Favorites
                .Include(f => f.Book)
                .Include(f => f.User)
                .OrderByDescending(f => f.FavoriteId) // ✅ FIX: Thêm ordering
                .ToListAsync();
        }
    }
}