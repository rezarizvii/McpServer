using System.Collections.Generic;

public class FilterQuery
{
    // Common paging/sorting
    public int? page { get; set; }
    public int? itemsPerPage { get; set; }

    // Dynamic bag for ALL the crazy order[] / filter keys
    // Example input JSON: { "order[company.companyname]": "asc", "company.companycode": "ABC123" }
    public Dictionary<string, string> Filters { get; set; } = new();
}
