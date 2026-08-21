using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using CardDemo.OnlineTransactionManagement.Application;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

public static class EndpointMapping
{
    public static IEndpointRouteBuilder MapCardDemoOnlineTransactionManagement(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/transactions");

        group.MapPost("/add", async (AddTransactionInput input, ITransactionService svc, CancellationToken ct) =>
        {
            var result = await svc.AddTransactionAsync(input, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapPost("/pay", async (MakePaymentInput input, ITransactionService svc, CancellationToken ct) =>
        {
            var result = await svc.MakePaymentAsync(input, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        return endpoints;
    }
}
