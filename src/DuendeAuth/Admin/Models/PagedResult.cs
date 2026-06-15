using System.ComponentModel;

namespace DuendeAuth.Admin.Models;

/// <summary>A paginated collection response.</summary>
public record PagedResult<T>(
    [property: Description("Items on the current page.")] IReadOnlyList<T> Data,
    [property: Description("Pagination metadata.")] PaginationMeta Pagination);
