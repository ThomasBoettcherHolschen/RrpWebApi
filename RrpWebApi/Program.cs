// See https://aka.ms/new-console-template for more information

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

// Order seems to matter here. UseServiceProviderFactory must be after all things that register in services.  
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{

    containerBuilder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IHandleChange<,>)).AsImplementedInterfaces();

    // get all closed types of IHandlerChange<,> 
    var handlerTypes = assembly.GetTypes().Where(t => t.IsClosedTypeOf(typeof(IHandleChange<,>)));
    foreach (var handlerType in handlerTypes)
    {
        // get the generic arguments of the IHandleChange<,> interface that is implemented by the handler type.
        var genericArguments = handlerType.GetInterfaces()
            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleChange<,>))
            .GetGenericArguments();
        
        // get the generic type definition of HandlerRegisterer
        var registererType = typeof(HandlerRegisterer<,>);
        // create a closed type of HandlerRegisterer with the generic arguments of the handler type.
        var closedRegistererType = registererType.MakeGenericType(genericArguments);
        // register the closed type of HandlerRegisterer as IRegisterEndpoint
        containerBuilder.RegisterType(closedRegistererType).As<IRegisterEndpoint>();
    }
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Loop through all the registrations of IRegisterEndpoint and call RegisterEndpoint on each one.
var endpointRegistrations = app.Services.GetServices<IRegisterEndpoint>();
foreach (var registration in endpointRegistrations)
{
    await registration.RegisterEndpoint(app);
}

app.Run();


