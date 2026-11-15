# (1162) 読むだけ：国の兵が 0 の国×四半期（nation_s soldierK）と徴兵の法（monlaw_s ConscriptionLaw）を数える。人口 10 千人以上の国だけ。
import sys, os, csv
from collections import defaultdict
RAW, S0, S1, OUT = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
def rd(p):
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x
cnt = defaultdict(int); runs = defaultdict(int); natn = defaultdict(int)
for s in range(S0, S1 + 1):
    law = {}
    for ix, x in rd(os.path.join(RAW, f"monlaw_s{s}.tsv")): law[(int(x[ix["quarter"]]), x[ix["nation"]])] = int(float(x[ix["ConscriptionLaw"]]))
    ser = defaultdict(list)
    for ix, x in rd(os.path.join(RAW, f"nation_s{s}.tsv")):
        try: pop = float(x[ix["populationK"]])
        except ValueError: pop = float("nan")
        ser[x[ix["nation"]]].append((int(x[ix["quarter"]]), float(x[ix["soldierK"]]), pop))
    for nat, v in ser.items():
        v.sort(); run = 0; best = 0
        for q, sk, pop in v:
            if not (pop >= 10): continue
            cnt["国×四半期"] += 1
            if not (sk > 1e-9):
                cnt["兵 0"] += 1; cnt[f"兵 0×法={law.get((q, nat), -1)}"] += 1; run += 1; best = max(best, run)
            else: run = 0
        if best >= 40: runs["兵 0 が 40 四半期以上続いた国"] += 1; natn[nat] += 1
with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1162) 国の兵 0 生={RAW} 種={S0}〜{S1}（法 0＝志願・1＝選抜徴兵・2＝国民皆兵・-1＝monlaw に行が無い）\n")
    for k in sorted(cnt): o.write(f"{k}\t{cnt[k]}\n")
    for k in sorted(runs): o.write(f"{k}\t{runs[k]}\n")
    for n, c in sorted(natn.items(), key=lambda z: -z[1])[:15]: o.write(f"{n}\t{c}\n")
