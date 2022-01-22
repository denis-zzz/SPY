using UnityEngine;
using FYFY;
using System.Collections.Generic;
using System.Xml;
using UnityEngine.UI;
using TMPro;
using FYFY_plugins.PointerManager;
using System.IO;

/// <summary>
/// Read XML file and load level
/// </summary>
public class LevelGenerator : FSystem
{

    private Family levelGO = FamilyManager.getFamily(new AnyOfComponents(typeof(Position), typeof(CurrentAction)));
    private Family enemyScript = FamilyManager.getFamily(new AllOfComponents(typeof(HorizontalLayoutGroup), typeof(CanvasRenderer)), new NoneOfComponents(typeof(Image)));
    private Family editableScriptContainer = FamilyManager.getFamily(new AllOfComponents(typeof(UITypeContainer), typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(PointerSensitive)));
    private List<List<int>> map;
    private GameData gameData;
    private GameObject scriptContainer;

    public LevelGenerator()
    {
        if (Application.isPlaying)
        {
            GameObject gameDataGO = GameObject.Find("GameData");
            if (gameDataGO == null)
                GameObjectManager.loadScene("TitleScreen");

            gameData = gameDataGO.GetComponent<GameData>();
            gameData.Level = GameObject.Find("Level");
            scriptContainer = enemyScript.First();
            XmlToLevel(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
            GameObject.Find("LevelName").GetComponent<TMP_Text>().text = Path.GetFileNameWithoutExtension(gameData.levelList[gameData.levelToLoad.Item1][gameData.levelToLoad.Item2]);
        }
    }

    private void generateMap()
    {
        for (int i = 0; i < map.Count; i++)
        {
            for (int j = 0; j < map[i].Count; j++)
            {
                switch (map[i][j])
                {
                    case 0: // Path
                        createCell(i, j);
                        break;
                    case 1: // Wall
                        createCell(i, j);
                        createWall(i, j);
                        break;
                    case 2: // Spawn
                        createCell(i, j);
                        createSpawnExit(i, j, true);
                        break;
                    case 3: // Exit
                        createCell(i, j);
                        createSpawnExit(i, j, false);
                        break;
                    case 4: // Teleporter destination
                        createTeleporterDest(i, j);
                        break;
                }
            }
        }
    }

    private GameObject createEntity(int i, int j, Direction.Dir direction, string type, List<GameObject> script = null)
    {
        GameObject entity = null;
        Sprite agentSpriteIcon = null;
        switch (type)
        {
            case "player": // Robot
                entity = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Robot Kyle") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 1.5f, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);
                agentSpriteIcon = Resources.Load("UI Images/robotIcon", typeof(Sprite)) as Sprite;
                break;
            case "enemy": // Enemy
                entity = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Drone") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 5f, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);
                agentSpriteIcon = Resources.Load("UI Images/droneIcon", typeof(Sprite)) as Sprite;
                break;
            case "solution":
                entity = Object.Instantiate<GameObject>(Resources.Load("Prefabs/TeleporterSolution") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 1.5f, j * 3), Quaternion.Euler(-90, 0, 0), gameData.Level.transform);
                break;
        }
        entity.GetComponent<Position>().x = i;
        entity.GetComponent<Position>().z = j;

        if (type != "solution")
        {
            entity.GetComponent<Direction>().direction = direction;
        }

        //add new container to entity
        ScriptRef scriptref = entity.GetComponent<ScriptRef>();
        GameObject containerParent = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Container") as GameObject);
        scriptref.uiContainer = containerParent;
        scriptref.scriptContainer = containerParent.transform.Find("Container").Find("Viewport").Find("ScriptContainer").gameObject;
        containerParent.transform.SetParent(scriptContainer.gameObject.transform);
        containerParent.transform.Find("Header").Find("agent").GetComponent<Image>().sprite = agentSpriteIcon;

        AgentColor ac = MainLoop.instance.GetComponent<AgentColor>();
        scriptref.uiContainer.transform.Find("Container").GetComponent<Image>().color = (type == "player" ? ac.playerBackground : ac.droneBackground);

        if (script != null)
        {
            /*Comment faire :
         Cr?er un nouveau syst?me DynamicCanvas avec le code en dessous, Dans system Dynamic Canvas=> famille allOfComponent (typeof(XMLTarget))
        Composant XMLTarget {type:String, script:List<GO>(); ...}
        Dans le systeme DynamicCanvas, faire la fonction pour ajouter les instructions dans le canvas. Faire pour qu'elle s'ex?cute d?s qu'on g?n?re un component XMLTarget (grace ? addEntryCallback)
        D?s qu'on a fini la fonction, il faut supprimer le composant, sinon addEntryCallback ne se r?execute pas ? la prochaine cr?ation de composant

        Du coup ici, on aura juste :
            GameObjectManager.addComponent<XMLTarget>(containerParent, { type = type; script = script; ... })



         */

            if (type == "player" && editableScriptContainer.First().transform.childCount == 1)
            { //player & empty script (1 child for position bar)
                GameObject editableCanvas = editableScriptContainer.First();
                for (int k = 0; k < script.Count; k++)
                {
                    script[k].transform.SetParent(editableCanvas.transform); //add actions to editable container
                    GameObjectManager.bind(script[k]);
                    GameObjectManager.refresh(editableCanvas);
                }
                foreach (BaseElement act in editableCanvas.GetComponentsInChildren<BaseElement>())
                {
                    GameObjectManager.addComponent<Dropped>(act.gameObject);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(editableCanvas.GetComponent<RectTransform>());
                /*GameObjectManager.addComponent<EditableCanvas>(editableCanvas, new { script = script });*/

            }

            else if (type == "enemy")
            {
                foreach (GameObject go in script)
                {
                    go.transform.SetParent(scriptref.scriptContainer.transform); //add actions to container
                    List<GameObject> basicActionGO = getBasicActionGO(go);
                    if (basicActionGO.Count != 0)
                        foreach (GameObject baGO in basicActionGO)
                            baGO.GetComponent<Image>().color = MainLoop.instance.GetComponent<AgentColor>().droneAction;
                }
                computeNext(scriptref.scriptContainer);
            }
            else if (type == "solution")
            {
                foreach (GameObject go in script)
                {
                    go.transform.SetParent(scriptref.scriptContainer.transform); //add actions to container
                }
            }
        }
        GameObjectManager.bind(containerParent);
        GameObjectManager.bind(entity);
        return entity;
    }

    private List<GameObject> getBasicActionGO(GameObject go)
    {
        List<GameObject> res = new List<GameObject>();
        if (go.GetComponent<BasicAction>())
            res.Add(go);
        foreach (Transform child in go.transform)
        {
            if (child.GetComponent<BasicAction>())
                res.Add(child.gameObject);
            else if (child.GetComponent<UITypeContainer>() && child.GetComponent<BaseElement>())
            {
                List<GameObject> childGO = getBasicActionGO(child.gameObject);
                foreach (GameObject cgo in childGO)
                {
                    res.Add(cgo);
                }
            }
        }
        return res;
    }

    private void createDoor(int i, int j, Direction.Dir orientation, int slotID)
    {
        GameObject door = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Door") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 3, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);

        door.GetComponent<ActivationSlot>().slotID = slotID;
        door.GetComponent<Position>().x = i;
        door.GetComponent<Position>().z = j;
        door.GetComponent<Direction>().direction = orientation;
        GameObjectManager.bind(door);
    }

    private void createActivable(int i, int j, List<int> slotIDs, Direction.Dir orientation)
    {
        GameObject activable = Object.Instantiate<GameObject>(Resources.Load("Prefabs/ActivableConsole") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 3, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);

        activable.GetComponent<Activable>().slotID = slotIDs;
        activable.GetComponent<Position>().x = i;
        activable.GetComponent<Position>().z = j;
        activable.GetComponent<Direction>().direction = orientation;
        GameObjectManager.bind(activable);
    }

    private void createSpawnExit(int i, int j, bool type)
    {
        GameObject spawnExit;
        if (type)
            spawnExit = Object.Instantiate<GameObject>(Resources.Load("Prefabs/TeleporterSpawn") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 1.5f, j * 3), Quaternion.Euler(-90, 0, 0), gameData.Level.transform);
        else
            spawnExit = Object.Instantiate<GameObject>(Resources.Load("Prefabs/TeleporterExit") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 1.5f, j * 3), Quaternion.Euler(-90, 0, 0), gameData.Level.transform);

