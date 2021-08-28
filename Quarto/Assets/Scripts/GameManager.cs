using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum GameState { START, PLAYER_1_SELECT, PLAYER_1_PLAY, PLAYER_2_SELECT, PLAYER_2_PLAY, END }

public class Board {
	public struct Tile {
		public float xCenter;
		public float zCenter;
		public Piece pieceAllocated;

		public Tile(int x, int z) {
			xCenter = x;
			zCenter = z;
			pieceAllocated = null;
		}

		public bool HasVector(Vector3 vec) {
			float x = vec.x;
			float z = vec.z;
			float borderLeft = xCenter - 2;
			float borderRight = xCenter + 2;
			float borderUp = zCenter + 2;
			float borderDown = zCenter - 2;

			if (x > borderLeft && x < borderRight &&
				z < borderUp && z > borderDown)
				return true;
			else
				return false;
		}

		public void AllocatePiece(Piece piece) {
			pieceAllocated = piece;
		}
	}

	public readonly Tile[,] tiles = new Tile[4, 4];

	public Board() {
		Tile startPosition = new Tile(-6, 6);

		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 4; j++) {
				if (j == 0) tiles[i, j] = startPosition;
				else {
					tiles[i, j] = tiles[i, j - 1];
					tiles[i, j].xCenter += 4;
				}
			}
			startPosition.zCenter -= 4;
		}

		// for (int i = 0; i < 4; i++) {
		// 	for (int j = 0; j < 4; j++) {
		// 		Debug.Log(tilesPositions[i, j].xPos.ToString() + ", " + tilesPositions[i, j].zPos.ToString());
		// 	}
		// }
	}

	private bool CheckHorizontal() {
		int count = 0;
		Piece lastPiece = null;

		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 4; j++) {
				if (tiles[i, j].pieceAllocated != null) {
					if (lastPiece == null) {
						lastPiece = tiles[i, j].pieceAllocated;
						count++;
					}
					else if (lastPiece.IsLookalike(tiles[i, j].pieceAllocated)) {
						lastPiece = tiles[i, j].pieceAllocated;
						count++;
					}

					if (count == 4)
						return true;
				}
			}
			count = 0;
			lastPiece = null;
		}

		return false;
	}

	private bool CheckVertical() {
		int count = 0;
		Piece lastPiece = null;

		for (int j = 0; j < 4; j++) {
			for (int i = 0; i < 4; i++) {
				if (tiles[i, j].pieceAllocated != null) {
					if (lastPiece == null) {
						lastPiece = tiles[i, j].pieceAllocated;
						count++;
					}
					else if (lastPiece.IsLookalike(tiles[i, j].pieceAllocated)) {
						lastPiece = tiles[i, j].pieceAllocated;
						count++;
					}

					if (count == 4)
						return true;
				}
			}
			count = 0;
			lastPiece = null;
		}

		return false;
	}

	private bool CheckLeftDiagonal() {
		int count = 0;
		Piece lastPiece = null;

		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 4; j++) {
				if (i == j) {
					if (tiles[i, j].pieceAllocated != null) {
						if (lastPiece == null) {
							lastPiece = tiles[i, j].pieceAllocated;
							count++;
						}
						else if (lastPiece.IsLookalike(tiles[i, j].pieceAllocated)) {
							lastPiece = tiles[i, j].pieceAllocated;
							count++;
						}

						if (count == 4)
							return true;
					}
				}
			}
		}

		return false;
	}

	private bool CheckRightDiagonal() {
		int count = 0;
		Piece lastPiece = null;

		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 4; j++) {
				if (i + j == 3) {
					if (tiles[i, j].pieceAllocated != null) {
						if (lastPiece == null) {
							lastPiece = tiles[i, j].pieceAllocated;
							count++;
						}
						else if (lastPiece.IsLookalike(tiles[i, j].pieceAllocated)) {
							lastPiece = tiles[i, j].pieceAllocated;
							count++;
						}

						if (count == 4)
							return true;
					}
				}
			}
		}

		return false;
	}

	public bool CheckForEndGame() {
		Piece[] pieces = GameObject.FindObjectsOfType<Piece>();

		for (int i = 0; i < pieces.Length; i ++) {
			if (pieces[i].containedTile_i != 10 &&
				pieces[i].containedTile_j != 10) {
				tiles[pieces[i].containedTile_i, pieces[i].containedTile_j].AllocatePiece(pieces[i]);
			}
		}

		if (CheckHorizontal() || CheckVertical() || CheckLeftDiagonal() || CheckRightDiagonal())
			return true;
		else
			return false;
	}
}

