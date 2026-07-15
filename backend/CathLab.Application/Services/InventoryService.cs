using CathLab.Application.Common;
using CathLab.Application.DTOs.Pharmacy;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;

namespace CathLab.Application.Services;

public class InventoryService
{
    private readonly IInventoryProductRepository _products;
    private readonly IInventoryItemRepository _items;
    private readonly IStockMovementRepository _movements;

    public InventoryService(
        IInventoryProductRepository products,
        IInventoryItemRepository items,
        IStockMovementRepository movements)
    {
        _products = products;
        _items = items;
        _movements = movements;
    }

    public async Task<InventoryProductDto> CreateProductAsync(CreateInventoryProductDto dto)
    {
        if (!Enum.TryParse<DeviceType>(dto.DeviceType, ignoreCase: true, out var deviceType))
            throw new ArgumentException($"Unknown device type '{dto.DeviceType}'.");

        var existing = await _products.FindByReferenceAsync(dto.Reference);
        if (existing is not null)
            throw new InvalidOperationException($"A product with reference '{dto.Reference}' already exists.");

        var product = new InventoryProduct
        {
            Id = Guid.NewGuid(),
            Reference = dto.Reference,
            ProductName = dto.ProductName,
            DeviceType = deviceType,
            Manufacturer = dto.Manufacturer,
            UnitOfMeasure = dto.UnitOfMeasure,
            MinimumStockLevel = dto.MinimumStockLevel,
            CurrentStock = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _products.CreateAsync(product);
        return ToDto(product);
    }

    public async Task<PagedResult<InventoryProductDto>> GetProductsAsync(string? search, int page, int pageSize)
    {
        var (items, total) = await _products.GetListAsync(search, page, pageSize);
        return new PagedResult<InventoryProductDto>(
            items.Select(ToDto).ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<List<InventoryProductDto>> GetLowStockAsync()
    {
        var products = await _products.GetLowStockAsync();
        return products.Select(ToDto).ToList();
    }

    /// Register a new physical unit into stock (e.g. scanning in a delivery).
    public async Task<InventoryItemDto> ReceiveItemAsync(ReceiveInventoryItemDto dto, Guid receivedBy)
    {
        var product = await _products.FindByIdAsync(dto.ProductId)
            ?? throw new InvalidOperationException("Product not found.");

        var existing = await _items.FindByBarcodeAsync(dto.Barcode);
        if (existing is not null)
            throw new InvalidOperationException($"An item with barcode '{dto.Barcode}' already exists.");

        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            Barcode = dto.Barcode,
            LotNumber = dto.LotNumber,
            ExpiryDate = dto.ExpiryDate,
            SerialNumber = dto.SerialNumber,
            Status = ItemStatus.InStock,
            ReceivedAt = DateTime.UtcNow
        };
        await _items.CreateAsync(item);

        await RecordMovementAsync(product, MovementType.Reception, quantityDelta: 1,
            note: $"Received {dto.Barcode}", createdBy: receivedBy);

        return ToDto(item, product.ProductName);
    }

    /// Called the moment a cardiologist selects this item in the segmentation
    /// UI, before the procedure report is finalized. Locks it against
    /// concurrent reservation by another procedure.
    public async Task ReserveItemAsync(Guid inventoryItemId, Guid cathStudyId)
    {
        var item = await _items.FindByIdAsync(inventoryItemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        if (item.Status != ItemStatus.InStock)
            throw new InvalidOperationException($"Item is '{item.Status}', it must be InStock to reserve.");

        item.Status = ItemStatus.Reserved;
        item.ReservedForCathStudyId = cathStudyId;
        item.ReservedAt = DateTime.UtcNow;
        await _items.UpdateAsync(item);
    }

    /// Cardiologist deselected the item before opening it - back to shelf, no stock movement.
    public async Task ReleaseReservationAsync(Guid inventoryItemId)
    {
        var item = await _items.FindByIdAsync(inventoryItemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        if (item.Status != ItemStatus.Reserved)
            throw new InvalidOperationException($"Item is '{item.Status}', only Reserved items can be released.");

        item.Status = ItemStatus.InStock;
        item.ReservedForCathStudyId = null;
        item.ReservedAt = null;
        await _items.UpdateAsync(item);
    }

    /// Terminal transition: the device was actually implanted/used.
    /// Called by ProcedureService when a LesionTreatment is recorded -
    /// don't call this directly from a controller.
    public async Task ConsumeItemAsync(Guid inventoryItemId, Guid consumedBy)
    {
        var item = await _items.FindByIdAsync(inventoryItemId)
        ?? throw new InvalidOperationException("Inventory item not found.");

    if (item.Status != ItemStatus.Reserved)
        throw new InvalidOperationException($"Item is '{item.Status}', it must be Reserved to be consumed.");

        item.Status = ItemStatus.Consumed;
        item.ConsumedAt = DateTime.UtcNow;
        await _items.UpdateAsync(item);

        var product = await _products.FindByIdAsync(item.ProductId)
            ?? throw new InvalidOperationException("Product not found.");
        await RecordMovementAsync(product, MovementType.Consumption, quantityDelta: -1,
            note: $"Consumed {item.Barcode}", createdBy: consumedBy);
    }

    /// Opened but never implanted - sterile packaging broken, can't go back on the shelf.
    public async Task MarkWastedAsync(Guid inventoryItemId, string? note, Guid recordedBy)
    {
        var item = await _items.FindByIdAsync(inventoryItemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        if (item.Status != ItemStatus.Reserved)
            throw new InvalidOperationException("Only a reserved (opened) item can be marked wasted.");

        item.Status = ItemStatus.Wasted;
        await _items.UpdateAsync(item);

        var product = await _products.FindByIdAsync(item.ProductId)
            ?? throw new InvalidOperationException("Product not found.");
        await RecordMovementAsync(product, MovementType.Waste, quantityDelta: -1,
            note: note ?? $"Wasted {item.Barcode}", createdBy: recordedBy);
    }

    public async Task<List<InventoryItemDto>> GetExpiringItemsAsync(int withinDays)
    {
        var items = await _items.GetExpiringAsync(withinDays);
        return items.Select(i => ToDto(i, productName: null)).ToList();
    }

    private async Task RecordMovementAsync(InventoryProduct product, MovementType type, int quantityDelta, string? note, Guid createdBy)
    {
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            CreatedBy = createdBy,
            MovementType = type,
            Quantity = quantityDelta,
            ReferenceNote = note,
            MovedAt = DateTime.UtcNow
        };
        await _movements.CreateAsync(movement);

        // CurrentStock is a cache; the StockMovement rows remain the
        // source of truth (see the note on InventoryProduct in the diagram).
        product.CurrentStock += quantityDelta;
        await _products.UpdateAsync(product);
    }

    private static InventoryProductDto ToDto(InventoryProduct p) => new()
    {
        Id = p.Id,
        Reference = p.Reference,
        ProductName = p.ProductName,
        DeviceType = p.DeviceType.ToString(),
        Manufacturer = p.Manufacturer,
        UnitOfMeasure = p.UnitOfMeasure,
        MinimumStockLevel = p.MinimumStockLevel,
        CurrentStock = p.CurrentStock,
        CreatedAt = p.CreatedAt
    };

    private static InventoryItemDto ToDto(InventoryItem i, string? productName) => new()
    {
        Id = i.Id,
        ProductId = i.ProductId,
        ProductName = productName ?? i.Product?.ProductName ?? string.Empty,
        DeviceType = i.Product?.DeviceType.ToString() ?? string.Empty,
        Barcode = i.Barcode,
        LotNumber = i.LotNumber,
        ExpiryDate = i.ExpiryDate,
        SerialNumber = i.SerialNumber,
        Status = i.Status.ToString(),
        ReservedForCathStudyId = i.ReservedForCathStudyId,
        ReceivedAt = i.ReceivedAt,
        ReservedAt = i.ReservedAt,
        ConsumedAt = i.ConsumedAt
    };

    /// <summary>
/// Items currently reserved for a specific CathStudy - used to populate
/// the Stent/Balloon device dropdown in the segmentation UI, so the
/// cardiologist picks from what's already been scanned in for this
/// procedure rather than the entire catalog.
/// </summary>
public async Task<List<InventoryItemDto>> GetReservedItemsForStudyAsync(Guid cathStudyId)
{
    var items = await _items.GetReservedForStudyAsync(cathStudyId);
    return items.Select(i => ToDto(i, productName: null)).ToList();
}

public async Task<List<InventoryItemDto>> ReceiveBatchAsync(ReceiveInventoryBatchDto dto, Guid receivedBy)
{
    var results = new List<InventoryItemDto>();
    foreach (var line in dto.Items)
    {
        var item = await ReceiveItemAsync(new ReceiveInventoryItemDto
        {
            ProductId = dto.ProductId,
            Barcode = line.Barcode,
            LotNumber = line.LotNumber,
            ExpiryDate = line.ExpiryDate,
            SerialNumber = line.SerialNumber
        }, receivedBy);
        results.Add(item);
    }
    return results;
}
public async Task<List<InventoryItemDto>> GetAvailableItemsAsync(string? deviceType)
{
    DeviceType? parsed = null;
    if (!string.IsNullOrWhiteSpace(deviceType))
    {
        if (!Enum.TryParse<DeviceType>(deviceType, ignoreCase: true, out var dt))
            throw new ArgumentException($"Unknown device type '{deviceType}'.");
        parsed = dt;
    }

    var items = await _items.GetAvailableAsync(parsed);
    return items.Select(i => ToDto(i, productName: null)).ToList();
}
}