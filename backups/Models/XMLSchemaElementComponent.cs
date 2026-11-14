// DO NOT MODIFY OUTSIDE OF MSLC.STATLOG
// This file comes from MSLC.Statlog, any changes to it will be wiped out when the nuget package is updated. 
// Please make the appropriate enhancements to MSLC.Statlog instead of here.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.EntityFrameworkCore.Internal;

using Syncfusion.MVC.EJ;

using static System.String;

namespace MSLC.StatLog.UI.Models
{
    public class XMLSchemaElementComponent
    {
        private XMLSchemaComponent _schema = null;
        XmlSchemaElement _element = null;
        XMLSchemaElementComponent _parentElementComponent = null;
        private readonly List<XMLSchemaElementComponent> _childrenElementComponentList = new List<XMLSchemaElementComponent>();
        private readonly List<Tuple<string>> _attributes;
        private readonly List<XElement> _roots;

        public XMLSchemaElementComponent(XMLSchemaComponent schema, XmlSchemaElement element, List<XElement> roots,
            XMLSchemaElementComponent parentElementComponent = null)
        {
            _schema = schema;
            _element = element;
            _parentElementComponent = parentElementComponent;
            _roots = roots?.ToList() ?? new List<XElement>();

            // Get the complex type of the element.
            XmlSchemaComplexType complexType = element.ElementSchemaType as XmlSchemaComplexType;

            _attributes = new List<Tuple<string>>();
            if (complexType != null && complexType.AttributeUses.Count > 0)
            {
                foreach (DictionaryEntry de in complexType.AttributeUses)
                {
                    XmlSchemaAttribute attribute = (XmlSchemaAttribute)de.Value;
                    _attributes.Add(new Tuple<string>(attribute.Name));
                }
            }

            // Get the sequence particle of the complex type.
            // Iterate over each XmlSchemaElement in the Items collection.
            if (complexType?.ContentTypeParticle is XmlSchemaSequence sequence)
            {
                foreach (XmlSchemaObject o in sequence.Items)
                {
                    if (!(o is XmlSchemaElement childElement)) continue;
                    var childRoots = new List<XElement>();
                    foreach (XElement root in _roots)
                    {
                        var rootDescendants = root.Descendants(childElement.Name).ToList();
                        if (rootDescendants.Count > 0)
                        {
                            childRoots.AddRange(rootDescendants);
                        }
                    }
                    _childrenElementComponentList.Add(new XMLSchemaElementComponent(schema,
                        childElement,
                        childRoots,
                        this));
                }
            }
        }

        public string RenderHTML() => RenderHTML(new StringBuilder()).ToString();
        public StringBuilder RenderHTML(StringBuilder sb)
        {
            switch (_element.ElementSchemaType.TypeCode)
            {
                case XmlTypeCode.None:
                    return ParseNoneType(sb);
                case XmlTypeCode.String:
                    return TextBoxComponent(sb);
                case XmlTypeCode.Date:
                    return DateComponent(sb);
                case XmlTypeCode.Decimal:
                    return DecimalComponent(sb);
                case XmlTypeCode.Integer:
	                return IntegerComponent(sb);
				case XmlTypeCode.Item:
                case XmlTypeCode.Node:
                case XmlTypeCode.Document:
                case XmlTypeCode.Element:
                case XmlTypeCode.Attribute:
                case XmlTypeCode.Namespace:
                case XmlTypeCode.ProcessingInstruction:
                case XmlTypeCode.Comment:
                case XmlTypeCode.Text:
                case XmlTypeCode.AnyAtomicType:
                case XmlTypeCode.UntypedAtomic:
                case XmlTypeCode.Boolean:
                case XmlTypeCode.Float:
                case XmlTypeCode.Double:
                case XmlTypeCode.Duration:
                case XmlTypeCode.DateTime:
                case XmlTypeCode.Time:
                case XmlTypeCode.GYearMonth:
                case XmlTypeCode.GYear:
                case XmlTypeCode.GMonthDay:
                case XmlTypeCode.GDay:
                case XmlTypeCode.GMonth:
                case XmlTypeCode.HexBinary:
                case XmlTypeCode.Base64Binary:
                case XmlTypeCode.AnyUri:
                case XmlTypeCode.QName:
                case XmlTypeCode.Notation:
                case XmlTypeCode.NormalizedString:
                case XmlTypeCode.Token:
                case XmlTypeCode.Language:
                case XmlTypeCode.NmToken:
                case XmlTypeCode.Name:
                case XmlTypeCode.NCName:
                case XmlTypeCode.Id:
                case XmlTypeCode.Idref:
                case XmlTypeCode.Entity:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.Long:
                case XmlTypeCode.Int:
                case XmlTypeCode.Short:
                case XmlTypeCode.Byte:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.UnsignedLong:
                case XmlTypeCode.UnsignedInt:
                case XmlTypeCode.UnsignedShort:
                case XmlTypeCode.UnsignedByte:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.YearMonthDuration:
                case XmlTypeCode.DayTimeDuration:
                default:
                    throw new ArgumentOutOfRangeException($"Element '{_element.ElementSchemaType.TypeCode}' is missing a component display.");
            }
        }

