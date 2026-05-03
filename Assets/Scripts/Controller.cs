using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        // Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        // Para cada posición, rellenar con 1's las casillas adyacentes y llenar lista "adjacency"
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            // Inicializamos la lista por si acaso no lo estuviera en Tile.cs
            tiles[i].adjacency = new List<int>();

            int row = i / Constants.TilesPerRow;
            int col = i % Constants.TilesPerRow;

            // Arriba (fila > 0)
            if (row > 0)
            {
                matriu[i, i - Constants.TilesPerRow] = 1;
                tiles[i].adjacency.Add(i - Constants.TilesPerRow);
            }
            // Abajo (fila < 7)
            if (row < Constants.TilesPerRow - 1)
            {
                matriu[i, i + Constants.TilesPerRow] = 1;
                tiles[i].adjacency.Add(i + Constants.TilesPerRow);
            }
            // Izquierda (columna > 0)
            if (col > 0)
            {
                matriu[i, i - 1] = 1;
                tiles[i].adjacency.Add(i - 1);
            }
            // Derecha (columna < 7)
            if (col < Constants.TilesPerRow - 1)
            {
                matriu[i, i + 1] = 1;
                tiles[i].adjacency.Add(i + 1);
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        // Recopilamos todas las casillas a las que puede ir
        List<Tile> selectableTiles = new List<Tile>();
        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                selectableTiles.Add(tile);
            }
        }

        // Elegimos una aleatoria si hay opciones
        if (selectableTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, selectableTiles.Count);
            Tile chosenTile = selectableTiles[randomIndex];

            // Movemos al caco a esa casilla
            robber.GetComponent<RobberMove>().MoveToTile(chosenTile);

            // Actualizamos la variable currentTile del caco
            robber.GetComponent<RobberMove>().currentTile = chosenTile.numTile;
        }
    }

    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        // Iniciamos el nodo de partida
        Tile startNode = tiles[indexcurrentTile];
        startNode.visited = true;
        startNode.distance = 0; // Distancia inicial es 0
        nodes.Enqueue(startNode);

        // Si es un poli, necesitamos saber dónde está el OTRO poli para no atravesarlo
        int otherCopTile = -1;
        if (cop == true)
        {
            int otherCopId = (clickedCop == 0) ? 1 : 0;
            otherCopTile = cops[otherCopId].GetComponent<CopMove>().currentTile;
        }

        // BFS clásico
        while (nodes.Count > 0)
        {
            Tile current = nodes.Dequeue();

            // Límite de movimientos: si la distancia ya es 2, no exploramos los vecinos de esta casilla
            if (current.distance >= 2)
                continue;

            foreach (int adjIndex in current.adjacency)
            {
                // Un policía no puede moverse pasando por el otro policía
                if (cop == true && adjIndex == otherCopTile)
                    continue;

                Tile adjTile = tiles[adjIndex];

                if (!adjTile.visited)
                {
                    adjTile.visited = true;
                    adjTile.distance = current.distance + 1;
                    adjTile.parent = current;      // Esto es clave para que la ficha sepa hacer el caminito visualmente
                    adjTile.selectable = true;     // Como la distancia es >0 y <=2, es seleccionable

                    nodes.Enqueue(adjTile);
                }
            }
        }
    }









}
