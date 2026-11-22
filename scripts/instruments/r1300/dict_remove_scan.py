# (1300) 設計の材料 sv：セーブに載る辞書と集合の欄のうち、Remove か Clear を受ける物を数える（読むだけ・コミットしない・合流が scripts/instruments/r1300/ へ写す）
#   python -X utf8 -I dict_remove_scan.py <SimCore の木> <欄の一覧（grep の出力）> <出力 tsv>
#   欄の一覧の作り方：grep -rnP "^\s*(public|internal|private|protected)[^(=]*\b(Dictionary|HashSet|SortedDictionary)<[^;(]*>\??\s+(\w+)\s*(=|;|\{)" --include=*.cs <木> | grep -v "/Instruments/\|static\|NonSerialized"
#   限界：名前だけで数える（同じ名前の別の欄も数える）・Clear は空き枠を消すので列挙の順を壊さない（Remove だけが読み込みの後の順を変える）
import os, re, sys

def main():
    root, listing, out = sys.argv[1], sys.argv[2], sys.argv[3]
    src = {}
    for d, _, fs in os.walk(root):
        if any(x in d for x in ("Instruments", os.sep + "obj", os.sep + "bin")):
            continue
        for f in fs:
            if f.endswith(".cs"):
                p = os.path.join(d, f)
                with open(p, "rb") as fh:
                    src[p] = fh.read().decode("utf-8", "replace")
    with open(listing, "rb") as fh:
        rows = fh.read().decode("utf-8", "replace").splitlines()
    res = []
    for r in rows:
        m = re.search(r"(Dictionary|HashSet|SortedDictionary)<.*?>\??\s+(\w+)\s*(=|;|\{)", r)
        if not m:
            continue
        name, kind = m.group(2), m.group(1)
        rem = sum(len(re.findall(r"\b" + re.escape(name) + r"\s*\.\s*Remove\s*\(", s)) for s in src.values())
        clr = sum(len(re.findall(r"\b" + re.escape(name) + r"\s*\.\s*(Clear|RemoveWhere)\s*\(", s)) for s in src.values())
        lm = re.match(r"(.*?\.cs):(\d+):", r)
        loc = f"{os.path.basename(lm.group(1))}:{lm.group(2)}" if lm else "?"
        res.append(f"{kind}\t{name}\tRemove={rem}\tClear={clr}\t{loc}")
    with open(out, "wb") as fh:
        fh.write(("\n".join(res) + "\n").encode("utf-8"))
    print(len(res), "欄")
    for x in res:
        if "Remove=0" not in x:
            print(x)

if __name__ == "__main__":
    main()
