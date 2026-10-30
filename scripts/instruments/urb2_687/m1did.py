# (687) 線 urb2 後半・第 3 版：M1 の記録（州全体の都市の割合 u＝1−ruralPopulationK/populationK の差 Δu と、助走の跳びを引いた差の差）。読むだけ。
# 使い方：python m1did.py <基線の ins> <腕の ins> [種の数=10] [種の開始=1]
# Δu(t)＝腕−基線（同じ種・同じ州・各十年の最初の四半期〔year%10==0 かつ quarter%4==0〕）。Δu(Q0)＝同じ州の quarter==0 の行（助走の直後）の差。
# DiD(t)＝Δu(t)−Δu(Q0)。種ごとに州×十年の中央値。k_urb_delta.py（igi3_657）の写しを元にし、Q0 の行を別に拾う。
import sys, csv, os, statistics
a, b = sys.argv[1], sys.argv[2]; ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10; s0 = int(sys.argv[4]) if len(sys.argv) > 4 else 1
def load(d, s):
    out = {}; q0 = {}
    with open(f"{d}/region_s{s}.tsv", encoding="utf-8") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        iq, iy, ina, ire, iac, ip, ir = [h.index(x) for x in ("quarter", "year", "nation", "region", "activeFlag", "populationK", "ruralPopulationK")]
        for row in r:
            if row[iac] != "1": continue
            q = int(row[iq]); y = int(row[iy])
            try: pp = float(row[ip]); u = 1 - float(row[ir]) / pp if pp > 0 else None
            except: continue
            if u is None: continue
            if q == 0: q0[(row[ina], row[ire])] = u
            if q % 4 == 0 and y % 10 == 0: out[(y, row[ina], row[ire])] = u
    return out, q0
pl = []; dd = []; q0m = []
for s in range(s0, s0 + ns):
    if not (os.path.exists(f"{a}/region_s{s}.tsv") and os.path.exists(f"{b}/region_s{s}.tsv")): print("missing", s); pl.append(float("nan")); dd.append(float("nan")); continue
    A, A0 = load(a, s); B, B0 = load(b, s)
    d = [B[k] - A[k] for k in A if k in B]
    did = [(B[k] - A[k]) - (B0[(k[1], k[2])] - A0[(k[1], k[2])]) for k in A if k in B and (k[1], k[2]) in A0 and (k[1], k[2]) in B0]
    q0d = [B0[k] - A0[k] for k in A0 if k in B0]
    p = statistics.median(d) if d else float("nan"); q = statistics.median(did) if did else float("nan"); z = statistics.median(q0d) if q0d else float("nan")
    pl.append(p); dd.append(q); q0m.append(z)
    print(f"s{s} n {len(d)} median_du {p:+.4f} median_did {q:+.4f} median_du_Q0 {z:+.4f} (n_did {len(did)})")
print("median_of_medians du", f"{statistics.median(pl):+.4f}", " did", f"{statistics.median(dd):+.4f}", " Q0", f"{statistics.median(q0m):+.4f}")
print("max|median_du|", f"{max(abs(x) for x in pl):.4f}", " max|median_did|", f"{max(abs(x) for x in dd):.4f}")
