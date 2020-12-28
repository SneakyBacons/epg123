﻿using System.Xml.Serialization;

namespace hdhr2mxf.XMLTV
{
    public class XmltvVideo
    {
        [XmlElement("present")]
        public string Present { get; set; }

        [XmlElement("colour")]
        public string Colour { get; set; }

        [XmlElement("aspect")]
        public string Aspect { get; set; }

        [XmlElement("quality")]
        public string Quality { get; set; }
    }
}
