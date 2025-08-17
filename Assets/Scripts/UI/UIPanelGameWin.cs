using UnityEngine;
using UnityEngine.UI;

public class UIPanelGameWin : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnClose;   // Back to main menu

    private UIMainManager m_mngr;

    private void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnDestroy()
    {
        if (btnClose) btnClose.onClick.RemoveAllListeners();
    }

    private void OnClickClose()
    {
        m_mngr.ShowMainMenu();
    }


    public void Hide()  { gameObject.SetActive(false); }
    public void Show()  { gameObject.SetActive(true); }
    public void Setup(UIMainManager mngr) { m_mngr = mngr; }
}
