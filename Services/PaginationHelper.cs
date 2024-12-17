using Microsoft.EntityFrameworkCore;

public class PaginationHelper
{
    public static async Task<(List<T> items, int totalItems, int countPages, int currentPage)> PaginateAsync<T>(IQueryable<T> query, int currentPage, int itemsPerPage)
    {
        int totalItems = await query.CountAsync();
        int countPages = (int)Math.Ceiling((double)totalItems / itemsPerPage);

        if (currentPage < 1) 
            currentPage = 1;
        if (currentPage > countPages)  
            currentPage = countPages;

        var items = await query
            .Skip((currentPage - 1) * itemsPerPage)
            .Take(itemsPerPage)
            .ToListAsync();

        return (items, totalItems, countPages, currentPage);
    }
}