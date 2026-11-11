"""Checks that can run without Unity, Android tools or a headset."""

import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parent.parent
PINS = {
    "com.meta.xr.sdk.all": "207.0.0",
    "com.unity.xr.hands": "1.7.2",
    "com.unity.xr.management": "4.5.3",
    "com.unity.xr.openxr": "1.17.0",
    "com.unity.test-framework": "1.4.6",
}


def git(*args, **kwargs):
    return subprocess.run(
        ["git", "-C", str(ROOT), *args], check=True, capture_output=True, **kwargs
    ).stdout


def require(condition, message):
    if not condition:
        raise ValueError(message)


def unique_keys(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"Duplicate JSON key: {key}")
        result[key] = value
    return result


GATE = ["PALMFOLK_DEVELOPMENT"]


def check_release_gate(sources):
    """T10: a release build compiles out recording and every developer tool. Every assembly in a Developer folder
    carries the development define constraint, nothing outside them references one, and no shipped source names a
    developer type outside a PALMFOLK_DEVELOPMENT block."""
    asmdefs = {}
    for path in sorted((ROOT / "Assets/Palmfolk").rglob("*.asmdef")):
        asmdefs[path.parent] = json.loads(path.read_text(), object_pairs_hook=unique_keys)
    gated = set()
    for folder, asmdef in asmdefs.items():
        if "Developer" in folder.relative_to(ROOT).parts:
            require(asmdef.get("defineConstraints") == GATE, f"Developer assembly must use the single development gate: {asmdef['name']}")
            gated.add(asmdef["name"])
        else:
            require("PALMFOLK_DEVELOPMENT" not in asmdef.get("defineConstraints", []), f"Gated assembly outside a Developer folder: {asmdef['name']}")
    for asmdef in asmdefs.values():
        if asmdef["name"] in gated:
            continue
        leaked = gated & set(asmdef.get("references", []))
        require(not leaked, f"{asmdef['name']} references developer assemblies {sorted(leaked)}; release builds would not compile")

    def owner(name):
        folder = (ROOT / name).parent
        while folder not in asmdefs and folder != ROOT:
            folder = folder.parent
        return asmdefs.get(folder)

    developer_types, shipped = set(), []
    for name in sources:
        if not name.startswith("Assets/Palmfolk/"):
            continue
        asmdef = owner(name)
        if asmdef is None and "Editor" in Path(name).parts:
            continue  # Unity's special Editor folder: an editor-only assembly, never in a player build
        require(asmdef is not None, f"Source outside any assembly: {name}")
        if "Developer" in Path(name).parts:
            require(asmdef["name"] in gated, f"Developer source compiled into an ungated assembly: {name}")
        if asmdef["name"] in gated:
            text = (ROOT / name).read_text(encoding="utf-8")
            developer_types |= set(re.findall(r"\b(?:class|struct|interface|enum)\s+([A-Z]\w*)", text))
        elif "Editor" not in asmdef.get("includePlatforms", []):
            shipped.append(name)
    require(developer_types, "No developer types found; the gate check is looking in the wrong place")
    pattern = re.compile(r"\b(" + "|".join(sorted(developer_types)) + r")\b")
    for name in shipped:
        text = (ROOT / name).read_text(encoding="utf-8")
        text = re.sub(r"#if PALMFOLK_DEVELOPMENT\b.*?#endif", "", text, flags=re.DOTALL)
        text = re.sub(r"//[^\n]*|/\*.*?\*/", "", text, flags=re.DOTALL)
        text = re.sub(r'"(?:\\.|[^"\\])*"', '""', text)
        found = pattern.search(text)
        require(found is None, f"Shipped source names developer type {found.group(1) if found else ''}: {name}")
    print(f"PASS: release gate ({len(gated)} developer assemblies, {len(developer_types)} developer types absent from {len(shipped)} shipped sources)")


