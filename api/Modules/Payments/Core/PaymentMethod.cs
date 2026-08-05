using System.Text.Json.Serialization;

namespace InternetProvider.Api.Modules.Payments.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Mock,
    Mpesa,
    Airtel
}