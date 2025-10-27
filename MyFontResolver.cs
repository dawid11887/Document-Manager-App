using PdfSharp.Fonts;
using System;
using System.IO;

public class MyFontResolver : IFontResolver
{
    private static readonly string fontFolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    public byte[] GetFont(string faceName)
    {
        switch (faceName)
        {
            case "Arial":
                return File.ReadAllBytes(Path.Combine(fontFolder, "arial.ttf"));
            case "Arial#b":
                return File.ReadAllBytes(Path.Combine(fontFolder, "arialbd.ttf"));
            case "Arial#i":
                return File.ReadAllBytes(Path.Combine(fontFolder, "ariali.ttf"));
            case "Arial#bi":
                return File.ReadAllBytes(Path.Combine(fontFolder, "arialbi.ttf"));
            case "Courier New":
                return File.ReadAllBytes(Path.Combine(fontFolder, "cour.ttf"));
            case "Courier New#b":
                return File.ReadAllBytes(Path.Combine(fontFolder, "courbd.ttf"));
            case "Courier New#i":
                return File.ReadAllBytes(Path.Combine(fontFolder, "couri.ttf"));
            case "Courier New#bi":
                return File.ReadAllBytes(Path.Combine(fontFolder, "courbi.ttf"));
            default:
                throw new InvalidOperationException($"Nieznana czcionka: {faceName}");
        }
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        familyName = familyName.ToLowerInvariant();

        if (familyName.Contains("arial"))
        {
            var suffix = isBold ? (isItalic ? "#bi" : "#b") : (isItalic ? "#i" : "");
            return new FontResolverInfo("Arial" + suffix);
        }

        if (familyName.Contains("courier"))
        {
            var suffix = isBold ? (isItalic ? "#bi" : "#b") : (isItalic ? "#i" : "");
            return new FontResolverInfo("Courier New" + suffix);
        }

        // fallback to Arial normal if unknown font
        return new FontResolverInfo("Arial");
    }
}
