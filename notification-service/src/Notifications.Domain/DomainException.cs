using System;

namespace Notifications.Domain;

public sealed class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string? message = null) : base(message ?? code)
    {
        Code = code;
    }
}
