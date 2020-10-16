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
        private IMongoCollection<BsonDocument> _index;
        private IMongoCollection<BsonDocument> _links;

        private MongoClient _client;
        public SefariaMongoDBService()
        {
            
            _client = new MongoClient("mongodb://localhost:27017");
            var database = _client.GetDatabase("sefaria");

            _texts = database.GetCollection<BsonDocument>("texts");
            _summaries = database.GetCollection<BsonDocument>("summaries");
            _index = database.GetCollection<BsonDocument>("index");
            _links = database.GetCollection<BsonDocument>("links");

            //var amount = _texts.CountDocuments(new BsonDocument());
        }

        public Topic GetSummaryTopics()
        {
            //Topic root = new Topic() { Name = "toc" };
            Topic root = GetNestedTopics(_summaries.Find(_ => true).FirstOrDefault());

            
            //root.

            return root;
        }

        public void UpdateTopicsWithIndexData(SefariaSQLiteConversionContext targetContext) {
            
            //TODO: Need to parse Index and retrieve all alternative names with primary prioritizations
            var indexedList = _index.Find(_ => true).ToList();
            var topics = targetContext.Topics.Where(_ => true).ToList();

            for (int j = 0; j < indexedList.Count; j++)
            {
                var index = indexedList[j];
                //for (int i = 0; i < index.Elements.Count(); i++)
                //{
                    //var element = index.GetElement(i);
                    var schema = index.GetElement("schema").Value.AsBsonDocument;

                    var titles = schema.GetValue("titles", null).AsBsonArray;
                    var key = schema.GetValue("key", null).AsString;

                    if (key != null) {
                        var topic = topics.Where(t => t.Name == key).FirstOrDefault();
                        if (topic != null) {
                            for (int k = 0; k < titles.Count; k++)
                            {
                                var title = titles[k];
                                var text = title.AsBsonDocument.GetValue("text", null).AsString;
                                var lang = title.AsBsonDocument.GetValue("lang", null).AsString;
                                var primary = title.AsBsonDocument.GetValue("primary", false).AsBoolean;

                                if (text != null)
                                {
                                    Label label = null;
                                    //topics.Where(t => t == topic).SelectMany(t => t.LabelGroup.Labels).FirstOrDefault(l => l.Text == text);
                                    bool isAdded = false;
                                    foreach (var l in topic.LabelGroup.Labels)
                                    {
                                        if (l.Text == text)
                                        {
                                            label = l;
                                            break;
                                        }
                                    }

                                    if (label == null)
                                    {
                                        isAdded = true;
                                        label = new Label();
                                        label.LabelGroupId = (int)topic.LabelGroupId;
                                        label.Text = text;
                                        label.LanguageId = GetLanguageIdByString(lang);
                                    }

                                    if (label != null)
                                    {
                                        label.Primary = primary;
                                    }

                                    if (isAdded)
                                    {
                                        targetContext.Add(targetContext.Labels, label);
                                    }
                                    else
                                    {
                                        targetContext.Labels.Update(label);
                                    }
                                }
                            }
                        }
                        
                    }

                //}
            }
            
        }

        private List<Topic> GetTopicList(Topic t) {
            List<Topic> result = new List<Topic>();
            result.Add(t);
            
            foreach (var item in t.Children)
            {
                result.AddRange(GetTopicList(item));
            }
            return result;
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

        public List<BsonDocument> GetTexts(int startLocation, int amount) {
            var result = new List<BsonDocument>();
            if (startLocation < TextsCount()) {
                result = _texts.Find(_ => true).Skip(startLocation).Limit(amount).ToList();
            }
            
            return result;
        }

        public Text ParseText(BsonDocument value, SefariaSQLiteConversionContext targetContext) {
            Text text = new Text();
            LabelGroup versionTitleLG = new LabelGroup();
            versionTitleLG.Labels = new List<Label>();
            //BsonDocument value = _texts.Find(_ => true).Skip(index).FirstOrDefault();

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
                        text.Chapter = GenerateChapterTree(text,element.Value);
                        break;
                    default:
                        break;
                    
                }
            }

            text.ChapterCount = CountChapters(text.Chapter);

            return text;
        }
        public List<Chapter> ListChapters(Chapter c) {
            List<Chapter> result = new List<Chapter>();
            result.Add(c);
            if(c.Children != null)
            foreach (var item in c.Children)
            {
                result.AddRange(ListChapters(item));
            }

            return result;
        }
        private int CountChapters(Chapter c) {
            int count = 0;
            if (c != null) {
                count += 1;
                if (c.Children != null)
                {
                    foreach (var item in c.Children)
                    {
                        count += CountChapters(item);
                    }
                }
            }
            return count;
        }

        private Chapter GenerateChapterTree(Text txt, BsonValue value, Chapter parent = null, int index = 1)
        {
            Chapter instance = new Chapter { Index = index, TopicText = txt, TopicTextId = txt.Id };
            if (parent != null) {
                instance.ParentChapter = parent;
            }

            //To recreate fast Lookup Path
            instance.Path = (parent!=null && parent.ParentChapter != null? parent.Path+":":"")+instance.Index.ToString();

            switch (value.BsonType) {
                case BsonType.Array:
                    var array = value.AsBsonArray;
                    instance.Children = new List<Chapter>();
                    for (int i = 0; i < array.Count; i++)
                    {
                        instance.Children.Add(GenerateChapterTree(txt, array[i], instance, i+1));
                        instance.HasChild = true;
                    }
                    break;
                case BsonType.Document:
                    var document = value.AsBsonDocument;
                    instance.Children = new List<Chapter>();
                    for (int i = 0; i < document.Elements.Count();i++)
                    {
                        var element = document.GetElement(i);
                        var child = GenerateChapterTree(txt, element.Value, instance, i+1);
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

        public List<BsonDocument> GetLinks()
        {
            return _links.Find(_ => true).ToList();
        }

        public List<BsonDocument> GetLinks(int startLocation, int amount) {
            var result = new List<BsonDocument>();
            if (startLocation < LinksCount())
            {
                result = _links.Find(_ => true).Skip(startLocation).Limit(amount).ToList();
            }

            return result;
        }

        public LinkItem ParseLink(BsonDocument value, SefariaSQLiteConversionContext targetContext) {
            try
            {
                LinkItem link = new LinkItem();
                //link.LinkGroup = new LinkGroup();

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

                string parseLocation(string location) {
                    var gemaraIndicators = new string[] { "a", "b" };
                    for (int i = 0; i < gemaraIndicators.Length; i++)
                    {
                        var indicator = gemaraIndicators[i];
                        var foundIndex = location.IndexOf(indicator, 0, StringComparison.OrdinalIgnoreCase);
                        if (foundIndex >= 0) {
                            var parts = location.Split(":");
                            for (int j = 0; j < parts.Length; j++)
                            {
                                if (parts[j].Contains(indicator)) {
                                    parts[j] = parts[j].Replace(indicator, "");
                                    var value = int.Parse(parts[j]);
                                    value = (value - 1) * 2 + (i + 1);
                                    parts[j] = value.ToString();
                                    break;
                                }
                            }

                            return string.Join(":", parts);
                        }
                    }

                    return location;
                }

                var primaryTopicSeperator = PrimaryTopic.LastIndexOf(' ');
                string primaryTopicName = PrimaryTopic.Substring(0, primaryTopicSeperator);
                string primaryTopicLocation = parseLocation(PrimaryTopic.Substring(primaryTopicSeperator + 1));

                int primaryTopicId = targetContext.Topics.Where(t => t.Name == primaryTopicName).Select(t => t.Id).FirstOrDefault();

                var secondaryTopicSeperator = SecondaryTopic.LastIndexOf(' ');
                string secondaryTopicName = SecondaryTopic.Substring(0, secondaryTopicSeperator);
                string secondaryTopicLocation = parseLocation(SecondaryTopic.Substring(secondaryTopicSeperator + 1));
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
                else{
                    link.DebugInfo = $"{PrimaryTopic}{((primaryTopicId==0)?" 'Not Found'":"")} _ {SecondaryTopic}{((secondaryTopicId == 0) ? " 'Not Found'" : "")}";
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


