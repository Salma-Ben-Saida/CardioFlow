using CathLab.Application.Common;
using CathLab.Application.DTOs.Pharmacy;
using CathLab.Application.Interfaces;
using CathLab.Domain.Entities;
using CathLab.Domain.Repositories;

namespace CathLab.Application.Services;

public class InventoryService
{
    private readonly IInventoryProductRepository _products;
    private readonly IInventoryItemRepository _items;
    private readonly IStockMovementRepository _movements;
    private readonly IUserRepository _users;

    public InventoryService(
        IInventoryProductRepository products,
        IInventoryItemRepository items,
        IStockMovementRepository movements,
        IUserRepository users
    )
    {
        _products = products;
        _items = items;
        _movements = movements;
        _users = users;
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

    public async Task<InventoryProductDto> UpdateProductAsync(Guid id, UpdateInventoryProductDto dto)
    {
        var product = await _products.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found.");

        if (!string.IsNullOrWhiteSpace(dto.ProductName))
            product.ProductName = dto.ProductName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Reference))
        {
            var reference = dto.Reference.Trim();
            if (!string.Equals(product.Reference, reference, StringComparison.Ordinal))
            {
                var existing = await _products.FindByReferenceAsync(reference);
                if (existing is not null && existing.Id != id)
                    throw new InvalidOperationException("A product with this reference already exists.");
            }
            product.Reference = reference;
        }

        if (!string.IsNullOrWhiteSpace(dto.DeviceType))
        {
            if (!Enum.TryParse<DeviceType>(dto.DeviceType, ignoreCase: true, out var deviceType))
                throw new ArgumentException($"Unknown device type '{dto.DeviceType}'.");
            product.DeviceType = deviceType;
        }

        if (dto.Manufacturer != null)
            product.Manufacturer = string.IsNullOrWhiteSpace(dto.Manufacturer) ? null : dto.Manufacturer.Trim();

        if (dto.UnitOfMeasure != null)
            product.UnitOfMeasure = string.IsNullOrWhiteSpace(dto.UnitOfMeasure) ? null : dto.UnitOfMeasure.Trim();

        if (dto.MinimumStockLevel.HasValue)
            product.MinimumStockLevel = dto.MinimumStockLevel.Value;

        await _products.UpdateAsync(product);
        return ToDto(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _products.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Product not found.");

        var itemCount = product.Items.Count;
        if (itemCount > 0)
            throw new InvalidOperationException("Cannot delete a product that still has inventory items attached.");

        await _products.DeleteAsync(product);
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

    public async Task<InventoryItemDto> UpdateItemAsync(Guid id, UpdateInventoryItemDto dto)
    {
        var item = await _items.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Inventory item not found.");

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var barcode = dto.Barcode.Trim();
            if (!string.Equals(item.Barcode, barcode, StringComparison.Ordinal))
            {
                var duplicate = await _items.FindByBarcodeAsync(barcode);
                if (duplicate is not null && duplicate.Id != id)
                    throw new InvalidOperationException("An item with this barcode already exists.");
            }

            item.Barcode = barcode;
        }

        if (dto.LotNumber != null)
            item.LotNumber = string.IsNullOrWhiteSpace(dto.LotNumber) ? null : dto.LotNumber.Trim();

        if (dto.ExpiryDate.HasValue)
            item.ExpiryDate = dto.ExpiryDate.Value;

        if (dto.SerialNumber != null)
            item.SerialNumber = string.IsNullOrWhiteSpace(dto.SerialNumber) ? null : dto.SerialNumber.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            if (!Enum.TryParse<ItemStatus>(dto.Status, ignoreCase: true, out var status))
                throw new ArgumentException($"Unknown item status '{dto.Status}'.");

            item.Status = status;
        }