public class GameManager : NetworkBehaviour {
	public static GameManager Instance;

	[SyncVar(hook = "OnStateChange")] public GameState state;
	public Board board;

	private bool _gameStarted = false;

	public List<GameObject> players;

	[SyncVar(hook = "OnPlayer1Change")] public GameObject playerOne;

	[SyncVar(hook = "OnPlayer2Change")] public GameObject playerTwo;

	private GameObject _currentPlayer;
	[SyncVar(hook = "OnPieceChange")] public GameObject lastSelectedPiece;

	[SyncVar(hook = "OnGameoverChange")] public bool isGameOver = false;

	private void Awake() {
		if (Instance != null) {
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	private void OnPlayer1Change(GameObject oldValue, GameObject newValue) {
		playerOne = newValue;
	}

	private void OnPlayer2Change(GameObject oldValue, GameObject newValue) {
		playerTwo = newValue;
	}

	private void OnPieceChange(GameObject oldValue, GameObject newValue) {
		lastSelectedPiece = newValue;

		if (_currentPlayer != null && newValue != null)
			_currentPlayer.GetComponent<PlayerBehaviour>().StartAddHighlight();
	}

	private void OnGameoverChange(bool oldValue, bool newValue) {
		isGameOver = newValue;
	}

	private void Update() {
		if (players.Count == 2 && !_gameStarted) {
			state = GameState.START;
			SetupBoard();

			_gameStarted = true;
		}
	}

	private void OnStateChange(GameState oldState, GameState newState) {
		state = newState;

		if (state == GameState.PLAYER_1_PLAY || state == GameState.PLAYER_2_PLAY)
			NewTurn();
	}

	private void SetupBoard() {
		board = new Board();

		if (isServer) FlipCoin();

		StartCoroutine(InitiateGame());
	}

	[Server]
	private void FlipCoin() {
		int random = Random.Range(0, 2);
		if (random == 0) {
			playerOne = players[0];
			playerTwo = players[1];
		} else {
			playerOne = players[1];
			playerTwo = players[0];
		}
	}

	private void AssignPlayerNumbers() {
		playerOne.GetComponent<PlayerBehaviour>().playerNumber = 1;
		playerTwo.GetComponent<PlayerBehaviour>().playerNumber = 2;
	}

	private IEnumerator InitiateGame() {
		AssignPlayerNumbers();
		yield return new WaitForSeconds(1.5f);

		_currentPlayer = playerOne;
		if (isServer) state = GameState.PLAYER_1_SELECT;
		GameObject.Find("Canvas").GetComponent<UIHandler>().playerTurn.text = "Player 1 Turn";
		PlayTurn();
	}

	public void NewTurn() {
		if (state == GameState.PLAYER_1_PLAY) {
			_currentPlayer = playerOne;
			GameObject.Find("Canvas").GetComponent<UIHandler>().playerTurn.text = "Player 1 Turn";
		}
		else if (state == GameState.PLAYER_2_PLAY) {
			_currentPlayer = playerTwo;
			GameObject.Find("Canvas").GetComponent<UIHandler>().playerTurn.text = "Player 2 Turn";
		}

		PlayTurn();
	}

	private void PlayTurn() {
		_currentPlayer.GetComponent<PlayerBehaviour>().canPlay = true;
	}

	public void EndTurn() {
		_currentPlayer.GetComponent<PlayerBehaviour>().EndTurn();
	}
}
