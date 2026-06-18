using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RunMapNodeTypeIconCatalogTests
{
    [Test]
    public void FindIcon_ReturnsSpriteForMatchingNodeType()
    {
        RunMapNodeTypeIconCatalog catalog = ScriptableObject.CreateInstance<RunMapNodeTypeIconCatalog>();
        Sprite battleSprite = CreateTestSprite("battle");
        Sprite shopSprite = CreateTestSprite("shop");
        catalog.Entries = new List<RunMapNodeTypeIconEntry>
        {
            new RunMapNodeTypeIconEntry(RunMapNodeType.Battle, battleSprite),
            new RunMapNodeTypeIconEntry(RunMapNodeType.Shop, shopSprite)
        };

        Assert.That(catalog.FindIcon(RunMapNodeType.Battle), Is.EqualTo(battleSprite));
        Assert.That(catalog.FindIcon(RunMapNodeType.Shop), Is.EqualTo(shopSprite));
        Assert.That(catalog.FindIcon(RunMapNodeType.FinalBoss), Is.Null);

        Object.DestroyImmediate(catalog);
        Object.DestroyImmediate(battleSprite);
        Object.DestroyImmediate(shopSprite);
    }

    private static Sprite CreateTestSprite(string name)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = name + "_texture";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        sprite.name = name;
        return sprite;
    }
}
