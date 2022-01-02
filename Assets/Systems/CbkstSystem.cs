using UnityEngine;
using FYFY;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System;

public class CbkstSystem : FSystem
{
    Dictionary<string, List<string>> dependency_dict;
    static Dictionary<string, List<string>> cbkst_dict;
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
        if (node.NextSibling != null)
            recurse_XML(node.NextSibling);

        if (dependency_dict.ContainsKey(node.Name) == false)
            dependency_dict.Add(node.Name, new List<string> { });

        if (node.ParentNode.Name != "tree" && dependency_dict[node.Name].Contains(node.ParentNode.Name) == false)
            dependency_dict[node.Name].Add(node.ParentNode.Name);
    }

    public void build_cbkst_dict(Dictionary<string, List<string>> dependency_dict)
    {
        List<string> skills = new List<string>(dependency_dict.Keys);
        recurse_list(skills, dependency_dict);
    }

    private void recurse_list(List<string> skills, Dictionary<string, List<string>> dependency_dict)
    {
        if (skills.Count == 0 || dependency_dict.Count == 0)
            return;

        if (cbkst_dict.ContainsKey(String.Join(",", skills)) == false)
            cbkst_dict.Add(String.Join(",", skills), new List<string> { });

        List<string> no_dependency = skills.Where(x => dependency_dict[x].Count == 0).ToList();

        foreach (string skill in no_dependency)
        {
            Dictionary<string, List<string>> new_dependency = remove(dependency_dict, skill);

            List<string> new_list = new List<string>(new_dependency.Keys);

            if (new_list.Count > 0 && cbkst_dict.ContainsKey(String.Join(",", new_list)) == false)
                cbkst_dict.Add(String.Join(",", new_list), new List<string> { });

            try
            {
                if (cbkst_dict[String.Join(",", new_list)].Contains(String.Join(",", skills)) == false)
                {
                    cbkst_dict[String.Join(",", new_list)].Add(String.Join(",", skills));
                }
            }
            catch (Exception)
            {
                { }
            }

            recurse_list(new_list, new_dependency);
        }
    }

    private Dictionary<string, List<string>> remove(Dictionary<string, List<string>> dependency_dict, string skill)
    {
        Dictionary<string, List<string>> copy = dependency_dict.ToDictionary(p => p.Key, p => p.Value.ToList());
        List<string> removeKeys = copy.Where(x => x.Value.Contains(skill)).Select(x => x.Key).ToList();

        foreach (string key in removeKeys)
            copy[key].Remove(skill);

        copy.Remove(skill);

        return copy;
    }
}
