// Value object: these fields are always used together, so they are
// one concept here.
// Four fields (ID, name, city, zip) that always describe the same real-world concept — the merchant where the transaction occurred. They are read and written as a group. An owned value object keeps the entity clean and communicates intent.
#nullable enable

namespace CardDemo.OnlineTransactionManagement.Domain;

public class MerchantInfo
{
    /// <summary>Merchant identifier</summary>
    public int MerchantId { get; set; }

    /// <summary>Merchant business name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Merchant city</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Merchant postal/ZIP code</summary>
    public string ZipCode { get; set; } = string.Empty;
}