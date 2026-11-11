# (1038) 紙の台本 0：基線（keep_1035/base_ins）で、侵略した国のその四半期の余剰 S＝sumSC/calls（S_C'＝landPotential×(1−b)−need）の符号を数える
import sys, csv, os
root = sys.argv[1]
tot = dict(inv=0, invS=0, calls=0, callsS=0, callsSh=0, callsShS=0)
for s in range(1, 11):
    we = {}
    with open(os.path.join(root, f"warentry_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            c = int(row[ix["calls"]])
            if c <= 0: continue
            S = float(row[ix["sumSC"]]) / c
            we[(int(row[ix["quarter"]]), row[ix["nation"]])] = S
            tot["calls"] += c; tot["callsS"] += c if S > 0 else 0
            sh = int(row[ix["sh_calls"]]); tot["callsSh"] += sh; tot["callsShS"] += sh if S > 0 else 0
    inv = invS = 0
    with open(os.path.join(root, f"invade_s{s}.tsv"), encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            q = int(row[ix["q"]]); S = we.get((q, row[ix["invader"]]))
            if S is None: S = we.get((q - 1, row[ix["invader"]]))
            inv += 1; invS += 1 if (S is not None and S > 0) else 0
    print(f"seed {s}: invade={inv} invadeWithSpos={invS}")
    tot["inv"] += inv; tot["invS"] += invS
print("total", " ".join(f"{k}={v}" for k, v in tot.items()))
