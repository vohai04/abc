using BookInfoFinder.Data;

using BookInfoFinder.Models;

using Microsoft.AspNetCore.Hosting;

using Microsoft.EntityFrameworkCore;

using SixLabors.ImageSharp;

using SixLabors.ImageSharp.Processing;

using SixLabors.ImageSharp.Formats.Jpeg;

using System;

using System.Collections.Generic;

using System.IO;

using System.Net.Http;

using System.Threading.Tasks;



public class BookService : IBookService

{

    private readonly BookContext _context;

    private readonly IWebHostEnvironment _env;



    public BookService(BookContext context, IWebHostEnvironment env)

    {

        _context = context;

        _env = env;

    }



    public async Task<List<Book>> GetAllBooksAsync()

    {

        return await _context.Books

            .Include(b => b.Author)

            .Include(b => b.Category)

            .Include(b => b.Publisher)

            .ToListAsync();

    }



    public async Task<Book?> GetBookByIdAsync(int id)

    {

        return await _context.Books

            .Include(b => b.Author)

            .Include(b => b.Category)

            .Include(b => b.Publisher)

            .FirstOrDefaultAsync(b => b.BookId == id);

    }



    // Các hàm search giữ nguyên...



    public async Task AddBookAsync(Book book, string? imageUrl)

    {

        if (!string.IsNullOrWhiteSpace(imageUrl))

        {

            book.ImageBase64 = await SaveImageFromUrlAsync(imageUrl);

        }

        else

        {

            book.ImageBase64 = null;

        }



        _context.Books.Add(book);

        await _context.SaveChangesAsync();

    }



    public async Task UpdateBookAsync(Book book, string? imageUrl)

    {

        if (!string.IsNullOrWhiteSpace(imageUrl))

        {

            book.ImageBase64 = await SaveImageFromUrlAsync(imageUrl);

        }

        // Nếu imageUrl trống thì giữ nguyên ảnh cũ



        _context.Books.Update(book);

        await _context.SaveChangesAsync();

    }



    public async Task DeleteBookAsync(int id)

    {

        var book = await _context.Books.FindAsync(id);

        if (book != null)

        {

            // Xóa file ảnh nếu là file vật lý

            if (!string.IsNullOrWhiteSpace(book.ImageBase64)

                && book.ImageBase64.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)

                && _env != null)

            {

                var relativePath = book.ImageBase64.TrimStart('/');

                var filePath = Path.Combine(_env.WebRootPath, relativePath);



                if (File.Exists(filePath))

                {

                    try

                    {

                        File.Delete(filePath);

                    }

                    catch { }

                }

            }



            _context.Books.Remove(book);

            await _context.SaveChangesAsync();

        }

    }



      public async Task<string> SaveImageFromUrlAsync(string imageUrl)

    {

        var uploadsFolder = Path.Combine(_env.WebRootPath, "images");

        if (!Directory.Exists(uploadsFolder))

            Directory.CreateDirectory(uploadsFolder);



        var ext = Path.GetExtension(imageUrl).Split('?')[0];

        var uniqueName = Guid.NewGuid().ToString() + ext;

        var filePath = Path.Combine(uploadsFolder, uniqueName);



        using (var httpClient = new HttpClient())

        {

            var response = await httpClient.GetAsync(imageUrl);

            if (!response.IsSuccessStatusCode)

                throw new Exception("Không tải được ảnh từ URL!");



            using (var inputStream = await response.Content.ReadAsStreamAsync())

            using (var image = Image.Load(inputStream))

            {

                image.Mutate(x => x.Resize(new ResizeOptions

                {

                    Size = new Size(400, 600),

                    Mode = ResizeMode.Max

                }));

                image.Save(filePath, new JpegEncoder { Quality = 80 });

            }

        }



        return $"/images/{uniqueName}";

    }



   // Thêm categoryCustom vào tham số

public async Task<int> CountSearchBooksAsync(

    string? title, string? author, string? category, DateTime? date, string? tag)

{

    var query = _context.Books

        .Include(b => b.Author)

        .Include(b => b.Category)

        .Include(b => b.Publisher)

        .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)

        .AsQueryable();



    if (!string.IsNullOrEmpty(title))

        query = query.Where(b => b.Title.Contains(title));

    if (!string.IsNullOrEmpty(author))

        query = query.Where(b => b.Author != null && b.Author.Name.Contains(author));

    if (!string.IsNullOrEmpty(category))

        query = query.Where(b => b.Category != null && b.Category.Name.Contains(category));

    if (date.HasValue)

        query = query.Where(b => b.PublicationDate.Year == date.Value.Year);

    if (!string.IsNullOrEmpty(tag))

        query = query.Where(b => b.BookTags.Any(bt => bt.Tag != null && bt.Tag.Name.Contains(tag)));



    return await query.CountAsync();

}



   public async Task<List<Book>> SearchBooksPagedAsync(

    string? title, string? author, string? category, DateTime? date, int page, int pageSize, string? tag)

{

    var query = _context.Books

        .Include(b => b.Author)

        .Include(b => b.Category)

        .Include(b => b.Publisher)

        .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)

        .AsQueryable();



    if (!string.IsNullOrEmpty(title))

        query = query.Where(b => b.Title.Contains(title));

    if (!string.IsNullOrEmpty(author))

        query = query.Where(b => b.Author != null && b.Author.Name.Contains(author));

    if (!string.IsNullOrEmpty(category))

        query = query.Where(b => b.Category != null && b.Category.Name.Contains(category));

    if (date.HasValue)

        query = query.Where(b => b.PublicationDate.Year == date.Value.Year);

    if (!string.IsNullOrEmpty(tag))

        query = query.Where(b => b.BookTags.Any(bt => bt.Tag != null && bt.Tag.Name.Contains(tag)));



    return await query

        .OrderBy(b => b.Title)

        .Skip((page - 1) * pageSize)

        .Take(pageSize)

        .ToListAsync();

}

    public async Task<List<string>> SuggestBookTitlesAsync(string keyword)

    {

        keyword = keyword?.ToLower() ?? "";

        return await _context.Books

            .Where(b => b.Title != null && b.Title.ToLower().Contains(keyword))

            .Select(b => b.Title)

            .Distinct()

            .Take(10)

            .ToListAsync();

    }

  public async Task<(List<Book> Books, int TotalCount)> GetAllBooksPagedAsync(int page, int pageSize)

    {

        var query = _context.Books

            .Include(b => b.Author)

            .Include(b => b.Category)

            .Include(b => b.Publisher)

            .Include(b => b.BookTags).ThenInclude(bt => bt.Tag)

            .OrderBy(b => b.Title);



        var totalCount = await query.CountAsync();

        var books = await query

            .Skip((page - 1) * pageSize)

            .Take(pageSize)

            .ToListAsync();



        return (books, totalCount);

    }

}