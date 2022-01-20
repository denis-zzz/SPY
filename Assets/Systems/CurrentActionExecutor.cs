using UnityEngine;
using FYFY;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This system executes new currentActions
/// </summary>
public class CurrentActionExecutor : FSystem {
	private Family wallGO = FamilyManager.getFamily(new AllOfComponents(typeof(Position)), new AnyOfTags("Wall", "Door"));
	private Family activableConsoleGO = FamilyManager.getFamily(new AllOfComponents(typeof(Activable),typeof(Position),typeof(AudioSource)));
    private Family newCurrentAction_f = FamilyManager.getFamily(new AllOfComponents(typeof(CurrentAction), typeof(BasicAction)));
	private Family teleporterGO = FamilyManager.getFamily(new AllOfComponents(typeof(Position), typeof(AudioSource)), new AnyOfTags("Teleporter"));
	private Family solutionGO = FamilyManager.getFamily(new AnyOfTags("Solution"));

	private Dictionary<int, GameObject> idToAgent;
	public CurrentActionExecutor(){
		if (Application.isPlaying)
		{
			newCurrentAction_f.addExitCallback(onOldCurrentAction);
			newCurrentAction_f.addEntryCallback(onNewCurrentAction);
			// newCurrentAction_f.addExitCallback(onNewCurrentAction);
			idToAgent = new Dictionary<int, GameObject>();
		}
    }

	private void onOldCurrentAction(int uniqueId)
	{
		GameObject solutionItem = solutionGO.First();
		GameObject agent = idToAgent[uniqueId];
		// parse all teleporters
		foreach (GameObject teleporter in teleporterGO)
		{
			// check if positions are equals
			if (agent.GetComponent<Position>().x == teleporter.GetComponent<Position>().x && agent.GetComponent<Position>().z == teleporter.GetComponent<Position>().z)
			{
				Debug.Log("Teleporter stepped on");
				agent.GetComponent<Position>().x = teleporter.GetComponent<Teleporter>().x2;
				agent.GetComponent<Position>().z = teleporter.GetComponent<Teleporter>().z2;
				if (teleporter.GetComponent<Teleporter>().direction != 4)
				{
					agent.GetComponent<Direction>().direction = (Direction.Dir)teleporter.GetComponent<Teleporter>().direction;

				}
				agent.transform.localPosition = new Vector3(agent.GetComponent<Position>().x * 3, agent.transform.localPosition.y, agent.GetComponent<Position>().z * 3);
			}
		}
		if (agent.GetComponent<Position>().x == solutionItem.GetComponent<Position>().x && agent.GetComponent<Position>().z == solutionItem.GetComponent<Position>().z)
		{
			GameObject go = solutionItem.GetComponent<ScriptRef>().uiContainer;
			GameObjectManager.setGameObjectState(go, !go.activeInHierarchy);
			agent.GetComponent<Position>().x = solutionItem.GetComponent<Teleporter>().x2;
			agent.GetComponent<Position>().z = solutionItem.GetComponent<Teleporter>().z2;
			if (solutionItem.GetComponent<Teleporter>().direction != 4)
			{
				agent.GetComponent<Direction>().direction = (Direction.Dir)solutionItem.GetComponent<Teleporter>().direction;

			}
			agent.transform.localPosition = new Vector3(agent.GetComponent<Position>().x * 3, agent.transform.localPosition.y, agent.GetComponent<Position>().z * 3);
		}

	}
	// each time a new currentAction is added, 
	private void onNewCurrentAction(GameObject currentAction)
	{
		CurrentAction ca = currentAction.GetComponent<CurrentAction>();
		idToAgent.Add(currentAction.GetInstanceID(), ca.agent);
		if (ca.agent.CompareTag("Player"))
		{
			// We notify that player is moving
			if (!MainLoop.instance.gameObject.GetComponent<PlayerIsMoving>())
			{
				GameObjectManager.addComponent<PlayerIsMoving>(MainLoop.instance.gameObject);
			}
		}

		// process action depending on action type
		switch (currentAction.GetComponent<BasicAction>().actionType)
		{
			case BasicAction.ActionType.Forward:
				ApplyForward(ca.agent);
				Debug.Log("Forward command executed");
				// If marcher sur téléporteur, se téléporter

				break;
			case BasicAction.ActionType.TurnLeft:
				ApplyTurnLeft(ca.agent);
				break;
			case BasicAction.ActionType.TurnRight:
				ApplyTurnRight(ca.agent);
				break;
			case BasicAction.ActionType.TurnBack:
				ApplyTurnBack(ca.agent);
				break;
			case BasicAction.ActionType.Wait:
				break;
			case BasicAction.ActionType.Activate:
				foreach (GameObject actGo in activableConsoleGO)
				{
					if (actGo.GetComponent<Position>().x == ca.agent.GetComponent<Position>().x && actGo.GetComponent<Position>().z == ca.agent.GetComponent<Position>().z)
					{
						actGo.GetComponent<AudioSource>().Play();
						// toggle activable GameObject
						if (actGo.GetComponent<TurnedOn>())
							GameObjectManager.removeComponent<TurnedOn>(actGo);
						else
							GameObjectManager.addComponent<TurnedOn>(actGo);
					}
				}
				break;
		}
	}

	private void ApplyForward(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				if(!checkObstacle(go.GetComponent<Position>().x,go.GetComponent<Position>().z + 1)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z + 1;
				}
				break;
			case Direction.Dir.South:
				if(!checkObstacle(go.GetComponent<Position>().x,go.GetComponent<Position>().z - 1)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z - 1;
				}
				break;
			case Direction.Dir.East:
				if(!checkObstacle(go.GetComponent<Position>().x + 1,go.GetComponent<Position>().z)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x + 1;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z;
				}
				break;
			case Direction.Dir.West:
				if(!checkObstacle(go.GetComponent<Position>().x - 1,go.GetComponent<Position>().z)){
					go.GetComponent<Position>().x = go.GetComponent<Position>().x - 1;
					go.GetComponent<Position>().z = go.GetComponent<Position>().z;
				}
				break;
		}
		go.GetComponent<Position>().animate = true;
	}

	private void ApplyTurnLeft(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
		}
	}

	private void ApplyTurnRight(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
		}
	}

	private void ApplyTurnBack(GameObject go){
		switch (go.GetComponent<Direction>().direction){
			case Direction.Dir.North:
				go.GetComponent<Direction>().direction = Direction.Dir.South;
				break;
			case Direction.Dir.South:
				go.GetComponent<Direction>().direction = Direction.Dir.North;
				break;
			case Direction.Dir.East:
				go.GetComponent<Direction>().direction = Direction.Dir.West;
				break;
			case Direction.Dir.West:
				go.GetComponent<Direction>().direction = Direction.Dir.East;
				break;
		}
	}

	private bool checkObstacle(int x, int z){
		foreach( GameObject go in wallGO){
			if(go.activeInHierarchy && go.GetComponent<Position>().x == x && go.GetComponent<Position>().z == z)
				return true;
		}
		return false;
	}
}
