using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Audio")]
    private AudioSource audio;

    [Header("Button")]
    [SerializeField]
    private GameObject MainButtons;
    [SerializeField]
    private GameObject StageSelectButtons;
    [SerializeField]
    private GameObject bossStageButton;

    [Header("UI")]
    [SerializeField]
    private GameObject excelThumbnail;
    [SerializeField]
    private GameObject excelThumbnailCover;

    [Header("Data")]
    private int gameProgress = 0;

    void Awake()
    {
        audio = GetComponent<AudioSource>();
        gameProgress = PlayerPrefs.GetInt("GameProgress"); // 1 : RunStage Ŭ����, 2 : BossStage Ŭ����

        if (gameProgress != 2) // ���� ���������� ������ ���
        {
            excelThumbnailCover.SetActive(true);
            excelThumbnail.SetActive(false);
        }
        else // ���� ���������� ������ �ʾ��� ���
        {
            excelThumbnailCover.SetActive(false);
            excelThumbnail.SetActive(true);
        }
    }

    public void GameStart()
    {
        audio.Play();
        MainButtons.SetActive(false);
        StageSelectButtons.SetActive(true);
        bossStageButton.SetActive(gameProgress >= 1 ? true : false);
    }

    public void StageSelect(int stageIndex)
    {
        audio.Play();
        SceneManager.LoadScene(stageIndex);
    }

    public void GameExit()
    {
        audio.Play();
        Application.Quit();
    }

    public void Return()
    {
        audio.Play();
        MainButtons.SetActive(true);
        StageSelectButtons.SetActive(false);
    }
}