using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class CbkstSystem_wrapper : MonoBehaviour
{
    private void Start()
    {
        this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
    }

    public void build_dependance_dict_from_XML(System.String filename)
    {
        MainLoop.callAppropriateSystemMethod("CbkstSystem", "build_dependance_dict_from_XML", filename);
    }

    public void build_cbkst_dict(System.Collections.Generic.Dictionary`2[System.String, System.Collections.Generic.List`1[System.String]] dependency_dict)
	{
		MainLoop.callAppropriateSystemMethod("CbkstSystem", "build_cbkst_dict", dependency_dict);
	}

}
