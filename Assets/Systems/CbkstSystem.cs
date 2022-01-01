using UnityEngine;
using FYFY;
using System.Collections.Generic;
using System.Xml;

public class CbkstSystem : FSystem
{
    Dictionary<string, List<string>> dependency_dict;
    Dictionary<string, List<string>> cbkst_dict;
    public CbkstSystem()
    {
        dependency_dict = new Dictionary<string, List<string>>();
        cbkst_dict = new Dictionary<string, List<string>>();
    }

    public void build_dependance_dict_from_XML(string filename)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(filename);

        XmlNode root = doc.ChildNodes[1];
        foreach (XmlNode child in root.ChildNodes)
        {
            recurse_XML(child);
        }
    }

    private void recurse_XML(XmlNode node)
    {
        if (node.HasChildNodes)
            recurse_XML(node.FirstChild);
        else if (node.NextSibling != null)
            recurse_XML(node.NextSibling);

        if (dependency_dict.ContainsKey(node.Name) == false)
            dependency_dict.Add(node.Name, new List<string> { });

        if (node.ParentNode.Name != "tree" && dependency_dict[node.Name].Contains(node.ParentNode.Name) == false)
            dependency_dict[node.Name].Add(node.ParentNode.Name);
    }

    public void build_cbkst_dict(Dictionary<string, List<string>> dependance_dict)
    {

    }
}
