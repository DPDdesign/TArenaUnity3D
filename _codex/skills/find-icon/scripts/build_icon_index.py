from __future__ import annotations

import argparse
import collections
import json
import struct
import sys
from datetime import datetime, timezone
from pathlib import Path

sys.dont_write_bytecode = True

from icon_semantics import infer_tags, tokenize


SKILL_DIR = Path(__file__).resolve().parents[1]
PROJECT_ROOT = SKILL_DIR.parents[2]
DEFAULT_ROOT = PROJECT_ROOT / "Assets" / "RPG Icons Pixel Art"
INDEX_PATH = SKILL_DIR / "references" / "icon_index.jsonl"
SUMMARY_PATH = SKILL_DIR / "references" / "icon_summary.md"


def read_png_size(path: Path) -> tuple[int | None, int | None]:
    try:
        with path.open("rb") as handle:
            header = handle.read(24)
    except OSError:
        return None, None

    if len(header) < 24 or not header.startswith(b"\x89PNG\r\n\x1a\n"):
        return None, None

    return struct.unpack(">II", header[16:24])


def variant_from_parts(parts: tuple[str, ...]) -> str:
    lowered = {part.lower() for part in parts}
    if "transperent" in lowered or "transparent" in lowered:
        return "transparent"
    if "background" in lowered:
        return "background"
    return "plain"


def concept_key(parts: tuple[str, ...]) -> str:
    ignored = {"png", "background", "transperent", "transparent"}
    kept = [part for part in parts if part.lower() not in ignored]
    return "/".join(kept)


def category_from_relative(parts: tuple[str, ...]) -> str:
    return parts[0] if parts else ""


def build_entries(root: Path) -> list[dict]:
    entries = []
    root = root.resolve()
    for path in sorted(root.rglob("*.png")):
        if not path.is_file():
            continue

        rel_to_root = path.relative_to(root)
        rel_to_project = path.relative_to(PROJECT_ROOT)
        parts = rel_to_root.parts
        category = category_from_relative(parts)
        variant = variant_from_parts(parts)
        width, height = read_png_size(path)
        path_text = " ".join(parts)
        tokens = sorted(set(tokenize(path_text)))
        tags = infer_tags(path_text, tokens)

        entries.append(
            {
                "path": str(rel_to_project),
                "root": str(root.relative_to(PROJECT_ROOT)),
                "category": category,
                "variant": variant,
                "concept_key": concept_key(parts),
                "file": path.name,
                "width": width,
                "height": height,
                "bytes": path.stat().st_size,
                "tokens": tokens,
                "tags": tags,
            }
        )
    return entries


def write_index(entries: list[dict], index_path: Path) -> None:
    index_path.parent.mkdir(parents=True, exist_ok=True)
    with index_path.open("w", encoding="utf-8", newline="\n") as handle:
        for entry in entries:
            handle.write(json.dumps(entry, ensure_ascii=True, sort_keys=True))
            handle.write("\n")


def write_summary(entries: list[dict], root: Path, summary_path: Path) -> None:
    categories = collections.Counter(entry["category"] for entry in entries)
    variants = collections.Counter(entry["variant"] for entry in entries)
    concepts = {entry["concept_key"] for entry in entries}
    tag_counts = collections.Counter(tag for entry in entries for tag in entry["tags"])
    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")

    lines = [
        "# Icon Index Summary",
        "",
        f"- Generated: {now}",
        f"- Root: `{root.relative_to(PROJECT_ROOT)}`",
        f"- PNG files: {len(entries)}",
        f"- Deduplicated icon concepts: {len(concepts)}",
        f"- Categories: {len(categories)}",
        "",
        "## Variants",
        "",
    ]

    for variant, count in variants.most_common():
        lines.append(f"- `{variant}`: {count}")

    lines.extend(["", "## Top Tags", ""])
    for tag, count in tag_counts.most_common(80):
        lines.append(f"- `{tag}`: {count}")

    lines.extend(["", "## Categories", ""])
    for category, count in categories.most_common():
        lines.append(f"- `{category}`: {count}")

    summary_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build the local game icon search index.")
    parser.add_argument("--root", action="append", help="Icon root folder. Can be repeated.")
    parser.add_argument("--index", default=str(INDEX_PATH), help="Output JSONL index path.")
    parser.add_argument("--summary", default=str(SUMMARY_PATH), help="Output summary markdown path.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    roots = [Path(value) for value in args.root] if args.root else [DEFAULT_ROOT]
    all_entries: list[dict] = []

    for root in roots:
        root_path = root if root.is_absolute() else PROJECT_ROOT / root
        if not root_path.exists():
            raise FileNotFoundError(f"Icon root does not exist: {root_path}")
        all_entries.extend(build_entries(root_path))

    write_index(all_entries, Path(args.index))
    summary_root = roots[0] if len(roots) == 1 else Path("<multiple roots>")
    summary_root = summary_root if summary_root.is_absolute() else PROJECT_ROOT / summary_root
    write_summary(all_entries, summary_root, Path(args.summary))
    print(f"Wrote {len(all_entries)} PNG entries to {args.index}")
    print(f"Wrote summary to {args.summary}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
