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

            using (XmlReader reader = XmlReader.Create(xmlPath))
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
                    //if (reader.IsStartElement())
                    //    System.Diagnostics.Debug.Assert(reader.Name == "Rule");
                    reader.ReadStartElement("Rule");
                    //reader.Read();

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
            if (namingRules.contextSeparator == null)
                namingRules.contextSeparator = string.Empty;
            return namingRules;
        }

        public string ContextSeparator
        {
            get { return contextSeparator; }
        }

        public int ContextDepth
        {
            get { return contextDepth; }
        }

        public string FilenameRegex
        {
            get { return filenameRegex; }
        }

        public List<NamingRule> Rules
        {
            get { return rules; }
        }
    }

    class NamingRule
    {
        string regex;
        string replacement;

        public NamingRule(string regex, string replacement)
        {
            this.regex = regex ?? throw new ArgumentNullException(nameof(regex));
            this.replacement = replacement ?? string.Empty;
        }

        public string Regex
        {
            get { return regex; }
        }

        public string Replacement
        {
            get { return replacement; }
        }


    }
}
