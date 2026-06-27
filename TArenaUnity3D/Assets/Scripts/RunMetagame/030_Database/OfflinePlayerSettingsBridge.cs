using UnityEngine;

public class OfflinePlayerSettingsBridge : MonoBehaviour
{
    [SerializeField] private MouseControler mouseControler;
    [SerializeField] private bool loadFromDatabaseOnAwake = true;
    [SerializeField] private bool applySmartCastToMouseController = true;
    [SerializeField] private bool smartCastEnabled;
    [SerializeField] private float animationSpeedMultiplier = OfflineDatabaseAccountBootstrap.DefaultAnimationSpeedPreferenceValue;

    public bool SmartCastEnabled
    {
        get { return smartCastEnabled; }
    }

    public float AnimationSpeedMultiplier
    {
        get { return animationSpeedMultiplier; }
    }

    private void Awake()
    {
        if (loadFromDatabaseOnAwake)
        {
            LoadFromDatabase();
        }
        else
        {
            ApplyRuntimeSettings();
        }
    }

    public void LoadFromDatabase()
    {
        smartCastEnabled = OfflinePlayerPreferences.IsSmartCastEnabled();
        animationSpeedMultiplier = OfflinePlayerPreferences.GetAnimationSpeedMultiplier();
        ApplyRuntimeSettings();
    }

    public void SaveToDatabase()
    {
        OfflinePlayerPreferences.SetSmartCastEnabled(smartCastEnabled);
        OfflinePlayerPreferences.SetAnimationSpeedMultiplier(animationSpeedMultiplier);
    }

    public void Save()
    {
        SaveToDatabase();
    }

    public void SetSmartCastEnabled(bool enabled)
    {
        smartCastEnabled = enabled;
        ApplyRuntimeSettings();
    }

    public void SetAnimationSpeedMultiplier(float multiplier)
    {
        animationSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ApplyRuntimeSettings()
    {
        if (!applySmartCastToMouseController)
        {
            return;
        }

        if (mouseControler == null)
        {
            mouseControler = FindObjectOfType<MouseControler>();
        }

        if (mouseControler != null)
        {
            mouseControler.SetSmartCastEnabled(smartCastEnabled);
        }
    }
}
