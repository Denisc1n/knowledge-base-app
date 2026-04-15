namespace KnowledgeBase.Application.Exceptions;

public class InvalidAdminOperationException : Exception
{
    public InvalidAdminOperationException(string message)
        : base(message)
    {
    }
}
