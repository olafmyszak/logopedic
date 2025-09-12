using System.Linq.Expressions;
using LogopedicBackend.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Extensions;

public static class QueryableExtensions
{
    public static async Task<PagedResultDto<TDto>> ToPagedResultAsync<TEntity, TDto>(this IQueryable<TEntity> query,
        int pageNumber, int pageSize, Expression<Func<TEntity, TDto>> selector, CancellationToken ct = default)
    {
        var totalCount = await query.CountAsync(ct);
        var skip = (pageNumber - 1) * pageSize;

        var items = await query.Skip(skip)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(ct);

        return new PagedResultDto<TDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}