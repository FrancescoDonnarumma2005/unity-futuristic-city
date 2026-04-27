using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a science-themed multiple choice quiz presented inside a world-space canvas.
/// Works with standard Unity UI interactions so both desktop (mouse/keyboard) and VR pointers can answer.
/// </summary>
public class WorldQuizController : MonoBehaviour
{
    [Header("World Space Canvas")]
    [SerializeField] private Canvas quizCanvas;
    [SerializeField] private Camera worldSpaceCamera;

    [Header("Mode Awareness")]
    [SerializeField] private GameplayModeManager gameplayModeManager;
    [SerializeField] private Camera desktopQuizCamera;
    [SerializeField] private Camera vrQuizCamera;

    [Header("UI References")]
    [SerializeField] private Text topicLabel;
    [SerializeField] private Text questionLabel;
    [SerializeField] private Text progressLabel;
    [SerializeField] private Transform answersContainer;
    [SerializeField] private Button answerButtonPrefab;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultLabel;

    [Header("Answer Colors")]
    [SerializeField] private Color normalAnswerColor = Color.white;
    [SerializeField] private Color correctAnswerColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color incorrectAnswerColor = new Color(0.9f, 0.2f, 0.2f);

    private readonly List<QuizQuestion> questionBank = new List<QuizQuestion>();
    private readonly List<Button> activeButtons = new List<Button>();
    private readonly Dictionary<Button, Outline> buttonOutlineCache = new Dictionary<Button, Outline>();
    private readonly List<QuestionResult> answeredQuestions = new List<QuestionResult>();

    private int currentQuestionIndex = -1;
    private int correctAnswersCount;
    private bool quizCompleted;
    private bool waitingForNextQuestion;
    private Coroutine nextQuestionRoutine;

