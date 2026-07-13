namespace CathLab.Application.Interfaces;

/// <summary>
/// Wraps multi-step writes (e.g. consume inventory item + record treatment +
/// mark lesion treated) in a single DB transaction so a crash mid-sequence
/// can't leave stock consumed with no matching treatment record.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> operation);
}