using UnityEngine;
using UnityEngine.UI;

namespace ColorBlockJam.UI
{
    /// <summary>Owns navigation between the home screen and its settings overlay.</summary>
    [DisallowMultipleComponent]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            Close();
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }

        public void Configure(GameObject targetPanel, Button targetOpenButton, Button targetCloseButton)
        {
            panel = targetPanel;
            openButton = targetOpenButton;
            closeButton = targetCloseButton;
        }

        public void Open()
        {
            if (panel != null) panel.SetActive(true);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
