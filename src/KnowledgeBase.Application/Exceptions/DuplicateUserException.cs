namespace KnowledgeBase.Application.Exceptions;

public class DuplicateUserException : Exception
{
    public DuplicateUserException(string fieldName, string value)
        : base($"A user with the same {fieldName} already exists.")
    {
        FieldName = fieldName;
        Value = value;
    }

    public string FieldName { get; }
    public string Value { get; }
}
