using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace Stef.CleanHtml
{
    public class CleanHtmlManager
    {
        private static object _Sync = new object();
        private static CleanHtmlManager _Current;

        public CleanHtmlManager()
        {
        }

        public static CleanHtmlManager Current
        {
            get
            {
                if (_Current == null)
                {
                    lock (_Sync)
                    {
                        if (_Current == null)
                        {
                            _Current = new CleanHtmlManager();
                        }
                    }
                }

                return _Current;
            }
        }

        public void Clean(
            CleanHtmlSqlOptions sqlOptions, 
            CleanHtmlOptions cleanOptions = null)
        {
            if (cleanOptions == null)
                cleanOptions = CleanHtmlOptions.CreateDefault();

            var hasBackupDirectory = !string.IsNullOrEmpty(sqlOptions.BackupDirectory);

            if (hasBackupDirectory)
            {
                if (!Directory.Exists(sqlOptions.BackupDirectory))
                    Directory.CreateDirectory(sqlOptions.BackupDirectory);
            }

            using (var command = sqlOptions.Connection.CreateCommand())
            {
                command.CommandText = string.Concat(
                    "select \"",
                    sqlOptions.IdColumnName,
                    "\", \"",
                    sqlOptions.HtmlColumnName,
                    "\" from \"",
                    sqlOptions.TableName,
                    "\" where \"",
                    sqlOptions.HtmlColumnName,
                    "\" is not null",
                    !string.IsNullOrEmpty(sqlOptions.Where)
                        ? string.Concat(" and ", sqlOptions.Where)
                        : null,
                    " order by 1");

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetValue(0);
                        var htmlObj = reader.GetValue(1);

                        if (htmlObj is DBNull)
                            continue;

                        var html = (string)htmlObj;
                        if (html.Equals(string.Empty))
                            continue;

                        if (html.Contains("</table>"))
                            continue;

                        if (hasBackupDirectory)
                        {
                            var backupFile = Path.Combine(
                                sqlOptions.BackupDirectory,
                                string.Concat(sqlOptions.TableName, "_", sqlOptions.HtmlColumnName, "_", sqlOptions.IdColumnName, ".html"));

                            if (!File.Exists(backupFile))
                                File.WriteAllText(backupFile, html);
                        }

                        var htmlClean = Clean(html, cleanOptions);
                        //Speichern

                        continue;
                    }
                }
            }
        }
        public string Clean(string html, CleanHtmlOptions options)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            RemoveTags(doc, options);
            ReplaceFont(doc);
            ReplaceTags(doc, options);
            RemoveEmptyBlocks(doc, options);
            CheckInvalidTags(doc, options);
            RemoveDummyNodes(doc);
            CleanBreakLine(doc, options);
            CheckBlockSideBySideInline(doc, options);
            LevelUpNestedBlocks(doc, options);
            RemoveEmptyBlocks(doc, options);

            return doc
                .DocumentNode
                .OuterHtml
                .Replace(Environment.NewLine, "");
        }

        private void RemoveTags(HtmlDocument doc, CleanHtmlOptions options)
        {
            foreach (var item in options.GetRemoveTagList())
            {
                var nodeList = doc
                    .DocumentNode
                    .Descendants(item)
                    .ToList();

                foreach (var node in nodeList)
                {
                    node.Remove();
                }
            }
        }
        private void ReplaceFont(HtmlDocument doc)
        {
            var nodeList = doc
                .DocumentNode
                .Descendants("font")
                .ToList();

            foreach (var node in nodeList)
            {
                node.Name = "span";

                var color = node.Attributes["color"];
                if (color != null && color.Value != null)
                {
                    node.SetAttributeValue(
                        "style", 
                        string.Concat("color:", color.Value, ";", node.Attributes["style"]?.Value));

                    node.Attributes.Remove("color");
                }
            }
        }
        private void ReplaceTags(HtmlDocument doc, CleanHtmlOptions options)
        {
            var replaceTagList = options
                .GetReplaceTagList();

            if (!replaceTagList.Any())
                return;

            foreach (var item in replaceTagList)
            {
                var descList = doc
                    .DocumentNode
                    .Descendants(item.Item1)
                    .ToList();

                foreach (var desc in descList)
                {
                    var newItem = doc.CreateElement(item.Item2);

                    foreach (var node in desc.ChildNodes.ToList())
                    {
                        desc.ChildNodes.Remove(node);
                        newItem.ChildNodes.Add(node);
                    }

                    foreach (var attribute in desc.Attributes)
                    {
                        newItem.Attributes.Add(attribute);
                    }

                    desc.ParentNode.InsertBefore(newItem, desc);
                    desc.Remove();
                }
            }
        }
        private void CheckInvalidTags(HtmlDocument doc, CleanHtmlOptions options)
        {
            var supportedTagDic = options
                .GetSupportedTagList()
                .ToDictionary(c => c.Item1);

            var supportedStyleDic = options
                .GetSupportedStyleList()
                .ToDictionary(c => c);

            var supportedAttributeDic = options
                .GetSupportedAttributeList()
                .ToDictionary(c => c);

            var descList = doc
                .DocumentNode
                .Descendants();

            foreach (var desc in descList)
            {
                if (!supportedTagDic.ContainsKey(desc.Name))
                    throw new InvalidOperationException($"{desc.Name} wird nicht unterstützt");

                foreach (var attribute in desc.Attributes.ToList())
                {
                    if (supportedAttributeDic.ContainsKey(attribute.Name))
                        continue;

                    desc.Attributes.Remove(attribute);
                }

                var style = desc.GetAttributeValue("style", string.Empty);
                if (string.IsNullOrEmpty(style))
                    continue;

                var newStyleList = new List<string>();

                var tokenList = style
                    .Replace("&quot;", "'")
                    .Split(';')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                foreach (var token in tokenList)
                {
                    var keyValue = token.Split(':');
                    if (keyValue.Length != 2)
                        throw new InvalidOperationException($"Invalid style {token}");

                    if (!supportedStyleDic.ContainsKey(keyValue[0]))
                        continue;

                    newStyleList.Add(token);
                }

                if (newStyleList.Any())
                    desc.SetAttributeValue("style", string.Join(";", newStyleList));
                else
                    desc.Attributes.Remove("style");
            }
        }
        private void RemoveDummyNodes(HtmlDocument doc)
        {
            var nodeList = doc
                .DocumentNode
                .Descendants("#text")
                .ToList();

            foreach (var node in nodeList)
            {
                if (node.InnerText != null && !string.IsNullOrEmpty(node.InnerText.Trim()))
                    continue;

                node.Remove();
            }
        }
        private void CleanBreakLine(HtmlDocument doc, CleanHtmlOptions options)
        {
            var tagDic = options
                .GetSupportedTagList()
                .ToDictionary(c => c.Item1, c => c.Item2);

            var descList = doc
                .DocumentNode
                .Descendants("br")
                .ToList();

            foreach (var desc in descList)
            {
                if (desc.ParentNode == null)
                    continue;
                if (desc.ParentNode == doc.DocumentNode)
                    continue;

                if (tagDic[desc.ParentNode.Name] && desc.ParentNode.ChildNodes.Count == 1)
                    continue;

                if (!tagDic[desc.ParentNode.Name] && desc.ParentNode.ChildNodes.Count == 1)
                {
                    desc.ParentNode.Name = options.MainBlockElement;
                    continue;
                }

                var indexOfInline = desc
                    .ParentNode
                    .ChildNodes
                    .Where(c => !tagDic[c.Name])
                    .ToList()
                    .IndexOf(desc);

                var wrap = doc.CreateElement(options.MainBlockElement);
                desc.ParentNode.InsertBefore(wrap, desc);
                desc.Remove();

                if (indexOfInline == 0)
                    wrap.ChildNodes.Add(desc);
            }
        }
        private void CheckBlockSideBySideInline(HtmlDocument doc, CleanHtmlOptions options)
        {
            CheckBlockSideBySideInline(doc.DocumentNode, options);
        }
        private void CheckBlockSideBySideInline(HtmlNode node, CleanHtmlOptions options)
        {
            var tagDic = options
                .GetSupportedTagList()
                .ToDictionary(c => c.Item1, c => c.Item2);

            var childNodeList = node
                .ChildNodes
                .ToList();

            var needsWrap = childNodeList
                .Select(c => tagDic[c.Name])
                .Distinct()
                .Count() > 1;

            if (needsWrap)
            {
                HtmlNode wrap = null;
                foreach (var childNode in childNodeList)
                {
                    var isBlock = tagDic[childNode.Name];

                    if (isBlock)
                    {
                        wrap = null;
                        continue;
                    }

                    if (wrap == null)
                    {
                        wrap = node.OwnerDocument.CreateElement(options.MainBlockElement);
                        node.InsertBefore(wrap, childNode);
                    }

                    childNode.Remove();
                    wrap.ChildNodes.Add(childNode);
                }
            }

            foreach (var childNode in childNodeList)
            {
                CheckBlockSideBySideInline(childNode, options);
            }
        }
        private void LevelUpNestedBlocks(HtmlDocument doc, CleanHtmlOptions options)
        {
            LevelUpNestedBlocks(doc.DocumentNode, options);
        }
        private void LevelUpNestedBlocks(HtmlNode node, CleanHtmlOptions options)
        {
            var blockDic = options
                .GetSupportedTagList()
                .Where(c => c.Item2)
                .ToDictionary(c => c.Item1);

            var childNodeList = node
                .ChildNodes
                .ToList();

            if (!childNodeList.Any())
                return;

            var isBlock = node.ParentNode != null
                && blockDic.ContainsKey(node.Name)
                && childNodeList.Any(c => c.Name == options.MainBlockElement);

            if (isBlock)
            {
                foreach (var childNode in childNodeList)
                {
                    childNode.Remove();
                    node.ParentNode.InsertBefore(childNode, node);
                }

                node.Remove();
            }

            foreach (var childNode in childNodeList)
            {
                LevelUpNestedBlocks(childNode, options);
            }
        }
        private void RemoveEmptyBlocks(HtmlDocument doc, CleanHtmlOptions options)
        {
            var blockList = options
                .GetSupportedTagList()
                .Where(c => c.Item2)
                .Select(c => c.Item1)
                .ToList();

            foreach (var block in blockList)
            {
                var descList = doc
                    .DocumentNode
                    .Descendants(block)
                    .ToList();

                foreach (var desc in descList)
                {
                    if (desc.ChildNodes.Count > 0)
                        continue;

                    desc.Remove();
                }
            }
        }
    }
}
