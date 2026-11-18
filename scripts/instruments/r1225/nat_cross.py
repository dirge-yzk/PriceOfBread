# (1225) 読むだけ：ideology_s の国の民族主義の取り分（ideoNationalNationalismShare）の 1790〜1871 の種の中央と、0.25 以上の行の割合（国ごと・種の範囲は引数・年末の行と picked の四半期の行を混ぜずに年末だけ＝quarter%4==3）
import sys, csv, collections, statistics
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); y0, y1 = int(sys.argv[4]), int(sys.argv[5])
per = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(s0, s1 + 1):
    with open(f"{d}/ideology_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            y = int(r["year"]); q = int(r["quarter"])
            if y < y0 or y > y1 or q % 4 != 3: continue
            try: v = float(r["ideoNationalNationalismShare"])
            except ValueError: continue
            per[r["nation"]][s].append(v)
names = sys.argv[6].split(",")
for nm in names:
    if nm not in per: print(nm, "none"); continue
    meds = [statistics.mean(v) for v in per[nm].values()]; ge = sum(sum(x >= 0.25 for x in v) for v in per[nm].values()); tot = sum(len(v) for v in per[nm].values())
    print(f"{nm}\tseeds={len(meds)}\tmean_med={statistics.median(meds):.3f}\tge025={ge}/{tot}")
