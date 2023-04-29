using MongoDB.Bson.Serialization.Attributes;

public class Address
{
    public Location Location { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    [BsonElement("zipcode")]
    public string ZipCode { get; set; } = string.Empty;
}