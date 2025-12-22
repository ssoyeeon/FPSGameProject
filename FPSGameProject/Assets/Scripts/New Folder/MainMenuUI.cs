using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button startGameButton;

    private void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(() => {
                SceneManager.Instance.LoadScene("TestScene");
            });
        }
    }
}