// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Schema;

namespace MSLC.StatLog.UI.Models
{
    public class XMLSchemaComponent
    {
        public readonly HtmlHelper HTML;
        private readonly XmlSchema _schema;
        private readonly List<XMLSchemaElementComponent> _defaultElementList = new List<XMLSchemaElementComponent>();
        private string _message;
        private readonly int _ID;
        private readonly XElement _root;
        private bool _readOnly = false;

        public XMLSchemaComponent(HtmlHelper html, string schema, string xml, int id)
        {
            if (string.IsNullOrWhiteSpace(schema))
            {
                _message = "Missing schema to build component with.";
                return;
            }

            _ID = id;
            HTML = html;
            XmlSchemaSet schemaSet = GetSchemaFromString(schema);
            if (schemaSet.Schemas().Count == 0) return;

            foreach(XmlSchema xmlSchema in schemaSet.Schemas())
            {
                _schema = xmlSchema;
                break;
            }

            if (_schema == null)
            {
                _message = "XML Schema parsing encountered an error.";
                return;
            }

            if (xml?.Length > 0)
            {
                XDocument doc = XDocument.Parse(xml);
                doc.Validate(schemaSet, ValidationEventHandler);
                _root = doc.Root;
            }

            foreach (XmlSchemaElement element in _schema.Elements.Values)
            {
                var roots = new List<XElement>();
                if (_root != null)
                {
                    roots.Add(_root);
                }
                _defaultElementList.Add(new XMLSchemaElementComponent(this, element, roots));
            }
        }

        public bool readOnly => _readOnly;

        private void ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (Enum.TryParse("Error", out XmlSeverityType type))
            {
                if (type == XmlSeverityType.Error)
                    _message = $"Error XML doesn't match the XML Schema. Error Message: {e.Message}";
            }
        }

        private XmlSchemaSet GetSchemaFromString(string xml)
        {
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            TextReader textReader = new StringReader(xml);
            schemaSet.Add(XmlSchema.Read(textReader, (sender, args) => _message =
                $"Error Reading XML Schema. Error Message: {args.Message}"));
            schemaSet.Compile();
            return schemaSet;
        }

        public string RenderHTML()
        {
            if (_message != null)
                return _message;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<div class='MSLCXMLSchemaDisplay' xmlID={0}>", _ID);
            foreach (XMLSchemaElementComponent elementComponent in _defaultElementList)
            {
                elementComponent.RenderHTML(sb);
            }
            return sb.Append("</div>").ToString();
        }

        public XMLSchemaComponent SetReadOnly(bool readOnly)
        {
            _readOnly = readOnly;
            return this;
        }

        public override string ToString()
        {
            return RenderHTML();
        }
    }
}