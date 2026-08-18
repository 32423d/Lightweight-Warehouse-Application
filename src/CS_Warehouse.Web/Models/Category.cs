using System.ComponentModel.DataAnnotations;

namespace CS_Warehouse.Web.Models;

/// <summary>
/// Groups catalog products. Each product belongs to one category.
/// </summary>
public sealed class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];
}
