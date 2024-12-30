namespace DSLRNet.Core.Contracts;

using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("TextureAtlas")]
public class TextureAtlas
{
    [XmlAttribute("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [XmlElement("SubTexture")]
    public List<SubTexture> SubTextures { get; set; } = [];
}

public class SubTexture
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("x")]
    public int X { get; set; }

    [XmlAttribute("y")]
    public int Y { get; set; }

    [XmlAttribute("width")]
    public int Width { get; set; }

    [XmlAttribute("height")]
    public int Height { get; set; }

    [XmlAttribute("half")]
    public int Half { get; set; }
}

public class TextureAtlasSerializer 
{ 
    public static string Serialize(TextureAtlas textureAtlas) 
    {
        XmlSerializer serializer = new(typeof(TextureAtlas)); 
        using StringWriter stringWriter = new(); 
        serializer.Serialize(stringWriter, textureAtlas); 
        return stringWriter.ToString(); 
    } 
    public static TextureAtlas? Deserialize(string xml) 
    {
        XmlSerializer serializer = new(typeof(TextureAtlas)); 
        using StringReader stringReader = new(xml); 
        return serializer.Deserialize(stringReader) as TextureAtlas; 
    } 
}

