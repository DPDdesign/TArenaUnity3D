#!/usr/bin/env python3
"""Validate Unity prefab YAML for TArena UI mockup work.

This catches common hand-editing defects before Unity import. It is deliberately
conservative and does not try to replace Unity's own prefab importer.
"""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path


OBJECT_HEADER_RE = re.compile(r"^--- !u!(?P<class_id>\d+) &(?P<file_id>-?\d+)")
LOCAL_REF_RE = re.compile(r"\{fileID:\s*(?P<file_id>-?\d+)(?P<body>[^}]*)\}")
COMPONENT_REF_RE = re.compile(
    r"^\s*-\s+component:\s+\{fileID:\s*(?P<file_id>-?\d+)(?P<body>[^}]*)\}"
)
SCRIPT_REF_RE = re.compile(
    r"^\s*m_Script:\s+\{fileID:\s*(?P<file_id>-?\d+)(?P<body>[^}]*)\}"
)
GUID_RE = re.compile(r"guid:\s*(?P<guid>[0-9a-fA-F]+)")
ZERO_FIELD_RE = re.compile(
    r"^\s*(?P<field>[A-Za-z_][A-Za-z0-9_]*)\s*:\s*\{fileID:\s*0\s*\}"
)
REPEATED_INSTANCE_NAME_RE = re.compile(
    r"(?:^|_)(?:row|card|button|node|edge|option|slot|item|frame)s?[_ -]?\d+$|"
    r"(?:row|card|button|node|edge|option|slot|item|frame)[_ -]?\d+$",
    re.IGNORECASE,
)

CLASS_GAME_OBJECT = 1
CLASS_TRANSFORM = 4
CLASS_MONO_BEHAVIOUR = 114
CLASS_CANVAS_RENDERER = 222
CLASS_RECT_TRANSFORM = 224
CLASS_PREFAB_INSTANCE = 1001

GUID_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc"
GUID_BUTTON = "4e29b1a8efbd4b44bb3f3716e73f07ff"
GUID_LEGACY_TEXT = "5f7201a12d95ffc409449d95f23cf332"
GUID_TMP_TEXT = "f4688fdb7df04437aeb418b961361dc5"
GUID_SLIDER = "67db9e8f0e2ae9c40bc1e2b64352a6b4"
GUID_SCROLLBAR = "2a4db7a114972834c8e4117be1d82ba3"

KNOWN_UI_GUIDS = {
    GUID_IMAGE,
    GUID_BUTTON,
    GUID_LEGACY_TEXT,
    GUID_TMP_TEXT,
    GUID_SLIDER,
    GUID_SCROLLBAR,
}


@dataclass(frozen=True)
class UnityObject:
    class_id: int
    file_id: int
    start_line: int
    lines: list[str]


@dataclass
class Finding:
    severity: str
    path: Path
    line: int
    message: str

    def format(self) -> str:
        location = f"{self.path}:{self.line}" if self.line else str(self.path)
        return f"{self.severity}: {location}: {self.message}"


def parse_objects(lines: list[str]) -> list[UnityObject]:
    objects: list[UnityObject] = []
    current_class: int | None = None
    current_id: int | None = None
    current_start = 0
    current_lines: list[str] = []

    for index, line in enumerate(lines, start=1):
        header = OBJECT_HEADER_RE.match(line)
        if header:
            if current_class is not None and current_id is not None:
                objects.append(
                    UnityObject(
                        current_class,
                        current_id,
                        current_start,
                        current_lines,
                    )
                )
            current_class = int(header.group("class_id"))
            current_id = int(header.group("file_id"))
            current_start = index
            current_lines = [line]
            continue

        if current_class is not None:
            current_lines.append(line)

    if current_class is not None and current_id is not None:
        objects.append(
            UnityObject(current_class, current_id, current_start, current_lines)
        )

    return objects


def is_local_ref(body: str) -> bool:
    return "guid:" not in body and "type:" not in body


def find_refs(line: str) -> list[int]:
    refs: list[int] = []
    for match in LOCAL_REF_RE.finditer(line):
        if is_local_ref(match.group("body")):
            refs.append(int(match.group("file_id")))
    return refs


def find_guid(text: str) -> str | None:
    match = GUID_RE.search(text)
    if not match:
        return None
    return match.group("guid").lower()


