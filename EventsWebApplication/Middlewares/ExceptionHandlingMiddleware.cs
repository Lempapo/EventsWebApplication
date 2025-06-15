using System.Net;
using EventsWebApplication.Exceptions;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ResourceNotFoundException resourceNotFoundException)
        {
            logger.LogWarning(resourceNotFoundException, resourceNotFoundException.Message);

            context.Response.ContentType = "text";
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            await context.Response.WriteAsync(resourceNotFoundException.Message);
        }
        catch (BusinessRuleViolationException businessRuleViolationException)
        {
            logger.LogWarning(businessRuleViolationException, businessRuleViolationException.Message);

            context.Response.ContentType = "text";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            await context.Response.WriteAsync(businessRuleViolationException.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred.",
                details = exception.Message 
            });

            await context.Response.WriteAsync(result);
        }
    }
}