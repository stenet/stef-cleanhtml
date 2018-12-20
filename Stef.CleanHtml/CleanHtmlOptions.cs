using System;
using System.Collections.Generic;
using System.Linq;

namespace Stef.CleanHtml
{
    public class CleanHtmlOptions
    {
        private List<string> _RemoveTagList;
        private Dictionary<string, string> _ReplaceTagDic;
        private Dictionary<string, object> _SupportedAttributeDic;
        private Dictionary<string, object> _SupportedStyleDic;
        private Dictionary<string, bool> _SupportedTagDic;

        public CleanHtmlOptions(string mainBlockElement)
        {
            MainBlockElement = mainBlockElement.ToLower();

            _RemoveTagList = new List<string>();
            _ReplaceTagDic = new Dictionary<string, string>();
            _SupportedStyleDic = new Dictionary<string, object>();
            _SupportedAttributeDic = new Dictionary<string, object>();
            _SupportedTagDic = new Dictionary<string, bool>();
        }

        public static CleanHtmlOptions CreateDefault()
        {
            var result = new CleanHtmlOptions("p");
            
            result.AddReplaceTag("div", "p");
            result.AddReplaceTag("o:p", "p");
            result.AddReplaceTag("b", "strong");

            result.AddRemoveTag("hr");
            result.AddRemoveTag("#comment");

            result.AddSupportedStyle("color");
            result.AddSupportedStyle("background-color");

            result.AddSupportedTag("#text", false);
            result.AddSupportedTag("span", false);
            result.AddSupportedTag("a", false);
            result.AddSupportedTag("strong", false);
            result.AddSupportedTag("img", false);
            result.AddSupportedTag("em", false);
            result.AddSupportedTag("u", false);
            result.AddSupportedTag("h1", true);
            result.AddSupportedTag("h2", true);
            result.AddSupportedTag("h3", true);
            result.AddSupportedTag("h4", true);
            result.AddSupportedTag("h5", true);
            result.AddSupportedTag("h6", true);
            result.AddSupportedTag("br", false);
            result.AddSupportedTag("ul", true);
            result.AddSupportedTag("ol", true);
            result.AddSupportedTag("li", true);
            result.AddSupportedTag("p", true);

            result.AddSupportedAttribute("style");
            result.AddSupportedAttribute("target");
            result.AddSupportedAttribute("href");

            return result;
        }

        public string MainBlockElement { get; private set; }

        public void AddReplaceTag(string oldTag, string newTag)
        {
            oldTag = oldTag.ToLower();
            newTag = newTag.ToLower();

            _ReplaceTagDic[oldTag] = newTag;
        }
        public void AddRemoveTag(string tag)
        {
            _RemoveTagList.Add(tag);
        }
        public void AddSupportedStyle(string style)
        {
            style = style.ToLower();

            _SupportedStyleDic[style] = null;
        }
        public void AddSupportedAttribute(string attribute)
        {
            attribute = attribute.ToLower();

            _SupportedAttributeDic[attribute] = null;
        }
        public void AddSupportedTag(string tag, bool isBlock)
        {
            tag = tag.ToLower();

            _SupportedTagDic[tag] = isBlock;
        }

        public List<string> GetRemoveTagList()
        {
            return _RemoveTagList;
        }
        public List<Tuple<string, string>> GetReplaceTagList()
        {
            return _ReplaceTagDic
                .Select(c => new Tuple<string, string>(c.Key, c.Value))
                .ToList();
        }
        public List<string> GetSupportedStyleList()
        {
            return _SupportedStyleDic
                .Select(c => c.Key)
                .ToList();
        }
        public List<string> GetSupportedAttributeList()
        {
            return _SupportedAttributeDic
                .Select(c => c.Key)
                .ToList();
        }
        public List<Tuple<string, bool>> GetSupportedTagList()
        {
            return _SupportedTagDic
                .Select(c => new Tuple<string, bool>(c.Key, c.Value))
                .ToList();
        }
    }
}
