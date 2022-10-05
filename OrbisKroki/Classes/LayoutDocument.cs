using System.Collections.Generic;

namespace OrbisKroki.Classes
{
    public class LayoutDocument
    {
        public LayoutDocument()
        {
        }
        public int? layerId { get; set; }
        public string fullName { get; set; }
        public string name { get; set; }
        public List<LayoutElement> elements { get; set; }
    }
}
