# (1037) c98lw 紙の台本（読むだけ・エンジンを呼ばない）：労働力の重み 形 A（手 女0.6・子0.35／機械の端 1.0）
# 使い方: python paper_lw.py [keep の計器の生のフォルダ（任意・factory_s と pop_s を読む）]
# 1) 形 A の b（＝エンジンの能力の比〔新の work÷新の基準〕÷〔旧の work÷旧の基準〕）の格子 → G2 の帯
# 2) 突然変異ごとに、代表の行で門 G1〜G5 が赤か緑か（㉚）
# 3) 旗 0（何もしない実装）で門が落ちるか
# 4) 盤面（任意）：工場の蒸気・電化の充足の年ごとの平均（factory_s）・工員の児童労働の割合（pop_s）
import sys, glob, collections, statistics

OLD = 0.75     # Coefficients.OldLaborFactor
CWF = 0.5      # Coefficients.ChildWorkFactor（旗 0 と技師）
FH, CH = 0.6, 0.35   # 手の重み（女・子）・機械の端は 1.0
# 年齢の割合（フランス 1783 の age_s1・男女半々＝(1033) と同じ AI の仮定）
A, O, C = 0.591, 0.049, 0.104

def work_terms(part, clr, wf, wc, H=1.0, a=A, o=O, c=C):
    men = (a / 2 + OLD * o / 2) * H
    fem = (a / 2 * part + OLD * o / 2 * part) * wf * H
    ch = c * wc * clr
    return men, fem, ch

def work(part, clr, wf, wc, **k):
    return sum(work_terms(part, clr, wf, wc, **k))

def mix(hand, m): return hand + (1.0 - hand) * m

def w_old(part, clr, **k): return work(part, clr, 1.0, CWF, **k)
def w_new(part, clr, m, fh=FH, ch=CH, **k): return work(part, clr, mix(fh, m), mix(ch, m), **k)

# ---- 1) b の格子（工員・基準を捕えた月の m0=0）
def b_worker(m, part0, clr0, part, clr, m0=0.0):
    return (w_new(part, clr, m) / w_new(part0, clr0, m0)) / (w_old(part, clr) / w_old(part0, clr0))

print('## 1) 形 A の b（工員・m0=0・基準の月は 参加0.4・児童労働0.93）')
for m in (0.0, 0.5, 0.8, 0.9, 1.0):
    row = []
    for part, clr in ((0.4, 0.93), (0.4, 0.5), (0.4, 0.0), (0.7, 0.93), (0.7, 0.0)):
        row.append(f'参加{part}・児{clr}:{b_worker(m, 0.4, 0.93, part, clr):.3f}')
    print(f'm={m}: ' + ' '.join(row))
grid = []
for m in (0.8, 0.85, 0.9, 0.95, 1.0):
    for part0 in (0.4, 0.5):
        for clr0 in (0.6, 0.8, 0.93, 1.0):
            for part in (0.4, 0.5, 0.6, 0.7, 0.75):
                for clr in (0.0, 0.1, 0.3, 0.6, 0.93, 1.0):
                    grid.append(b_worker(m, part0, clr0, part, clr))
print(f'G2 の帯の材料（m 0.8〜1・参加 0.4〜0.75・児童労働 0〜1 の格子 {len(grid)} 点）：最小 {min(grid):.3f}・中央 {statistics.median(grid):.3f}・最大 {max(grid):.3f}')
print('  → G2 の帯（固定）：工員の母集合の b の中央値が 1.08 以上 1.32 以下（格子の最小・最大の外へ約 0.01 の余白）')

# ---- 2) 突然変異の表（代表の行）
# 工員の行 W：基準 参加0.4・児0.93・m0=0、今 参加0.5・児0.6・m=0.9（計器の m）。女だけの行 W0：今の児 0（G4a）。
# 農民の行 P：同じ構成・州の m=0.6（農民は m を読まない）。技師の行 E：旧の式。
def b_from(eng_now, eng_init, old_now, old_init): return (eng_now / eng_init) / (old_now / old_init)
rows = {
    'W': dict(part0=0.4, clr0=0.93, part=0.5, clr=0.6, m=0.9),
    'W0': dict(part0=0.4, clr0=0.93, part=0.5, clr=0.0, m=0.9),
    'P': dict(part0=0.4, clr0=0.93, part=0.4, clr=0.93, m=0.6),
    'E': dict(part0=0.4, clr0=0.93, part=0.5, clr=0.6, m=0.9),
}
def paper_b(kind, r):   # 計器の紙の式（形 A）
    p0, c0, p, c, m = r['part0'], r['clr0'], r['part'], r['clr'], r['m']
    if kind in ('W', 'W0'): return b_from(w_new(p, c, m), w_new(p0, c0, 0.0), w_old(p, c), w_old(p0, c0))
    if kind == 'P': return b_from(w_new(p, c, 0.0), w_new(p0, c0, 0.0), w_old(p, c), w_old(p0, c0))
    return 1.0   # E