        spawnExit.GetComponent<Position>().x = i;
        spawnExit.GetComponent<Position>().z = j;
        GameObjectManager.bind(spawnExit);
    }

    private void createTeleporter(int x1, int z1, int x2, int z2, int direction)
    {
        GameObject teleporter;

        teleporter = Object.Instantiate<GameObject>(Resources.Load("Prefabs/TeleporterSource") as GameObject, gameData.Level.transform.position + new Vector3(x1 * 3, 1.5f, z1 * 3), Quaternion.Euler(-90, 0, 0), gameData.Level.transform);



        teleporter.GetComponent<Position>().x = x1;
        teleporter.GetComponent<Position>().z = z1;
        teleporter.GetComponent<Teleporter>().x2 = x2;
        teleporter.GetComponent<Teleporter>().z2 = z2;
        teleporter.GetComponent<Teleporter>().direction = direction;

        GameObjectManager.bind(teleporter);
    }

    private void createCoin(int i, int j)
    {
        GameObject coin = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Coin") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 3, j * 3), Quaternion.Euler(90, 0, 0), gameData.Level.transform);
        coin.GetComponent<Position>().x = i;
        coin.GetComponent<Position>().z = j;
        GameObjectManager.bind(coin);
    }

    private void createCell(int i, int j)
    {
        GameObject cell = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Cell") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 0, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);
        GameObjectManager.bind(cell);
    }

    private void createWall(int i, int j)
    {
        GameObject wall = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Wall") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 3, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);
        wall.GetComponent<Position>().x = i;
        wall.GetComponent<Position>().z = j;
        GameObjectManager.bind(wall);
    }

    private void createTeleporterDest(int i, int j)
    {
        GameObject cell = Object.Instantiate<GameObject>(Resources.Load("Prefabs/Cell") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 0, j * 3), Quaternion.Euler(0, 0, 0), gameData.Level.transform);
        GameObjectManager.bind(cell);
        GameObject teleporter = Object.Instantiate<GameObject>(Resources.Load("Prefabs/TeleporterDestination") as GameObject, gameData.Level.transform.position + new Vector3(i * 3, 1.5f, j * 3), Quaternion.Euler(-90, 0, 0), gameData.Level.transform);
        GameObjectManager.bind(teleporter);
    }

    private void eraseMap()
    {
        foreach (GameObject go in levelGO)
        {
            GameObjectManager.unbind(go.gameObject);
            Object.Destroy(go.gameObject);
        }
    }

    public void XmlToLevel(string fileName)
    {

        gameData.totalActionBloc = 0;
        gameData.totalStep = 0;
        gameData.totalExecute = 0;
        gameData.totalCoin = 0;
        gameData.levelToLoadScore = null;
        gameData.dialogMessage = new List<(string, string)>();
        gameData.actionBlocLimit = new Dictionary<string, int>();
        map = new List<List<int>>();

        XmlDocument doc = new XmlDocument();
        doc.Load(fileName);

        XmlNode root = doc.ChildNodes[1];
        foreach (XmlNode child in root.ChildNodes)
        {
            switch (child.Name)
            {
                case "map":
                    readXMLMap(child);
                    break;
                case "dialogs":
                    string src = null;
                    //optional xml attribute
                    if (child.Attributes["img"] != null)
                        src = child.Attributes.GetNamedItem("img").Value;
                    gameData.dialogMessage.Add((child.Attributes.GetNamedItem("dialog").Value, src));
                    break;
                case "teleporters":
                    readXMLTeleporters(child);
                    break;
                case "actionBlocLimit":
                    readXMLLimits(child);
                    break;
                case "coin":
                    createCoin(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value));
                    break;
                case "activable":
                    readXMLActivable(child);
                    break;
                case "door":
                    createDoor(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
                    (Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value), int.Parse(child.Attributes.GetNamedItem("slot").Value));
                    break;

                case "player":
                    createEntity(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
                    (Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value), "player", readXMLScript(child.ChildNodes[0], true));
                    break;

                case "enemy":
                    GameObject enemy = createEntity(int.Parse(child.Attributes.GetNamedItem("posX").Value), int.Parse(child.Attributes.GetNamedItem("posZ").Value),
                    (Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value), "enemy", readXMLScript(child.ChildNodes[0]));
                    enemy.GetComponent<DetectRange>().range = int.Parse(child.Attributes.GetNamedItem("range").Value);
                    enemy.GetComponent<DetectRange>().selfRange = bool.Parse(child.Attributes.GetNamedItem("selfRange").Value);
                    enemy.GetComponent<DetectRange>().type = (DetectRange.Type)int.Parse(child.Attributes.GetNamedItem("typeRange").Value);
                    break;

                case "solution":
                    GameObject solution = createEntity(int.Parse(child.Attributes.GetNamedItem("x1").Value), int.Parse(child.Attributes.GetNamedItem("z1").Value),
                    (Direction.Dir)int.Parse(child.Attributes.GetNamedItem("direction").Value), "solution", readXMLScript(child.ChildNodes[0]));
                    solution.GetComponent<Teleporter>().x2 = int.Parse(child.Attributes.GetNamedItem("x2").Value);
                    solution.GetComponent<Teleporter>().z2 = int.Parse(child.Attributes.GetNamedItem("z2").Value);
                    break;

                case "score":
                    gameData.levelToLoadScore = new int[2];
                    gameData.levelToLoadScore[0] = int.Parse(child.Attributes.GetNamedItem("threeStars").Value);
                    gameData.levelToLoadScore[1] = int.Parse(child.Attributes.GetNamedItem("twoStars").Value);
                    break;

                case "minAction":
                    gameData.minAction = int.Parse(child.Attributes.GetNamedItem("minAction").Value);
                    break;

                case "bestTime":
                    gameData.bestTime = int.Parse(child.Attributes.GetNamedItem("bestTime").Value);
                    break;
            }
        }

        eraseMap();
        generateMap();
        GameObjectManager.addComponent<GameLoaded>(MainLoop.instance.gameObject);
    }

    private void readXMLMap(XmlNode mapNode)
    {
        foreach (XmlNode lineNode in mapNode.ChildNodes)
        {
            List<int> line = new List<int>();
            foreach (XmlNode rowNode in lineNode.ChildNodes)
            {
                line.Add(int.Parse(rowNode.Attributes.GetNamedItem("value").Value));
            }
            map.Add(line);
        }
    }

    private void readXMLLimits(XmlNode limitsNode)
    {
        string actionName = null;
        foreach (XmlNode limitNode in limitsNode.ChildNodes)
        {
            //gameData.actionBlocLimit.Add(int.Parse(limitNode.Attributes.GetNamedItem("limit").Value));
            actionName = limitNode.Attributes.GetNamedItem("actionType").Value;
            if (!gameData.actionBlocLimit.ContainsKey(actionName))
            {
                gameData.actionBlocLimit[actionName] = int.Parse(limitNode.Attributes.GetNamedItem("limit").Value);
            }
        }
    }

    private void readXMLTeleporters(XmlNode teleportersNode)
    {
        int x1;
        int z1;
        int x2;
        int z2;
        int direction;
        foreach (XmlNode teleportNode in teleportersNode.ChildNodes)
        {

            x1 = int.Parse(teleportNode.Attributes.GetNamedItem("x1").Value);
            z1 = int.Parse(teleportNode.Attributes.GetNamedItem("z1").Value);
            x2 = int.Parse(teleportNode.Attributes.GetNamedItem("x2").Value);
            z2 = int.Parse(teleportNode.Attributes.GetNamedItem("z2").Value);
            direction = int.Parse(teleportNode.Attributes.GetNamedItem("direction").Value);
            createTeleporter(x1, z1, x2, z2, direction);
        }
    }

    private void readXMLActivable(XmlNode activableNode)
    {
        List<int> slotsID = new List<int>();

        foreach (XmlNode child in activableNode.ChildNodes)
        {
            slotsID.Add(int.Parse(child.Attributes.GetNamedItem("slot").Value));
        }

        createActivable(int.Parse(activableNode.Attributes.GetNamedItem("posX").Value), int.Parse(activableNode.Attributes.GetNamedItem("posZ").Value),
         slotsID, (Direction.Dir)int.Parse(activableNode.Attributes.GetNamedItem("direction").Value));
    }

    private List<GameObject> readXMLScript(XmlNode scriptNode, bool editable = false)
    {
        if (scriptNode != null)
        {
            List<GameObject> script = new List<GameObject>();
            foreach (XmlNode actionNode in scriptNode.ChildNodes)
            {
                script.Add(readXMLAction(actionNode, editable));
            }

            return script;
        }
        return null;
    }

    private GameObject readXMLAction(XmlNode actionNode, bool editable = false)
    {
        GameObject obj = null;
        BaseElement action = null;
        GameObject prefab = null;
        bool firstchild;

        string actionKey = actionNode.Attributes.GetNamedItem("actionType").Value;
        switch (actionKey)
        {
            case "If":
                prefab = Resources.Load("Prefabs/IfDetectBloc") as GameObject;
                obj = Object.Instantiate(prefab);
                obj.GetComponent<UIActionType>().linkedTo = GameObject.Find("If");
                action = obj.GetComponent<IfAction>();
                //read xml
                ((IfAction)action).ifDirection = int.Parse(actionNode.Attributes.GetNamedItem("ifDirection").Value);
                ((IfAction)action).ifEntityType = int.Parse(actionNode.Attributes.GetNamedItem("ifEntityType").Value);
                ((IfAction)action).range = int.Parse(actionNode.Attributes.GetNamedItem("range").Value);
                ((IfAction)action).ifNot = bool.Parse(actionNode.Attributes.GetNamedItem("ifNot").Value);

                //add to gameobject
                obj.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().value = ((IfAction)action).ifEntityType;
                obj.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().value = ((IfAction)action).ifDirection;
                obj.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().text = ((IfAction)action).range.ToString();

                if (!((IfAction)action).ifNot)
                    obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value = 0;
                else
                    obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value = 1;

                //not interactable actions
                obj.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().interactable = editable;
                obj.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().interactable = editable;
                obj.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().interactable = editable;
                obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().interactable = editable;

                Object.Destroy(obj.GetComponent<UITypeContainer>());

                //add children
                firstchild = false;
                if (actionNode.HasChildNodes)
                {
                    foreach (XmlNode actNode in actionNode.ChildNodes)
                    {
                        GameObject child = (readXMLAction(actNode, editable));
                        child.transform.SetParent(obj.transform);
                        if (!firstchild)
                        {
                            firstchild = true;
                            ((IfAction)action).firstChild = child;
                        }
                    }
                }
                break;

            case "For":
                prefab = Resources.Load("Prefabs/ForBloc") as GameObject;
                obj = Object.Instantiate(prefab);
                obj.GetComponent<UIActionType>().linkedTo = GameObject.Find("For");
                action = obj.GetComponent<ForAction>();

                //read xml
                ((ForAction)action).nbFor = int.Parse(actionNode.Attributes.GetNamedItem("nbFor").Value);

                //add to gameobject
                if (editable)
                {
                    obj.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = ((ForAction)action).nbFor.ToString();
                }
                else
                {
                    obj.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().text = (((ForAction)action).currentFor).ToString() + " / " + ((ForAction)action).nbFor.ToString();
                    Object.Destroy(obj.GetComponent<UITypeContainer>());
                }
                obj.transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>().interactable = editable;

                //add children
                firstchild = false;
                if (actionNode.HasChildNodes)
                {
                    foreach (XmlNode actNode in actionNode.ChildNodes)
                    {
                        GameObject child = (readXMLAction(actNode, editable));
                        child.transform.SetParent(action.transform);
                        if (!firstchild)
                        {
                            firstchild = true;
                            ((ForAction)action).firstChild = child;
                        }
                    }
                }
                break;

            case "Forever":
                prefab = Resources.Load("Prefabs/InfiniteLoop") as GameObject;
                obj = Object.Instantiate(prefab);
                obj.GetComponent<UIActionType>().linkedTo = null;
                action = obj.GetComponent<ForeverAction>();

                if (!editable)
                    //add to gameobject
                    Object.Destroy(obj.GetComponent<UITypeContainer>());

                //add children
                firstchild = false;
                if (actionNode.HasChildNodes)
                {
                    foreach (XmlNode actNode in actionNode.ChildNodes)
                    {
                        GameObject child = (readXMLAction(actNode, editable));
                        child.transform.SetParent(action.transform);
                        if (!firstchild)
                        {
                            firstchild = true;
                            ((ForeverAction)action).firstChild = child;
                        }
                    }
                }
                break;

            case "While":
                prefab = Resources.Load("Prefabs/WhileBloc") as GameObject;
                obj = Object.Instantiate(prefab);
                obj.GetComponent<UIActionType>().linkedTo = GameObject.Find("While");
                action = obj.GetComponent<WhileAction>();
                //read xml
                ((WhileAction)action).ifDirection = int.Parse(actionNode.Attributes.GetNamedItem("ifDirection").Value);
                ((WhileAction)action).ifEntityType = int.Parse(actionNode.Attributes.GetNamedItem("ifEntityType").Value);
                ((WhileAction)action).range = int.Parse(actionNode.Attributes.GetNamedItem("range").Value);
                ((WhileAction)action).ifNot = bool.Parse(actionNode.Attributes.GetNamedItem("ifNot").Value);

                //add to gameobject
                obj.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().value = ((WhileAction)action).ifEntityType;
                obj.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().value = ((WhileAction)action).ifDirection;
                obj.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().text = ((WhileAction)action).range.ToString();

                if (!((WhileAction)action).ifNot)
                    obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value = 0;
                else
                    obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().value = 1;

                //not interactable actions
                obj.transform.GetChild(0).Find("DropdownEntityType").GetComponent<TMP_Dropdown>().interactable = editable;
                obj.transform.GetChild(0).Find("DropdownDirection").GetComponent<TMP_Dropdown>().interactable = editable;
                obj.transform.GetChild(0).Find("InputFieldRange").GetComponent<TMP_InputField>().interactable = editable;
                obj.transform.GetChild(0).Find("DropdownIsOrIsNot").GetComponent<TMP_Dropdown>().interactable = editable;

                Object.Destroy(obj.GetComponent<UITypeContainer>());

                //add children
                firstchild = false;
                if (actionNode.HasChildNodes)
                {
                    foreach (XmlNode actNode in actionNode.ChildNodes)
                    {
                        GameObject child = (readXMLAction(actNode, editable));
                        child.transform.SetParent(obj.transform);
                        if (!firstchild)
                        {
                            firstchild = true;
                            ((WhileAction)action).firstChild = child;
                        }
                    }
                }
                break;

            default:

                prefab = Resources.Load("Prefabs/" + actionKey + "ActionBloc") as GameObject;
                obj = Object.Instantiate(prefab);
                obj.GetComponent<UIActionType>().linkedTo = GameObject.Find(actionKey);
                action = obj.GetComponent<BasicAction>();

                break;
        }
        obj.GetComponent<UIActionType>().prefab = prefab;
        action.target = obj;
        if (!editable)
            Object.Destroy(obj.GetComponent<PointerSensitive>());
        return obj;
    }

    // link actions together => define next property
    public static void computeNext(GameObject container)
    {
        for (int i = 0; i < container.transform.childCount; i++)
        {
            Transform child = container.transform.GetChild(i);
            if (i < container.transform.childCount - 1 && child.GetComponent<BaseElement>())
            {
                child.GetComponent<BaseElement>().next = container.transform.GetChild(i + 1).gameObject;
            }
            else if (i == container.transform.childCount - 1 && child.GetComponent<BaseElement>() && container.GetComponent<BaseElement>())
            {
                if (container.GetComponent<ForAction>() || container.GetComponent<ForeverAction>() || container.GetComponent<WhileAction>())
                    child.GetComponent<BaseElement>().next = container;
                else if (container.GetComponent<IfAction>())
                    child.GetComponent<BaseElement>().next = container.GetComponent<BaseElement>().next;
            }
            //if or for action
            if (child.GetComponent<IfAction>() || child.GetComponent<ForAction>() || child.GetComponent<ForeverAction>() || child.GetComponent<WhileAction>())
                computeNext(child.gameObject);
        }
    }
}
