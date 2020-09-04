using Converter.Model.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Converter.Service
{
    //https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mongo-app?view=aspnetcore-3.1&tabs=visual-studio
    class SefariaMongoDBService
    {
        private IMongoCollection<BsonDocument> _texts;
        private IMongoCollection<BsonDocument> _summaries;
        private IMongoCollection<BsonDocument> _links;

        private MongoClient _client;
        public SefariaMongoDBService()
        {
            
            _client = new MongoClient("mongodb://localhost:27017");
            var database = _client.GetDatabase("sefaria");

            _texts = database.GetCollection<BsonDocument>("texts");
            _summaries = database.GetCollection<BsonDocument>("summaries");
            _links = database.GetCollection<BsonDocument>("links");

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
                        var titleName = element.Value.AsString;
                        Topic topic = targetContext.FindFirstOrDefaultWhere(targetContext.Topics, t => t.Name == titleName);
                        if (topic != null)
                        {
                            text.Topic = topic;
                        }
                        else {
                            text.Topic = new Topic { Name = titleName, LabelGroup = versionTitleLG };
                            targetContext.Add(targetContext.Topics,text.Topic);
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

        public long LinksCount() {
            return _links.CountDocuments(new BsonDocument());
        }

        public List<BsonDocument> GetLinks() {
            return _links.Find(_ => true).ToList();
        }

        public LinkItem ParseLink(BsonDocument value, SefariaSQLiteConversionContext targetContext) {
            try
            {
                LinkItem link = new LinkItem();
                link.LinkGroup = new LinkGroup();

                //BsonDocument value = _links.Find(_ => true).Skip(index).FirstOrDefault();
                string PrimaryTopic = null;
                string SecondaryTopic = null;
                BsonValue availableLangs = null;
                foreach (var element in value.Elements)
                {
                    try
                    {
                        //Need to fix for { "_id" : ObjectId("51de8ce8edbab4523cea399f")}
                        switch (element.Name)
                        {
                            case "availableLangs":
                                availableLangs = element.Value;
                                break;
                            case "expandedRefs0":
                                PrimaryTopic = element.Value.AsBsonArray.Values.ToList()[0].AsString;
                                break;
                            case "expandedRefs1":
                                SecondaryTopic = element.Value.AsBsonArray.Values.ToList()[0].AsString;
                                break;
                            case "refs":
                                var refs = element.Value.AsBsonArray.Values.ToList();
                                if (refs != null && refs.Count >= 2)
                                {
                                    PrimaryTopic = refs[0].AsString;
                                    SecondaryTopic = refs[1].AsString;
                                }
                                break;
                            default:
                                break;

                        }
                    }
                    catch (Exception e)
                    {
                        string json = value?.ToString()??"";
                        Console.WriteLine($"GetLinkAt {json}, element: {element.Name}, Exception: {e}");
                    }

                }

                if (PrimaryTopic == null || SecondaryTopic == null)
                {
                    return null;
                }

                var primaryTopicSeperator = PrimaryTopic.LastIndexOf(' ');
                string primaryTopicName = PrimaryTopic.Substring(0, primaryTopicSeperator);
                string primaryTopicLocation = PrimaryTopic.Substring(primaryTopicSeperator + 1);
                int primaryTopicId = targetContext.Topics.Where(t => t.Name == primaryTopicName).Select(t => t.Id).FirstOrDefault();

                var secondaryTopicSeperator = SecondaryTopic.LastIndexOf(' ');
                string secondaryTopicName = SecondaryTopic.Substring(0, secondaryTopicSeperator);
                string secondaryTopicLocation = SecondaryTopic.Substring(secondaryTopicSeperator + 1);
                int secondaryTopicId = targetContext.Topics.Where(t => t.Name == secondaryTopicName).Select(t => t.Id).FirstOrDefault();

                link.PrimaryLocation = primaryTopicLocation;
                link.SecondaryLocation = secondaryTopicLocation;

                bool isLinkGroupNew = false;
                if (primaryTopicId != 0 && secondaryTopicId != 0)
                {
                    link.LinkGroup = targetContext.FindTrackedFirstOrDefaultWhere(targetContext.LinkGroups, (lg => lg.PrimaryTopicId == primaryTopicId && lg.SecondaryTopicId == secondaryTopicId));
                    if (link.LinkGroup == null)
                    {
                        link.LinkGroup = targetContext.LinkGroups
                            .Where(lg => lg.PrimaryTopicId == primaryTopicId && lg.SecondaryTopicId == secondaryTopicId)
                            .Include(lg => lg.LinkedLanguages).FirstOrDefault();
                    }
                    if (link.LinkGroup == null)
                    {
                        isLinkGroupNew = true;
                        link.LinkGroup = new LinkGroup
                        {
                            PrimaryTopicId = primaryTopicId,
                            SecondaryTopicId = secondaryTopicId
                        };
                        targetContext.Add(targetContext.LinkGroups, link.LinkGroup);
                    }

                    if (availableLangs != null && availableLangs.IsBsonArray)
                    {
                        if (link.LinkGroup.LinkedLanguages == null)
                        {
                            link.LinkGroup.LinkedLanguages = new List<LinkLanguage>();
                        }
                        if (availableLangs.AsBsonArray.Count >= 1 && availableLangs.AsBsonArray[0].IsBsonArray)
                        {
                            foreach (var item in availableLangs.AsBsonArray[0].AsBsonArray)
                            {
                                LinkLanguage linkLanguage = null;
                                int languageId = GetLanguageIdByString(item.AsString);
                                //linkLanguage = targetContext.FindFirstOrDefaultWhere(targetContext.LinkLanguages, (l => l.LinkGroup == link.LinkGroup && l.TopicId == primaryTopicId && l.LanguageId == languageId));
                                linkLanguage = link.LinkGroup.LinkedLanguages.Where(l => l.TopicId == primaryTopicId && l.LanguageId == languageId).FirstOrDefault();
                                if (linkLanguage == null)
                                {
                                    linkLanguage = new LinkLanguage
                                    {
                                        LinkGroup = link.LinkGroup,
                                        LanguageId = languageId,
                                        TopicId = primaryTopicId
                                    };
                                    link.LinkGroup.LinkedLanguages.Add(linkLanguage);
                                    targetContext.Add(targetContext.LinkLanguages, linkLanguage);
                                }
                                linkLanguage.Count++;
                            }
                        }
                        if (availableLangs.AsBsonArray.Count >= 2 && availableLangs.AsBsonArray[1].IsBsonArray)
                        {
                            foreach (var item in availableLangs.AsBsonArray[1].AsBsonArray)
                            {
                                LinkLanguage linkLanguage = null;
                                int languageId = GetLanguageIdByString(item.AsString);
                                //linkLanguage = targetContext.FindFirstOrDefaultWhere(targetContext.LinkLanguages, (l => l.LinkGroup == link.LinkGroup && l.TopicId == secondaryTopicId && l.LanguageId == languageId));
                                linkLanguage = link.LinkGroup.LinkedLanguages.Where(l => l.TopicId == secondaryTopicId && l.LanguageId == languageId).FirstOrDefault();
                                if (linkLanguage == null)
                                {
                                    linkLanguage = new LinkLanguage
                                    {
                                        LinkGroup = link.LinkGroup,
                                        LanguageId = languageId,
                                        TopicId = secondaryTopicId
                                    };
                                    link.LinkGroup.LinkedLanguages.Add(linkLanguage);
                                    targetContext.Add(targetContext.LinkLanguages, linkLanguage);
                                }
                                linkLanguage.Count++;
                            }
                        }
                    }
                }

                return link;
            }
            catch (Exception ex) {
                Console.WriteLine($"GetLinkAt: Exception: {ex}");
            }

            return null;
        }

        private int GetLanguageIdByString(string value) {
            int result = (int)LanguageTypes.Undefined;
            switch (value.ToLower())
            {
                case "he":
                    result = (int)LanguageTypes.Hebrew;
                    break;
                case "en":
                    result = (int)LanguageTypes.English;
                    break;
            }

            return result;
        }

        private T FindFirstOrDefaultWhere<T>(DbSet<T> target, Func<T, bool> predicate) where T: class
        {
            var resultLocal = target.Local.Where(predicate).FirstOrDefault();
            if (resultLocal != null) return resultLocal; 
            return target.Where(predicate).FirstOrDefault();
        }

        //public void Dispose() {
            //_client.Cluster.Dispose();
            //_client = null;
            
            //
            //_texts = null;

        //}
    }
}


