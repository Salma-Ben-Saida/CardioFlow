public class ReceiveInventoryBatchDto
{
    public Guid ProductId { get; set; }
    public List<ReceiveInventoryItemLineDto> Items { get; set; } = new();
}

public class ReceiveInventoryItemLineDto
{
    public string Barcode { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
}