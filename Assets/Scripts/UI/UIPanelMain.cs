using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnTimer;
    [SerializeField] private Button btnMoves;
    [SerializeField] private Button btnAutoplay; // Add autoplay button
    [SerializeField] private Button btnAutoLose; // Add autolose button

    private UIMainManager m_mngr;

    private void Awake()
    {
        if (btnMoves != null)
        {
            btnMoves.onClick.AddListener(OnClickMoves);
        }
        if (btnTimer != null)
        {
            btnTimer.onClick.AddListener(OnClickTimer);
        }
        if (btnAutoplay != null) // Set up listener for autoplay button
        {
            btnAutoplay.onClick.AddListener(OnClickAutoplay);
        }
        if (btnAutoLose != null) // Set up listener for autolose button
        {
            btnAutoLose.onClick.AddListener(OnClickAutoLose);
        }
    }

    private void OnDestroy()
    {
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        if (btnTimer) btnTimer.onClick.RemoveAllListeners();
        if (btnAutoplay) btnAutoplay.onClick.RemoveAllListeners(); // Remove listener for autoplay button
        if (btnAutoLose) btnAutoLose.onClick.RemoveAllListeners(); // Remove listener for autolose button
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickTimer()
    {
        m_mngr.LoadLevelTimer();
    }

    private void OnClickMoves()
    {
        m_mngr.LoadLevelMoves();
    }

    private void OnClickAutoplay() // Method for autoplay button
    {
        m_mngr.LoadLevelMoves();
        m_mngr.StartAutoplay();
    }

    private void OnClickAutoLose() // Method for autolose button
    {
        m_mngr.LoadLevelMoves();
        m_mngr.StartAutoLose();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}
