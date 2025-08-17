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
    private GameSettings m_gameSettings;
    private Camera m_cam;

    private float m_timeAfterFill;
    private bool m_hintIsShown;
    private bool m_gameOver;

    private Cell[] m_bottomCells;
    private HashSet<Cell> m_bottomCellSet;

    private const float MOVE_TO_BOTTOM_TIME = 0.25f;
    private const float BOTTOM_SHIFT_TIME   = 0.15f;

    private int m_remainingOnBoard;

    public enum PlayMode { Classic, TimeAttack }
    private PlayMode m_playMode = PlayMode.Classic;
    private bool IsTimeAttack => m_playMode == PlayMode.TimeAttack;
    public void EnableTimeAttack(bool enable) => m_playMode = enable ? PlayMode.TimeAttack : PlayMode.Classic;

    private Coroutine m_autoCR;
    private float m_autoStepDelay = 0.5f;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        m_gameManager = gameManager;
        m_gameSettings = gameSettings;
        m_gameManager.StateChangedAction += OnGameStateChange;
        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);
        m_board.Fill();

        MakeInitialCountsDivisibleBy3();
        MarkInitialCells();
        BuildBottomAreaAsCells();

        m_remainingOnBoard = m_gameSettings.BoardSizeX * m_gameSettings.BoardSizeY;
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED: IsBusy = false; break;
            case GameManager.eStateGame.PAUSE:        IsBusy = true;  break;
            case GameManager.eStateGame.GAME_OVER:    m_gameOver = true; break;
        }
    }

    public void Update()
    {
        if (m_gameOver || IsBusy) return;

        if (!m_hintIsShown)
        {
            m_timeAfterFill += Time.deltaTime;
            if (m_timeAfterFill > m_gameSettings.TimeForHint) m_timeAfterFill = 0f;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit  = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            var cell = hit.collider ? hit.collider.GetComponent<Cell>() : null;
            if (!cell) return;

            bool isBottom = m_bottomCellSet != null && m_bottomCellSet.Contains(cell);
            if (isBottom && IsTimeAttack) TryReturnToInitial(cell);
            else                           TryCollect(cell);
        }
    }

    //BottomArea
    private void BuildBottomAreaAsCells()
    {
        int n = Mathf.Max(1, m_gameSettings.BottomAreaMax);
        m_bottomCells = new Cell[n];

        var boardBottomRow = GetComponentsInChildren<Cell>(true)
            .Where(c => c.BoardY == 0)
            .OrderBy(c => c.BoardX)
            .ToList();

        var root = new GameObject("BottomArea").transform;
        root.SetParent(this.transform, false);

        var bgPrefab = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);

        int startCol = 0;
        if (boardBottomRow.Count >= n) startCol = (boardBottomRow.Count - n) / 2;

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject($"BottomCell_{i}");
            go.transform.SetParent(root, false);

            float x;
            if (boardBottomRow.Count > i + startCol)      x = boardBottomRow[i + startCol].transform.position.x;
            else if (boardBottomRow.Count > 0)            x = boardBottomRow[0].transform.position.x + i;
            else                                          x = i;

            float y = m_gameSettings.BottomAreaPosition;
            go.transform.position = new Vector3(x, y, 0f);

            if (bgPrefab != null)
            {
                var bg = GameObject.Instantiate(bgPrefab, go.transform);
                bg.transform.localPosition = Vector3.zero;
            }

            var cell = go.AddComponent<Cell>();
            var col  = go.AddComponent<BoxCollider2D>();
            col.isTrigger = false;

            m_bottomCells[i] = cell;
        }

        m_bottomCellSet = new HashSet<Cell>(m_bottomCells);
    }

    //Collect
    private void TryCollect(Cell fromCell)
    {
        if (fromCell == null || fromCell.IsEmpty) return;
        if (m_gameOver || IsBusy) return;

        int occupied = GetBottomOccupiedCount();
        if (occupied >= m_bottomCells.Length) { if (!IsTimeAttack) Lose(); return; }

        IsBusy = true;

        var item  = fromCell.Item;
        var nitem = item as NormalItem;

        int insertIndex = (nitem != null) ? GetInsertIndexForType(nitem.ItemType, occupied) : occupied;
        Vector3 dstPos  = m_bottomCells[insertIndex].transform.position;

        item.SetSortingLayerHigher();
        item.View.DOMove(dstPos, MOVE_TO_BOTTOM_TIME).OnComplete(() =>
        {
            fromCell.Free();
            ShiftRightFromIndex(insertIndex, occupied);

            var dstCell = m_bottomCells[insertIndex];
            dstCell.Assign(item);
            dstCell.ApplyItemPosition(false);
            SoundManager.Instance.PlayCollect();

            OnMoveEvent();
            m_remainingOnBoard--;

            bool cleared = TryClearTripleExactThree();

            if (m_remainingOnBoard <= 0) { Win(); }
            else if (!cleared && !IsTimeAttack && GetBottomOccupiedCount() >= m_bottomCells.Length) { Lose(); }

            IsBusy = false;
        });
    }

    private bool TryClearTripleExactThree()
    {
        var occupied = new List<(int idx, Cell cell, NormalItem item)>();
        for (int i = 0; i < m_bottomCells.Length; i++)
        {
            var c = m_bottomCells[i];
            if (!c.IsEmpty && c.Item is NormalItem n) occupied.Add((i, c, n));
        }
        if (occupied.Count < 3) return false;

        var map = new Dictionary<NormalItem.eNormalType, List<int>>();
        foreach (var o in occupied)
        {
            if (!map.TryGetValue(o.item.ItemType, out var list))
            {
                list = new List<int>();
                map[o.item.ItemType] = list;
            }
            list.Add(o.idx);
        }

        var triple = map.FirstOrDefault(p => p.Value.Count == 3);
        if (triple.Value == null) return false;

        var idxs = triple.Value.OrderByDescending(v => v).ToList();
        foreach (int i in idxs)
        {
            var cell = m_bottomCells[i];
            var it   = cell.Item;
            cell.Clear();
            it.ExplodeView();
        }

        CompactBottomCells();
        return true;
    }

    //Arrange
    private void CompactBottomCells()
    {
        var leftItems = new List<Item>();
        foreach (var c in m_bottomCells)
        {
            if (!c.IsEmpty) { leftItems.Add(c.Item); c.Free(); }
        }
        for (int i = 0; i < leftItems.Count; i++)
        {
            var cell = m_bottomCells[i];
            var it   = leftItems[i];
            cell.Assign(it);
            cell.ApplyItemPosition(false);
        }
    }

    private int GetBottomOccupiedCount()
    {
        int c = 0;
        for (int i = 0; i < m_bottomCells.Length; i++)
            if (!m_bottomCells[i].IsEmpty) c++; else break;
        return c;
    }

    private int GetInsertIndexForType(NormalItem.eNormalType t, int occupied)
    {
        int last = -1;
        for (int i = 0; i < occupied; i++)
        {
            var n = m_bottomCells[i].Item as NormalItem;
            if (n != null && n.ItemType == t) last = i;
        }
        return (last >= 0) ? last + 1 : occupied;
    }

    private void ShiftRightFromIndex(int insertIndex, int occupied)
    {
        for (int dst = Mathf.Min(m_bottomCells.Length - 1, occupied); dst > insertIndex; dst--)
        {
            int src = dst - 1;
            var srcCell = m_bottomCells[src];
            var dstCell = m_bottomCells[dst];
            if (srcCell.IsEmpty) continue;

            var moving = srcCell.Item;
            srcCell.Free();
            dstCell.Assign(moving);

            if (moving != null && moving.View != null)
                moving.View.DOMove(dstCell.transform.position, BOTTOM_SHIFT_TIME);
            else
                dstCell.ApplyItemPosition(false);
        }
    }

    //Arrange
    private void TryReturnToInitial(Cell bottomCell)
    {
        if (bottomCell == null || bottomCell.IsEmpty) return;
        if (IsBusy || m_gameOver) return;

        var init = bottomCell.Item.InitialCell;
        if (init == null || !init.IsEmpty) return;

        IsBusy = true;

        var item  = bottomCell.Item;
        var dstPos = init.transform.position;

        item.SetSortingLayerHigher();
        item.View.DOMove(dstPos, 0.25f).OnComplete(() =>
        {
            bottomCell.Free();
            init.Assign(item);
            init.ApplyItemPosition(false);

            OnMoveEvent();
            m_remainingOnBoard++;

            CompactBottomCells();
            IsBusy = false;
        });
    }

    private void Win()
    {
        if (m_gameOver) return;
        m_gameOver = true;
        m_gameManager.GameWin();
    }

    private void Lose()
    {
        if (m_gameOver) return;
        m_gameOver = true;
        m_gameManager.GameOver();
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    //InitDivisibleBy3
    private void MakeInitialCountsDivisibleBy3()
    {
        var allCells = GetComponentsInChildren<Cell>(true).ToList();

        var byType = new Dictionary<NormalItem.eNormalType, List<Cell>>();
        foreach (NormalItem.eNormalType t in Enum.GetValues(typeof(NormalItem.eNormalType)))
            byType[t] = new List<Cell>();

        foreach (var cell in allCells)
        {
            if (cell.IsEmpty) continue;
            if (cell.Item is NormalItem n) byType[n.ItemType].Add(cell);
        }

        int total = byType.Values.Sum(v => v.Count);
        if (total % 3 != 0) return;

        var rem = new Dictionary<NormalItem.eNormalType, int>();
        var r1  = new List<NormalItem.eNormalType>();
        var r2  = new List<NormalItem.eNormalType>();

        foreach (var kv in byType)
        {
            int r = kv.Value.Count % 3;
            rem[kv.Key] = r;
            if (r == 1) r1.Add(kv.Key);
            else if (r == 2) r2.Add(kv.Key);
        }

        void MoveOne(NormalItem.eNormalType src, NormalItem.eNormalType dst)
        {
            var listSrc = byType[src];
            var cell = listSrc[listSrc.Count - 1];
            listSrc.RemoveAt(listSrc.Count - 1);

            cell.Clear();
            var newItem = new NormalItem();
            newItem.SetType(dst);
            newItem.SetView();
            newItem.SetViewRoot(this.transform);
            cell.Assign(newItem);
            cell.ApplyItemPosition(false);

            byType[dst].Add(cell);
            rem[src] = (rem[src] + 2) % 3;
            rem[dst] = (rem[dst] + 1) % 3;
        }

        while (r2.Count > 0 && r1.Count > 0)
        {
            var t2 = r2[0];
            var t1 = r1[0];
            MoveOne(t2, t1);
            MoveOne(t2, t1);
            r2.RemoveAt(0);
            r1.RemoveAt(0);
        }

        while (r1.Count >= 3)
        {
            var a = r1[0]; var b = r1[1]; var c = r1[2];
            MoveOne(a, c);
            MoveOne(b, c);
            r1.RemoveRange(0, 3);
        }

        while (r2.Count >= 3)
        {
            var a = r2[0]; var b = r2[1]; var c = r2[2];
            MoveOne(c, a);
            MoveOne(c, b);
            r2.RemoveRange(0, 3);
        }
    }

    //InitDivisibleBy3
    private void MarkInitialCells()
    {
        foreach (var c in GetComponentsInChildren<Cell>(true))
            if (!c.IsEmpty && c.Item != null && c.Item.InitialCell == null)
                c.Item.InitialCell = c;
    }

    //AI
    private IEnumerable<Cell> EnumerateBoardCells()
    {
        foreach (var c in GetComponentsInChildren<Cell>(true))
        {
            if (m_bottomCellSet != null && m_bottomCellSet.Contains(c)) continue;
            if (c.IsEmpty) continue;
            yield return c;
        }
    }

    //AI
    private void GetBottomAndBoardByType(out Dictionary<NormalItem.eNormalType, int> bottomCnt,
                                         out Dictionary<NormalItem.eNormalType, List<Cell>> onBoardCells)
    {
        bottomCnt = new Dictionary<NormalItem.eNormalType, int>();
        onBoardCells = new Dictionary<NormalItem.eNormalType, List<Cell>>();

        foreach (NormalItem.eNormalType t in Enum.GetValues(typeof(NormalItem.eNormalType)))
        {
            bottomCnt[t] = 0;
            onBoardCells[t] = new List<Cell>();
        }

        foreach (var bc in m_bottomCells)
        {
            if (bc.IsEmpty) continue;
            if (bc.Item is NormalItem bn) bottomCnt[bn.ItemType]++;
        }

        foreach (var c in EnumerateBoardCells())
        {
            if (c.Item is NormalItem n) onBoardCells[n.ItemType].Add(c);
        }
    }

    //AI
    private Cell ChooseCellForWin()
    {
        GetBottomAndBoardByType(out var bottomCnt, out var byTypeCells);
        int occupied = GetBottomOccupiedCount();
        int cap = m_bottomCells.Length;

        foreach (var kv in bottomCnt)
            if (kv.Value == 2 && byTypeCells[kv.Key].Count > 0)
                return byTypeCells[kv.Key][0];

        foreach (var kv in bottomCnt)
            if (kv.Value == 1 && byTypeCells[kv.Key].Count >= 2 && occupied <= cap - 2)
                return byTypeCells[kv.Key][0];

        foreach (var kv in byTypeCells.OrderByDescending(p => p.Value.Count))
            if (bottomCnt[kv.Key] == 0 && kv.Value.Count >= 3 && occupied <= cap - 3)
                return kv.Value[0];

        foreach (int want in new[] { 0, 1, 2 })
            foreach (var t in byTypeCells.Keys)
                if (bottomCnt[t] == want && byTypeCells[t].Count > 0)
                    return byTypeCells[t][0];

        return null;
    }

    //AI
    private Cell ChooseCellForLose()
    {
        GetBottomAndBoardByType(out var bottomCnt, out var byTypeCells);

        foreach (var t in byTypeCells.Keys)
            if (bottomCnt[t] == 0 && byTypeCells[t].Count > 0)
                return byTypeCells[t][0];

        foreach (var t in byTypeCells.Keys)
            if (bottomCnt[t] == 1 && byTypeCells[t].Count > 0)
                return byTypeCells[t][0];

        foreach (var t in byTypeCells.Keys)
            if (byTypeCells[t].Count > 0)
                return byTypeCells[t][0];

        return null;
    }

    //AI
    public void AutoPlayWin(float stepDelay = -1f)
    {
        if (stepDelay <= 0f) stepDelay = m_autoStepDelay;
        StopAutoPlay();
        m_autoCR = StartCoroutine(AutoPlayLoop(true, stepDelay));
    }

    //AI
    public void AutoPlayLose(float stepDelay = -1f)
    {
        if (stepDelay <= 0f) stepDelay = m_autoStepDelay;
        StopAutoPlay();
        m_autoCR = StartCoroutine(AutoPlayLoop(false, stepDelay));
    }

    //AI
    public void StopAutoPlay()
    {
        if (m_autoCR != null) { StopCoroutine(m_autoCR); m_autoCR = null; }
    }

    //AI
    private IEnumerator AutoPlayLoop(bool aimWin, float delay)
    {
        while (!m_gameOver)
        {
            while (IsBusy) { yield return null; if (m_gameOver) yield break; }

            Cell pick = aimWin ? ChooseCellForWin() : ChooseCellForLose();
            if (pick == null) yield break;

            TryCollect(pick);

            while (IsBusy) { yield return null; if (m_gameOver) yield break; }
            yield return new WaitForSeconds(delay);
        }
    }

    //Legacy
    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems(true);
        yield return new WaitForSeconds(0.2f);
        m_board.FillGapsWithNewItems();
        yield return new WaitForSeconds(0.2f);
    }

    //Legacy
    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();
        yield return new WaitForSeconds(0.2f);
        m_board.Fill();
        yield return new WaitForSeconds(0.2f);
    }

    //Legacy
    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();
        yield return new WaitForSeconds(0.3f);
    }

    //Legacy
    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    //Legacy
    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }
}
