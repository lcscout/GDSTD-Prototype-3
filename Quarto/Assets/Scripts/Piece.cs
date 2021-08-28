using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Piece : NetworkBehaviour {
	public bool isLight;
	public bool isTall;
	public bool isSquare;
	public bool isHoled;

	[SyncVar] public int containedTile_i = 10;
	[SyncVar] public int containedTile_j = 10;

	[SyncVar] public bool isPlaced = false;

	[SyncVar] public bool shouldUpdateOutline = false;
	[SyncVar] public string outlineUpdateAction;

	private void Start() {
		Outline outline = gameObject.AddComponent<Outline>();

		outline.OutlineMode = Outline.Mode.OutlineAll;
		outline.OutlineColor = Color.blue;
		outline.OutlineWidth = 8f;

		outline.enabled = false;
	}

	private void Update() {
		if (shouldUpdateOutline) {
			if (outlineUpdateAction == "remove")
				GetComponent<Outline>().enabled = false;
			else if (outlineUpdateAction == "add")
				GetComponent<Outline>().enabled = true;

			shouldUpdateOutline = false;
		}
	}

	public void AddHighlight() {
		shouldUpdateOutline = true;
		outlineUpdateAction = "add";

		CmdAddHighlight();
	}

	[Command]
	private void CmdAddHighlight() {
		shouldUpdateOutline = true;
		outlineUpdateAction = "add";
	}

	public void RemoveHighlight() {
		shouldUpdateOutline = true;
		outlineUpdateAction = "remove";

		CmdRemoveHighlight();
	}

	[Command]
	private void CmdRemoveHighlight() {
		shouldUpdateOutline = true;
		outlineUpdateAction = "remove";
	}

	public void MoveTo(Vector3 pos) {
		transform.position = pos;
		isPlaced = true;

		CmdMove(pos);
	}

	[Command]
	private void CmdMove(Vector3 pos) {
		transform.position = pos;
		isPlaced = true;
	}

	public void ContainTile(int i, int j) {
		containedTile_i = i;
		containedTile_j = j;

		CmdContainTile(i, j);
	}

	[Command]
	private void CmdContainTile(int i, int j) {
		containedTile_i = i;
		containedTile_j = j;
	}

	public bool IsLookalike(Piece piece) {
		if (isLight == piece.isLight ||
			(isTall == piece.isTall) ||
			(isSquare == piece.isSquare) ||
			(isHoled == piece.isHoled))
			return true;
		else
			return false;
	}
}
