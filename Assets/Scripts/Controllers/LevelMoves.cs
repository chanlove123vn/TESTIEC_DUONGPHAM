using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelMoves : LevelCondition
{
    private int m_movesCount;

    private BoardController m_board;

    public override void Setup(float value, Text txt, BoardController board)
    {
        base.Setup(value, txt);

        m_movesCount = 0;
        m_board = board;

        m_board.OnMoveEvent += OnMove;

        UpdateText();
    }

    private void OnMove()
    {
        if (m_conditionCompleted) return;

        m_movesCount++;      
        UpdateText();

    }

    protected override void UpdateText()
    {
        if (m_txt != null)
            m_txt.text = $"MOVES:\n{m_movesCount}";
    }

    protected override void OnDestroy()
    {
        if (m_board != null) m_board.OnMoveEvent -= OnMove;
        base.OnDestroy();
    }
}
