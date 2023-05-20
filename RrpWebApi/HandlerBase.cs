using Microsoft.AspNetCore.Mvc;

namespace RrpWebApi;

// Handlerbase is a generic class that implements IRegisterEndpoint and is used to register endpoints
public class HandlerBase<TRequest, TResponse> : IRegisterEndpoint
{
    public Task RegisterEndpoint(IEndpointRouteBuilder builder)
    {
        // we get the IEndpointRouteBuilder as parameter.
        // create a delegate that acts as method called by each web request
        Func<TRequest, IHandleChange<TRequest, TResponse>, Task<TResponse>> Handler()
        {
            // since handler is injected with [FromServices] here we don't have to care about this instance.
            // It is just a vehicle to get the handler injected
            return async (request, [FromServices] handler) => 
                await handler.HandleAsync(request, CancellationToken.None).ConfigureAwait(false);
        }
        
        builder.MapPost($"/api/{typeof(TRequest).Name}", Handler());
        return Task.CompletedTask;
    }
}