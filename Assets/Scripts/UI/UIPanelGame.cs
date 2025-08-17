using UnityEngine;
using UnityEngine.UI;

public class UIPanelGame : MonoBehaviour, IMenu
{
    public Text LevelConditionView;

    [SerializeField] private Button btnPause;
    [SerializeField] private Button btnAIWIN;
    [SerializeField] private Button btnAILOSE;

    private UIMainManager m_mngr;

    //UI
    private void TryBindButtons()
    {
        if (!btnPause)  btnPause  = transform.Find("btnPause")?.GetComponent<Button>();
        if (!btnAIWIN)  btnAIWIN  = transform.Find("btnAIWIN")?.GetComponent<Button>();
        if (!btnAILOSE) btnAILOSE = transform.Find("btnAILOSE")?.GetComponent<Button>();
    }

    //Lifecycle
    private void Awake()
    {
        TryBindButtons();
        if (btnPause)  btnPause.onClick.AddListener(OnClickPause);
        if (btnAIWIN)  btnAIWIN.onClick.AddListener(OnClickAIWin);
        if (btnAILOSE) btnAILOSE.onClick.AddListener(OnClickAILose);
    }

    private void OnEnable()
    {
        // phòng khi panel được instantiate trễ
        TryBindButtons();
    }

    private void OnDestroy()
    {
        if (btnPause)  btnPause.onClick.RemoveListener(OnClickPause);
        if (btnAIWIN)  btnAIWIN.onClick.RemoveListener(OnClickAIWin);
        if (btnAILOSE) btnAILOSE.onClick.RemoveListener(OnClickAILose);
    }

    private void OnClickPause()
    {
        m_mngr.ShowPauseMenu();
    }

    private void OnClickAIWin()
    {
        var bc = FindObjectOfType<BoardController>();
        if (bc) bc.AutoPlayWin(0.5f);
    }

    private void OnClickAILose()
    {
        var bc = FindObjectOfType<BoardController>();
        if (bc) bc.AutoPlayLose(0.5f);
    }

    public void Setup(UIMainManager mngr) { m_mngr = mngr; }
    public void Show()  { gameObject.SetActive(true); }
    public void Hide()  { gameObject.SetActive(false); }
}
