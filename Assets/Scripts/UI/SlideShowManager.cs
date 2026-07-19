using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SlideShowManager : MonoBehaviour
{
    [Header("Content Viewport")]
    public Image contentDisplay;
    public List<GameObject> contentPanels;

    [Header("Navigation Dots")]
    public GameObject dotsContainer;
    public GameObject dotPrefab;

    [Header("Pagination Buttons")]
    public Button nextButton;
    public Button prevButton;

    [Header("Page Settings")]
    public bool useTimer = false;
    public bool isLimitedSwipe = false;
    public float autoMoveTime = 5f;
    private float timer;
    public int currentIndex = 0;
    public float swipeThreshold = 50f;
    private Vector2 touchStartPos;

    // Reference to the RectTransform of the content area
    public RectTransform contentArea;

    private readonly List<Image> dotImages = new List<Image>();
    private bool dotsInitialized;

    void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextContent);
        }

        if (prevButton != null)
        {
            prevButton.onClick.AddListener(PreviousContent);
        }
    }

    void OnEnable()
    {
        currentIndex = 0;
        timer = autoMoveTime;

        InitializeDots();
        ShowContent();

        CancelInvoke(nameof(AutoMoveContent));
        if (useTimer)
        {
            InvokeRepeating(nameof(AutoMoveContent), 1f, 1f);
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(AutoMoveContent));
    }

    void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextContent);
        }

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(PreviousContent);
        }
    }

    void InitializeDots()
    {
        dotImages.Clear();

        if (dotsContainer == null)
        {
            return;
        }

        if (!dotsInitialized && dotPrefab != null && dotsContainer.transform.childCount == 0)
        {
            int panelCount = GetPanelCount();
            for (int i = 0; i < panelCount; i++)
            {
                Instantiate(dotPrefab, dotsContainer.transform);
            }
        }

        for (int i = 0; i < dotsContainer.transform.childCount; i++)
        {
            Image dotImage = dotsContainer.transform.GetChild(i).GetComponent<Image>();
            if (dotImage != null)
            {
                dotImages.Add(dotImage);
            }
        }

        dotsInitialized = true;
    }

    void UpdateDots()
    {
        for (int i = 0; i < dotImages.Count; i++)
        {
            Image dotImage = dotImages[i];
            if (dotImage == null)
            {
                continue;
            }

            bool isActive = i == currentIndex;
            dotImage.color = isActive ? Color.white : Color.gray;
            dotImage.fillAmount = isActive ? 1f : 0f;
        }
    }

    void Update()
    {
        DetectSwipe();
    }

    void DetectSwipe()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            touchStartPos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 touchEndPos = Mouse.current.position.ReadValue();
            float swipeDistance = touchEndPos.x - touchStartPos.x;
            RectTransform swipeArea = contentArea != null ? contentArea : transform as RectTransform;

            if (swipeArea != null && Mathf.Abs(swipeDistance) > swipeThreshold && IsTouchInContentArea(swipeArea, touchStartPos))
            {
                int panelCount = GetPanelCount();
                if (isLimitedSwipe && ((currentIndex == 0 && swipeDistance > 0) || (currentIndex == panelCount - 1 && swipeDistance < 0)))
                {
                    return;
                }

                if (swipeDistance > 0)
                {
                    PreviousContent();
                }
                else
                {
                    NextContent();
                }
            }
        }
    }

    // Check if the touch position is within the content area bounds
    bool IsTouchInContentArea(RectTransform swipeArea, Vector2 touchPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(swipeArea, touchPosition);
    }

    void AutoMoveContent()
    {
        if (GetPanelCount() <= 1)
        {
            return;
        }

        timer -= 1f; // Decrease timer every second

        if (timer <= 0)
        {
            timer = autoMoveTime;
            NextContent();
        }

        UpdateDots(); // Update dots on every timer tick
    }

    void NextContent()
    {
        int panelCount = GetPanelCount();
        if (panelCount == 0)
        {
            return;
        }

        if (currentIndex < panelCount - 1)
        {
            currentIndex++;
            ShowContent();
            UpdateDots();
        }
    }

    void PreviousContent()
    {
        int panelCount = GetPanelCount();
        if (panelCount == 0)
        {
            return;
        }

        if (currentIndex > 0)
        {
            currentIndex--;
            ShowContent();
            UpdateDots();
        }
    }

    void ShowContent()
    {
        int panelCount = GetPanelCount();
        if (panelCount == 0)
        {
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, panelCount - 1);
        timer = autoMoveTime;

        for (int i = 0; i < panelCount; i++)
        {
            bool isActive = i == currentIndex;
            if (contentPanels[i] != null)
            {
                contentPanels[i].SetActive(isActive);
            }
        }
        if (prevButton != null)
            prevButton.gameObject.SetActive(currentIndex > 0);
            
        if (nextButton != null)
            nextButton.gameObject.SetActive(currentIndex < panelCount - 1);

        UpdateDots();
    }

    public void SetCurrentIndex(int newIndex)
    {
        int panelCount = GetPanelCount();
        if (newIndex >= 0 && newIndex < panelCount)
        {
            currentIndex = newIndex;
            ShowContent();
            UpdateDots();
        }
    }

    int GetPanelCount()
    {
        return contentPanels != null ? contentPanels.Count : 0;
    }
}
