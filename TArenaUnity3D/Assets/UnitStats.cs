using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStats : MonoBehaviour
{
    [Header("Unit")]
    public GameObject CurrentUnit;
    public bool UpdateEveryFrame = true;

    [Header("Stats Texts")]
    public TMP_Text Name;
    public TMP_Text MaxHP;
    public TMP_Text CurrentHP;
    public TMP_Text HPBar_Text;
    public GameObject HPBar;
    public TMP_Text Attack;
    public TMP_Text Defence;
    public TMP_Text Damage;
    public TMP_Text Movement;
    public TMP_Text Initiative;
    public TMP_Text Health;
    public TMP_Text Count;

    TosterHexUnit currentToster;

    void OnEnable()
    {
        ShowCurrentUnitStats();
    }

    void Update()
    {
        if (UpdateEveryFrame)
        {
            ShowCurrentUnitStats();
        }
    }

    public void This()
    {
        ShowCurrentUnitStats();
    }

    public void ShowCurrentUnitStats()
    {
        TosterHexUnit toster = ResolveCurrentToster();
        if (toster == null)
        {
            return;
        }

        Show(toster);
    }

    public void Show(GameObject unit)
    {
        CurrentUnit = unit;
        currentToster = ResolveCurrentToster();
        Show(currentToster);
    }

    public void Show(TosterHexUnit toster)
    {
        if (toster == null)
        {
            return;
        }

        currentToster = toster;
        DisplayInfo(new UnitStatsData(
            toster.GetAtt(),
            toster.GetDef(),
            toster.GetMinDmg(),
            toster.GetMaxDMG(),
            Mathf.Max(0, toster.GetMS() - 1),
            toster.GetIni(),
            toster.TempHP,
            toster.GetHP()));
        SetTextIfAvailable(Name, toster.Name);
        SetTextIfAvailable(Count, toster.Amount.ToString());
    }

    public void DisplayInfo(UnitStatsData stats)
    {
        if (stats == null)
        {
            return;
        }

        SetTextIfAvailable(MaxHP, stats.MaxHealth.ToString());
        SetTextIfAvailable(CurrentHP, stats.CurrentHealth.ToString());
        SetTextIfAvailable(Health, stats.CurrentHealth.ToString() + " / " + stats.MaxHealth.ToString());
        SetTextIfAvailable(HPBar_Text, stats.CurrentHealth.ToString() + " / " + stats.MaxHealth.ToString());
        SetHPBarFill(HPBar, stats.CurrentHealth, stats.MaxHealth);
        SetTextIfAvailable(Attack, stats.Attack.ToString());
        SetTextIfAvailable(Defence, stats.Defence.ToString());
        SetTextIfAvailable(Damage, stats.Damage.ToString());
        SetTextIfAvailable(Movement, stats.Movement.ToString());
        SetTextIfAvailable(Initiative, stats.Initiative.ToString());
    }

    TosterHexUnit ResolveCurrentToster()
    {
        if (currentToster != null && IsCurrentUnitView(currentToster.tosterView))
        {
            return currentToster;
        }

        TosterView view = CurrentUnit == null ? null : CurrentUnit.GetComponentInParent<TosterView>();
        if (view == null && CurrentUnit != null)
        {
            view = CurrentUnit.GetComponentInChildren<TosterView>();
        }

        if (view == null)
        {
            return CurrentUnit == null ? currentToster : null;
        }

        HexMap[] maps = FindObjectsOfType<HexMap>();
        foreach (HexMap map in maps)
        {
            TosterHexUnit toster = FindTosterForView(map, view);
            if (toster != null)
            {
                return toster;
            }
        }

        return null;
    }

    TosterHexUnit FindTosterForView(HexMap map, TosterView view)
    {
        if (map == null || map.Teams == null || view == null)
        {
            return null;
        }

        foreach (TeamClass team in map.Teams)
        {
            if (team == null || team.Tosters == null)
            {
                continue;
            }

            foreach (TosterHexUnit toster in team.Tosters)
            {
                if (toster != null && toster.tosterView == view)
                {
                    return toster;
                }
            }
        }

        return null;
    }

    bool IsCurrentUnitView(TosterView view)
    {
        if (CurrentUnit == null || view == null)
        {
            return false;
        }

        Transform unitTransform = CurrentUnit.transform;
        Transform viewTransform = view.transform;
        return unitTransform == viewTransform ||
            unitTransform.IsChildOf(viewTransform) ||
            viewTransform.IsChildOf(unitTransform);
    }

    void SetTextIfAvailable(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    string GetAverageDamageText(TosterHexUnit toster)
    {
        return Mathf.CeilToInt((toster.GetMinDmg() + toster.GetMaxDMG()) / 2f).ToString();
    }

    void SetHPBarFill(GameObject hpBar, int currentHP, int maxHP)
    {
        Image fillImage = GetHPBarFillImage(hpBar);
        if (fillImage == null)
        {
            return;
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillAmount = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
    }

    Image GetHPBarFillImage(GameObject hpBar)
    {
        if (hpBar == null)
        {
            return null;
        }

        Transform fillTransform = hpBar.transform.Find("Fill");
        if (fillTransform != null)
        {
            Image fillImage = fillTransform.GetComponent<Image>();
            if (fillImage != null)
            {
                return fillImage;
            }
        }

        Image directImage = hpBar.GetComponent<Image>();
        if (directImage != null)
        {
            return directImage;
        }

        return hpBar.GetComponentInChildren<Image>();
    }
}
