# (1037) c98lw 門の台本（読むだけ）：lwk_s（計器つきの腕の生）から G0〜G5 を読む。事前登録 measurements/m1037_20261111_prereg_lw.txt の [3] と同じ定義。
# 使い方: python gates_lw.py <計器の生のフォルダ（lwk_s*.tsv がある）>
# b_eng＝(workEngine÷initialWorkEngine)÷(workOld÷initialWorkOldShadow)  ← エンジンが使った能力の比を、同じ盤面の旧の規則の比で割る
# b_pap＝(workNew÷initialWorkNewShadow)÷(workOld÷initialWorkOldShadow)  ← 計器が自分で数えた紙の式（形 A）
import sys, glob, math, statistics

TOL = 1e-3
d = sys.argv[1]
files = sorted(glob.glob(d + '/lwk_s*.tsv'))
W, W_all, P, E = [], [], [], []
n_rows = 0; bad_m = 0; over_m = 0; nan_b = 0; m_hist = [0] * 11
for p in files:
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t'); n_rows += 1
            def g(k):
                try: return float(c[ix[k]])
                except ValueError: return float('nan')
            role = int(c[ix['role']]); m = g('mShare'); m0 = g('m0Share'); clr = g('clrShare')
            if not (m >= 0) or not math.isfinite(m): bad_m += 1; continue
            if m > 1 + 1e-9: over_m += 1
            if role & 1: m_hist[min(10, int(m * 10))] += 1
            wo, wn, we = g('workOldIndex'), g('workNewIndex'), g('workEngineIndex')
            ie, io, inn = g('initialWorkEngineIndex'), g('initialWorkOldShadowIndex'), g('initialWorkNewShadowIndex')
            if not (wo > 0 and ie > 0 and io > 0 and inn > 0): continue
            be = (we / ie) / (wo / io); bp = (wn / inn) / (wo / io)
            if not (math.isfinite(be) and math.isfinite(bp)): nan_b += 1; continue
            rec = (be, bp, clr, m, c[ix['year']], c[ix['nation']])
            if role & 1:
                if m >= 0.8 and m0 <= 0.1: W.append(rec)
            elif role & 2:
                if m >= 0.5: P.append(rec)
            elif role & 4:
                E.append(rec)

def agree_share(rows):
    if not rows: return float('nan'), 0
    ok = sum(1 for r in rows if abs(r[0] / r[1] - 1.0) <= TOL)
    return ok / len(rows), len(rows)

print(f'files={len(files)} rows={n_rows} m 非有限か負={bad_m} m>1={over_m} b 非有限={nan_b}')
print('工員の m の分布（0.1 刻み・最後は 1.0 以上）：' + ' '.join(str(x) for x in m_hist))
disc = [r for r in W if abs(r[1] - 1.0) >= 0.03]
s1, n1 = agree_share(disc)
print(f'G1 工員 m≥0.8・m0≤0.1・|b_pap−1|≥0.03：一致の割合 {s1:.4f}（N={n1}）門＝0.999 以上・N≥200 → {"通る" if n1 >= 200 and s1 >= 0.999 else "落ちる"}')
med = statistics.median([r[0] for r in W]) if W else float('nan')
print(f'G2 工員 m≥0.8・m0≤0.1 の b_eng の中央値 {med:.4f}（N={len(W)}）門＝1.08〜1.32・N≥200 → {"通る" if len(W) >= 200 and 1.08 <= med <= 1.32 else "落ちる"}')
if W:
    qs = statistics.quantiles([r[0] for r in W], n=10)
    print('   b_eng の十分位：' + ' '.join(f'{q:.3f}' for q in qs))
    print(f'   b_pap の中央値 {statistics.median([r[1] for r in W]):.4f}')
s3, n3 = agree_share(P)
print(f'G3 農民（労働力を売らない）州の m≥0.5：一致の割合 {s3:.4f}（N={n3}）門＝0.999 以上・N≥200 → {"通る" if n3 >= 200 and s3 >= 0.999 else "落ちる"}')
if P: print(f'   農民の b_eng の中央値 {statistics.median([r[0] for r in P]):.4f}・b_pap の中央値 {statistics.median([r[1] for r in P]):.4f}（記録）')
a = [r for r in disc if r[2] < 0.01]; s4a, n4a = agree_share(a)
print(f'G4a G1 の母集合で児童労働 0.01 未満：一致の割合 {s4a:.4f}（N={n4a}）門＝0.999 以上・N≥50（未満なら記録）→ {"記録（母集合不足）" if n4a < 50 else ("通る" if s4a >= 0.999 else "落ちる")}')
b = [r for r in disc if r[2] >= 0.3]; s4b, n4b = agree_share(b)
print(f'G4b G1 の母集合で児童労働 0.3 以上：一致の割合 {s4b:.4f}（N={n4b}）門＝0.999 以上・N≥200 → {"通る" if n4b >= 200 and s4b >= 0.999 else "落ちる"}')
s5 = sum(1 for r in E if abs(r[0] - 1.0) <= 1e-6) / len(E) if E else float('nan')
print(f'G5 技師（高度労働力だけを売る）b_eng＝1：割合 {s5:.4f}（N={len(E)}）門＝0.999 以上・N≥200 → {"通る" if len(E) >= 200 and s5 >= 0.999 else "落ちる"}')
