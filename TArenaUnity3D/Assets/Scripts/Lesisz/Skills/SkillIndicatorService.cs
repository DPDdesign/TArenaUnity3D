using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SkillIndicatorService : MonoBehaviour
{
    const float GroundOffset = 0.05f;
    const float ArrowWidth = 1.3f;
    const float MovingArrowSpacing = 4.4f;
    const float ArrowLengthScale = 1.35f;
    const float MovingArrowSpeed = 3f;
    const float GrowInDurationSeconds = 0.12f;
    const float PulseAmount = 0.06f;
    const float PulseSpeed = 3f;
    const float RotateSpeed = 120f;
    const float HexScale = 1.15f;
    const float ArcScale = 1.55f;
    static readonly int FillTexturePropertyId = Shader.PropertyToID("_FillTex");

    [SerializeField] Material shineMaterial;

    static SkillIndicatorService instance;
    static bool missingServiceWarningShown;

    readonly Queue<PooledIndicator> pooledIndicators = new Queue<PooledIndicator>();
    readonly List<PooledIndicator> activeIndicators = new List<PooledIndicator>();

    class PooledIndicator
    {
        public GameObject GameObject;
        public Transform Transform;
        public SpriteRenderer Renderer;
        public SkillIndicatorType IndicatorType;
        public float Age;
        public float BaseRotationZ;
        public float BaseSizeX;
        public float BaseSizeY;
        public Vector3 BaseScale;
        public float RotationSpeed;
        public bool UsesSizedDrawMode;
        public bool MovesAlongLine;
        public Vector3 LineStart;
        public Vector3 LineDirection;
        public float LineDistance;
        public float LineOffset;
        public float LineSpeed;
        public MaterialPropertyBlock PropertyBlock;
    }

    void Awake()
    {
        if (instance == null || instance == this)
        {
            instance = this;
        }
    }

    void OnDisable()
    {
        HideAllInternal();
        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        for (int i = 0; i < activeIndicators.Count; i++)
        {
            AnimateIndicator(activeIndicators[i], Time.deltaTime);
        }
    }

    public static void HideAll()
    {
        SkillIndicatorService service = GetService();
        if (service == null)
        {
            return;
        }

        service.HideAllInternal();
    }

    public static void ShowPreview(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        HexClass hoverHex,
        SkillCast previewCast,
        IList<HexCoord> previewTargets,
        HexMap hexMap)
    {
        SkillIndicatorService service = GetService();
        if (service == null)
        {
            return;
        }

        service.ShowPreviewInternal(entry, caster, hoverHex, previewCast, previewTargets, hexMap);
    }

    static SkillIndicatorService GetService()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<SkillIndicatorService>();
        }

        if (instance == null && !missingServiceWarningShown)
        {
            Debug.LogWarning("SkillIndicatorService is missing from the scene. Skill indicator preview will not render.");
            missingServiceWarningShown = true;
        }

        return instance;
    }

    void ShowPreviewInternal(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        HexClass hoverHex,
        SkillCast previewCast,
        IList<HexCoord> previewTargets,
        HexMap hexMap)
    {
        HideAllInternal();

        if (entry == null ||
            caster == null ||
            hexMap == null ||
            entry.indicatorType == SkillIndicatorType.None ||
            entry.indicatorPlacement == SkillIndicatorPlacement.None ||
            entry.indicatorSprite == null ||
            entry.indicatorMaterial == null)
        {
            return;
        }

        Vector3 casterPosition = ResolveUnitPosition(caster);
        List<Vector3> markerPositions = ResolveMarkerPositions(entry, caster, hoverHex, previewCast, previewTargets, hexMap);
        if (markerPositions.Count == 0)
        {
            return;
        }

        if (entry.indicatorType == SkillIndicatorType.Line || entry.indicatorType == SkillIndicatorType.Scatter)
        {
            int arrowCount = entry.indicatorType == SkillIndicatorType.Line ? 1 : markerPositions.Count;
            for (int i = 0; i < markerPositions.Count && i < arrowCount; i++)
            {
                ShowArrow(entry, casterPosition, markerPositions[i]);
            }

            return;
        }

        ShowGroundMarkers(entry, markerPositions, ResolveDirectionAngle(casterPosition, ResolvePrimaryPosition(markerPositions, hoverHex)));
    }

    List<Vector3> ResolveMarkerPositions(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        HexClass hoverHex,
        SkillCast previewCast,
        IList<HexCoord> previewTargets,
        HexMap hexMap)
    {
        List<Vector3> positions = new List<Vector3>();
        switch (entry.indicatorPlacement)
        {
            case SkillIndicatorPlacement.CasterToHover:
            case SkillIndicatorPlacement.UnderHover:
                AddHexPosition(positions, hoverHex);
                break;

            case SkillIndicatorPlacement.UnderAffectedHexes:
                AddHexPositions(positions, previewCast != null ? previewCast.AffectedHexes : null, hexMap);
                if (positions.Count == 0)
                {
                    AddHexPosition(positions, hoverHex);
                }
                break;

            case SkillIndicatorPlacement.UnderTargets:
                AddHexPositions(positions, previewTargets, hexMap);
                if (positions.Count == 0 && previewCast != null)
                {
                    AddHexPositions(positions, previewCast.SelectedHexes, hexMap);
                }
                if (positions.Count == 0)
                {
                    AddHexPosition(positions, hoverHex);
                }
                break;

            case SkillIndicatorPlacement.UnderAllAllies:
                AddTeamHexPositions(positions, caster != null ? caster.Team : null);
                break;

            case SkillIndicatorPlacement.UnderAllEnemies:
                AddEnemyHexPositions(positions, caster, hexMap);
                break;
        }

        return positions;
    }

    void AddHexPositions(List<Vector3> positions, IList<HexCoord> hexes, HexMap hexMap)
    {
        if (hexes == null || hexMap == null)
        {
            return;
        }

        for (int i = 0; i < hexes.Count; i++)
        {
            HexCoord coord = hexes[i];
            if (coord == null)
            {
                continue;
            }

            AddHexPosition(positions, hexMap.GetHexAt(coord.C, coord.R));
        }
    }

    void AddTeamHexPositions(List<Vector3> positions, TeamClass team)
    {
        if (team == null || team.Tosters == null)
        {
            return;
        }

        for (int i = 0; i < team.Tosters.Count; i++)
        {
            TosterHexUnit toster = team.Tosters[i];
            if (toster == null || toster.isDead || toster.Hex == null)
            {
                continue;
            }

            AddHexPosition(positions, toster.Hex);
        }
    }

    void AddEnemyHexPositions(List<Vector3> positions, TosterHexUnit caster, HexMap hexMap)
    {
        if (caster == null || caster.Team == null || hexMap == null || hexMap.Teams == null)
        {
            return;
        }

        for (int i = 0; i < hexMap.Teams.Count; i++)
        {
            TeamClass team = hexMap.Teams[i];
            if (team == null || team == caster.Team)
            {
                continue;
            }

            AddTeamHexPositions(positions, team);
        }
    }

    void AddHexPosition(List<Vector3> positions, HexClass hex)
    {
        if (hex == null || hex.MyHex == null)
        {
            return;
        }

        Vector3 position = hex.MyHex.transform.position;
        position.y += GroundOffset;
        if (ContainsNearbyPosition(positions, position))
        {
            return;
        }

        positions.Add(position);
    }

    bool ContainsNearbyPosition(List<Vector3> positions, Vector3 candidate)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if ((positions[i] - candidate).sqrMagnitude <= 0.0001f)
            {
                return true;
            }
        }

        return false;
    }

    Vector3 ResolvePrimaryPosition(List<Vector3> positions, HexClass hoverHex)
    {
        if (hoverHex != null && hoverHex.MyHex != null)
        {
            Vector3 hoverPosition = hoverHex.MyHex.transform.position;
            hoverPosition.y += GroundOffset;
            return hoverPosition;
        }

        return positions[0];
    }

    void ShowArrow(SkillPresentationEntry entry, Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
        {
            return;
        }

        Vector3 normalizedDirection = direction / distance;
        Vector3 movementDirection = normalizedDirection;
        float movementRotationZ = Mathf.Atan2(movementDirection.z, movementDirection.x) * Mathf.Rad2Deg;
        int arrowCount = Mathf.Max(1, Mathf.CeilToInt(distance / MovingArrowSpacing));
        float spacing = distance / arrowCount;

        for (int i = 0; i < arrowCount; i++)
        {
            PooledIndicator indicator = RentIndicator(entry);
            indicator.IndicatorType = entry.indicatorType;
            indicator.Age = 0f;
            indicator.BaseRotationZ = movementRotationZ;
            indicator.Transform.rotation = Quaternion.Euler(90f, 0f, movementRotationZ);

            SpriteRenderer renderer = indicator.Renderer;
            renderer.drawMode = SpriteDrawMode.Simple;

            Vector2 spriteSize = renderer.sprite != null
                ? new Vector2(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y)
                : Vector2.one;
            float uniformScale = ArrowWidth / Mathf.Max(0.01f, spriteSize.y);
            Vector2 previewScale = ResolvePreviewScale(entry);
            indicator.BaseScale = new Vector3(
                uniformScale * ArrowLengthScale * previewScale.x,
                uniformScale * previewScale.y,
                uniformScale);
            indicator.Transform.localScale = indicator.BaseScale;

            indicator.UsesSizedDrawMode = false;
            indicator.MovesAlongLine = true;
            indicator.LineStart = start + Vector3.up * GroundOffset;
            indicator.LineDirection = movementDirection;
            indicator.LineDistance = distance;
            indicator.LineOffset = i * spacing;
            indicator.LineSpeed = ResolveEffectSpeed(entry, MovingArrowSpeed);
            UpdateMovingLineIndicator(indicator);
        }
    }

    void ShowGroundMarkers(SkillPresentationEntry entry, List<Vector3> markerPositions, float directionAngle)
    {
        if (entry.indicatorType == SkillIndicatorType.AoE && markerPositions.Count > 1)
        {
            ShowGroundMarker(entry, ResolveAreaCenter(markerPositions), directionAngle, ResolveAreaScale(markerPositions));
            return;
        }

        for (int i = 0; i < markerPositions.Count; i++)
        {
            ShowGroundMarker(entry, markerPositions[i], directionAngle, 1f);
        }
    }

    void ShowGroundMarker(SkillPresentationEntry entry, Vector3 position, float directionAngle, float areaScale)
    {
        PooledIndicator indicator = RentIndicator(entry);
        indicator.IndicatorType = entry.indicatorType;
        indicator.Age = 0f;
        indicator.Transform.position = position;
        indicator.BaseRotationZ = directionAngle;
        indicator.RotationSpeed = ResolveEffectSpeed(entry, RotateSpeed);

        if (entry.indicatorType == SkillIndicatorType.Arc)
        {
            indicator.Transform.rotation = Quaternion.Euler(90f, 0f, directionAngle);
            indicator.BaseScale = ResolveGroundScale(entry, ArcScale, areaScale);
        }
        else
        {
            indicator.Transform.rotation = Quaternion.Euler(90f, 0f, directionAngle);
            indicator.BaseScale = ResolveGroundScale(entry, HexScale, areaScale);
        }

        indicator.UsesSizedDrawMode = false;
        indicator.MovesAlongLine = false;
        indicator.Renderer.drawMode = SpriteDrawMode.Simple;
        indicator.Transform.localScale = indicator.BaseScale;
    }

    static Vector3 ResolveGroundScale(SkillPresentationEntry entry, float baseScale, float areaScale)
    {
        Vector2 previewScale = ResolvePreviewScale(entry);
        float safeAreaScale = Mathf.Max(1f, areaScale);
        return new Vector3(
            baseScale * safeAreaScale * previewScale.x,
            baseScale * safeAreaScale * previewScale.y,
            baseScale * safeAreaScale);
    }

    static Vector2 ResolvePreviewScale(SkillPresentationEntry entry)
    {
        if (entry == null)
        {
            return Vector2.one;
        }

        Vector2 scale = entry.indicatorPrefabScaleXY;
        if (scale.x <= 0f)
        {
            scale.x = 1f;
        }

        if (scale.y <= 0f)
        {
            scale.y = 1f;
        }

        return scale;
    }

    static float ResolveEffectSpeed(SkillPresentationEntry entry, float fallback)
    {
        if (entry == null || entry.indicatorEffectSpeed <= 0f)
        {
            return fallback;
        }

        return entry.indicatorEffectSpeed;
    }

    static Vector3 ResolveAreaCenter(List<Vector3> positions)
    {
        if (positions == null || positions.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 min = positions[0];
        Vector3 max = positions[0];
        for (int i = 1; i < positions.Count; i++)
        {
            Vector3 position = positions[i];
            min.x = Mathf.Min(min.x, position.x);
            min.z = Mathf.Min(min.z, position.z);
            max.x = Mathf.Max(max.x, position.x);
            max.z = Mathf.Max(max.z, position.z);
        }

        return new Vector3((min.x + max.x) * 0.5f, positions[0].y, (min.z + max.z) * 0.5f);
    }

    static float ResolveAreaScale(List<Vector3> positions)
    {
        if (positions == null || positions.Count <= 1)
        {
            return 1f;
        }

        float nearestHexDistance = float.MaxValue;
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                Vector2 a = new Vector2(positions[i].x, positions[i].z);
                Vector2 b = new Vector2(positions[j].x, positions[j].z);
                float distance = Vector2.Distance(a, b);
                if (distance > 0.01f)
                {
                    nearestHexDistance = Mathf.Min(nearestHexDistance, distance);
                }
            }
        }

        if (nearestHexDistance == float.MaxValue)
        {
            return 1f;
        }

        Vector3 center = ResolveAreaCenter(positions);
        float farthestDistance = 0f;
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2 a = new Vector2(center.x, center.z);
            Vector2 b = new Vector2(positions[i].x, positions[i].z);
            farthestDistance = Mathf.Max(farthestDistance, Vector2.Distance(a, b));
        }

        return Mathf.Max(1f, 1f + (farthestDistance * 2f / nearestHexDistance));
    }

    PooledIndicator RentIndicator(SkillPresentationEntry entry)
    {
        PooledIndicator indicator = pooledIndicators.Count > 0 ? pooledIndicators.Dequeue() : CreateIndicator();
        indicator.GameObject.SetActive(true);
        indicator.Renderer.sprite = entry.indicatorSprite;
        indicator.Renderer.sharedMaterial = entry.indicatorMaterial;
        indicator.Renderer.color = Color.white;
        ApplyFillTexture(indicator, entry.indicatorFillTexture);
        activeIndicators.Add(indicator);
        return indicator;
    }

    static void ApplyFillTexture(PooledIndicator indicator, Texture texture)
    {
        if (indicator == null || indicator.Renderer == null)
        {
            return;
        }

        if (texture == null)
        {
            indicator.Renderer.SetPropertyBlock(null);
            return;
        }

        if (indicator.PropertyBlock == null)
        {
            indicator.PropertyBlock = new MaterialPropertyBlock();
        }

        indicator.PropertyBlock.Clear();
        indicator.PropertyBlock.SetTexture(FillTexturePropertyId, texture);
        indicator.Renderer.SetPropertyBlock(indicator.PropertyBlock);
    }

    PooledIndicator CreateIndicator()
    {
        GameObject go = new GameObject("SkillIndicator");
        go.transform.SetParent(transform, false);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 40;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return new PooledIndicator
        {
            GameObject = go,
            Transform = go.transform,
            Renderer = renderer,
            PropertyBlock = new MaterialPropertyBlock()
        };
    }

    void AnimateIndicator(PooledIndicator indicator, float deltaTime)
    {
        if (indicator == null || indicator.GameObject == null || indicator.GameObject.activeSelf == false)
        {
            return;
        }

        indicator.Age += deltaTime;
        float grow = Mathf.Clamp01(indicator.Age / GrowInDurationSeconds);
        float pulse = 1f + Mathf.Sin(Time.time * PulseSpeed) * PulseAmount;

        if (indicator.IndicatorType == SkillIndicatorType.Line || indicator.IndicatorType == SkillIndicatorType.Scatter)
        {
            if (indicator.MovesAlongLine)
            {
                UpdateMovingLineIndicator(indicator);
                return;
            }

            if (indicator.UsesSizedDrawMode)
            {
                indicator.Renderer.size = new Vector2(
                    Mathf.Max(0.01f, indicator.BaseSizeX * grow),
                    indicator.BaseSizeY * pulse);
            }
            else
            {
                indicator.Transform.localScale = new Vector3(
                    indicator.BaseScale.x * grow,
                    indicator.BaseScale.y * pulse,
                    indicator.BaseScale.z);
            }

            return;
        }

        float rotationOffset = Time.time * indicator.RotationSpeed;
        indicator.Transform.rotation = Quaternion.Euler(90f, 0f, indicator.BaseRotationZ + rotationOffset);
        indicator.Transform.localScale = indicator.BaseScale * grow * pulse;

        Color color = indicator.Renderer.color;
        color.a = indicator.IndicatorType == SkillIndicatorType.Hex ? grow : 1f;
        indicator.Renderer.color = color;
    }

    void UpdateMovingLineIndicator(PooledIndicator indicator)
    {
        if (indicator == null || indicator.LineDistance <= 0.01f)
        {
            return;
        }

        float distanceAlongLine = Mathf.Repeat(Time.time * indicator.LineSpeed + indicator.LineOffset, indicator.LineDistance);
        indicator.Transform.position = indicator.LineStart + indicator.LineDirection * distanceAlongLine;
        indicator.Transform.rotation = Quaternion.Euler(90f, 0f, indicator.BaseRotationZ);
        indicator.Transform.localScale = indicator.BaseScale;
    }

    void HideAllInternal()
    {
        for (int i = activeIndicators.Count - 1; i >= 0; i--)
        {
            ReturnIndicator(activeIndicators[i]);
        }

        activeIndicators.Clear();
    }

    void ReturnIndicator(PooledIndicator indicator)
    {
        if (indicator == null || indicator.GameObject == null)
        {
            return;
        }

        indicator.GameObject.SetActive(false);
        indicator.Renderer.sprite = null;
        indicator.Renderer.sharedMaterial = shineMaterial;
        indicator.Renderer.SetPropertyBlock(null);
        indicator.Renderer.color = Color.white;
        indicator.Renderer.drawMode = SpriteDrawMode.Simple;
        indicator.Renderer.size = Vector2.one;
        indicator.RotationSpeed = 0f;
        indicator.UsesSizedDrawMode = false;
        indicator.MovesAlongLine = false;
        indicator.LineStart = Vector3.zero;
        indicator.LineDirection = Vector3.zero;
        indicator.LineDistance = 0f;
        indicator.LineOffset = 0f;
        indicator.LineSpeed = 0f;
        indicator.Transform.localPosition = Vector3.zero;
        indicator.Transform.localRotation = Quaternion.identity;
        indicator.Transform.localScale = Vector3.one;
        pooledIndicators.Enqueue(indicator);
    }

    static Vector3 ResolveUnitPosition(TosterHexUnit unit)
    {
        if (unit != null)
        {
            if (unit.tosterView != null)
            {
                return unit.tosterView.transform.position;
            }

            if (unit.Hex != null && unit.Hex.MyHex != null)
            {
                return unit.Hex.MyHex.transform.position;
            }
        }

        return Vector3.zero;
    }

    static float ResolveDirectionAngle(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        return Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
    }
}
