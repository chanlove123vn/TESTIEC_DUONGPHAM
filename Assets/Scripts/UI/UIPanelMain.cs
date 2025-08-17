using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnMoves;
    [SerializeField] private Button btnOtherMode; // Attack Time
    [SerializeField] private Button btnTimer;     // optional (legacy)

    private UIMainManager m_mngr;

    //UI
    private void TryBind()
    {
        if (!btnMoves)     btnMoves     = transform.Find("btnMoves")?.GetComponent<Button>();
        if (!btnOtherMode) btnOtherMode = transform.Find("btnOtherMode")?.GetComponent<Button>();
        if (!btnTimer)     btnTimer     = transform.Find("btnTimer")?.GetComponent<Button>();
    }

    private void Awake()
    {
        TryBind();
        if (btnMoves)     btnMoves.onClick.AddListener(() => m_mngr.LoadLevelMoves());
        if (btnOtherMode) btnOtherMode.onClick.AddListener(() => m_mngr.LoadLevelTimeAttack());
        if (btnTimer)     btnTimer.onClick.AddListener(() => m_mngr.LoadLevelTimer());
    }

    private void OnDestroy()
    {
        if (btnMoves)     btnMoves.onClick.RemoveAllListeners();
        if (btnOtherMode) btnOtherMode.onClick.RemoveAllListeners();
        if (btnTimer)     btnTimer.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr) { m_mngr = mngr; }
    public void Show()  { gameObject.SetActive(true); }
    public void Hide()  { gameObject.SetActive(false); }
}
