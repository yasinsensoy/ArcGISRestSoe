﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace OrbisKrokiTest.Classes
{
    public class KrokiSettings
    {
        public KrokiSettings()
        {
            Elements = new List<LayoutElement>();
        }
        [JsonProperty("elements")]
        public List<LayoutElement> Elements { get; set; }
    }
}
