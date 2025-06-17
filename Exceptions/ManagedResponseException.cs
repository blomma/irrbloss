using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Irrbloss.Exceptions;

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
            Detail = exception.Message,
        };

        ProblemDetails = problemDetails;
    }

    public ManagedResponseException(HttpStatusCode statusCode, string message)
    {
        var problemDetails = new ProblemDetails
        {
            Title = message,
            Status = (int)statusCode,
            Detail = message,
        };

        ProblemDetails = problemDetails;
    }
}
