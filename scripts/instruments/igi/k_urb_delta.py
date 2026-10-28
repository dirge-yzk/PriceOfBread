# (619) 線 igi：都市の割合の動きの向き（読むだけ）。州×十年（各十年の最初の四半期）の u＝1−ruralPopulationK/populationK を、基線と腕で同じ種・同じ州どうし引いた Δu の中央値（種ごと）。
# 使い方：python k_urb_delta.py <基線の ins> <腕の ins> [種の数=10]   （両方に active な州×十年だけ）
import sys, csv, os, statistics
a, b = sys.argv[1], sys.argv[2]; ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def load(d, s):
    p = f"{d}/region_s{s}.tsv"; out = {}
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        iq, iy, ina, ire, iac, ip, ir = [h.index(x) for x in ('quarter', 'year', 'nation', 'region', 'activeFlag', 'populationK', 'ruralPopulationK')]
        for row in r:
            if row[iac] != '1' or int(row[iq]) % 4 != 0 or int(row[iy]) % 10 != 0: continue
            try: pp = float(row[ip]); out[(int(row[iy]), row[ina], row[ire])] = 1 - float(row[ir]) / pp if pp > 0 else None
            except: pass
    return out
meds = []; dropped = 0; total = 0
for s in range(1, ns + 1):
    if not (os.path.exists(f"{a}/region_s{s}.tsv") and os.path.exists(f"{b}/region_s{s}.tsv")): print("missing", s, "(不合格に数える)"); continue
    A, B = load(a, s), load(b, s)
    d = [B[k] - A[k] for k in A if k in B and A[k] is not None and B[k] is not None]
    total += len(A); dropped += len(A) - len(d)
    m = statistics.median(d) if d else float('nan'); meds.append(m)
    print(f"s{s} n {len(d)} median_du {m:+.4f} mean_du {sum(d)/max(1,len(d)):+.4f}")
print("seeds_pos", sum(1 for m in meds if m > 0), "of", ns, "median_of_medians", f"{statistics.median(meds):+.4f}" if meds else "-")
print("seeds_gt_0.005", sum(1 for m in meds if m > 0.005), "of", ns, "(分母は固定・欠けた種は不合格・Kc1 は 8 以上)")
print("dropped_state_decades", dropped, "of", total, "(基線にあって腕で組めなかった州×十年・持ち主の変更など)")
