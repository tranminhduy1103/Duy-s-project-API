using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DuyProject.API.Models
{
    public class Logo : EntityBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        public string ImagePath { get; set; }
    }
}