    private static readonly KeyCode[] DesktopNumberKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4
    };

    private static readonly KeyCode[] DesktopKeypadKeys =
    {
        KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4
    };

    private bool subscribedToModeChanges;

    private void Awake()
    {
        if (quizCanvas == null)
        {
            quizCanvas = GetComponentInChildren<Canvas>();
        }

        if (quizCanvas != null)
        {
            quizCanvas.renderMode = RenderMode.WorldSpace;
        }

        SetupModeAwareness();
        BuildQuestionBank();
    }

    private void OnEnable()
    {
        SetupModeAwareness();
    }

    private void OnDisable()
    {
        UnregisterModeAwareness();
    }

    private void OnDestroy()
    {
        UnregisterModeAwareness();
    }

    private void SetupModeAwareness()
    {
        if (gameplayModeManager == null)
        {
            gameplayModeManager = FindObjectOfType<GameplayModeManager>();
        }

        if (gameplayModeManager != null)
        {
            if (!subscribedToModeChanges)
            {
                gameplayModeManager.ModeChanged += HandleModeChanged;
                subscribedToModeChanges = true;
            }

            HandleModeChanged(gameplayModeManager.IsInVR);
        }
        else
        {
            ApplyCanvasCamera(worldSpaceCamera);
        }
    }

    private void UnregisterModeAwareness()
    {
        if (subscribedToModeChanges && gameplayModeManager != null)
        {
            gameplayModeManager.ModeChanged -= HandleModeChanged;
            subscribedToModeChanges = false;
        }
    }

    private void HandleModeChanged(bool useVR)
    {
        Camera targetCamera = useVR ? vrQuizCamera : desktopQuizCamera;
        if (targetCamera == null)
        {
            targetCamera = worldSpaceCamera;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        ApplyCanvasCamera(targetCamera);
    }

    private void ApplyCanvasCamera(Camera targetCamera)
    {
        if (quizCanvas == null || targetCamera == null)
        {
            return;
        }

        quizCanvas.worldCamera = targetCamera;
    }

    private void Start()
    {
        ResetQuiz();
    }

    private void Update()
    {
        if (quizCompleted || activeButtons.Count == 0)
        {
            return;
        }

        for (int i = 0; i < activeButtons.Count && i < DesktopNumberKeys.Length; i++)
        {
            if (Input.GetKeyDown(DesktopNumberKeys[i]) || Input.GetKeyDown(DesktopKeypadKeys[i]))
            {
                HandleAnswer(i);
                break;
            }
        }
    }

    private void ResetQuiz()
    {
        currentQuestionIndex = -1;
        correctAnswersCount = 0;
        quizCompleted = false;
        waitingForNextQuestion = false;
        if (nextQuestionRoutine != null)
        {
            StopCoroutine(nextQuestionRoutine);
            nextQuestionRoutine = null;
        }

        answeredQuestions.Clear();
        ShowNextQuestion();
    }

    private void BuildQuestionBank()
    {
        if (questionBank.Count > 0)
        {
            return;
        }

        // Physics
        questionBank.Add(new QuizQuestion(
            "Fisica",
            "Secondo la seconda legge di Newton, cosa determina l'accelerazione di un oggetto?",
            new[]
            {
                "La risultante delle forze divisa per la massa",
                "La somma delle velocità precedenti",
                "La differenza tra massa e peso",
                "La quantità di energia potenziale accumulata"
            },
            0));

        questionBank.Add(new QuizQuestion(
            "Fisica",
            "Qual è l'unità di misura del campo elettrico nel SI?",
            new[]
            {
                "Volt per metro (V/m)",
                "Newton per metro (N/m)",
                "Tesla (T)",
                "Coulomb (C)"
            },
            0));

        questionBank.Add(new QuizQuestion(
            "Fisica",
            "Quale particella subatomica porta carica elettrica negativa?",
            new[]
            {
                "L'elettrone",
                "Il protone",
                "Il neutrone",
                "Il positrone"
            },
            0));

        // Chemistry
        questionBank.Add(new QuizQuestion(
            "Chimica",
            "Qual è il pH di una soluzione neutra a 25°C?",
            new[]
            {
                "7",
                "1",
                "14",
                "4"
            },
            0));

        questionBank.Add(new QuizQuestion(
            "Chimica",
            "Come si chiama il legame che tiene uniti gli atomi della molecola d'acqua?",
            new[]
            {
                "Legame covalente polare",
                "Legame ionico",
                "Legame metallico",
                "Legame a idrogeno tra ossigeno e ossigeno"
            },
            0));

        questionBank.Add(new QuizQuestion(
            "Chimica",
            "Che cosa rappresenta il numero di Avogadro?",
            new[]
            {
                "Il numero di particelle contenute in una mole",
                "La massa di una mole di carbonio",
                "Il rapporto tra protoni e neutroni",
                "La costante di equilibrio di una reazione"
            },
            0));

        // Biology
        questionBank.Add(new QuizQuestion(
            "Biologia",
            "Quale organello cellulare è responsabile della produzione di ATP tramite respirazione cellulare?",
            new[]
            {
                "Il mitocondrio",
                "Il reticolo endoplasmatico ruvido",
                "L'apparato di Golgi",
                "Il cloroplasto"
            },
            0));

        questionBank.Add(new QuizQuestion(
            "Biologia",
            "In quale fase della mitosi i cromatidi fratelli si separano e migrano ai poli opposti?",
            new[]
            {
                "Anafase",
                "Profase",
                "Metafase",
                "Telofase"
            },
            0));

        questionBank.Add(new QuizQuestion(
            "Biologia",
            "Quale molecola trasporta l'ossigeno nel sangue umano?",
            new[]
            {
                "L'emoglobina",
                "Il glucosio",
                "Il DNA",
                "Il colesterolo"
            },
            0));
    }

    private void ShowNextQuestion()
    {
        currentQuestionIndex++;
        if (currentQuestionIndex >= questionBank.Count)
        {
            CompleteQuiz();
            return;
        }

        RenderQuestion(questionBank[currentQuestionIndex]);
    }

    private void RenderQuestion(QuizQuestion question)
    {
        quizCompleted = false;
        waitingForNextQuestion = false;
        if (topicLabel != null)
        {
            topicLabel.text = question.Topic;
        }

        if (questionLabel != null)
        {
            questionLabel.text = question.Question;
        }

        if (progressLabel != null)
        {
            progressLabel.text = $"Domanda {currentQuestionIndex + 1}/{questionBank.Count}";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        ClearAnswerButtons();
        if (answersContainer == null || answerButtonPrefab == null)
        {
            Debug.LogWarning("WorldQuizController: reference to answers container or prefab is missing.");
            return;
        }

        for (int i = 0; i < question.Answers.Length; i++)
        {
            Button buttonInstance = Instantiate(answerButtonPrefab, answersContainer);
            buttonInstance.interactable = true;
            var label = buttonInstance.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{i + 1}. {question.Answers[i]}";
            }

            int capturedIndex = i;
            buttonInstance.onClick.AddListener(() => HandleAnswer(capturedIndex));
            SetButtonOutlineColor(buttonInstance, normalAnswerColor);
            activeButtons.Add(buttonInstance);
        }
    }

    private void HandleAnswer(int answerIndex)
    {
        if (quizCompleted || waitingForNextQuestion || currentQuestionIndex < 0 || currentQuestionIndex >= questionBank.Count)
        {
            return;
        }

        QuizQuestion currentQuestion = questionBank[currentQuestionIndex];
        bool isCorrect = answerIndex == currentQuestion.CorrectAnswerIndex;

        if (isCorrect)
        {
            correctAnswersCount++;
        }

        answeredQuestions.Add(new QuestionResult
        {
            topic = currentQuestion.Topic,
            question = currentQuestion.Question,
            chosenAnswer = answerIndex >= 0 && answerIndex < currentQuestion.Answers.Length
                ? currentQuestion.Answers[answerIndex]
                : string.Empty,
            correctAnswer = currentQuestion.Answers[currentQuestion.CorrectAnswerIndex],
            isCorrect = isCorrect
        });

        HighlightAnswers(answerIndex, currentQuestion.CorrectAnswerIndex);

        waitingForNextQuestion = true;
        if (nextQuestionRoutine != null)
        {
            StopCoroutine(nextQuestionRoutine);
        }

        nextQuestionRoutine = StartCoroutine(AdvanceAfterDelay());
    }

    private void CompleteQuiz()
    {
        quizCompleted = true;
        waitingForNextQuestion = false;
        if (nextQuestionRoutine != null)
        {
            StopCoroutine(nextQuestionRoutine);
            nextQuestionRoutine = null;
        }
        ClearAnswerButtons();

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        if (resultLabel != null)
        {
            resultLabel.text = $"Hai risposto correttamente a {correctAnswersCount} su {questionBank.Count} domande.";
        }

        if (progressLabel != null)
        {
            progressLabel.text = "Quiz completato!";
        }
        SendResultsToJavascript();
    }

    private void ClearAnswerButtons()
    {
        foreach (Button button in activeButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                buttonOutlineCache.Remove(button);
                Destroy(button.gameObject);
            }
        }

        activeButtons.Clear();
    }

    private void HighlightAnswers(int chosenIndex, int correctIndex)
    {
        for (int i = 0; i < activeButtons.Count; i++)
        {
            var button = activeButtons[i];
            if (button == null)
            {
                continue;
            }

            button.interactable = false;

            if (i == correctIndex)
            {
                SetButtonOutlineColor(button, correctAnswerColor);
            }
            else if (i == chosenIndex && chosenIndex != correctIndex)
            {
                SetButtonOutlineColor(button, incorrectAnswerColor);
            }
            else
            {
                SetButtonOutlineColor(button, normalAnswerColor);
            }
        }
    }

    private IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        waitingForNextQuestion = false;
        nextQuestionRoutine = null;
        ShowNextQuestion();
    }

    private void SetButtonOutlineColor(Button button, Color color)
    {
        if (button == null)
        {
            return;
        }

        Outline outline = GetButtonOutline(button);
        if (outline != null)
        {
            outline.effectColor = color;
        }
    }

    private Outline GetButtonOutline(Button button)
    {
        if (button == null)
        {
            return null;
        }

        if (buttonOutlineCache.TryGetValue(button, out var cachedOutline) && cachedOutline != null)
        {
            return cachedOutline;
        }

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.GetComponentInChildren<Outline>();
        }

        if (outline != null)
        {
            buttonOutlineCache[button] = outline;
        }

        return outline;
    }

    private void SendResultsToJavascript()
    {
        var payload = new QuizBreakdownPayload
        {
            questions = answeredQuestions.ToArray()
        };

        string breakdownJson = JsonUtility.ToJson(payload);

#if UNITY_WEBGL && !UNITY_EDITOR
        EdugateReceiveQuizResults(correctAnswersCount, questionBank.Count, breakdownJson);
#else
        Debug.Log($"[WorldQuiz] Results {correctAnswersCount}/{questionBank.Count}: {breakdownJson}");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void EdugateReceiveQuizResults(int score, int totalQuestions, string breakdownJson);
#endif

    [Serializable]
    private class QuizQuestion
    {
        public string Topic;
        public string Question;
        public string[] Answers;
        public int CorrectAnswerIndex;

        public QuizQuestion(string topic, string question, string[] answers, int correctAnswerIndex)
        {
            Topic = topic;
            Question = question;
            Answers = answers;
            CorrectAnswerIndex = Mathf.Clamp(correctAnswerIndex, 0, answers.Length - 1);
        }
    }

    [Serializable]
    private struct QuestionResult
    {
        public string topic;
        public string question;
        public string chosenAnswer;
        public string correctAnswer;
        public bool isCorrect;
    }

    [Serializable]
    private class QuizBreakdownPayload
    {
        public QuestionResult[] questions;
    }
}
