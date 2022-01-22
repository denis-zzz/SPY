using UnityEngine;
using FYFY;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System;
using System.IO;

public class CbkstSystem : FSystem
{
    private static GameData gameData;
    public CbkstSystem()
    {
        if (Application.isPlaying)
        {
            GameObject gameDataGO = GameObject.Find("GameData");

            gameData = gameDataGO.GetComponent<GameData>();

            string treePath = Application.streamingAssetsPath
            + Path.DirectorySeparatorChar + "Level_trees"
            + Path.DirectorySeparatorChar + "tree1.xml";

            build_dependance_dict_from_XML(treePath);
            build_cbkst_dict(gameData.dependency_dict);
        }
    }

    private void build_dependance_dict_from_XML(string filename)
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

        if (gameData.dependency_dict.ContainsKey(node.Name) == false)
            gameData.dependency_dict.Add(node.Name, new List<string> { });

        if (node.ParentNode.Name != "tree" && gameData.dependency_dict[node.Name].Contains(node.ParentNode.Name) == false)
            gameData.dependency_dict[node.Name].Add(node.ParentNode.Name);
    }

    private void build_cbkst_dict(Dictionary<string, List<string>> dependency_dict)
    {
        List<string> skills = new List<string>(gameData.dependency_dict.Keys);
        recurse_list(skills, gameData.dependency_dict);
    }

    private static void recurse_list(List<string> skills, Dictionary<string, List<string>> dependency_dict)
    {
        if (skills.Count == 0 || dependency_dict.Keys.Count == 0)
            return;

        if (gameData.cbkst_dict.ContainsKey(String.Join(",", skills)) == false)
            gameData.cbkst_dict.Add(String.Join(",", skills), new List<string> { });

        List<string> no_dependency = skills.Where(x => dependency_dict[x].Count == 0).ToList();

        foreach (string skill in no_dependency)
        {
            Dictionary<string, List<string>> new_dependency = remove(dependency_dict, skill);

            List<string> new_list = new List<string>(new_dependency.Keys);
            if (new_list.Count > 0 && gameData.cbkst_dict.ContainsKey(String.Join(",", new_list)) == false)
                gameData.cbkst_dict.Add(String.Join(",", new_list), new List<string> { });
            try
            {
                if (gameData.cbkst_dict[String.Join(",", new_list)].Contains(String.Join(",", skills)) == false)
                {
                    gameData.cbkst_dict[String.Join(",", new_list)].Add(String.Join(",", skills));
                }
            }
            catch (Exception e)
            {
                { }
            }


            recurse_list(new_list, new_dependency);
        }
    }

    private static Dictionary<string, List<string>> remove(Dictionary<string, List<string>> dependency_dict, string skill)
    {
        Dictionary<string, List<string>> copy = dependency_dict.ToDictionary(p => p.Key, p => p.Value.ToList());
        List<string> removeKeys = copy.Where(x => x.Value.Contains(skill)).Select(x => x.Key).ToList();

        foreach (string key in removeKeys)
            copy[key].Remove(skill);

        copy.Remove(skill);

        return copy;
    }
}
