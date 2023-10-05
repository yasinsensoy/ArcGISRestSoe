using System.Collections.Generic;

namespace OrbisKrokiTest.Classes
{
    //http://www.freeformatter.com/mime-types-list.html
    public enum ContentTypeFileExtensionEnum
    {
        AI = 0,
        BMP = 1,
        EMF = 2,
        EPS = 3,
        GIF = 4,
        JPG = 5,
        PDF = 6,
        PNG = 7,
        SVG = 8,
        TIF = 9
    }
    public class ContentType
    {
        public static Dictionary<ContentTypeFileExtensionEnum, ContentType> list = new Dictionary<ContentTypeFileExtensionEnum, ContentType>();
        public string Name { get; set; }
        public string Extension { get; set; }
        public string MimeType { get; set; }
        static ContentType()
        {
            list.Add(ContentTypeFileExtensionEnum.AI, AI);
            list.Add(ContentTypeFileExtensionEnum.BMP, BMP);
            list.Add(ContentTypeFileExtensionEnum.EMF, EMF);
            list.Add(ContentTypeFileExtensionEnum.EPS, EPS);
            list.Add(ContentTypeFileExtensionEnum.GIF, GIF);
            list.Add(ContentTypeFileExtensionEnum.JPG, JPG);
            list.Add(ContentTypeFileExtensionEnum.PDF, PDF);
            list.Add(ContentTypeFileExtensionEnum.PNG, PNG);
            list.Add(ContentTypeFileExtensionEnum.SVG, SVG);
            list.Add(ContentTypeFileExtensionEnum.TIF, TIF);
        }
        public static ContentType AI => new ContentType() { Name = "PostScript", Extension = ".ai", MimeType = "application/postscript" };
        public static ContentType BMP => new ContentType() { Name = "Bitmap Image File", Extension = ".bmp", MimeType = "image/bmp" };
        public static ContentType EMF => new ContentType() { Name = "Enhanced Windows Metafile", Extension = ".emf", MimeType = "application/emf" };
        public static ContentType EPS => new ContentType() { Name = "Encapsulated Postscript Vector graphics", Extension = ".eps", MimeType = "application/eps" };
        public static ContentType GIF => new ContentType() { Name = "Graphics Interchange Format", Extension = ".gif", MimeType = "image/gif" };
        public static ContentType JPG => new ContentType() { Name = "JPEG Image", Extension = ".jpg", MimeType = "image/jpeg" };
        public static ContentType PDF => new ContentType() { Name = "Adobe Portable Document Format", Extension = ".pdf", MimeType = "application/pdf" };
        public static ContentType PNG => new ContentType() { Name = "Portable Network Graphics (PNG)", Extension = ".png", MimeType = "image/png" };
        public static ContentType SVG => new ContentType() { Name = "Scalable Vector Graphics (SVG)", Extension = ".svg", MimeType = "	image/svg+xml" };
        public static ContentType TIF => new ContentType() { Name = "Tagged Image File Format", Extension = ".tif", MimeType = "image/tiff" };
        public static ContentType GetContentType(ContentTypeFileExtensionEnum extension) => list.ContainsKey(extension) ? list[extension] : PNG;
    }
}
