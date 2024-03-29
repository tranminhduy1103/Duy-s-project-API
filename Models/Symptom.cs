using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DuyProject.API.Models
{
    public class Symptom : EntityBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string ReferenceImage { get; set; }
    }
}