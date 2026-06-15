using System.ComponentModel;

namespace DuendeAuth.Admin.Models;

/// <summary>Pagination metadata for a collection response.</summary>
public record PaginationMeta(
    [property: Description("Current page number (1-based).")] int Page,
    [property: Description("Number of items per page.")] int PageSize,
    [property: Description("Total number of items across all pages.")] int TotalCount,
    [property: Description("Total number of pages.")] int TotalPages);
