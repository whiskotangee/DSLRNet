namespace DSLRNet.Core.Contracts;

using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("TextureAtlas")]
public class TextureAtlas
{
    [XmlAttribute("imagePath")]
    public string ImagePath { get; set; }

    [XmlElement("SubTexture")]
    public List<SubTexture> SubTextures { get; set; }
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
        var serializer = new XmlSerializer(typeof(TextureAtlas)); 
        using var stringWriter = new StringWriter(); 
        serializer.Serialize(stringWriter, textureAtlas); 
        return stringWriter.ToString(); 
    } 
    public static TextureAtlas? Deserialize(string xml) 
    { 
        var serializer = new XmlSerializer(typeof(TextureAtlas)); 
        using var stringReader = new StringReader(xml); 
        return serializer.Deserialize(stringReader) as TextureAtlas; 
    } 
}

