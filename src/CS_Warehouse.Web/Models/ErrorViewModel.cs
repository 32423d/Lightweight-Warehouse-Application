namespace CS_Warehouse.Web.Models;

/// <summary>
/// Supplies the request identifier for the error page.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
