using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RollingNumberController : MonoBehaviour
{
    [System.Serializable]
    public class Digit
    {
        public RectTransform mask;     
        public RectTransform rollText;
        public int baseDigit;

        [HideInInspector] public TMP_Text tmp;
        [HideInInspector] public float lineHeight;
    }

    public List<Digit> digits = new List<Digit>();

    public List<TMP_FontAsset> fontAssets;

    [Header("Motion")]
    public float rollSpeed = 25f;
    public bool allowDownward = true;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Monospace Width (optional)")]
    [Tooltip("Override monospace with material property. Leave 0 to rely on <mspace=1em> tags.")]
    public float monoSpacingMaterialOverride = 0f;

    [SerializeField] private float displayedValue = 0f;
    [SerializeField] private float targetValue = 0f;

    [SerializeField] private TMP_Text thousandsPlaceComma;
    [SerializeField] private TMP_Text millionsPlaceComma;


    private float totalHeight = 0f;

    public void CycleNumberFont(int index = -1)
    {
        int finalIndex = index;
        if (finalIndex == -1)
        {
            int currentFontIndex = fontAssets.IndexOf(digits[0].tmp.font);
            finalIndex = (currentFontIndex + 1) % fontAssets.Count;
        }
        else if (finalIndex >= 0)
        {
            finalIndex = finalIndex % fontAssets.Count;        
        }

        for (int i = 0; i < digits.Count; i++)
        {
            digits[i].tmp.font = fontAssets[finalIndex];
        }

        thousandsPlaceComma.font = fontAssets[finalIndex];
        thousandsPlaceComma.ForceMeshUpdate();
        millionsPlaceComma.font = fontAssets[finalIndex];
        millionsPlaceComma.ForceMeshUpdate();

        this.totalHeight = 0f;
        foreach (var d in digits)
        {
            if (d.tmp == null) continue;
            d.tmp.ForceMeshUpdate();
            if (d.tmp.textInfo.lineCount > 0)
                d.lineHeight = d.tmp.textInfo.lineInfo[0].lineHeight;
            else
                d.lineHeight = d.tmp.fontSize; // fallback

            d.mask.sizeDelta = new Vector2(d.mask.sizeDelta.x, d.lineHeight);

            this.totalHeight += d.lineHeight;
        }
    }

    void Awake()
    {
        // Cache TMP refs + measure line height from actual layout
        foreach (var d in digits)
        {
            if (!d.rollText) continue;
            d.tmp = d.rollText.GetComponent<TMP_Text>();

            if (monoSpacingMaterialOverride != 0f && d.tmp != null)
            {
                // Uses TMP material property "Mono Spacing"
                var mat = d.tmp.fontMaterial; // instance
                mat.EnableKeyword("MONOSPACE"); // harmless if missing
                d.tmp.fontMaterial = mat;
            }
        }

        // Force TMP to generate geometry so we can get line heights
        Canvas.ForceUpdateCanvases();

        this.totalHeight = 0f;
        foreach (var d in digits)
        {
            if (d.tmp == null) continue;
            d.tmp.ForceMeshUpdate();
            if (d.tmp.textInfo.lineCount > 0)
                d.lineHeight = d.tmp.textInfo.lineInfo[0].lineHeight;
            else
                d.lineHeight = d.tmp.fontSize; 

            this.totalHeight += d.lineHeight;
        }

        SetInstant(0);
        UpdateColumns(); 
    }

    void Update()
    {
        if (!Mathf.Approximately(displayedValue, targetValue))
        {
            float dir = Mathf.Sign(targetValue - displayedValue);
            if (!allowDownward && dir < 0f) dir = 1f;

            float dist = Mathf.Abs(targetValue - displayedValue);
            float t = Mathf.InverseLerp(0f, Mathf.Max(1f, dist), dist);
            float eased = Mathf.Lerp(0.5f, 1.0f, ease.Evaluate(1f - t));

            displayedValue += dir * rollSpeed * eased * Time.deltaTime;

            if ((dir > 0f && displayedValue > targetValue) ||
                (dir < 0f && displayedValue < targetValue))
                displayedValue = targetValue;

            UpdateColumns();
        }
    }

    void UpdateColumns()
    {
        for (int k = 0; k < digits.Count; k++)
        {
            Digit d = digits[k];
            if (d.tmp == null) continue;

            if(displayedValue >= 1000000)
            {
                thousandsPlaceComma.alpha = 1f;
                millionsPlaceComma.alpha = 1f;
            }
            else if(displayedValue >= 1000)
            {
                thousandsPlaceComma.alpha = 1f;
                millionsPlaceComma.alpha = 0f;
            }
            else
            {
                thousandsPlaceComma.alpha = 0f;
                millionsPlaceComma.alpha = 0f;
            }

            int baseDigit = RollingNumberController.DigitAt(displayedValue, k);

            if(baseDigit == -1)
            {
                d.tmp.gameObject.SetActive(false);

                continue;
            }
            else
            {
                d.tmp.gameObject.SetActive(true);
            }

            d.baseDigit = baseDigit;

            float offsetY = (baseDigit * d.lineHeight) - (d.lineHeight / 2f);

            Vector2 pos = d.rollText.anchoredPosition;
            pos.y = offsetY;
            d.rollText.anchoredPosition = pos;
        }
    }
    public static int DigitAt(float value, int digitIndex)
    {
        int intValue = Mathf.Abs((int)value);

        int maxDigits = intValue == 0 ? 1 : (int)Mathf.Floor(Mathf.Log10(intValue)) + 1;

        if (digitIndex >= maxDigits)
            return -1;

        int shifted = intValue / (int)Mathf.Pow(10, digitIndex);

        return shifted % 10;
    }

    public void SetTarget(int newTarget)
    {
        targetValue = Mathf.Max(0, newTarget);
    }

    public void Add(int delta)
    {
        targetValue = Mathf.Max(0, Mathf.RoundToInt(targetValue) + delta);

        UpdateColumns();
    }

    public void SetInstant(int value)
    {
        displayedValue = targetValue = Mathf.Max(0, value);

        UpdateColumns();
    }
}
