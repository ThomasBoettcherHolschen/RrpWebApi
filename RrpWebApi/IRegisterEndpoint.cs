namespace RrpWebApi;

public interface IRegisterEndpoint
{
    Task RegisterEndpoint(IEndpointRouteBuilder builder);
}