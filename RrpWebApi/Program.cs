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
    // register all handlers. Handlers should inherit from HandlerBase. Handlerbase implements IRegisterEndpoint. 
    // so all handlers will be registered as IRegisterEndpoint.
    // this gives us the ability to loop through all the registrations of IRegisterEndpoint and call RegisterEndpoint on each one.
    // so all handlers got WebApi Endpoints registered automatically.
    containerBuilder.RegisterAssemblyTypes(assembly).AsClosedTypesOf(typeof(IHandleChange<,>)).AsImplementedInterfaces();
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


