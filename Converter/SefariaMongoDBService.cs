using Converter.Model.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace Converter.Service
{
    //https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mongo-app?view=aspnetcore-3.1&tabs=visual-studio
    class SefariaMongoDBService
    {
        private readonly IMongoCollection<BsonDocument> _texts;
        public SefariaMongoDBService()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("sefaria");

            _texts = database.GetCollection<BsonDocument>("texts");
            var amount = _texts.CountDocuments(new BsonDocument());
        }

        public long TextsCount()
        {
            return _texts.CountDocuments(new BsonDocument()); 
        }

        public BsonDocument GetTextAt(int index) {
            return _texts.Find(_ => true).Skip(index).FirstOrDefault();
        }
    }
}

namespace Converter.Model.MongoDB
{
    [BsonIgnoreExtraElements]
    class Text {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id;

        [BsonElement("language")]
        public string Language;

        [BsonElement("title")]
        public string Title;

        [BsonElement("versionSource")]
        public string VersionSource;

        [BsonElement("versionTitle")]
        public string VersionTitle;

        [BsonElement("versionTitleInHebrew")]
        public string VersionTitleHebrew;

        [BsonElement("chapter")]
        [BsonRepresentation(BsonType.Document)]
        public List<Chapter> Chapter;
    }
    [BsonIgnoreExtraElements]
    class Chapter {

        public string Value;
        public List<Chapter> List;
    }


}

