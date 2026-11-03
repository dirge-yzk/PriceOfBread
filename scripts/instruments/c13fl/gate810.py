# (810) c13fl：門 G2〜G6 の読み手（読むだけ）。事前登録 measurements/m810_20261103_prereg_fl.txt [5]（G1 は r794/inv_income.py・G7 は m808/gdp_zero10.py を別に回す）。
# 使い方：python -X utf8 -I gate810.py <ins フォルダ> <種 1-10 か 1,2> [--base <旗 0 の ins フォルダ> <種>]
#   divhost_s の rentMonths==3 の行（同じ四半期・同じ国の最後の呼び出し）だけを読む。divworld_s は四半期の最後の行。
#   G2 落とした請求権の一致：u＝divReq−paid＞1e-9×max(1,fci) の行で r＝(fci−fciAfter)÷min(u,fci) ∈[0.999,1.001]
#   G3 単位のずれ：fci>0 かつ rentMonth3>0 の行で 請求÷fci＞0.02 の割合 ≦0.05（記録：p99・最大・0.25 超の数）
#   G4 (a) の形の一致：fci>0 の行で |divReq−divH| ≦1e-9×max(divReq,divH,1e-300)
#   G5 (c) の形の一致：paid>0 かつ fci>0 の行で |retDelta−4×paid÷fci×cred×PolicyRateCapitalDraw(1.0)| ≦1e-9×項＋1e-12（★追記 1）
#   G6 流入の全停止：Σ newInGold（1850〜1949・種の平均）÷旗 0 ≧0.2（--base が要る）
#   記録：Σ 払い・Σ 未払い・Σ 落とした請求権（金建て・種の平均・10 年ごと）・台帳の差 Δ(Σabroad−Σfci) が 1e-9×Σfci を超える四半期の数・世界の Σfci（金建て）の 1850／1900／1949
import sys, csv, os, collections
PRD = 1.0   # PolicyRateCapitalDraw の既定（係数の上書きがある腕では読み手に渡す必要がある＝この線の腕は既定）
def seeds_of(spec): return list(range(int(spec.split('-')[0]), int(spec.split('-')[1]) + 1)) if '-' in spec else [int(x) for x in spec.split(',')]
def read_rows(d, s):
    last = {}
    with open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: last[(row[ix['quarter']], row[ix['nation']])] = row
    return ix, last
def read_world(d, s):
    p = os.path.join(d, f'divworld_s{s}.tsv'); last = {}
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: last[row[ix['quarter']]] = row
    return ix, last
def newin(d, seeds):
    tot = 0.0; dep = 0.0; ok = True
    for s in seeds:
        w = read_world(d, s)
        if w is None: ok = False; continue
        ix, last = w
        for q, row in last.items():
            y = int(row[ix['year']])
            if 1850 <= y <= 1949: tot += float(row[ix['newInGold']]) / len(seeds); dep += float(row[ix['depositGold']]) / len(seeds)
    return tot, dep, ok
d = sys.argv[1]; seeds = seeds_of(sys.argv[2])
base = None
if '--base' in sys.argv:
    k = sys.argv.index('--base'); base = (sys.argv[k + 1], seeds_of(sys.argv[k + 2]))
