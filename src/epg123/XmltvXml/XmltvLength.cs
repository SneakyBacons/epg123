﻿using System.Xml.Serialization;

namespace epg123.XmltvXml
{
    public class XmltvLength
    {
        [XmlAttribute("units")]
        public string Units { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}
