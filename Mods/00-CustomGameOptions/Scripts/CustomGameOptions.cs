using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;

public static class CustomGameOptions
{
    private static bool ConfigsLoaded = false;
    private static string SaveDataPath = "";
    private static string ModPath = "";
    private static string GameOptionsFilename = "CustomGameOptions.xml";
    public static bool ReloadUI = false;
    public static Dictionary<string, string> StoredOptions;

    static CustomGameOptions()
    {
        StoredOptions = new Dictionary<string, string>();
        var mod = ModManager.GetMod("CustomGameOptions", true);
        ModPath = mod.Path;
        SaveDataPath = mod.Path + "/SaveData";
        Directory.CreateDirectory(SaveDataPath);
        Log.Out("DataConfigPath = " + SaveDataPath);
        LoadConfigs();
    }

    public static void LoadConfigs()
    {
        if (!ConfigsLoaded)
        {
            ConfigsLoaded = true;
            LoadGameOptions();
            Log.Out("Custom Game Options Loaded.");
        }
    }
  
    private static void LoadGameOptions()
    {
        if (GameManager.IsDedicatedServer)
        {
            var path = ModPath + "/Config/XUi_Menu/";
            if (File.Exists(path + "windows.xml"))
            {
                var xmlFile = new XmlFile(path, "windows.xml");
                var documentElement = xmlFile.XmlDoc.DocumentElement;
                if (documentElement.ChildNodes.Count == 0)
                {
                    throw new Exception("Inavlid CustomGameOptions file, windows.xml");
                }

                var nodes = xmlFile.XmlDoc.SelectNodes("/windows/insertAfter/rect[@tab_key='xuiCustomGameOptions']/grid/gameoption");

                foreach (XmlNode node in nodes)
                {
                    var name = node.Attributes?.GetNamedItem("name")?.Value;
                    var defaultValue = node.Attributes?.GetNamedItem("default_value")?.Value;

                    if (!string.IsNullOrEmpty(name) &&
                        !string.IsNullOrEmpty(defaultValue))
                    {
                        StoredOptions[name] = defaultValue;
                    }
                }
            }

            Log.Out("Custom Game Options Default Values Loaded.");
        }

        if (!string.IsNullOrEmpty(SaveDataPath))
        {
            var path = SaveDataPath + "/" + GameOptionsFilename;
            if (File.Exists(path))
            {
                XmlFile xmlFile = new XmlFile(SaveDataPath, GameOptionsFilename);
                XmlElement documentElement = xmlFile.XmlDoc.DocumentElement;
                if (documentElement.ChildNodes.Count == 0)
                {
                    throw new Exception("No element <customgameoptions> found!");
                }

                foreach (object obj in documentElement.ChildNodes)
                {
                    XmlNode xmlNode = (XmlNode)obj;
                    if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.Name.Equals("config"))
                    {
                        XmlElement xmlElement = (XmlElement)xmlNode;

                        if (!xmlElement.HasAttribute("name"))
                        {
                            throw new Exception("Attribute 'name' missing on item");
                        }

                        if (!xmlElement.HasAttribute("value"))
                        {
                            throw new Exception("Attribute 'value' missing on item");
                        }

                        var value = xmlElement.GetAttribute("value");
                        var name = xmlElement.GetAttribute("name");

                        StoredOptions[name] = value;
                    }
                }
            }
        }

        if (GameManager.IsDedicatedServer)
        {
            SaveGameOptions();
        }
    }

    public static void SaveGameOptions()
    {
        XmlDocument xmlDocument = new XmlDocument();
        XmlDeclaration newChild = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
        xmlDocument.InsertBefore(newChild, xmlDocument.DocumentElement);
        XmlNode parent = xmlDocument.AppendChild(xmlDocument.CreateElement("customgameoptions"));

        foreach(var key in StoredOptions.Keys)
        {
            parent.AppendChild(CreateXMLGameOptionChild(xmlDocument, key, StoredOptions[key].ToString()));
        }

        Log.Out(SerializeToString(xmlDocument));

        // Make sure folder is there before saving
        Directory.CreateDirectory(SaveDataPath);
        var path = SaveDataPath + "/" + GameOptionsFilename;
        File.WriteAllText(path, SerializeToString(xmlDocument), Encoding.UTF8);
    }

    private static XmlElement CreateXMLGameOptionChild(XmlDocument xmlDocument, string name, string value)
    {
        XmlElement xmlElement = xmlDocument.CreateElement("config");
        XmlAttribute xmlAttributeName = xmlDocument.CreateAttribute("name");
        xmlAttributeName.Value = name;
        xmlElement.Attributes.Append(xmlAttributeName);
        XmlAttribute xmlAttributeValue = xmlDocument.CreateAttribute("value");
        xmlAttributeValue.Value = value.ToLower();
        xmlElement.Attributes.Append(xmlAttributeValue);

        return xmlElement;
    }

    private static string SerializeToString(XmlDocument xmlDocument)
    {
        string result;
        using (StringWriter stringWriter = new StringWriter())
        {
            using (XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter))
            {
                xmlDocument.WriteTo(xmlTextWriter);
                result = stringWriter.ToString();
            }
        }
        return result;
    }

    public static bool HasValue(string name)
    {
        return StoredOptions.ContainsKey(name);
    }

    public static void SetValue(string name, object value)
    {
        StoredOptions[name] = value.ToString();
    }

    public static int GetInt(string name, int defaultValue = 0)
    {
        if(!StoredOptions.ContainsKey(name))
        {
            return defaultValue;
        }

        return int.TryParse(StoredOptions[name], out var output) ? output : defaultValue;
    }

    public static string GetString(string name, string defaultValue = "")
    {
        if (!StoredOptions.ContainsKey(name))
        {
            return defaultValue;
        }

        return StoredOptions[name].ToString();
    }

    public static bool GetBool(string name, bool defaultValue = false)
    {
        if (!StoredOptions.ContainsKey(name))
        {
            return defaultValue;
        }

        return bool.TryParse(StoredOptions[name], out var output) ? output : defaultValue;
    }
}
