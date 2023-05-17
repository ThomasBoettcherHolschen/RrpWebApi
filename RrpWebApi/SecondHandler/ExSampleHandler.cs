namespace RrpWebApi.SecondHandler;

public class ExSampleHandler : IHandleChange<ExSampleRequest, ExSampleResponse>
{
    public Task<ExSampleResponse> HandleAsync(ExSampleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ExSampleResponse { SomeOtherData = request.SomeData, SomeOtherIntData = request.SomeIntData, DateTime = request.DateTime});
    }
}