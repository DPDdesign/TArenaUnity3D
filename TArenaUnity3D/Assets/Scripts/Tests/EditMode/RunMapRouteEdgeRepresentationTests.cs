#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class RunMapRouteEdgeRepresentationTests
{
    private GameObject root;
    private RunMapRouteEdgeRepresentation representation;
    private Image background;
    private Image fill;
    private Image glow;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("RunMapRouteEdge", typeof(RectTransform), typeof(Image));
        background = root.GetComponent<Image>();
        representation = root.AddComponent<RunMapRouteEdgeRepresentation>();

        fill = CreateImage("Edge_Fill");
        glow = CreateImage("Edge_Glow");

        representation.Background = background;
        representation.Fill = fill;
        representation.Glow = glow;
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Bind_WithCompletedTarget_SetsFullPassedFill()
    {
        representation.Bind(Node("source", RunMapNodeState.Completed, false), Node("target", RunMapNodeState.Completed, false));

        Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
        Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Horizontal));
        Assert.That(fill.fillAmount, Is.EqualTo(1f));
        Assert.That(fill.gameObject.activeSelf, Is.True);
        Assert.That(glow.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void Bind_WithAvailableTarget_SetsHalfAvailableFillAndGlow()
    {
        representation.Bind(Node("source", RunMapNodeState.Completed, false), Node("target", RunMapNodeState.Available, true));

        Assert.That(fill.fillAmount, Is.EqualTo(0.5f));
        Assert.That(fill.gameObject.activeSelf, Is.True);
        Assert.That(glow.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void Bind_WithAvailableState_UsesAvailableFillEvenWhenCanTravelFlagIsFalse()
    {
        representation.Bind(Node("source", RunMapNodeState.Completed, false), Node("target", RunMapNodeState.Available, false));

        Assert.That(fill.fillAmount, Is.EqualTo(0.5f));
        Assert.That(fill.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void Bind_WithLockedTarget_ClearsFillAndGlow()
    {
        representation.Bind(Node("source", RunMapNodeState.Completed, false), Node("target", RunMapNodeState.Locked, false));

        Assert.That(fill.fillAmount, Is.EqualTo(0f));
        Assert.That(fill.gameObject.activeSelf, Is.False);
        Assert.That(glow.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void Bind_WithNullTarget_ClearsFillAndGlow()
    {
        representation.Bind(Node("source", RunMapNodeState.Completed, false), null);

        Assert.That(fill.fillAmount, Is.EqualTo(0f));
        Assert.That(fill.gameObject.activeSelf, Is.False);
        Assert.That(glow.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void Bind_WithLockedSourceAndCompletedMergeTarget_ClearsFillAndGlow()
    {
        representation.Bind(Node("source", RunMapNodeState.Locked, false), Node("target", RunMapNodeState.Completed, false));

        Assert.That(fill.fillAmount, Is.EqualTo(0f));
        Assert.That(fill.gameObject.activeSelf, Is.False);
        Assert.That(glow.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void Bind_WithAvailableSourceAndCompletedMergeTarget_ClearsFillAndGlow()
    {
        representation.Bind(Node("source", RunMapNodeState.Available, true), Node("target", RunMapNodeState.Completed, false));

        Assert.That(fill.fillAmount, Is.EqualTo(0f));
        Assert.That(fill.gameObject.activeSelf, Is.False);
        Assert.That(glow.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void Bind_WithLockedSourceAndAvailableTarget_ClearsFillAndGlow()
    {
        representation.Bind(Node("source", RunMapNodeState.Locked, false), Node("target", RunMapNodeState.Available, true));

        Assert.That(fill.fillAmount, Is.EqualTo(0f));
        Assert.That(fill.gameObject.activeSelf, Is.False);
        Assert.That(glow.gameObject.activeSelf, Is.False);
    }

    private Image CreateImage(string name)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(root.transform, false);
        return child.AddComponent<Image>();
    }

    private static RunMapNodeViewData Node(string nodeId, RunMapNodeState state, bool canTravel)
    {
        return new RunMapNodeViewData(
            nodeId,
            "path-test",
            RunMapNodeType.Battle,
            state,
            1,
            "Target",
            "Reward hint",
            "Risk hint",
            "enc-test",
            canTravel);
    }
}
#endif