g2n = g2v = 0; g2min = float('inf'); g2max = 0.0
g3n = g3o = 0; rates = []; over25 = 0
g4n = g4v = 0; g4max = 0.0
g5n = g5v = 0; g5max = 0.0
dec = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
ledger_bad = 0; ledger_n = 0; fsum = collections.defaultdict(float)
worst2 = []
for s in seeds:
    ix, last = read_rows(d, s)
    for (q, nat), row in last.items():
        if int(float(row[ix['rentMonths']])) != 3: continue
        e = float(row[ix['fxRate']]); rent3 = float(row[ix['rentMonth3']]); fci = float(row[ix['fci']])
        req = float(row[ix['divReqIndex']]); paid = float(row[ix['paidIndex']]); fa = float(row[ix['fciAfterIndex']])
        dh = float(row[ix['divHIndex']]); cred = float(row[ix['creditShare']]); rd = float(row[ix['retDeltaIndex']]); y = int(row[ix['year']])
        u = req - paid
        if u > 1e-9 * max(1.0, fci) and fci > 0:
            g2n += 1; r = (fci - fa) / min(u, fci)
            g2min = min(g2min, r); g2max = max(g2max, r)
            if not (0.999 <= r <= 1.001): g2v += 1; worst2.append((r, s, q, nat))
        if fci > 0 and rent3 > 0:
            g3n += 1; rr = req / fci; rates.append(rr)
            if rr > 0.02 * (1 + 1e-12): g3o += 1
            if rr > 0.25: over25 += 1
        if fci > 0:
            g4n += 1; m = max(req, dh, 1e-300); dv = abs(req - dh) / m
            g4max = max(g4max, dv)
            if dv > 1e-9: g4v += 1
        if paid > 0 and fci > 0:
            g5n += 1; ex = PRD * 4.0 * (paid / fci) * cred; dv = abs(rd - ex)
            g5max = max(g5max, dv / max(ex, 1e-300))
            if dv > 1e-9 * ex + 1e-12: g5v += 1
        a = dec[y // 10 * 10]; a[0] += paid * e / len(seeds); a[1] += max(0.0, u) * e / len(seeds); a[2] += max(0.0, fci - fa) * e / len(seeds)
    w = read_world(d, s)
    if w:
        wix, wl = w
        for q, row in wl.items():
            fs, fe, as_, ae = (float(row[wix[k]]) for k in ('fciStartGold', 'fciEndGold', 'abroadStartGold', 'abroadEndGold'))
            ledger_n += 1
            if abs((ae - fe) - (as_ - fs)) > 1e-9 * max(1.0, fe, ae): ledger_bad += 1
            y = int(row[wix['year']])
            if y in (1850, 1900, 1949): fsum[y] += fe / len(seeds)
rates.sort()
print(f'盤面 {d}・種 {seeds}')
print(f'G2 落とした請求権の一致：対象の行 {g2n}・外れ {g2v}・r の最小 {g2min if g2n else float("nan"):.6g}・最大 {g2max:.6g}  → {"通る" if g2n > 0 and g2v == 0 else "落ちる" if g2v > 0 else "対象なし"}')
for t in sorted(worst2)[:3] + sorted(worst2)[-3:]: print('   外れの例（r・種・四半期・国）', t)
if rates:
    m = len(rates)
    print(f'G3 単位のずれ：対象の行 {g3n}・請求÷fci＞0.02 の割合 {g3o / g3n:.4f}（≦0.05）  → {"通る" if g3o / g3n <= 0.05 else "落ちる"}｜記録 p50 {rates[m // 2]:.4g}・p99 {rates[m * 99 // 100]:.4g}・最大 {rates[-1]:.4g}・0.25 超 {over25}')
print(f'G4 (a) の形の一致：対象の行 {g4n}・外れ {g4v}・相対差の最大 {g4max:.3g}  → {"通る" if g4v == 0 and g4n > 0 else "落ちる"}')
print(f'G5 (c) の形の一致：対象の行 {g5n}・外れ {g5v}・相対差の最大 {g5max:.3g}  → {"通る" if g5v == 0 and g5n > 0 else "落ちる"}')
ni, dp, ok = newin(d, seeds)
if base:
    bni, bdp, bok = newin(base[0], base[1])
    ratio = ni / bni if bni > 0 else float('nan')
    print(f'G6 流入の全停止：新規の流入 1850〜1949（金建て・種の平均）{ni:.6g}÷旗 0 {bni:.6g}＝{ratio:.4f}（≧0.2）  → {"通る" if ratio >= 0.2 else "落ちる"}｜記録 預け {dp:.6g}（旗 0 {bdp:.6g}）')
else:
    print(f'G6（--base なし）：新規の流入 {ni:.6g}・預け {dp:.6g}')
print(f'記録：台帳の差 Δ(Σabroad−Σfci) が外れる四半期 {ledger_bad}／{ledger_n}・世界の Σfci（金建て・種の平均）' + '・'.join(f'{y} {fsum[y]:.4g}' for y in sorted(fsum)))
print('記録：年代\tΣ払い\tΣ未払い\tΣ落とした請求権（金建て・種の平均）')
for y in sorted(dec): print(f'  {y}\t{dec[y][0]:.4g}\t{dec[y][1]:.4g}\t{dec[y][2]:.4g}')
