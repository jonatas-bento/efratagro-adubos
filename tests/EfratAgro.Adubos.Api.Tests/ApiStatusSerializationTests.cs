using System.Text.Json;
using EfratAgro.Adubos.Application.Deliveries;
using EfratAgro.Adubos.Application.Finance;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EfratAgro.Adubos.Api.Tests;

public sealed class ApiStatusSerializationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly JsonSerializerOptions _serializerOptions;

    public ApiStatusSerializationTests(
        WebApplicationFactory<Program> factory)
    {
        _serializerOptions =
            factory.Services
                .GetRequiredService<IOptions<JsonOptions>>()
                .Value
                .SerializerOptions;
    }

    [Theory]
    [InlineData(DeliveryStatusCode.Unspecified, 0)]
    [InlineData(DeliveryStatusCode.Pending, 1)]
    [InlineData(DeliveryStatusCode.Delivered, 2)]
    [InlineData(DeliveryStatusCode.NotTracked, 3)]
    public void DeliveryStatusCode_ShouldSerializeAsNumber(
        DeliveryStatusCode status,
        int expected)
    {
        AssertNumericStatusCode(
            status,
            expected);
    }

    [Theory]
    [InlineData(ReceivableStatus.Pending, 1)]
    [InlineData(ReceivableStatus.Partial, 2)]
    [InlineData(ReceivableStatus.Overdue, 3)]
    [InlineData(ReceivableStatus.PartialOverdue, 4)]
    [InlineData(ReceivableStatus.Paid, 5)]
    public void ReceivableStatus_ShouldSerializeAsNumber(
        ReceivableStatus status,
        int expected)
    {
        AssertNumericStatusCode(
            status,
            expected);
    }

    private void AssertNumericStatusCode<TStatus>(
        TStatus status,
        int expected)
        where TStatus : struct, Enum
    {
        var json =
            JsonSerializer.Serialize(
                new StatusEnvelope<TStatus>(
                    status),
                _serializerOptions);

        using var document =
            JsonDocument.Parse(json);

        Assert.True(
            document.RootElement.TryGetProperty(
                "statusCode",
                out var statusCode),
            $"Expected JSON property 'statusCode'. JSON: {json}");

        Assert.Equal(
            JsonValueKind.Number,
            statusCode.ValueKind);

        Assert.Equal(
            expected,
            statusCode.GetInt32());
    }

    private sealed record StatusEnvelope<TStatus>(
        TStatus StatusCode)
        where TStatus : struct, Enum;
}
