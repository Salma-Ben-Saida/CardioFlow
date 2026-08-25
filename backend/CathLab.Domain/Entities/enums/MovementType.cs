public enum MovementType 
{
    Reception,   // Stock went up (New shipment arrived)
    Consumption, // Stock went down (Used in a patient during a procedure)
    Return,      // Stock went up/down (Sent back to vendor or returned to shelf unused)
    Waste      // Stock went down (Item opened but contaminated, dropped, or damaged before use)
}
