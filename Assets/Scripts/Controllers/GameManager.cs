using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode { TIMER, MOVES }

    //States
    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        GAME_WIN
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set { m_state = value; StateChangedAction(m_state); }
    }

    private GameSettings m_gameSettings;
    private BoardController m_boardController;
    private UIMainManager m_uiMenu;
    private LevelCondition m_levelCondition;

    //Lifecycle
    private void Awake()
    {
        State = eStateGame.SETUP;
        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);
        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    private void Start() { State = eStateGame.MAIN_MENU; }

    private void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }

    //State
    internal void SetState(eStateGame state)
    {
        State = state;
        if (State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
            SoundManager.Instance.PauseBGM(true);
        }
        else
        {
            SoundManager.Instance.PauseBGM(false);
            DOTween.PlayAll();
        }
    }

    //Load Classic / Timer (giữ nguyên API cũ)
    public void LoadLevel(eLevelMode mode)
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);

        if (mode == eLevelMode.MOVES)
        {
            m_levelCondition = gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_levelCondition = gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), this);
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;
        State = eStateGame.GAME_STARTED;
        SoundManager.Instance.PlayBGM(); 
    }

    //Load TimeAttack (60s)
    public void LoadLevelTimeAttack()
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);
        m_boardController.EnableTimeAttack(true);

        m_levelCondition = gameObject.AddComponent<LevelTime>();
        m_levelCondition.Setup(60f, m_uiMenu.GetLevelConditionView(), this);
        m_levelCondition.ConditionCompleteEvent += GameOver;

        State = eStateGame.GAME_STARTED;
        SoundManager.Instance.PlayBGM(); 
    }

    //Results
    public void GameOver() { StartCoroutine(WaitThenSetState(eStateGame.GAME_OVER)); }
    public void GameWin()  { StartCoroutine(WaitThenSetState(eStateGame.GAME_WIN)); }

    //Utils
    private IEnumerator WaitThenSetState(eStateGame target)
    {
        while (m_boardController != null && m_boardController.IsBusy)
            yield return new WaitForEndOfFrame();

        yield return new WaitForSeconds(1f);

        State = target;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;
            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }
}
