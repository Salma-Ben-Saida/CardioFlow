/// <summary>
/// Lifecycle of a single physical InventoryItem:
///   InStock  -> Reserved  (selected by the cardiologist in the segmentation UI,
///                          before the procedure report is finalized)
///   Reserved -> Consumed  (report finalized, device actually implanted)
///   Reserved -> Wasted    (opened but never implanted - can't return to stock)
///   Reserved -> InStock   (deselected before the packaging was opened)
/// </summary>
public enum ItemStatus
{
    InStock,
    Reserved,
    Consumed,
    Wasted,
    Expired
}