        private StringBuilder ParseNoneType(StringBuilder sb)
        {
            if (_roots.Count != 1 || !_attributes.Any(s => s.Item1.Equals("display")))
                return NoneComponent(sb);

            XElement xElement = _roots[0];
            XAttribute xAttribute = xElement.Attribute("display");

            switch (xAttribute?.Value)
            {
                case "grid":
                    return GridComponent(sb);
                default:
                    throw new ArgumentOutOfRangeException($"Element type '{_element.ElementSchemaType.TypeCode}' is missing a component display. Display value is set to {xAttribute?.Value ?? "Null"}");
            }
        }

        private StringBuilder GridComponent(StringBuilder sb)
        {

            if (_childrenElementComponentList.Count != 1)
                throw new FormatException("Can not format XML Element as a grid. XML Element with display type as grid contains more one element as a child");

            var headers = _childrenElementComponentList[0]._childrenElementComponentList ?? new List<XMLSchemaElementComponent>();
            var rows = _childrenElementComponentList[0]._roots ?? new List<XElement>();
            if (!headers.Any())
                throw new FormatException("Can not format XML Element as a grid. XML Element with display type as grid is missing grid headers");

            sb.AppendFormat("<table class=\"MSLCXmlTable\" xmlTag={0} xmlAttributes='display=\"grid\"'><tr>" , _roots[0].Name);
            foreach (var header in headers)
            {
                string name = header._element.Name;
                // In short, this regex replaces any '_' with a space as long as it doesn't have a '\' in front of it.
                string displayName = Regex.Replace(Regex.Replace(name, @"(?<!_)_((?!_))", " "), @"_+", "_");
                sb.AppendFormat("<th class=\"px-3\" name='{0}'>{1}</th>", name, displayName);
            }
            sb.Append("</tr>");
            foreach (XElement row in rows)
            {
                sb.AppendFormat("<tr xmlTag='{0}'>", _childrenElementComponentList[0]._element.Name);
                foreach (XMLSchemaElementComponent header in headers)
                {
                    sb.Append($"<td class=\"px-3\">");
                    GridRow(sb, row, header);
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            return sb.Append("</table>");
        }

        private StringBuilder GridRow(StringBuilder sb, XElement row, XMLSchemaElementComponent header)
        {
            switch (header._element.ElementSchemaType.TypeCode)
            {
                // case XmlTypeCode.None:
                //     return sb.Append(row.Elements(header._element.Name).FirstOrDefault()?.Value ?? Empty);
                case XmlTypeCode.String:
                    return GridTextBoxComponent(sb, row, header);
                case XmlTypeCode.Date:
                    return GridDateComponent(sb, _schema.HTML, row, header);
                case XmlTypeCode.Decimal:
                    return GridDecimalComponent(sb, _schema.HTML, row, header);
                case XmlTypeCode.Integer:
                    return GridIntegerComponent(sb, _schema.HTML, row, header);
            }
            return sb;
        }

        private static StringBuilder GridDecimalComponent(StringBuilder sb, HtmlHelper html, XElement row, XMLSchemaElementComponent header)
        {
            Guid guid = Guid.NewGuid();
            XElement cell = row.Elements(header._element.Name).FirstOrDefault();
            var validValue = decimal.TryParse(row.Elements(header._element.Name).FirstOrDefault()?.Value, out decimal value);
            bool.TryParse(cell?.Attribute("readonly")?.Value ?? bool.FalseString, out bool disabled);
			bool.TryParse(cell?.Attribute("ignore")?.Value ?? bool.FalseString, out bool ignore);
            var componentValue = validValue && !ignore ? value : 0;

			string numericComponent = html.EJ().NumericTextbox("numeric")
                    .Value(componentValue)
                    .Enabled(!disabled && !header._schema.readOnly)
                    .HtmlAttributes(new Dictionary<string, object> { { "parentID", guid.ToString() } })
                    .ClientSideEvents((events) => events.Change("GridDecimalComponent"))
                    .DecimalPlaces(2).ToHtmlString();

            return sb.AppendFormat("<div id={0} xmlTag=\"{1}\" xmlValue=\"{2}\" optional=\"{4}\" xmlAttributes/>{3}</div>",
                guid,
                header._element.Name,
				componentValue,
                numericComponent,
                ignore.ToString());
        }

        private static StringBuilder GridIntegerComponent(StringBuilder sb, HtmlHelper html, XElement row, XMLSchemaElementComponent header)
        {
            Guid guid = Guid.NewGuid();
            XElement cell = row.Elements(header._element.Name).FirstOrDefault();
            var validValue = int.TryParse(row.Elements(header._element.Name).FirstOrDefault()?.Value, out int value);
            bool.TryParse(cell?.Attribute("readonly")?.Value ?? bool.FalseString, out bool disabled);
			bool.TryParse(cell?.Attribute("ignore")?.Value ?? bool.FalseString, out bool ignore);
            var componentValue = validValue && !ignore ? value : 0;

			string numericComponent = html.EJ().NumericTextbox("numeric")
                    .Value(componentValue)
                    .Enabled(!disabled && !header._schema.readOnly)
                    .HtmlAttributes(new Dictionary<string, object> { { "parentID", guid.ToString() } })
                    .ClientSideEvents((events) => events.Change("GridDecimalComponent"))
                    .DecimalPlaces(0).ToHtmlString();
            
            return sb.AppendFormat("<div id={0} xmlTag=\"{1}\" xmlValue=\"{2}\" optional=\"{4}\" xmlAttributes/>{3}</div>",
                guid,
                header._element.Name,
				componentValue,
                numericComponent,
                ignore.ToString());
        }

        private static StringBuilder GridDateComponent(StringBuilder sb, HtmlHelper html, XElement row, XMLSchemaElementComponent header)
        {
            // Guid guid = new Guid();
            XElement cell = row.Elements(header._element.Name).FirstOrDefault();
            var validValue = DateTime.TryParse(row.Elements(header._element.Name).FirstOrDefault()?.Value, out DateTime value);
            bool.TryParse(cell?.Attribute("readonly")?.Value ?? bool.FalseString, out bool disabled);
			bool.TryParse(cell?.Attribute("ignore")?.Value ?? bool.FalseString, out bool ignore);
            var gridValueString = validValue && !ignore ? value.ToUniversalTime().ToString(CultureInfo.InvariantCulture) : "";
	
			string dateComponent = html.EJ().DatePicker("")
                .Value(gridValueString)
                .Enabled(!disabled && !header._schema.readOnly)
                .ClientSideEvents((events) => events.Change("GridDateComponent"))
                .ToHtmlString();

            return sb.AppendFormat("<div xmlTag=\"{0}\" xmlValue=\"{1}\" optional=\"{3}\"/>{2}</div>",
                header._element.Name,
				gridValueString,
                dateComponent,
                ignore.ToString());
        }

        private static StringBuilder GridTextBoxComponent(StringBuilder sb, XElement row, XMLSchemaElementComponent header)
        {
            XElement cell = row.Elements(header._element.Name).FirstOrDefault();
            var value = cell?.Value ?? Empty;
            bool.TryParse(cell?.Attribute("readonly")?.Value ?? bool.FalseString, out bool disabled);
			bool.TryParse(cell?.Attribute("ignore")?.Value ?? bool.FalseString, out bool ignore);

			return sb.AppendFormat("<input id=\"{0}\" xmlTag=\"{0}\" onchange=\"GridTextBoxComponent(this)\" xmlValue=\"{1}\" type=\"text\" value=\"{1}\" {2} optional=\"{3}\"/>",
                header._element.Name,
                ignore ? "" : value,
                disabled || header._schema.readOnly ? "disabled" : Empty,
                ignore.ToString());
        }

        private StringBuilder NoneComponent(StringBuilder sb)
        {
            return sb.AppendFormat("<div name=\"{0}\"></div>", _element.Name);
        }

        private StringBuilder DecimalComponent(StringBuilder sb)
        {
            return sb.Append(_schema.HTML.EJ()
                .NumericTextbox("numeric")
                .DecimalPlaces(2)
                .HtmlAttributes(GenerateHtmlAttributes(_element.Name))
                .ToHtmlString());
        }

        private StringBuilder IntegerComponent(StringBuilder sb)
        {
            return sb.Append(_schema.HTML.EJ()
                .NumericTextbox("numeric")
                .DecimalPlaces(0)
                .HtmlAttributes(GenerateHtmlAttributes(_element.Name))
                .ToHtmlString());
        }

        private StringBuilder DateComponent(StringBuilder sb)
        {
            return sb.Append(_schema.HTML.EJ()
                .DatePicker(_element.Name)
                .HtmlAttributes(GenerateHtmlAttributes(_element.Name))
                .ToHtmlString());
        }

        private IDictionary<string, object> GenerateHtmlAttributes(string elementName) => 
            new Dictionary<string, object> { { "xmlTag", elementName } };

        private StringBuilder TextBoxComponent(StringBuilder sb, string devaultValue = "", bool disabled = false)
        {
            return sb.AppendFormat("<input id=\"{0}\" xmlTag=\"{0}\" type=\"text\" value=\"{1}\" {2}/><br/>",
                _element.Name,
                devaultValue,
                disabled ? "disabled" : Empty);
        }

        private StringBuilder BasicRender(StringBuilder sb)
        {
            sb.Append(_element.Name);
            sb.Append(" -- Type: ");
            sb.Append(_element.ElementSchemaType.TypeCode);
            // switch (_element.ElementSchemaType)
            // {
            //     case XmlSchemaComplexType xmlSchemaComplexType:
            //         sb.Append(xmlSchemaComplexType.ContentType);
            //         break;
            //     case XmlSchemaSimpleType xmlSchemaSimpleType:
            //         sb.Append(xmlSchemaSimpleType.TypeCode);
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }

            if (_attributes.Count > 0)
            {
                sb.Append(" -- Attributes: ");
                sb.Append(_attributes.Select(a => a.Item1).ToArray().Join(","));
            }

            if (_roots.Count > 0)
            {
                sb.Append(" -- Root count: ");
                sb.Append(_roots.Count);
            }

            foreach (XMLSchemaElementComponent elementComponent in _childrenElementComponentList)
            {
                elementComponent.RenderHTML(sb);
            }
            return sb;
        }
    }
}