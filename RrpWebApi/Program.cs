// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using RrpWebApi;
using Serilog;

var assembly = System.Reflection.Assembly.GetExecutingAssembly();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Order seems to matter here.  
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IHandleChange<,>));
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

var logger = app.Services.GetService<ILogger<Program>>();
var handlers = assembly.GetTypes().Where(item => item.IsClosedTypeOf(typeof(IHandleChange<,>)));
foreach (var handlerType in handlers)
{
    logger.LogInformation("Found handler {Handler}, Arguments:{@Arguments}", handlerType.FullName, handlerType.GetGenericArguments());
    
    var implementedInterface = handlerType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleChange<,>));

    if (implementedInterface != null)
    {
        var requestType = implementedInterface.GetGenericArguments().First(); 
        var responseType = implementedInterface.GenericTypeArguments.Last();


        app.MapPost($"/api/{requestType.Name}", async (HttpContext context) =>
            {
                var handler = context.RequestServices.GetService(handlerType);
                var request = await JsonSerializer.DeserializeAsync(context.Request.Body, requestType,
                    cancellationToken: context.RequestAborted);
                var method = handlerType.GetMethod("HandleAsync");
                var response = await (dynamic) method.Invoke(handler, new[] { request, context.RequestAborted });
                return TypedResults.Ok(response);
            })
            .Accepts(requestType, "application/json")
            .Produces(StatusCodes.Status200OK, responseType)
            .Produces(StatusCodes.Status400BadRequest)
            .WithDescription(
                $"Request Reponse Pattern using Request : {requestType.Name} and Response : {responseType.Name}")
            .WithName(requestType.Name);

    }
}
app.Run();