def script_guid(obj: UnityObject) -> str | None:
    for line in obj.lines:
        match = SCRIPT_REF_RE.match(line)
        if not match:
            continue
        if int(match.group("file_id")) == 0:
            return "0"
        return find_guid(match.group("body"))
    return None


def game_object_name(obj: UnityObject) -> str:
    for line in obj.lines:
        stripped = line.strip()
        if stripped.startswith("m_Name:"):
            return stripped.split(":", 1)[1].strip()
    return ""


def button_has_callback(obj: UnityObject) -> bool:
    for line in obj.lines:
        stripped = line.strip()
        if not stripped.startswith("m_MethodName:"):
            continue
        method_name = stripped.split(":", 1)[1].strip()
        if method_name:
            return True
    return False


def validate_script_meta(script_dir: Path) -> list[Finding]:
    findings: list[Finding] = []
    if not script_dir.exists():
        findings.append(Finding("ERROR", script_dir, 0, "script directory does not exist"))
        return findings
    for script in script_dir.rglob("*.cs"):
        meta = script.with_name(script.name + ".meta")
        if not meta.exists():
            findings.append(
                Finding(
                    "ERROR",
                    script,
                    0,
                    "missing .cs.meta; let Unity import the script before wiring prefab GUIDs",
                )
            )
    return findings


def object_line_number(obj: UnityObject, local_index: int) -> int:
    return obj.start_line + local_index


