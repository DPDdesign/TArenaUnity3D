---
name: find-icon
description: Find matching local RPG icon PNGs for game UI, items, skills, spells, classes, loot, equipment, buffs, enemies, and fantasy concepts. Use when the user writes "/find icon ...", "find icon ...", "znajdz ikone ...", "dobierz ikonke ...", or asks Codex to return and preview at least five suitable icon files from this Unity project.
---

# Find Icon

## Quick Use

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. If this skill output is used in UI work,
pair it with TMP-based text components such as `TMP_Text` and
`TextMeshProUGUI`, not legacy `UnityEngine.UI.Text`.

Run the search script from the repository root:

```powershell
py -3 _codex\skills\find-icon\scripts\find_icon.py "opis ikony" --min 5 --limit 8
```

Then return at least five results. For each result, include:

- relative PNG path
- category
- short reason from matched tags

Show icon previews in the final answer, not only during tool use. Prefer a compact markdown image next to each candidate:

```markdown
![Icon 1](D:/Unity/Projects/TArenaUnity3D/TArenaUnity3D/Assets/...)
```

Also use `view_image` while deciding, so obviously wrong visual picks can be skipped. If image embedding is unavailable in the final renderer, still include markdown image links and clearly say that the previews may need to be opened from the listed paths.

## Defaults

- Prefer `TArenaUnity3D\Assets\RPG Icons Pixel Art`.
- Prefer `Transperent` variants for Unity UI unless the query asks for background/framed icons.
- Keep at least five icon candidates even when the match is broad.
- If the user asks for more variety, increase `--limit` to 12 or 20.
- If results are weak, still show the closest five and ask for one extra constraint such as element, weapon, class, or item type.

## Rebuild Index

The generated index lives in:

- `_codex\skills\find-icon\references\icon_index.jsonl`
- `_codex\skills\find-icon\references\icon_summary.md`

Rebuild it after adding/removing icon PNGs:

```powershell
py -3 _codex\skills\find-icon\scripts\build_icon_index.py --root "TArenaUnity3D\Assets\RPG Icons Pixel Art"
```

Use a different root only when the user points to another icon folder:

```powershell
py -3 _codex\skills\find-icon\scripts\build_icon_index.py --root "TArenaUnity3D\Assets\Some Other Icons"
```

## Search Notes

The index is based on folder names, file names, PNG dimensions, file size, variant folders, and curated Polish/English gameplay synonyms. It does not permanently caption every icon visually. Always preview the top results before answering so obviously wrong visual picks can be skipped.

Common supported concepts include:

- elements: fire, ice, lightning, holy, dark, blood, nature, air
- roles/classes: archer, barbarian, paladin, priest, druid, necromancer, warlock, summoner, thief, pirate, dwarf, elf, goblin, demon, undead
- gear: sword, axe, mace, dagger, spear, bow, staff, shield, armor, helmet, boots, bracers, belt, ring, amulet
- gameplay: skill, spell, buff, debuff, curse, loot, crafting, potion, food, resource, treasure, avatar
- forced movement: displaced, displacement, knockback, push, pull, odepchniety, odepchniecie, przesuniety, przesuniecie

## Output Shape

Answer in Polish when the user asks in Polish:

```markdown
Znalazlem pasujace ikony:

1. ![Icon 1](D:/Unity/Projects/TArenaUnity3D/TArenaUnity3D/Assets/RPG Icons Pixel Art/Pyromanser/PNG/Icon12.png)
   `TArenaUnity3D\Assets\RPG Icons Pixel Art\Pyromanser\PNG\Icon12.png`
   Powod: fire, mage, skill
...
```

After listing paths, confirm that previews were embedded in the final answer.
