using UnityEngine;
using FYFY;

[ExecuteInEditMode]
public class CurrentActionExecutor_wrapper : MonoBehaviour
{
	private void Start()
	{
		this.hideFlags = HideFlags.HideInInspector; // Hide this component in Inspector
	}

	public void reloadScene()
	{
		MainLoop.callAppropriateSystemMethod ("CurrentActionExecutor", "reloadScene", null);
	}

}
