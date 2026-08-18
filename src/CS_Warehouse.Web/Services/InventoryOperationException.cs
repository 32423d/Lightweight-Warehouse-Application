namespace CS_Warehouse.Web.Services;

/// <summary>
/// Reports a stock operation that the service rejects.
/// The controller shows this message to the user.
/// </summary>
public sealed class InventoryOperationException : Exception
{
    public InventoryOperationException(string message)
        : base(message)
    {
    }

    public InventoryOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
