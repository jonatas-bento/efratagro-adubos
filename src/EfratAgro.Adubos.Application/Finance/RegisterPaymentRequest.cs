using EfratAgro.Adubos.Domain.Finance;

namespace EfratAgro.Adubos.Application.Finance;

public sealed record RegisterPaymentRequest(
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    string? Notes);