def engine_b(kind, r, mut):
    p0, c0, p, c, m = r['part0'], r['clr0'], r['part'], r['clr'], r['m']
    if mut == 'flag0': return 1.0
    if kind == 'E':
        if mut == 'M9': return b_from(w_new(p, c, m), w_new(p0, c0, 0.0), w_old(p, c), w_old(p0, c0))
        return 1.0
    mm = 0.0 if kind == 'P' else m
    if kind == 'P' and mut == 'M2': mm = m
    if mut == 'M1' and kind != 'P': mm = 0.0
    if mut == 'M8' and kind != 'P': mm = min(1.0, m / 0.93)   # 分母を工場だけに＝建設などの 7% が抜ける（AI の仮定の大きさ）
    fh, ch = FH, CH
    def wn(pp, cc, mmm):
        wf, wc = mix(fh, mmm), mix(ch, mmm)
        if mut == 'M3': wc = wc * CWF
        if mut == 'M5': wc = ch
        if mut == 'M6': wf = fh
        return work(pp, cc, wf, wc)
    now = wn(p, c, mm)
    init = now if mut == 'M4' else wn(p0, c0, 0.0)
    if mut == 'M4': init_old_ratio = w_old(p, c) / w_old(p0, c0); return 1.0 / init_old_ratio
    return b_from(now, init, w_old(p, c), w_old(p0, c0))

TOL = 1e-3
def gates(mut):
    res = {}
    def agree(kind): return abs(engine_b(kind, rows[kind], mut) / paper_b(kind, rows[kind]) - 1.0) <= TOL
    res['G1'] = agree('W') and agree('W0')
    bw = engine_b('W', rows['W'], mut); res['G2'] = 1.08 <= bw <= 1.32
    res['G3'] = agree('P')
    res['G4a'] = agree('W0'); res['G4b'] = agree('W')
    res['G5'] = abs(engine_b('E', rows['E'], mut) - 1.0) <= 1e-6
    return res, bw

print('## 2) 突然変異ごとの門（代表の行・赤＝R・緑＝g）。b(W) はエンジンの工員の b')
print('| 変異 | b(W) | G1 | G2 | G3 | G4a | G4b | G5 |')
for mut, name in (('none', '正しい実装'), ('flag0', '旗 0（何もしない実装）'), ('M1', 'M1 m＝0 で固定'), ('M2', 'M2 農民にも m'),
                  ('M3', 'M3 子の重みに ChildWorkFactor を掛ける'), ('M4', 'M4 基準を毎月捕え直す'), ('M5', 'M5 女の項だけ m で動く'),
                  ('M6', 'M6 子の項だけ m で動く'), ('M8', 'M8 m の分母を工場だけに'), ('M9', 'M9 技師にも重み')):
    g, bw = gates(mut)
    cells = ' | '.join('g' if g[k] else 'R' for k in ('G1', 'G2', 'G3', 'G4a', 'G4b', 'G5'))
    print(f'| {name} | {bw:.3f} | {cells} |')
print(f'紙の b：W {paper_b("W", rows["W"]):.4f}・W0 {paper_b("W0", rows["W0"]):.4f}・P {paper_b("P", rows["P"]):.4f}・E 1')

# ---- 3) 衝撃への応答（m を固定・新÷旧の work の変化の率）
print('## 3) 衝撃への応答（work の変化の率・m 固定）')
for label, f in (('旧（女1・子0.5）', lambda p, c, m: w_old(p, c)), ('形 A', w_new)):
    for m in (0.0, 0.9):
        b0 = f(0.4, 0.93, m)
        print(f'{label} m={m}: 財産権 0.4→0.7 {f(0.7, 0.93, m) / b0 - 1:+.3f}・児童労働の禁止 {f(0.4, 0.0, m) / b0 - 1:+.3f}')

# ---- 4) 盤面（任意）
if len(sys.argv) > 1:
    d = sys.argv[1]
    a = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
    for p in sorted(glob.glob(d + '/factory_s*.tsv')):
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); iy = h.index('year'); i1 = h.index('steamFillMeanShare'); i2 = h.index('elecFillMeanShare')
            iw = h.index('outputRefValuePerMonth') if 'outputRefValuePerMonth' in h else -1
            for line in f:
                cc = line.rstrip('\n').split('\t'); y = int(cc[iy])
                if y % 20 != 10: continue
                try: s1 = float(cc[i1]); s2 = float(cc[i2]); w = float(cc[iw]) if iw >= 0 else 1.0
                except ValueError: continue
                if s1 != s1 or s2 != s2 or w != w: continue
                pf = s2 + (1 - s2) * s1
                a[y][0] += w * pf; a[y][1] += w; a[y][2] += 1
    print('## 4) 盤面：工場の動力の割合 p＝電化＋(1−電化)×蒸気（産出の参照価格で重み・factory_s）')
    for y in sorted(a): print(f'{y} p(w)={a[y][0] / a[y][1]:.3f} 行={a[y][2]}')
    cl = collections.defaultdict(lambda: [0.0, 0.0])
    for p in sorted(glob.glob(d + '/pop_s*.tsv')):
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t')
            isc, iy, icn, ipk, icl = h.index('scope'), h.index('year'), h.index('className'), h.index('popK'), h.index('childLaborRateShare')
            for line in f:
                cc = line.rstrip('\n').split('\t')
                if cc[isc] != 'C' or cc[icn] != '工場労働者': continue
                y = int(cc[iy])
                if y % 20 != 10: continue
                try: k = float(cc[ipk]); r = float(cc[icl])
                except ValueError: continue
                if k != k or r != r: continue
                cl[y][0] += k * r; cl[y][1] += k
    print('## 4b) 盤面：工場労働者の児童労働の割合（人数で重み・pop_s の C 行）')
    for y in sorted(cl): print(f'{y} clr={cl[y][0] / cl[y][1]:.3f}' if cl[y][1] > 0 else f'{y} -')
