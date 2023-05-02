namespace Irrbloss.Exceptions;

using System;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Serializable]
public class ManagedResponseException : Exception
{
    public ProblemDetails ProblemDetails { get; set; } = new();

    public ManagedResponseException(Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Title = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message
        };

        ProblemDetails = problemDetails;
    }

    public ManagedResponseException(HttpStatusCode statusCode, string message)
    {
        var problemDetails = new ProblemDetails
        {
            Title = message,
            Status = (int)statusCode,
            Detail = message
        };

        ProblemDetails = problemDetails;
    }

    protected ManagedResponseException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
