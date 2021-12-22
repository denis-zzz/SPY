using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FYFY;

using UnityEngine.UI;
using FYFY_plugins.PointerManager;

public class DynamicEditableCanvas : FSystem
{
    private Family editableScriptContainer = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer), typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(EditableCanvas)));

    public DynamicEditableCanvas()
    {
        if (Application.isPlaying)
        {
            editableScriptContainer.addEntryCallback(onNewCurrentAction);
        }
    }

    private void onNewCurrentAction(GameObject EditableCanvas_go)
    {
        if (EditableCanvas_go.transform.childCount == 1)
        { //player & empty script (1 child for position bar)
            EditableCanvas edit_canvas = EditableCanvas_go.GetComponent<EditableCanvas>();
            for (int k = 0; k < edit_canvas.script.Count; k++)
            {
                edit_canvas.script[k].transform.SetParent(EditableCanvas_go.transform); //add actions to editable container
                GameObjectManager.bind(edit_canvas.script[k]);
                GameObjectManager.refresh(EditableCanvas_go);
            }
            foreach (BaseElement act in EditableCanvas_go.GetComponentsInChildren<BaseElement>())
            {
                GameObjectManager.addComponent<Dropped>(act.gameObject);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(EditableCanvas_go.GetComponent<RectTransform>());
        }
        GameObjectManager.removeComponent<EditableCanvas>(EditableCanvas_go);
    }
}
