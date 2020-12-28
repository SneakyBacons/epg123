﻿using System.Xml.Serialization;

namespace hdhr2mxf.XMLTV
{
    public class XmltvPreviouslyShown
    {
        [XmlAttribute("start")]
        public string Start { get; set; }

        [XmlAttribute("channel")]
        public string Channel { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}
