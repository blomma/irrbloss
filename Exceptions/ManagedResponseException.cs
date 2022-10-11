namespace Irrbloss.Exceptions;

using System;
using System.Net;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Serializable]
public class ManagedresponseException : Exception
{
    public ProblemDetails ProblemDetails { get; set; } = new();

    public ManagedresponseException(Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Title = exception.Message,
            Status = StatusCodes.Status500InternalServerError,
            Detail = exception.Message
        };

        ProblemDetails = problemDetails;
    }

    public ManagedresponseException(HttpStatusCode statusCode, string message)
    {
        var problemDetails = new ProblemDetails
        {
            Title = message,
            Status = (int)statusCode,
            Detail = message
        };

        ProblemDetails = problemDetails;
    }

    protected ManagedresponseException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
