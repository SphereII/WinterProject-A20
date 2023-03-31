using System.Collections.Generic;

public static class ExtensionMethods
{
    public static bool GetBool(this Dictionary<string, string> dictionary, string key)
    {
        if (dictionary.ContainsKey(key))
        {
            var output = false;
            bool.TryParse(dictionary[key], out output);
            return output;
        }

        return false;
    }

    //public static void SetBool(this Dictionary<string, string> dictionary, string key, bool value)
    //{
    //    if (dictionary.ContainsKey(key))
    //        dictionary[key] = value.ToString();
    //    else
    //        dictionary.Add(key, value.ToString());
    //}

    public static string GetString(this Dictionary<string, string> dictionary, string key)
    {
        if (dictionary.ContainsKey(key))
        {
            return dictionary[key];
        }

        return string.Empty;
    }

    //public static void SetString(this Dictionary<string, string> dictionary, string key, string value)
    //{
    //    if (dictionary.ContainsKey(key))
    //        dictionary[key] = value;
    //    else
    //        dictionary.Add(key, value);
    //}

    public static int GetInt(this Dictionary<string, string> dictionary, string key)
    {
        if (dictionary.ContainsKey(key))
        {
            var output = 0;
            int.TryParse(dictionary[key], out output);
            return output;
        }

        return 0;
    }

    //public static void SetInt(this Dictionary<string, string> dictionary, string key, int value)
    //{
    //    if (dictionary.ContainsKey(key))
    //        dictionary[key] = value.ToString();
    //    else
    //        dictionary.Add(key, value.ToString());
    //}

    public static void Set(this Dictionary<string, string> dictionary, string key, object value)
    {
        if (dictionary.ContainsKey(key))
            dictionary[key] = value.ToString();
        else
            dictionary.Add(key, value.ToString());
    }
}