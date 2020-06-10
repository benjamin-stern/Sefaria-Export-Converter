using Converter.Model.SQLite;
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
        private IMongoCollection<BsonDocument> _summaries;
        private MongoClient _client;
        public SefariaMongoDBService()
        {
            
            _client = new MongoClient("mongodb://localhost:27017");
            var database = _client.GetDatabase("sefaria");

            _texts = database.GetCollection<BsonDocument>("texts");
            _summaries = database.GetCollection<BsonDocument>("summaries");
            //var amount = _texts.CountDocuments(new BsonDocument());
        }

        public Topic GetSummaryTopics()
        {
            //Topic root = new Topic() { Name = "toc" };
            Topic root = GetNestedTopics(_summaries.Find(_ => true).FirstOrDefault());
            return root;
        }

        private Topic GetNestedTopics(BsonDocument document, Topic parent = null, int index = 1)
        {
            Topic instance = new Topic { Index = index };
            if (parent != null)
            {
                instance.ParentTopic = parent;
            }

            instance.Children = new List<Topic>();
            instance.LabelGroup = new LabelGroup();
            instance.LabelGroup.Labels = new List<Label>();
            for (int i = 0; i < document.Elements.Count(); i++)
            {
                var element = document.GetElement(i);
                        
                switch (element.Name)
                {
                    case "name":
                        instance.Name = element.Value.AsString;
                        break;
                    case "category":
                        instance.Name = element.Value.AsString;
                        instance.LabelGroup.Labels.Add(new Label { LanguageId = (int)LanguageTypes.English, Text = element.Value.AsString });
                        break;
                    case "heCategory":
                        instance.LabelGroup.Labels.Add(new Label { LanguageId = (int)LanguageTypes.Hebrew, Text = element.Value.AsString });
                        break;
                    case "title":
                        instance.Name = element.Value.AsString;
                        instance.LabelGroup.Labels.Add(new Label { LanguageId = (int)LanguageTypes.English, Text = element.Value.AsString });
                        break;
                    case "heTitle":
                        instance.LabelGroup.Labels.Add(new Label { LanguageId = (int)LanguageTypes.Hebrew, Text = element.Value.AsString });
                        break;
                    case "contents":
                        var array = element.Value.AsBsonArray;
                        instance.Children = new List<Topic>();
                        for (int j = 0; j < array.Count; j++)
                        {
                            var item = array[j];
                            if(item.IsBsonDocument) instance.Children.Add(GetNestedTopics(item.AsBsonDocument, instance, j+1));
                        }
                        break;
                }
                        
            }

            return instance;
        }

        public long TextsCount()
        {
            return _texts.CountDocuments(new BsonDocument()); 
        }

        public Text GetTextAt(int index, SefariaSQLiteConversionContext targetContext) {
            Text text = new Text();
            LabelGroup versionTitleLG = new LabelGroup();
            versionTitleLG.Labels = new List<Label>();
            BsonDocument value = _texts.Find(_ => true).Skip(index).FirstOrDefault();
            foreach (var element in value.Elements)
            {
                switch (element.Name)
                {
                    case "title":
                        //text.Title = element.Value.AsString;
                        Topic topic = targetContext.Topics.Where(t => t.Name == element.Value.AsString).FirstOrDefault();
                        if (topic != null)
                        {
                            text.TopicId = topic.Id;
                        }
                        else {
                            //topic = targetContext.Texts.Select(txt=>txt.Topic).Where(t => t.Name == element.Value.AsString).FirstOrDefault();
                            //if (topic != null) {
                            //    text.TopicId = topic.Id;
                            //} else {
                                text.Topic = new Topic { Name = element.Value.AsString, LabelGroup = versionTitleLG };
                                targetContext.Topics.Add(text.Topic);
                            //}
                        }
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
                    case "license":
                        text.License=element.Value.AsString;
                        break;
                    case "versionTitle":
                        versionTitleLG.Labels.Add(new Label { LanguageId = (int)LanguageTypes.English, Text = element.Value.AsString });
                        text.VersionTitle = versionTitleLG;
                        break;
                    case "versionTitleInHebrew":
                        versionTitleLG.Labels.Add(new Label { LanguageId = (int)LanguageTypes.Hebrew, Text = element.Value.AsString });
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

        private Chapter GenerateChapterTree(BsonValue value, Chapter parent = null, int index = 1)
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
                        instance.Children.Add(GenerateChapterTree(array[i], instance, i+1));
                        instance.HasChild = true;
                    }
                    break;
                case BsonType.Document:
                    var document = value.AsBsonDocument;
                    instance.Children = new List<Chapter>();
                    for (int i = 0; i < document.Elements.Count();i++)
                    {
                        var element = document.GetElement(i);
                        var child = GenerateChapterTree(element.Value, instance, i+1);
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


