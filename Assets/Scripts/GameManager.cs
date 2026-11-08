using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    private const string BestTimeKey = "HighScore_BestTime";
    private const string BestScoreKey = "HighScore_BestScore";

    [Header("Gameplay References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("HUD UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [Header("Endgame UI Panel")]
    [SerializeField] private GameObject endgamePanel;
    [SerializeField] private TextMeshProUGUI endgameTitleText;
    [SerializeField] private TextMeshProUGUI finalStatsText;

    [Header("Countdown Settings")]
    private float elapsedTime;
    private int currentScore;
    private bool isGameActive;
    private bool isGameOver;

    void OnEnable()
    {
        Goal.OnGoalReached += HandleGoalReached;
        Hazard.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        Goal.OnGoalReached -= HandleGoalReached;
        Hazard.OnPlayerDied -= HandlePlayerDied;
    }

    void Start()
    {
        Time.timeScale = 1.0f;

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        }

        if (endgamePanel != null)
        {
            endgamePanel.SetActive(false);
        }

        UpdateHudUI();
    }

    void Update()
    {
        if (!isGameActive || isGameOver)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateHudUI();
    }

    
    private void HandleGoalReached(Goal goal)
    {
        if (isGameOver)
        {
            return;
        }

        currentScore += goal.ScoreValue;
        UpdateHudUI();

        if (goal.IsFinalGoal)
        {
            TriggerEndgame(true);
        }
    }

    private void HandlePlayerDied()
    {
        if (isGameOver)
        {
            return;
        }

        TriggerEndgame(false);
    }

    private void TriggerEndgame(bool isVictory)
    {
        isGameOver = true;
        isGameActive = false;
        SetPlayerControl(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float bestTime = PlayerPrefs.GetFloat(BestTimeKey, float.MaxValue);
        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        if (isVictory)
        {
            if (elapsedTime < bestTime)
            {
                bestTime = elapsedTime;
                PlayerPrefs.SetFloat(BestTimeKey, bestTime);
            }

            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
            }

            PlayerPrefs.Save();
        }

        if (endgamePanel != null)
        {
            endgamePanel.SetActive(true);
        }

        if (endgameTitleText != null)
        {
            endgameTitleText.text = isVictory ? "VICTORY!" : "GAME OVER";
            endgameTitleText.color = isVictory ? Color.yellow : Color.red;
        }

        if (finalStatsText != null)
        {
            string bestTimeFormatted = (bestTime < float.MaxValue) ? FormatTime(bestTime) : "--:--.--";
            finalStatsText.text =
                $"Time: {FormatTime(elapsedTime)}\n" +
                $"Score: {currentScore}\n\n" +
                $"Best Time: {bestTimeFormatted}\n" +
                $"Best Score: {bestScore}";
        }
    }
    public void ResetGame()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void SetPlayerControl(bool canControl)
    {
        if (playerMovement != null)
        {
            playerMovement.CanMove = canControl;
        }

        if (cameraFollow != null)
        {
            cameraFollow.CanControlCamera = canControl;
        }
    }

    private void UpdateHudUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {FormatTime(elapsedTime)}";
        }

        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60.0f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60.0f);
        int hundredths = Mathf.FloorToInt((timeInSeconds * 100.0f) % 100.0f);
        return $"{minutes:00}:{seconds:00}.{hundredths:00}";
    }
}