def validate_prefab(
    path: Path,
    *,
    strict_zero_fields: bool,
    allowed_zero_fields: set[str],
) -> list[Finding]:
    findings: list[Finding] = []

    try:
        text = path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        text = path.read_text(encoding="utf-8", errors="replace")

    lines = text.splitlines()
    if not lines or lines[0].strip() != "%YAML 1.1":
        findings.append(Finding("ERROR", path, 1, "missing Unity YAML header"))
    if len(lines) < 2 or not lines[1].startswith("%TAG !u! tag:unity3d.com,2011:"):
        findings.append(Finding("ERROR", path, 2, "missing Unity TAG header"))

    for index, line in enumerate(lines, start=1):
        if re.search(r"\}\s+m_[A-Za-z]", line):
            findings.append(
                Finding(
                    "ERROR",
                    path,
                    index,
                    "joined YAML fields detected; split the fields onto separate lines",
                )
            )

    objects = parse_objects(lines)
    object_ids: set[int] = set()
    duplicates: set[int] = set()
    for obj in objects:
        if obj.file_id in object_ids:
            duplicates.add(obj.file_id)
        object_ids.add(obj.file_id)

    for duplicate in sorted(duplicates):
        findings.append(
            Finding("ERROR", path, 0, f"duplicate local fileID {duplicate}")
        )

    game_object_ids = {obj.file_id for obj in objects if obj.class_id == CLASS_GAME_OBJECT}
    transform_ids = {
        obj.file_id
        for obj in objects
        if obj.class_id in (CLASS_TRANSFORM, CLASS_RECT_TRANSFORM)
    }

    children_by_transform: dict[int, set[int]] = {}
    parent_by_transform: dict[int, int] = {}
    component_class_by_id = {obj.file_id: obj.class_id for obj in objects}
    component_script_guid_by_id = {
        obj.file_id: script_guid(obj)
        for obj in objects
        if obj.class_id == CLASS_MONO_BEHAVIOUR
    }
    component_ids_by_game_object: dict[int, list[int]] = {}
    game_object_names_by_id: dict[int, str] = {}

    for obj in objects:
        if obj.class_id != CLASS_GAME_OBJECT:
            continue
        game_object_names_by_id[obj.file_id] = game_object_name(obj)
        for line in obj.lines:
            match = COMPONENT_REF_RE.match(line)
            if match and is_local_ref(match.group("body")):
                component_ids_by_game_object.setdefault(obj.file_id, []).append(
                    int(match.group("file_id"))
                )

    canvas_renderer_count = sum(
        1 for obj in objects if obj.class_id == CLASS_CANVAS_RENDERER
    )
    prefab_instance_count = sum(
        1 for obj in objects if obj.class_id == CLASS_PREFAB_INSTANCE
    )
    repeated_instance_names = sorted(
        name
        for name in game_object_names_by_id.values()
        if REPEATED_INSTANCE_NAME_RE.search(name)
    )
    image_count = 0
    tmp_text_count = 0
    legacy_text_count = 0
    button_count = 0
    slider_count = 0
    script_component_count = 0
    buttons_without_callbacks: list[UnityObject] = []

    for obj in objects:
        if obj.class_id == CLASS_GAME_OBJECT:
            for local_index, line in enumerate(obj.lines):
                match = COMPONENT_REF_RE.match(line)
                if not match or not is_local_ref(match.group("body")):
                    continue
                ref = int(match.group("file_id"))
                if ref != 0 and ref not in object_ids:
                    findings.append(
                        Finding(
                            "ERROR",
                            path,
                            object_line_number(obj, local_index),
                            f"GameObject component points to missing fileID {ref}",
                        )
                    )

        if obj.class_id in (CLASS_TRANSFORM, CLASS_RECT_TRANSFORM, CLASS_MONO_BEHAVIOUR):
            for local_index, line in enumerate(obj.lines):
                stripped = line.strip()
                if stripped.startswith("m_GameObject:"):
                    for ref in find_refs(line):
                        if ref != 0 and ref not in game_object_ids:
                            findings.append(
                                Finding(
                                    "ERROR",
                                    path,
                                    object_line_number(obj, local_index),
                                    f"component points to missing GameObject fileID {ref}",
                                )
                            )

        if obj.class_id in (CLASS_TRANSFORM, CLASS_RECT_TRANSFORM):
            for local_index, line in enumerate(obj.lines):
                stripped = line.strip()
                if stripped.startswith("m_Father:"):
                    refs = find_refs(line)
                    if refs:
                        parent_by_transform[obj.file_id] = refs[0]
                    for ref in refs:
                        if ref != 0 and ref not in transform_ids:
                            findings.append(
                                Finding(
                                    "ERROR",
                                    path,
                                    object_line_number(obj, local_index),
                                    f"transform parent points to missing transform fileID {ref}",
                                )
                            )
                elif stripped.startswith("- {fileID:"):
                    for ref in find_refs(line):
                        if ref != 0:
                            children_by_transform.setdefault(obj.file_id, set()).add(ref)
                        if ref != 0 and ref not in transform_ids:
                            findings.append(
                                Finding(
                                    "ERROR",
                                    path,
                                    object_line_number(obj, local_index),
                                    f"transform child points to missing transform fileID {ref}",
                                )
                            )

        if obj.class_id == CLASS_MONO_BEHAVIOUR:
            guid = script_guid(obj)
            if guid == "0" or guid is None:
                findings.append(
                    Finding(
                        "ERROR",
                        path,
                        obj.start_line,
                        "MonoBehaviour has no valid m_Script reference",
                    )
                )
            elif guid == GUID_IMAGE:
                image_count += 1
            elif guid == GUID_BUTTON:
                button_count += 1
                if not button_has_callback(obj):
                    buttons_without_callbacks.append(obj)
            elif guid == GUID_TMP_TEXT:
                tmp_text_count += 1
            elif guid == GUID_LEGACY_TEXT:
                legacy_text_count += 1
            elif guid == GUID_SLIDER:
                slider_count += 1
            if guid and guid not in KNOWN_UI_GUIDS:
                script_component_count += 1

            for local_index, line in enumerate(obj.lines):
                match = ZERO_FIELD_RE.match(line)
                if not match:
                    continue
                field = match.group("field")
                if field.startswith("m_") or field in allowed_zero_fields:
                    continue
                severity = "ERROR" if strict_zero_fields else "WARN"
                findings.append(
                    Finding(
                        severity,
                        path,
                        object_line_number(obj, local_index),
                        f"serialized field '{field}' is {{fileID: 0}}; verify it is optional",
                    )
                )

    visual_or_interactive_count = (
        image_count + tmp_text_count + legacy_text_count + button_count + slider_count
    )
    if transform_ids and visual_or_interactive_count == 0:
        findings.append(
            Finding(
                "ERROR",
                path,
                0,
                "RectTransform-only skeleton detected; prefab has no Image/Button/TMP/Slider UI components",
            )
        )
    if transform_ids and canvas_renderer_count == 0:
        findings.append(
            Finding(
                "ERROR",
                path,
                0,
                "prefab has RectTransforms but no CanvasRenderer components",
            )
        )
    if legacy_text_count:
        findings.append(
            Finding(
                "ERROR",
                path,
                0,
                f"legacy Unity Text components detected ({legacy_text_count}); use TextMeshProUGUI",
            )
        )
    if len(repeated_instance_names) >= 2 and prefab_instance_count == 0:
        sample = ", ".join(repeated_instance_names[:6])
        findings.append(
            Finding(
                "ERROR",
                path,
                0,
                "repeated-looking UI objects found but no nested PrefabInstance "
                f"objects exist; use child prefab instances instead of expanded copies ({sample})",
            )
        )
    for button in buttons_without_callbacks:
        findings.append(
            Finding(
                "ERROR",
                path,
                button.start_line,
                "Button component has no wired OnClick method",
            )
        )
    if any(name.startswith("Script_") for name in game_object_names_by_id.values()):
        for game_object_id, name in game_object_names_by_id.items():
            if not name.startswith("Script_"):
                continue
            component_ids = component_ids_by_game_object.get(game_object_id, [])
            has_script = any(
                component_class_by_id.get(component_id) == CLASS_MONO_BEHAVIOUR
                and component_script_guid_by_id.get(component_id) not in KNOWN_UI_GUIDS
                for component_id in component_ids
            )
            if not has_script:
                findings.append(
                    Finding(
                        "ERROR",
                        path,
                        0,
                        f"script-owner GameObject '{name}' has no project script component",
                    )
                )
    elif script_component_count == 0:
        findings.append(
            Finding(
                "WARN",
                path,
                0,
                "no Script_* owner or project script component found; confirm this prefab is intentionally visual-only",
            )
        )

    roots = {tid for tid in transform_ids if parent_by_transform.get(tid, 0) == 0}
    if not roots and transform_ids:
        findings.append(Finding("ERROR", path, 0, "no local transform root found"))

    reachable: set[int] = set()
    stack = list(roots)
    while stack:
        current = stack.pop()
        if current in reachable:
            continue
        reachable.add(current)
        stack.extend(children_by_transform.get(current, set()) - reachable)

    unreachable = sorted(transform_ids - reachable)
    for file_id in unreachable[:20]:
        findings.append(
            Finding(
                "WARN",
                path,
                0,
                f"transform fileID {file_id} is not reachable from a local root",
            )
        )
    if len(unreachable) > 20:
        findings.append(
            Finding(
                "WARN",
                path,
                0,
                f"{len(unreachable) - 20} more transforms are not reachable from a local root",
            )
        )

    return findings


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Validate Unity prefab YAML for TArena UI mockups."
    )
    parser.add_argument("prefabs", nargs="+", type=Path)
    parser.add_argument(
        "--strict-zero-fields",
        action="store_true",
        help="treat MonoBehaviour {fileID: 0} serialized fields as errors",
    )
    parser.add_argument(
        "--allow-zero-field",
        action="append",
        default=[],
        help="serialized field name that may intentionally remain {fileID: 0}",
    )
    parser.add_argument(
        "--script-dir",
        action="append",
        type=Path,
        default=[],
        help="generated script directory to check for missing .cs.meta files",
    )
    args = parser.parse_args(argv)

    all_findings: list[Finding] = []
    exit_code = 0
    allowed_zero_fields = set(args.allow_zero_field)

    for prefab in args.prefabs:
        if not prefab.exists():
            all_findings.append(Finding("ERROR", prefab, 0, "file does not exist"))
            continue
        all_findings.extend(
            validate_prefab(
                prefab,
                strict_zero_fields=args.strict_zero_fields,
                allowed_zero_fields=allowed_zero_fields,
            )
        )

    for script_dir in args.script_dir:
        all_findings.extend(validate_script_meta(script_dir))

    for finding in all_findings:
        print(finding.format())
        if finding.severity == "ERROR":
            exit_code = 1

    checked_count = len(args.prefabs)
    error_count = sum(1 for finding in all_findings if finding.severity == "ERROR")
    warn_count = sum(1 for finding in all_findings if finding.severity == "WARN")
    if error_count == 0:
        print(f"OK: checked {checked_count} prefab(s), {warn_count} warning(s)")
    else:
        print(
            f"FAILED: checked {checked_count} prefab(s), "
            f"{error_count} error(s), {warn_count} warning(s)"
        )

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
