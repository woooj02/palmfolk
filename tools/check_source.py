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


def main():
    files = sorted(set(git("ls-files", "-z", "--cached", "--others", "--exclude-standard").decode().split("\0")) - {""})
    manifest = json.loads((ROOT / "Packages/manifest.json").read_text(), object_pairs_hook=unique_keys)
    for name, version in PINS.items():
        require(manifest["dependencies"].get(name) == version, f"Unexpected package pin: {name}")
    version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()
    require("m_EditorVersion: 6000.0.66f2" in version.splitlines(), "Unity version must be pinned to 6000.0.66f2")
    print("PASS: Unity and SDK version pins")

    generated = {"library", "temp", "obj", "build", "builds", "logs", "usersettings", "playtest-logs", ".venv", "__pycache__"}
    forbidden_suffixes = {".apk", ".aab", ".keystore", ".jks", ".key", ".pem", ".p12", ".pfx", ".pyc"}
    for name in files:
        path = Path(name)
        require(not (set(part.lower() for part in path.parts) & generated), f"Generated/private directory must not be tracked: {name}")
        require(path.suffix.lower() not in forbidden_suffixes and not path.name.startswith(".env"), f"Private/build artifact must not be tracked: {name}")
    probes = ["Builds/test.apk", "test.keystore", "test.jks", "signing.key", "signing.pem", "signing.p12", "signing.pfx", ".env", ".env.local", "playtest-logs/session.json", "Library/test", "UserSettings/test", ".venv/test", "tools/__pycache__/test.pyc"]
    ignored = set(git("check-ignore", "--no-index", "--stdin", input="\n".join(probes) + "\n", text=True).splitlines())
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
        "docs/build-plan.md": "e1f7413e6a86682f094cdcb20f2ada4bcf9ebc057ac15cdab62fc2079e2198bb",
        "docs/requirements-v1.2.md": "c3f7715c365a9b34b83394b8446fbdfeed39e098723b9ee0297a0b84b722638c",
    }
    for name, expected in frozen.items():
        require(hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, f"Owner-controlled document changed: {name}")
    print("PASS: original plan and requirements unchanged")

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
    print("Source checks passed. Unity compilation, APK output and Quest 2 behavior are not verified.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, KeyError, subprocess.CalledProcessError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        sys.exit(1)
