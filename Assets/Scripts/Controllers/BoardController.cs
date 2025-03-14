using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;


    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;
        m_gameSettings = gameSettings;
        m_gameManager.StateChangedAction += OnGameStateChange;
        m_cam = Camera.main;
        m_board = new Board(this.transform, gameSettings);
        Fill();
    }


    private void Fill()
    {
        m_board.Fill();
        FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                //StopHints();
                break;
        }
    }
    public void MakeBestMove()
    {
        for (int x = 0; x < m_board.boardSizeX; x++)
        {
            for (int y = 0; y < m_board.boardSizeY; y++)
            {
                var cell = m_board.m_cells[x, y];
                if (!cell.IsEmpty)
                {
                    var item = cell.Item;

                    if (m_board.IsBottomAreaEmpty())
                    {
                        m_board.AddItemToBottomArea(item);
                        cell.Free();
                        return;
                    }

                    bool isDuplicate = m_board.m_bottomCells.Any(bottomCell => bottomCell.Item != null && bottomCell.Item.IsSameType(item));
                    if (isDuplicate)
                    {
                        m_board.AddItemToBottomArea(item);
                        cell.Free();
                        var matches = m_board.CheckBottomAreaForMatches();
                        if (matches.Count > 0)
                        {
                            foreach (var match in matches)
                            {
                                match.ExplodeItem();
                                match.Free();
                            }
                        }

                        if (m_board.IsBottomAreaFull())
                        {
                            ShowLoseScreen();
                            m_gameOver = true;
                        }
                        else if (IsBoardCleared())
                        {
                            ShowWinScreen();
                            m_gameOver = true;
                        }
                        return;
                    }
                }
            }
        }
    }




    public void MakeLoseMove()
    {
        for (int x = 0; x < m_board.boardSizeX; x++)
        {
            for (int y = 0; y < m_board.boardSizeY; y++)
            {
                var cell = m_board.m_cells[x, y];
                if (!cell.IsEmpty)
                {
                    var item = cell.Item;
                    bool isDuplicate = m_board.m_bottomCells.Any(bottomCell => bottomCell.Item != null && bottomCell.Item.IsSameType(item));

                    if (!isDuplicate)
                    {
                        m_board.AddItemToBottomArea(item);
                        cell.Free();
                        return;
                    }
                }
            }
        }
    }



    public void Update()
    {
        if (m_gameOver) return;
        if (IsBusy) return;

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                m_isDragging = true;
                m_hitCollider = hit.collider;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (m_isDragging && m_hitCollider != null)
            {
                var cell = m_hitCollider.GetComponent<Cell>();
                if (cell != null && !cell.IsEmpty)
                {
                    var item = cell.Item;

                    if (m_board.m_bottomCells.Contains(cell) && m_gameManager.LevelMode == GameManager.eLevelMode.TIMER)
                    {
                        m_board.MoveItemFromBottomToOriginal(cell);
                    }
                    else
                    {
                        cell.Free();
                        m_board.AddItemToBottomArea(item);

                        var matches = m_board.CheckBottomAreaForMatches();
                        if (matches.Count > 0)
                        {
                            foreach (var match in matches)
                            {
                                match.ExplodeItem();
                                match.Free();
                            }
                        }

                        if (m_board.IsBottomAreaFull())
                        {
                            if (m_gameManager.LevelMode != GameManager.eLevelMode.TIMER)
                            {
                                ShowLoseScreen();
                                m_gameOver = true;
                            }
                        }
                        else if (IsBoardCleared())
                        {
                            ShowWinScreen();
                            m_gameOver = true;
                        }
                    }
                }
            }

            ResetRayCast();
        }
    }



    public bool IsBoardCleared()
    {
        for (int x = 0; x < m_board.boardSizeX; x++)
        {
            for (int y = 0; y < m_board.boardSizeY; y++)
            {
                if (!m_board.m_cells[x, y].IsEmpty)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void ShowWinScreen()
    {
        m_gameManager.ShowWinScreen();
    }

    private void ShowLoseScreen()
    {
        m_gameManager.ShowLoseScreen();
    }

    private void ResetRayCast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        //if (cell1.Item is BonusItem)
        //{
        //    cell1.ExplodeItem();
        //    StartCoroutine(ShiftDownItemsCoroutine());
        //}
        //else if (cell2.Item is BonusItem)
        //{
        //    cell2.ExplodeItem();
        //    StartCoroutine(ShiftDownItemsCoroutine());
        //}
        //else
        //{
        //    List<Cell> cells1 = GetMatches(cell1);
        //    List<Cell> cells2 = GetMatches(cell2);

        //    List<Cell> matches = new List<Cell>();
        //    matches.AddRange(cells1);
        //    matches.AddRange(cells2);
        //    matches = matches.Distinct().ToList();

        //    if (matches.Count < m_gameSettings.MatchesMin)
        //    {
        //        m_board.Swap(cell1, cell2, () =>
        //        {
        //            IsBusy = false;
        //        });
        //    }
        //    else
        //    {
        //        OnMoveEvent();

        //        CollapseMatches(matches, cell2);
        //    }
        //}
    }

    private void FindMatchesAndCollapse()
    {
        //List<Cell> matches = m_board.FindFirstMatch();

        //if (matches.Count > 0)
        //{
        //    CollapseMatches(matches, null);
        //}
        //else
        //{
        //    m_potentialMatch = m_board.GetPotentialMatches();
        //    if (m_potentialMatch.Count > 0)
        //    {
        //        IsBusy = false;

        //        m_timeAfterFill = 0f;
        //    }
        //    else
        //    {
        //        //StartCoroutine(RefillBoardCoroutine());
        //        StartCoroutine(ShuffleBoardCoroutine());
        //    }
        //}
    }

    //private List<Cell> GetMatches(Cell cell)
    //{
    //    List<Cell> listHor = m_board.GetHorizontalMatches(cell);
    //    if (listHor.Count < m_gameSettings.MatchesMin)
    //    {
    //        listHor.Clear();
    //    }

    //    List<Cell> listVert = m_board.GetVerticalMatches(cell);
    //    if (listVert.Count < m_gameSettings.MatchesMin)
    //    {
    //        listVert.Clear();
    //    }

    //    return listHor.Concat(listVert).Distinct().ToList();
    //}

    //private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    //{
    //    for (int i = 0; i < matches.Count; i++)
    //    {
    //        matches[i].ExplodeItem();
    //    }

    //    if (matches.Count > m_gameSettings.MatchesMin)
    //    {
    //        m_board.ConvertNormalToBonus(matches, cellEnd);
    //    }

    //    StartCoroutine(ShiftDownItemsCoroutine());
    //}

    //private IEnumerator ShiftDownItemsCoroutine()
    //{
    //    m_board.ShiftDownItems();

    //    yield return new WaitForSeconds(0.2f);

    //    m_board.FillGapsWithNewItems();

    //    yield return new WaitForSeconds(0.2f);

    //    FindMatchesAndCollapse();
    //}

    //private IEnumerator RefillBoardCoroutine()
    //{
    //    m_board.ExplodeAllItems();

    //    yield return new WaitForSeconds(0.2f);

    //    m_board.Fill();

    //    yield return new WaitForSeconds(0.2f);

    //    FindMatchesAndCollapse();
    //}

    //private IEnumerator ShuffleBoardCoroutine()
    //{
    //    m_board.Shuffle();

    //    yield return new WaitForSeconds(0.3f);

    //    FindMatchesAndCollapse();
    //}


    //private void SetSortingLayer(Cell cell1, Cell cell2)
    //{
    //    if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
    //    if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    //}

    //private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    //{
    //    return cell1.IsNeighbour(cell2);
    //}

    internal void Clear()
    {
        //m_board.Clear();
    }

    internal bool IsBottomAreaFull()
    {
        return m_board.IsBottomAreaFull();
    }

    // Comment out or remove hint-related methods
    // private void ShowHint()
    // {
    //     m_hintIsShown = true;
    //     foreach (var cell in m_potentialMatch)
    //     {
    //         cell.AnimateItemForHint();
    //     }
    // }

    // private void StopHints()
    // {
    //     m_hintIsShown = false;
    //     foreach (var cell in m_potentialMatch)
    //     {
    //         cell.StopHintAnimation();
    //     }

    //     m_potentialMatch.Clear();
    // }
}