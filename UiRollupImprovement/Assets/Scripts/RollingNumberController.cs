using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RollingNumberController : MonoBehaviour
{
    [System.Serializable]
    public class Digit
    {
        public RectTransform mask;     // Rect with RectMask2D (the window)
        public RectTransform rollText; // The TextMeshProUGUI's RectTransform inside the mask
        [HideInInspector] public TMP_Text tmp;
        [HideInInspector] public float lineHeight;
        [HideInInspector] public int baseDigit; // what the column is currently showing (0-9)
        [HideInInspector] public float scroll;  // 0..1 toward next (up-roll)
    }

    [Tooltip("Least-significant digit first (ones at index 0).")]
    public List<Digit> digits = new List<Digit>();

    public List<TMP_FontAsset> fontAssets; // for swapping fonts if needed

    [Header("Motion")]
    public float rollSpeed = 25f;              // units per second the overall numeric value moves
    public bool allowDownward = true;          // if false, force upward only
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Monospace Width (optional)")]
    [Tooltip("Override monospace with material property. Leave 0 to rely on <mspace=1em> tags.")]
    public float monoSpacingMaterialOverride = 0f;

    [SerializeField] private float displayedValue = 0f;
    [SerializeField] private float targetValue = 0f;

    [SerializeField] private GameObject tensPlaceParent;
    [SerializeField] private GameObject thousandsPlaceParent;
    [SerializeField] private GameObject millionsPlaceParent;

    [SerializeField] private TMP_Text thousandsPlaceComma;
    [SerializeField] private TMP_Text millionsPlaceComma;


    private float totalHeight = 0f;

    public void CycleNumberFont(int index = -1)
    {
        for(int i = 0; i < digits.Count; i++)
        {
            if(digits[i].tmp == null) continue;
            if(index == -1)
            {
                int currentFontIndex = fontAssets.IndexOf(digits[i].tmp.font);
                int nextFontIndex = (currentFontIndex + 1) % fontAssets.Count;
                digits[i].tmp.font = fontAssets[nextFontIndex];
            }
            else
            {
                if(index >= 0 && index < fontAssets.Count)
                {
                    digits[i].tmp.font = fontAssets[index];
                }
                else if (index >= 0)
                {
                    int cycledIndex = index % fontAssets.Count;
                    digits[i].tmp.font = fontAssets[cycledIndex];
                }
            }
        }

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
                //mat.SetFloat(ShaderUtilities.ID_mo, monoSpacingMaterialOverride);
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
                d.lineHeight = d.tmp.fontSize; // fallback

            this.totalHeight += d.lineHeight;
        }

        SetInstant(0);
        UpdateColumns(); // position texts correctly
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
            //0-8 in this case, settings digits in pattern: XXX XXX XXX
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

            // For downward motion, flip the sense
            if (allowDownward && targetValue < displayedValue)
            {
                baseDigit = (baseDigit + 9) % 10; // show previous digit as base
            }

            d.baseDigit = baseDigit;

            float offsetY = baseDigit * d.lineHeight;

            Vector2 pos = d.rollText.anchoredPosition;
            //TODO: get all line heights of all digits if you want to properly support different fonts
            pos.y = offsetY;
            d.rollText.anchoredPosition = pos;
        }
    }
    public static int DigitAt(float value, int digitIndex)
    {
        // Work with absolute integer part of value
        int intValue = Mathf.Abs((int)value);

        // Example: value = 100, digitIndex = 4 → too high
        int maxDigits = intValue == 0 ? 1 : (int)Mathf.Floor(Mathf.Log10(intValue)) + 1;
        if (digitIndex >= maxDigits)
            return -1;

        // Strip away lower digits
        int shifted = intValue / (int)Mathf.Pow(10, digitIndex);

        // Isolate the requested digit
        return shifted % 10;
    }

    // --- Public API ---
    public void SetTarget(int newTarget)
    {
        targetValue = Mathf.Max(0, newTarget);
    }

    public void Add(int delta)
    {
        targetValue = Mathf.Max(0, Mathf.RoundToInt(targetValue) + delta);
    }

    public void SetInstant(int value)
    {
        displayedValue = targetValue = Mathf.Max(0, value);
    }
}