def main():
    files = sorted(set(git("ls-files", "-z", "--cached", "--others", "--exclude-standard").decode().split("\0")) - {""})
    manifest = json.loads((ROOT / "Packages/manifest.json").read_text(), object_pairs_hook=unique_keys)
    for name, version in PINS.items():
        require(manifest["dependencies"].get(name) == version, f"Unexpected package pin: {name}")
    version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()
    require("m_EditorVersion: 6000.0.66f2" in version.splitlines(), "Unity version must be pinned to 6000.0.66f2")
    print("PASS: Unity and SDK version pins")

    generated = {"library", "temp", "obj", "bin", "build", "builds", "logs", "usersettings", "playtest-logs", "testresults", ".venv", "__pycache__"}
    forbidden_suffixes = {".apk", ".aab", ".keystore", ".jks", ".key", ".pem", ".p12", ".pfx", ".pyc"}
    for name in files:
        path = Path(name)
        require(not (set(part.lower() for part in path.parts) & generated), f"Generated/private directory must not be tracked: {name}")
        require(path.suffix.lower() not in forbidden_suffixes and not path.name.startswith(".env"), f"Private/build artifact must not be tracked: {name}")
    probes = ["Builds/test.apk", "test.keystore", "test.jks", "signing.key", "signing.pem", "signing.p12", "signing.pfx", ".env", ".env.local", "playtest-logs/session.json", "Library/test", "UserSettings/test", ".venv/test", "tools/__pycache__/test.pyc"]
    # Bytes, not text: on Windows, text mode sends CRLF and git then matches nothing.
    ignored = set(git("check-ignore", "--no-index", "--stdin", input=("\n".join(probes) + "\n").encode()).decode().splitlines())
    require(ignored == set(probes), "Missing ignore rules: " + ", ".join(sorted(set(probes) - ignored)))
    print("PASS: build outputs, private files and local tooling stay out of Git")

    guids = {}
    for name in files:
        path = ROOT / name
        if not name.startswith("Assets/"):
            continue
        if path.suffix == ".meta":
            match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(), re.MULTILINE)
            require(match is not None, f"Invalid Unity GUID: {name}")
            guid = match.group(1)
            require(guid not in guids, f"Duplicate Unity GUID: {name} and {guids.get(guid)}")
            guids[guid] = name
            require(path.with_suffix("").exists(), f"Orphan .meta file: {name}")
        else:
            require(Path(str(path) + ".meta").is_file(), f"Missing Unity .meta file: {name}")
            parent = path.parent
            while parent != ROOT / "Assets":
                require(Path(str(parent) + ".meta").is_file(), f"Missing folder .meta file: {parent.relative_to(ROOT)}")
                parent = parent.parent
    print("PASS: Unity asset metadata and unique GUIDs")

    # The owner controls these documents. Intentional changes require updating
    # the baseline in the same reviewed change; this script never edits them.
    frozen = {
        "docs/build-plan.md": "d6d43d6f12e828eefae293cfdc694bc9398e209fb970ffd6dc29a37bbf80ddd3",
        "docs/requirements-v1.2.md": "c3f7715c365a9b34b83394b8446fbdfeed39e098723b9ee0297a0b84b722638c",
        "docs/requirements-v1.3.md": "ad695cf007c37fe0f87344befe978a07a8951606f662da4c52a6a5fa2b92b44c",
        "docs/requirements-v1.3-quality.md": "52e225bfc106bf993b26a8a9e8304f1a66afcc9cd81b101b19dea929a94fa89d",
    }
    for name, expected in frozen.items():
        require(hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, f"Owner-controlled document changed: {name}")
    print("PASS: owner-controlled plan and requirements unchanged")

    try:
        from tree_sitter import Language, Parser
        import tree_sitter_c_sharp
    except ImportError as error:
        raise ValueError("Install tools/requirements-checks.txt before running source checks") from error
    parser = Parser(Language(tree_sitter_c_sharp.language()))
    sources = [name for name in files if name.endswith(".cs")]
    for name in sources:
        tree = parser.parse((ROOT / name).read_bytes())
        require(not tree.root_node.has_error, f"C# syntax error: {name}")
    print(f"PASS: {len(sources)} C# files parsed (syntax only)")
    core = json.loads((ROOT / "Assets/Palmfolk/Core/Palmfolk.Core.asmdef").read_text())
    require(core.get("noEngineReferences") is True and not core.get("references"), "Core must have no engine/SDK assembly references")
    developer = json.loads((ROOT / "Assets/Palmfolk/Developer/Palmfolk.Developer.asmdef").read_text())
    require(developer.get("defineConstraints") == ["PALMFOLK_DEVELOPMENT"], "Developer assembly must use the single development gate")
    for name in sources:
        if name.startswith("Assets/Palmfolk/Core/"):
            text = (ROOT / name).read_text()
            require(not re.search(r"\b(UnityEngine|UnityEditor|PhysX|OVRPlugin|Stopwatch)\b|DateTime\.(Now|UtcNow)|Environment\.TickCount", text), f"Engine/wall-clock dependency in Core: {name}")
    print("PASS: engine-independent core and developer assembly gate")
    check_release_gate(sources)
    print("Source checks passed. Unity compilation, APK output and Quest 2 behavior are not verified.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError, subprocess.CalledProcessError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)
