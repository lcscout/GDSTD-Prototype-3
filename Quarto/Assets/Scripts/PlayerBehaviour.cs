using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerBehaviour : NetworkBehaviour {

	public bool canPlay = false;
	public int playerNumber = 0;

	private Board.Tile _selectedTile;
	private int _i;
	private int _j;

	public bool winner = false;
	private bool _hasShowedWinner = false;
	private bool _hasShowedNumber = false;

	private void Start() {
		if (GameManager.Instance != null)
			GameManager.Instance.players.Add(gameObject);
	}

	private void Update() {
		if (!isLocalPlayer) return;
		if (GameManager.Instance.isGameOver) {
			if (!_hasShowedWinner) ShowWinner();
			return;
		};
		if (playerNumber != 0 && !_hasShowedNumber)
			ShowPlayerNumber();

		if (canPlay && Input.GetMouseButtonUp(0)) {
			if (GameManager.Instance.state == GameState.PLAYER_1_PLAY ||
				GameManager.Instance.state == GameState.PLAYER_2_PLAY) {
				SelectTile();
				StartCoroutine(AssignAuthorityAndMove());
			} else if (GameManager.Instance.state == GameState.PLAYER_1_SELECT ||
				  GameManager.Instance.state == GameState.PLAYER_2_SELECT)
				SelectPiece();
		}
	}

	private void ShowWinner() {
		if (winner)
			GameObject.Find("Canvas").GetComponent<UIHandler>().winText.gameObject.SetActive(true);
		else
			GameObject.Find("Canvas").GetComponent<UIHandler>().loseText.gameObject.SetActive(true);

		_hasShowedWinner = true;
	}

	private void ShowEndButton() {
		if (playerNumber == 1 && GameManager.Instance.state == GameState.PLAYER_1_SELECT)
			GameObject.Find("Canvas").GetComponent<UIHandler>().endButton.gameObject.SetActive(true);
		else if (playerNumber == 2 && GameManager.Instance.state == GameState.PLAYER_2_SELECT)
			GameObject.Find("Canvas").GetComponent<UIHandler>().endButton.gameObject.SetActive(true);
		else
			GameObject.Find("Canvas").GetComponent<UIHandler>().endButton.gameObject.SetActive(false);
	}

	private void ShowPlayerNumber() {
		GameObject.Find("Canvas").GetComponent<UIHandler>().playerNumber.text = "Player: " + playerNumber;

		_hasShowedNumber = true;
	}

	private void SelectTile() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hit)) {
			for (int i = 0; i < GameManager.Instance.board.tiles.GetLength(0); i++) {
				for (int j = 0; j < GameManager.Instance.board.tiles.GetLength(1); j++) {
					if (GameManager.Instance.board.tiles[i, j].HasVector(hit.point)) {
						_i = i;
						_j = j;
						_selectedTile = GameManager.Instance.board.tiles[i, j];
						break;
					}
				}
			}
		}
	}

	private IEnumerator AssignAuthorityAndMove() {
		CmdAssignAuthority(GameManager.Instance.lastSelectedPiece.GetComponent<NetworkIdentity>());

		yield return new WaitForSeconds(0.5f);
		GameManager.Instance.lastSelectedPiece.GetComponent<Piece>().ContainTile(_i, _j);
		yield return new WaitForSeconds(0.5f);
		MovePiece();
	}

	[Command]
	public void CmdAssignAuthority(NetworkIdentity assignObj) {
		assignObj.RemoveClientAuthority();
		assignObj.AssignClientAuthority(connectionToClient);
	}

	private void MovePiece() {
		Piece piece = GameManager.Instance.lastSelectedPiece.GetComponent<Piece>();
		piece.RemoveHighlight();

		Vector3 pos;
		if (piece.isTall)
			pos = new Vector3(_selectedTile.xCenter, 2f, _selectedTile.zCenter);
		else
			pos = new Vector3(_selectedTile.xCenter, 1.5f, _selectedTile.zCenter);

		piece.MoveTo(pos);

		StartCoroutine(WaitFor(1.5f));

		if (GameManager.Instance.board.CheckForEndGame())
			EndGame();
		else
			CmdEnterSelectState();
	}

	private IEnumerator WaitFor(float seconds) {
		yield return new WaitForSeconds(seconds);
	}

	public void EndGame() {
		if (playerNumber == 1 && GameManager.Instance.state == GameState.PLAYER_1_PLAY)
			winner = true;
		else if (playerNumber == 2 && GameManager.Instance.state == GameState.PLAYER_2_PLAY)
			winner = true;

		CmdEndGame();
	}

	[Command]
	private void CmdEndGame() {
		GameManager.Instance.isGameOver = true;
	}

	[Command]
	private void CmdEnterSelectState() {
		GameManager.Instance.lastSelectedPiece = null;

		if (playerNumber == 1)
			GameManager.Instance.state = GameState.PLAYER_1_SELECT;
		else
			GameManager.Instance.state = GameState.PLAYER_2_SELECT;
	}

	private void SelectPiece() {
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hit)) {
			if (!hit.transform.gameObject.GetComponent<Piece>().isPlaced) {
				ShowEndButton();
				if (GameManager.Instance.lastSelectedPiece != null)
					StartRemoveHighlight(GameManager.Instance.lastSelectedPiece);
				CmdSelectPiece(hit.transform.gameObject);
			}
		}
	}

	public void StartAddHighlight() {
		if (isLocalPlayer) StartCoroutine(AssignAuthorityAndAddHighlight());
	}

	private IEnumerator AssignAuthorityAndAddHighlight() {
		CmdAssignAuthority(GameManager.Instance.lastSelectedPiece.GetComponent<NetworkIdentity>());

		yield return new WaitForSeconds(0.5f);
		AddHighlight();
	}

	private void AddHighlight() {
		GameManager.Instance.lastSelectedPiece.GetComponent<Piece>().AddHighlight();
	}
	public void StartRemoveHighlight(GameObject piece) {
		if (isLocalPlayer) StartCoroutine(AssignAuthorityAndRemoveHighlight(piece));
	}

	private IEnumerator AssignAuthorityAndRemoveHighlight(GameObject piece) {
		CmdAssignAuthority(piece.GetComponent<NetworkIdentity>());

		yield return new WaitForSeconds(0.5f);
		RemoveHighlight(piece);
	}

	private void RemoveHighlight(GameObject piece) {
		piece.GetComponent<Piece>().RemoveHighlight();
	}

	[Command]
	private void CmdSelectPiece(GameObject piece) {
		GameManager.Instance.lastSelectedPiece = piece;
	}

	public void EndTurn() {
		if (GameManager.Instance.lastSelectedPiece == null)
			return;

		if (isLocalPlayer) {
			canPlay = false;
			GameObject.Find("Canvas").GetComponent<UIHandler>().endButton.gameObject.SetActive(false);
			CmdEndTurn();
		}
	}

	[Command]
	private void CmdEndTurn() {
		if (playerNumber == 1) {
			GameManager.Instance.state = GameState.PLAYER_2_PLAY;
			GameManager.Instance.NewTurn();
		} else {
			GameManager.Instance.state = GameState.PLAYER_1_PLAY;
			GameManager.Instance.NewTurn();
		}
	}
}