        await _items.UpdateAsync(item);
        return ToDto(item, productName: null);
    }

    public async Task DeleteItemAsync(Guid id)
    {
        var item = await _items.FindByIdAsync(id)
            ?? throw new KeyNotFoundException("Inventory item not found.");

        await _items.DeleteAsync(item);
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
public async Task<List<InventoryItemDto>> GetAllReservedItemsAsync()
{
    var items = await _items.GetAllReservedAsync();
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

    /// <summary>
    /// Releases a reserved inventory item back to stock without recording a
    /// stock movement. Used when a planned treatment or lesion is deleted.
    /// </summary>
    internal async Task ReleaseItemAsync(Guid inventoryItemId)
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

    public async Task<PagedResult<StockMovementDto>> GetStockMovementsAsync(int page, int pageSize)
    {
        var (items, total) = await _movements.GetListAsync(page, pageSize);
        var inventoryItems = await _items.GetAllAsync();
        var dtos = new List<StockMovementDto>();

        foreach (var m in items)
        {
            var user = await _users.GetByIdAsync(m.CreatedBy);
            var username = user?.Username ?? string.Empty;
            var fullName = user?.FullName ?? string.Empty;
            var roleName = user?.Role.ToString();
            var displayName = !string.IsNullOrWhiteSpace(username)
                ? username
                : !string.IsNullOrWhiteSpace(fullName)
                    ? string.IsNullOrWhiteSpace(roleName)
                        ? fullName
                        : $"{fullName} ({roleName})"
                    : roleName ?? string.Empty;

            var productLabel = BuildInventoryItemLabel(m, inventoryItems);

            dtos.Add(new StockMovementDto
            {
                Id = m.Id,
                ProductId = m.ProductId,
                ProductName = productLabel,
                MovementType = m.MovementType.ToString(),
                Quantity = m.Quantity,
                ReferenceNote = null,
                MovedAt = m.MovedAt,
                CreatedBy = m.CreatedBy,
                actorName = displayName
            });
        }

        return new PagedResult<StockMovementDto>(dtos, total, page, pageSize);
    }

    private static string BuildInventoryItemLabel(StockMovement movement, IEnumerable<InventoryItem> inventoryItems)
    {
        var note = movement.ReferenceNote ?? string.Empty;
        var item = inventoryItems
            .Where(i => i.ProductId == movement.ProductId)
            .OrderBy(i => string.IsNullOrWhiteSpace(i.Barcode) ? 1 : 0)
            .FirstOrDefault(i =>
                !string.IsNullOrWhiteSpace(i.Barcode) &&
                note.Contains(i.Barcode, StringComparison.OrdinalIgnoreCase))
            ?? inventoryItems
                .Where(i => i.ProductId == movement.ProductId)
                .OrderBy(i => string.IsNullOrWhiteSpace(i.Barcode) ? 1 : 0)
                .FirstOrDefault();

        if (item is null)
            return string.Empty;

        var lot = string.IsNullOrWhiteSpace(item.LotNumber) ? "—" : item.LotNumber;
        return $"{item.Barcode}: {lot}";
    }

    public async Task<List<InventoryItemDto>> GetAllItemsAsync(string? status, string? deviceType)
    {
        // Parse status if provided
        ItemStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var s))
                throw new ArgumentException($"Unknown item status '{status}'.");
            parsedStatus = s;
        }

        // Parse deviceType if provided
        DeviceType? parsedDeviceType = null;
        if (!string.IsNullOrWhiteSpace(deviceType))
        {
            if (!Enum.TryParse<DeviceType>(deviceType, ignoreCase: true, out var dt))
                throw new ArgumentException($"Unknown device type '{deviceType}'.");
            parsedDeviceType = dt;
        }

        var items = await _items.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Filter by status if provided
        if (parsedStatus.HasValue)
        {
            switch (parsedStatus.Value)
            {
                case ItemStatus.Expired:
                    items = items.Where(i =>
                        i.Status == ItemStatus.Expired ||
                        (i.Status == ItemStatus.InStock && i.ExpiryDate <= today)
                    ).ToList();
                    break;
                case ItemStatus.InStock:
                    items = items.Where(i => i.Status == ItemStatus.InStock && i.ExpiryDate > today).ToList();
                    break;
                default:
                    items = items.Where(i => i.Status == parsedStatus.Value).ToList();
                    break;
            }
        }

        // Filter by deviceType if provided
        if (parsedDeviceType.HasValue)
        {
            items = items.Where(i => i.Product?.DeviceType == parsedDeviceType.Value).ToList();
        }

        return items.Select(i => ToDto(i, productName: null)).ToList();
    }
}