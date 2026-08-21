// Value object: these fields are always used together, so they are
// one concept here.
// Four merchant fields (ID, name, city, zip) form a coherent concept always used together; modelling as a value object keeps the entity clean and communicates intent
#nullable enable

namespace CardDemo.OnlineTransactionManagement.Domain;

public class MerchantInfo
{
    /// <summary>Merchant identifier</summary>
    public int Id { get; set; }

    /// <summary>Merchant business name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Merchant city</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Merchant postal code</summary>
    public string ZipCode { get; set; } = string.Empty;
}