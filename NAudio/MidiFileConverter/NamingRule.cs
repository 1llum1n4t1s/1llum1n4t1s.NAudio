using System;
using System.Collections.Generic;
using System.Xml;

namespace MarkHeath.MidiUtils
{
    class NamingRules
    {
        string filenameRegex;
        int contextDepth;
        string contextSeparator;
        List<NamingRule> rules;

        public static NamingRules LoadRules(string xmlPath)
        {
            var namingRules = new NamingRules();
            namingRules.rules = new List<NamingRule>();

            var settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
                DtdProcessing = DtdProcessing.Prohibit,
            };

            using (var reader = XmlReader.Create(xmlPath, settings))
            {
                reader.ReadStartElement("Rules");
                reader.ReadStartElement("GeneralSettings");

                reader.ReadStartElement("FilenameRegex");
                namingRules.filenameRegex = reader.ReadString();
                reader.ReadEndElement();

                reader.ReadStartElement("ContextDepth");
                namingRules.contextDepth = reader.ReadContentAsInt();
                if (namingRules.ContextDepth < 1 || namingRules.ContextDepth > 4)
                    throw new FormatException("Context Depth must be between 1 and 4");
                reader.ReadEndElement();

                reader.ReadStartElement("ContextSeparator");
                namingRules.contextSeparator = reader.ReadString();
                reader.ReadEndElement();

                reader.ReadEndElement();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        System.Diagnostics.Debug.Assert(reader.Name == "Rules");
                        break;
                    }
                    reader.ReadStartElement("Rule");

                    reader.ReadStartElement("SearchString");
                    var regex = reader.ReadString();
                    reader.ReadEndElement();
                    reader.ReadStartElement("Replacement");
                    var replacement = reader.ReadString();
                    reader.ReadEndElement();
                    reader.ReadEndElement();
                    namingRules.rules.Add(new NamingRule(regex, replacement));
                }
                reader.ReadEndElement();
            }
            if (string.IsNullOrEmpty(namingRules.filenameRegex))
                throw new FormatException("FilenameRegex must not be empty");
            namingRules.contextSeparator ??= string.Empty;
            return namingRules;
        }

        public string ContextSeparator => contextSeparator;

        public int ContextDepth => contextDepth;

        public string FilenameRegex => filenameRegex;

        public List<NamingRule> Rules => rules;
    }

    class NamingRule
    {
        public NamingRule(string regex, string replacement)
        {
            Regex = regex ?? throw new ArgumentNullException(nameof(regex));
            Replacement = replacement ?? string.Empty;
        }

        public string Regex { get; }

        public string Replacement { get; }
    }
}
