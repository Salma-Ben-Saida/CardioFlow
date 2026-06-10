public enum MovementType
{
    Reception,   // Stock went up (New shipment arrived)
    Consumption, // Stock went down (Used in a patient during a procedure)
    Return,      // Stock went up/down (Sent back to vendor or returned to shelf unused)
    Adjustment,  // Stock corrected (Manual inventory count fix)
    Waste,       // Stock went down (Item opened but contaminated, dropped, or damaged before use)
    Expiry       // Stock went down (Removed because it passed its expiration date)
}
