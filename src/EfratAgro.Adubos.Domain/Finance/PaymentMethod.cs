namespace EfratAgro.Adubos.Domain.Finance;

public enum PaymentMethod
{
    Unspecified = 0,
    Pix = 1,
    Cash = 2,
    Card = 3,
    BankTransfer = 4,
    Boleto = 5,
    Check = 6,
    Other = 99
}
