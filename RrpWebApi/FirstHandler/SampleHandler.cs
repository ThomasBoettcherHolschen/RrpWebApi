namespace RrpWebApi.FirstHandler;

public class SampleHandler : HandlerBase<SampleRequest, SampleResponse> , IHandleChange<SampleRequest, SampleResponse>
{
    public Task<SampleResponse> HandleAsync(SampleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new SampleResponse { SomeOtherData = request.SomeData, SomeOtherIntData = request.SomeIntData});
    }
}