using System;
using System.Collections.Generic;

public struct KeyLabelsPair
{
    public string Key;
    public IList<string> Labels;

    public static KeyLabelsPair CreateByKey(string key)
    {
        return new KeyLabelsPair {Key = key};
    }

    public static KeyLabelsPair CreateByLabels(params string[] labels)
    {
        return new KeyLabelsPair {Labels = labels};
    }

    public static KeyLabelsPair Create(string key, params string[] labels)
    {
        return new KeyLabelsPair {Key = key, Labels = labels};
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Key) && Labels?.Count == 0)
        {
            return "Key and labels are empty";
        }
        
        if (string.IsNullOrEmpty(Key))
            return $"Label:{string.Join(',', Labels)}";

        if (Labels == null || Labels.Count == 0)
        {
            return $"Key:{Key}";
        }
        
        return $"Key:{Key} Labels:{string.Join(',', Labels)}";
    }
}