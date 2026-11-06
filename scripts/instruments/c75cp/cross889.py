# (889) c75cp 門 G2：fm_s の parliamentControlShare（四半期の頭の軍事費の施行で写した統制力）と crownpc_s の pcPrevShare（前の四半期の末の断面）の交差。
#   python -X utf8 -I cross889.py <生のフォルダ（crownpc_s<種>.tsv と fm_s<種>.tsv）> [--selftest]
#   照合＝fm_s の netIncreasingFlag が空でない行ごとに、同じ (quarter, nationUid) の crownpc_s の行（firstRowFlag=0）を探し、2 つの文字列が一致するか（同じ double は同じ G10 の文字列）。
#     文字列が違えば数値の差も出す（印字の桁の床で許さない＝一致は文字列の一致だけ）。最初の行（firstRowFlag=1）の四半期は照合しない。
#   --selftest＝同じ照合に pcPrev を 1e-6 ずらした値を渡し、不一致が 1 本以上出ることを確かめる（門が何でも通す形でないこと）。
#   出力：種ごとの 照合した行・一致・不一致・crownpc_s に行が無い fm_s の行。最後の行  G2 通過|不通過 。
import sys, os, glob, csv

csv.field_size_limit(1 << 30)


def rows(path):
    with open(path, encoding="utf-8", newline="") as f:
        return list(csv.DictReader(f, delimiter="\t"))


def cross(crown, fm, shift=0.0):
    m, first = {}, set()
    for r in crown:
        k = (r["quarter"], r["nationUid"])
        if r["firstRowFlag"] == "0":
            v = r["pcPrevShare"]
            m[k] = v if shift == 0.0 else repr(float(v) + shift)
        else:
            first.add(k)
    ok = bad = miss = 0; worst = 0.0
    for f in fm:
        if f["netIncreasingFlag"] == "" or (f["quarter"], f["nationUid"]) in first:
            continue
        k = (f["quarter"], f["nationUid"])
        if k not in m:
            miss += 1
            continue
        if m[k] == f["parliamentControlShare"]:
            ok += 1
        else:
            bad += 1
            try:
                worst = max(worst, abs(float(m[k]) - float(f["parliamentControlShare"])))
            except ValueError:
                worst = float("inf")
    return ok, bad, miss, worst


def main():
    d = sys.argv[1]
    self_test = "--selftest" in sys.argv
    seeds = sorted(int(os.path.basename(p)[len("crownpc_s"):-4]) for p in glob.glob(os.path.join(d, "crownpc_s*.tsv")))
    if not seeds:
        print("crownpc_s が無い"); print("G2 不通過"); return 1
    allok = True; tot = [0, 0, 0]
    for s in seeds:
        crown = rows(os.path.join(d, f"crownpc_s{s}.tsv")); fm = rows(os.path.join(d, f"fm_s{s}.tsv"))
        ok, bad, miss, worst = cross(crown, fm)
        tot[0] += ok; tot[1] += bad; tot[2] += miss
        line = f"seed{s}\t照合{ok + bad}\t一致{ok}\t不一致{bad}\t行なし{miss}\t最大差{worst:g}"
        if self_test:
            o2, b2, _, _ = cross(crown, fm, 1e-6)
            line += f"\t自己試験(1e-6)一致{o2}・不一致{b2}"
            if b2 == 0 or o2 != 0:
                allok = False; line += "\t自己試験失敗"
        if bad or miss or ok == 0:
            allok = False
        print(line)
    print(f"合計\t一致{tot[0]}\t不一致{tot[1]}\t行なし{tot[2]}")
    print("G2 通過" if allok else "G2 不通過")
    return 0 if allok else 1


if __name__ == "__main__":
    sys.exit(main())
