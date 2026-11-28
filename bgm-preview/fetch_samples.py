"""Download only the CC0 recordings used in the arrangement."""

from concurrent.futures import ThreadPoolExecutor
from hashlib import sha1
import json
from pathlib import Path
import re
from urllib.parse import quote
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parent
COMMIT = "440300901dfe9275fd84e0b7763af1f8443ae62e"
REPOSITORY = "sgossner/VSCO-2-CE"
INSTRUMENTS = {
    "harp": ("Strings/Harp", r"_mf"),
    "pizzicato": ("Strings/Cello Section/pizzT", r"_v1_RR[12]"),
    "cello": ("Strings/Cello Section/susvib", r"_v1_1"),
    "viola": ("Strings/Viola Section/susvib", r"_v1_1"),
    "violin": ("Strings/Solo Violin/Arco Vib", r"_p"),
    "clarinet": ("Woodwinds/Clarinet/susLong", r"_v[12]_rr1_sum"),
    "flute": ("Woodwinds/Flute/susNV", r"_v1_1"),
}


def get(url):
    request = Request(url, headers={"User-Agent": "PriceOfBread-score"})
    with urlopen(request, timeout=30) as response:
        return response.read()


def blob_hash(content):
    return sha1(f"blob {len(content)}\0".encode() + content).hexdigest()


def fetch(entry, destination):
    if destination.exists() and blob_hash(destination.read_bytes()) == entry["sha"]:
        return
    source = quote(entry["path"])
    content = get(f"https://raw.githubusercontent.com/{REPOSITORY}/{COMMIT}/{source}")
    if blob_hash(content) != entry["sha"]:
        raise ValueError(f"Git blob mismatch for {source}")
    destination.write_bytes(content)


def main():
    tree_file = ROOT / ".vsco-tree.json"
    if not tree_file.exists():
        url = f"https://api.github.com/repos/{REPOSITORY}/git/trees/{COMMIT}?recursive=1"
        tree_file.write_bytes(get(url))
    index = json.loads(tree_file.read_text(encoding="utf-8-sig"))
    if index["sha"] != COMMIT or index.get("truncated"):
        raise ValueError("Unexpected VSCO 2 CE tree")

    entries = {entry["path"]: entry for entry in index["tree"]}
    samples = ROOT / ".samples"
    jobs = []
    for instrument, (folder, suffix) in INSTRUMENTS.items():
        pattern = re.compile(rf"{re.escape(folder)}/([^/]+{suffix}\.wav)$")
        directory = samples / instrument
        directory.mkdir(parents=True, exist_ok=True)
        for path, entry in entries.items():
            match = pattern.fullmatch(path)
            if match:
                jobs.append((entry, directory / match[1]))
    jobs.append((entries["LICENSE"], samples / "LICENSE"))
    with ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(lambda job: fetch(*job), jobs))
    print(f"Verified {len(jobs) - 1} VSCO 2 CE recordings")


if __name__ == "__main__":
    main()
