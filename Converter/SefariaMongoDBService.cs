﻿using Converter.Model.SQLite;
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
        private IMongoCollection<BsonDocument> _texts;
        private MongoClient _client;
        public SefariaMongoDBService()
        {
            
            _client = new MongoClient("mongodb://localhost:27017");
            var database = _client.GetDatabase("sefaria");

            _texts = database.GetCollection<BsonDocument>("texts");
            //var amount = _texts.CountDocuments(new BsonDocument());
        }

        public long TextsCount()
        {
            return _texts.CountDocuments(new BsonDocument()); 
        }

        public Text GetTextAt(int index) {
            Text text = new Text();
            LabelGroup versionTitleLG = new LabelGroup();
            versionTitleLG.Labels = new List<Label>();
            BsonDocument value = _texts.Find(_ => true).Skip(index).FirstOrDefault();
            foreach (var element in value.Elements)
            {
                switch (element.Name)
                {
                    case "title":
                        text.Title = element.Value.AsString;
                        break;
                    case "priority":
                        text.Priority = element.Value.ToInt32();
                        break;
                    case "versionNotes":
                        text.VersionNotes= element.Value.AsString;
                        break;
                    case "versionSource":
                        text.VersionSource = element.Value.BsonType != BsonType.Null? element.Value.AsString:null;
                        break;
                    case "language":
                        switch (element.Value.AsString)
                        {
                            case "en":
                                text.LanguageId = (int)LanguageTypes.English;
                                break;
                            case "he":
                                text.LanguageId = (int)LanguageTypes.Hebrew;
                                break;
                            default:
                                text.LanguageId = (int)LanguageTypes.Undefined;
                                break;
                        }
                        break;
                    case "versionTitle":
                        versionTitleLG.Labels.Add(new Label { LanguageId = (int)LanguageTypes.English, Text = element.Value.AsString });
                        text.VersionTitle = versionTitleLG;
                        break;
                    case "versionTitleInHebrew":
                        versionTitleLG.Labels.Add(new Label { LanguageId = (int)LanguageTypes.Hebrew, Text = element.Value.AsString });
                        text.VersionTitle = versionTitleLG;
                        break;
                    case "chapter":
                        text.Chapter = GenerateChapterTree(element.Value);
                        break;
                    default:
                        break;
                    
                }
            }
            
            return text;
        }

        private Chapter GenerateChapterTree(BsonValue value, Chapter parent = null, int index = 0)
        {
            Chapter instance = new Chapter { Index = index };
            if (parent != null) {
                instance.ParentChapter = parent;
            }

            switch (value.BsonType) {
                case BsonType.Array:
                    var array = value.AsBsonArray;
                    instance.Children = new List<Chapter>();
                    for (int i = 0; i < array.Count; i++)
                    {
                        instance.Children.Add(GenerateChapterTree(array[i], instance, i));
                        instance.HasChild = true;
                    }
                    break;
                case BsonType.Document:
                    var document = value.AsBsonDocument;
                    instance.Children = new List<Chapter>();
                    for (int i = 0; i < document.Elements.Count();i++)
                    {
                        var element = document.GetElement(i);
                        var child = GenerateChapterTree(element.Value, instance, i);
                        child.Text = element.Name;
                        instance.Children.Add(child);
                    }
                    
                    break;
                case BsonType.String:
                    instance.Text = value.AsString;
                    break;
            }

            return instance;
        }

        //public void Dispose() {
            //_client.Cluster.Dispose();
            //_client = null;
            
            //
            //_texts = null;

        //}
    }
}


