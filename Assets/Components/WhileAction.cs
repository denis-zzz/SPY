using UnityEngine;
using UnityEngine.EventSystems;

public class WhileAction : BaseElement
{
    public int ifEntityType; // 0: Wall, 1: Door, 2: Enemy, 3: Ally, 4: Console, 5: Coin
    public bool ifNot;
    public int range;
    public int ifDirection; // 0: Forward, 1: Backward, 2: Left, 3: Right
    public GameObject firstChild;
}
