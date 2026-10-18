namespace CathLab.Application.Common;

public class DuplicateEntityException : Exception
{
    public DuplicateEntityException(string message, Exception? inner = null)
        : base(message, inner) { }
}