from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True

from icon_semantics import expand_terms, normalize_text, tokenize


SKILL_DIR = Path(__file__).resolve().parents[1]
PROJECT_ROOT = SKILL_DIR.parents[2]
INDEX_PATH = SKILL_DIR / "references" / "icon_index.jsonl"
ELEMENT_TERMS = {"air", "blood", "dark", "displacement", "fire", "holy", "ice", "lightning", "nature", "poison"}
GENERIC_TERMS = {
    "ability",
    "equipment",
    "gear",
    "icon",
    "item",
    "loot",
    "mage",
    "magic",
    "magiczny",
    "skill",
    "skills",
    "spell",
    "transparent",
    "umiejetnosc",
    "weapon",
}


def load_index(path: Path) -> list[dict]:
    if not path.exists():
        raise FileNotFoundError(
            f"Missing icon index: {path}. Run build_icon_index.py first."
        )

    entries = []
    with path.open("r", encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if line:
                entries.append(json.loads(line))
    return entries


def variant_preference(query_terms: set[str], variant: str) -> int:
    wants_background = bool(query_terms & {"background", "frame", "framed", "ramka", "tlo"})
    wants_transparent = bool(query_terms & {"transparent", "transperent", "przezroczyste"})

    if wants_background:
        return 8 if variant == "background" else 0
    if wants_transparent:
        return 8 if variant == "transparent" else 0

    if variant == "transparent":
        return 3
    if variant == "plain":
        return 2
    return 0


def score_entry(entry: dict, raw_query: str, query_tokens: list[str], query_terms: set[str]) -> tuple[int, list[str]]:
    fields = set(entry.get("tokens", [])) | set(entry.get("tags", []))
    path_text = normalize_text(entry["path"])
    category_text = normalize_text(entry.get("category", ""))
    reasons: list[str] = []
    score = 0

    raw_normalized = normalize_text(raw_query)
    if raw_normalized and raw_normalized in path_text:
        score += 18
        reasons.append("phrase")

    for token in query_tokens:
        canonical_matches = expand_terms([token])
        if token in fields:
            score += 12
            reasons.append(token)
        elif canonical_matches & fields:
            score += 9
            reasons.extend(sorted(canonical_matches & fields))
        elif token in category_text:
            score += 7
            reasons.append(token)
        elif token in path_text:
            score += 4
            reasons.append(token)

    semantic_overlap = query_terms & fields
    if semantic_overlap:
        score += 5 * len(semantic_overlap)
        reasons.extend(sorted(semantic_overlap))

    if "skill" in query_terms and "skill" in fields:
        score += 8
    if "weapon" in query_terms and fields & {"sword", "axe", "mace", "dagger", "spear", "bow", "staff", "crossbow"}:
        score += 5
    if "mage" in query_terms and fields & {"fire", "ice", "lightning", "dark", "blood", "air", "holy"}:
        score += 5

    score += variant_preference(query_terms, entry.get("variant", "plain"))

    # Gentle variety boost: category names usually carry the strongest curated meaning.
    if any(term in category_text for term in query_terms):
        score += 4

    deduped_reasons = []
    for reason in reasons:
        if reason not in deduped_reasons:
            deduped_reasons.append(reason)
    return score, deduped_reasons[:8]


def best_unique_results(
    scored: list[tuple[int, list[str], dict]],
    limit: int,
    per_category: int,
    spread_ratio: float,
    query_terms: set[str],
) -> list[tuple[int, list[str], dict]]:
    best_by_concept: dict[str, tuple[int, list[str], dict]] = {}
    for score, reasons, entry in scored:
        key = entry.get("concept_key") or entry["path"]
        current = best_by_concept.get(key)
        if current is None or score > current[0]:
            best_by_concept[key] = (score, reasons, entry)

    sorted_results = sorted(
        best_by_concept.values(),
        key=lambda item: (
            -item[0],
            item[2].get("category", ""),
            natural_sort_key(item[2].get("file", "")),
            item[2]["path"],
        ),
    )

    if not sorted_results:
        return []

    top_score = sorted_results[0][0]
    top_category = sorted_results[0][2].get("category", "")
    spread_cutoff = top_score * spread_ratio
    priority_terms = query_terms & ELEMENT_TERMS
    concrete_terms = query_terms - priority_terms - GENERIC_TERMS
    selected: list[tuple[int, list[str], dict]] = []
    used_paths: set[str] = set()
    category_counts: dict[str, int] = {}

    for item in sorted_results:
        score, _, entry = item
        category = entry.get("category", "")
        fields = set(entry.get("tokens", [])) | set(entry.get("tags", []))
        if score < spread_cutoff:
            continue
        if (
            priority_terms
            and category != top_category
            and not (fields & priority_terms)
            and not (fields & concrete_terms)
        ):
            continue
        if category_counts.get(category, 0) >= per_category:
            continue
        selected.append(item)
        used_paths.add(entry["path"])
        category_counts[category] = category_counts.get(category, 0) + 1
        if len(selected) >= limit:
            return selected

    for item in sorted_results:
        entry = item[2]
        if entry["path"] in used_paths:
            continue
        selected.append(item)
        used_paths.add(entry["path"])
        if len(selected) >= limit:
            break

    return selected


def natural_sort_key(value: str) -> list[tuple[int, int | str]]:
    parts: list[tuple[int, int | str]] = []
    buffer = ""
    for char in value:
        if char.isdigit():
            buffer += char
        else:
            if buffer:
                parts.append((0, int(buffer)))
                buffer = ""
            parts.append((1, char.lower()))
    if buffer:
        parts.append((0, int(buffer)))
    return parts


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Find matching local game icons.")
    parser.add_argument("query", nargs="+", help="Icon description, Polish or English.")
    parser.add_argument("--index", default=str(INDEX_PATH), help="Path to icon_index.jsonl.")
    parser.add_argument("--min", type=int, default=5, help="Minimum result count.")
    parser.add_argument("--limit", type=int, default=8, help="Maximum result count.")
    parser.add_argument("--per-category", type=int, default=3, help="Soft cap per category during the first spread pass.")
    parser.add_argument("--spread-ratio", type=float, default=0.7, help="Only diversify among candidates scoring at least this fraction of the top score.")
    parser.add_argument("--json", action="store_true", help="Print machine-readable JSON.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    query = " ".join(args.query)
    query_tokens = tokenize(query)
    query_terms = expand_terms(query_tokens)
    entries = load_index(Path(args.index))

    limit = max(args.limit, args.min)
    scored = []
    for entry in entries:
        score, reasons = score_entry(entry, query, query_tokens, query_terms)
        if score > 0:
            scored.append((score, reasons, entry))

    if not scored:
        scored = [(0, ["closest"], entry) for entry in entries]

    results = best_unique_results(scored, limit, max(1, args.per_category), args.spread_ratio, query_terms)
    if len(results) < args.min:
        used = {entry["path"] for _, _, entry in results}
        for entry in entries:
            if entry["path"] in used:
                continue
            results.append((0, ["fallback"], entry))
            used.add(entry["path"])
            if len(results) >= args.min:
                break

    payload = [
        {
            "rank": index,
            "score": score,
            "path": item["path"],
            "category": item.get("category", ""),
            "variant": item.get("variant", ""),
            "tags": item.get("tags", []),
            "reasons": reasons,
            "width": item.get("width"),
            "height": item.get("height"),
            "bytes": item.get("bytes"),
        }
        for index, (score, reasons, item) in enumerate(results, start=1)
    ]

    if args.json:
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0

    print(f"Query: {query}")
    print(f"Expanded terms: {', '.join(sorted(query_terms))}")
    print(f"Results: {len(payload)}")
    for result in payload:
        reason_text = ", ".join(result["reasons"]) if result["reasons"] else "closest"
        tag_text = ", ".join(result["tags"][:10])
        print(
            f"{result['rank']}. score {result['score']} | {result['category']} | "
            f"{result['variant']} | {result['path']}"
        )
        print(f"   reason: {reason_text}")
        if tag_text:
            print(f"   tags: {tag_text}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
