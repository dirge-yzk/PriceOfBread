# (1102) 段 C′ の事前登録の空欄を埋める台本（M＝keep_1096/base_ins だけ・末尾 10 列と S1 の表を読む）。表を読むだけ・エンジンを呼ばない。
# 使い方: python -X utf8 -I rows_m.py <表のフォルダ> > rows_m_out.txt
# 式の写し（名乗る・InstrumentsRulePower.cs）：区分 :52〜64（持ち主＝F_S＋κF_I・在地＝(1−κ)F_I＋F_R・外国＝ΣF_f・無統治＝F_U）・
#   HShare＝H×popK÷ΣpopK×ρ（:266）・F_I＝N×τ×RuleInit1×仲介者の比（:44）・F_U＝N×τ×RuleInit3（:46）・配り＝O×w÷Σw・F_f＝配り×ρ（:358〜363）・
#   O＝max(0, L−min(W,L)−min(max(0,L−min(W,L)), ΣpopK×q))（:195〜197）。許容差は相対 1e-9（同点は若い番号＝持ち主の側）。
# 版：V0＝表の力のまま（q 固定・ρ が重みと力の 2 回）／Vb＝配りの重みから ρ を外す（w′＝Y×(1−min(1,s))×(1−b)・F_f′＝O×w′÷Σw′×ρ）／Nf＝持ち主の側の ρ と τ を凍結を外した列（rhoNoFreezeIndex・tauNoFreezeIndex）へ／qt＝q を四半期ごとに ΣL÷ΣpopK。
# 限界（名乗る）：①配りの行の ρ（rhoF）は凍結あり（S1 に凍結を外した外国の ρ は無い）・配りの四半期（0 と 40 おき）は全て mod 4＝0＝凍る季節。
#   ②O＝0 かつ海軍＝0 の国は配りの行が無い＝qt で O が正になる国の配りは数えられない（件数と O の取り分を出す）。③A の中核の分＝fState−HShare（表の G10 の値からの差）。
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]
TOL = 1e-9
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def best(fs, fi, fr, fo, fu, k):
    k = 1.0 if k > 1 else (k if k > 0 else 0.0)
    fo = fo if fo > 0 else 0.0
    tot = fs + fi + fr + fo + fu
    if not (tot > 0): return 'none'
    o, l, f, u = (fs + k * fi) / tot, ((1 - k) * fi + fr) / tot, fo / tot, fu / tot
    b, bv = 'owner', o
    if l > bv * (1 + TOL): b, bv = 'local', l
    if f > bv * (1 + TOL): b, bv = 'foreign', f
    if u > bv * (1 + TOL): b = 'ungov'
    return b
def qs(v, ps=(0.5, 0.9, 0.99, 1.0)):
    if not v: return 'なし'
    v = sorted(v); return '・'.join(f'{int(p*100)}%点 {v[min(len(v)-1, int(p*(len(v)-1)+0.5))]:.4g}' for p in ps)
COLS = ['quarter', 'nation', 'region', 'popK', 'qIndex', 'nIndex', 'tauIndex', 'lNationIndex', 'wNationIndex', 'hNationIndex', 'oNationIndex',
        'fStateIndex', 'fInterIndex', 'fResistIndex', 'fUngovIndex', 'fForeignSumIndex', 'pressureIndex', 'interRatioIndex', 'interShare', 'kappaIndex',
        'ownerSideShare', 'localSideShare', 'ungovSideShare', 'bestClass', 'nowRule0Share', 'nowRule1Share', 'nowRule3Share', 'rhoNewIndex',
        'rhoNoFreezeIndex', 'tauNoFreezeIndex', 'ruleInit1Share', 'ruleInit3Share', 'ownerSideShareR', 'ungovSideShareR', 'iProbeShare']
STR = {'nation', 'region', 'bestClass'}
per_seed = collections.defaultdict(dict)
frz_mod = collections.Counter(); frz_rows = 0; frz_regs_seed = []; inf_regs_seed = []; bake_ratio = []; gate_red_rows = 0; gate_red_regs = 0; gate_ratio = []; frz_rows_by_seed = []
i_lag = []; i_step = []; i_now = []; i_first = []; i_probe_first = []; i_noprev = 0; i_rows = 0
mil = {100: [], 300: [], 600: []}
vb_rows = vb_disc = vb_rho0 = 0; vb_lost = []
open_counts = []; qt_counts = collections.defaultdict(lambda: collections.defaultdict(list)); qt_missing = []
selfchk_bad = selfchk_n = 0
i_solve_rows = [0]; i_disc = [0]; i_same = [0]
v0_bad_ = [0]; v0_n_ = [0]; v0_rel_ = [0.0]
milj = {100: [], 300: [], 600: []}; milj_n = collections.Counter(); milj_hit = collections.Counter()
for s in range(1, 11):
    p = os.path.join(d, f'rulepower_s{s}.tsv')
    if not os.path.exists(p): continue
    fi = open(p, encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    R = []
    for line in fi:
        c = line.rstrip('\n').split('\t')
        R.append({k: (c[ix[k]] if k in STR else fl(c[ix[k]])) for k in COLS})
    fi.close()
    byq = collections.defaultdict(list)
    for r in R: byq[int(r['quarter'])].append(r)
    q0 = min(byq)
    # ---- 凍結の母集合（問い 3） ----
    first = {}; regs = set(); infr = set(); nfr = 0
    for q in sorted(byq):
        for r in byq[q]:
            g = r['region']
            if g not in first: first[g] = r
            a, b = r['rhoNewIndex'], r['rhoNoFreezeIndex']
            if a == a and b == b and abs(a - b) > TOL * max(abs(a), abs(b), 1e-300):
                frz_rows += 1; nfr += 1; frz_mod[q % 4] += 1; regs.add(g)
                if a == 0: infr.add(g)
                f0 = first[g]; a0, b0 = f0['rhoNewIndex'], f0['rhoNoFreezeIndex']
                if a0 > 0 and b0 > 0 and b > 0:
                    rat = (a / a0) / (b / b0)
                    if abs(rat - 1) > TOL: gate_red_rows += 1
                    gate_ratio.append(rat)
                else:
                    gate_red_rows += 1; gate_ratio.append(float('inf') if a0 == 0 else 0.0)
    frz_regs_seed.append(len(regs)); inf_regs_seed.append(len(infr)); frz_rows_by_seed.append(nfr)
    gate_red_regs += len(regs)
    for g, r in first.items():
        a0, b0 = r['rhoNewIndex'], r['rhoNoFreezeIndex']
        if a0 == a0 and b0 == b0 and abs(a0 - b0) > TOL * max(abs(a0), abs(b0), 1e-300):
            t, tn = r['tauIndex'], r['tauNoFreezeIndex']
            bake_ratio.append(tn / t if t > 0 else float('inf'))
    # ---- i（問い 4） ----
    prev = {}; seenreg = set()
    for q in sorted(byq):
        for r in byq[q]:
            key = (r['nation'], r['region']); i_rows += 1
            ish, ipr = r['interShare'], r['iProbeShare']
            firstreg = r['region'] not in seenreg; seenreg.add(r['region'])
            if firstreg: i_solve_rows[0] += 1
            elif ish == ish and ipr == ipr and abs(ish - ipr) > TOL * max(abs(ish), abs(ipr), 1e-300): i_disc[0] += 1
            elif ish == ish and ipr == ipr: i_same[0] += 1
            if q == q0: i_first.append(ish); i_probe_first.append(ipr)
            if ish == ish and ipr == ipr: i_now.append(abs(ish - ipr))
            pv = prev.get(key)
            if pv is not None and pv[0] == q - 1:
                if pv[1] == pv[1] and ipr == ipr: i_lag.append(abs(pv[1] - ipr))
                if pv[1] == pv[1] and ish == ish: i_step.append(abs(ish - pv[1]))
            else: i_noprev += 1
            prev[key] = (q, ish)
    # ---- 闘争度と無統治（問い 5） ----
    for q in mil:
        for r in byq.get(q, []):
            mil[q].append((r['pressureIndex'], r['nowRule3Share'], r['ungovSideShare'], r['localSideShare'], r['ownerSideShare'], r['nowRule0Share'] + r['nowRule1Share']))
    # 闘争度そのもの（secession_s の militancyMeanIndex＝AvgRegionMilitancy）で結ぶ・開幕の RuleInit[3] を引いた差で読む
    ps = os.path.join(d, f'secession_s{s}.tsv')
    if os.path.exists(ps):
        mm = {}
        with open(ps, encoding='utf-8') as fs_:
            hs_ = fs_.readline().rstrip('\n').split('\t'); isx = {k: i for i, k in enumerate(hs_)}
            for line in fs_:
                c = line.rstrip('\n').split('\t'); qq = int(c[isx['quarter']])
                if qq in milj: mm[(qq, c[isx['nation']], c[isx['region']])] = fl(c[isx['militancyMeanIndex']])
        for q in milj:
            for r in byq.get(q, []):
                milj_n[q] += 1
                m = mm.get((q, r['nation'], r['region']))
                if m is None or m != m: continue
                milj_hit[q] += 1
                milj[q].append((m, r['nowRule3Share'] - r['ruleInit3Share'], r['ungovSideShare'] - r['ruleInit3Share'], r['localSideShare'], r['nowRule1Share'] - r['ruleInit1Share']))
    # ---- 国の段（q_t） ----
    def natinfo(rows):
        nat = {}
        for r in rows:
            v = nat.get(r['nation'])
            if v is None: v = nat[r['nation']] = {'L': r['lNationIndex'], 'W': r['wNationIndex'], 'H': r['hNationIndex'], 'O': r['oNationIndex'], 'pop': 0.0, 'q': r['qIndex']}
            if r['popK'] > 0: v['pop'] += r['popK']
        return nat
    # ---- S1：配りの行 ----
    pa = os.path.join(d, f'rulepoweralloc_s{s}.tsv')
    alloc = collections.defaultdict(lambda: collections.defaultdict(list))
    alloc_raw = collections.defaultdict(lambda: collections.defaultdict(list))
    if os.path.exists(pa):
        fa = open(pa, encoding='utf-8'); ha = fa.readline().rstrip('\n').split('\t'); ia = {k: i for i, k in enumerate(ha)}
        for line in fa:
            c = line.rstrip('\n').split('\t')
            y, sv, bv, w, rho, al = fl(c[ia['yIndex']]), fl(c[ia['sShare']]), fl(c[ia['bShare']]), fl(c[ia['wgtIndex']]), fl(c[ia['rhoF']]), fl(c[ia['allocIndex']])
            den = y * (1 - min(1.0, sv)) * (1 - bv)
            if den > 0:
                vb_rows += 1
                if abs(rho - 1) > TOL: vb_disc += 1
                if rho == 0: vb_rho0 += 1
            alloc[int(c[ia['quarter']])][c[ia['nation']]].append((c[ia['region']], den if den > 0 else 0.0, rho))
            alloc_raw[int(c[ia['quarter']])][c[ia['nation']]].append((c[ia['region']], al, rho))
        fa.close()
    for q in sorted(alloc):
        rows = byq.get(q, [])
        if not rows: continue
        nat = natinfo(rows)
        totL = sum(v['L'] for v in nat.values()); totP = sum(v['pop'] for v in nat.values())
        qt = totL / totP if totP > 0 else float('nan')
        for v in nat.values():
            wu = min(v['W'], v['L']); v['Ht'] = min(max(0.0, v['L'] - wu), v['pop'] * qt); v['Ot'] = max(0.0, v['L'] - wu - v['Ht'])
        # Vb の外国の力（O は表の値＝q 固定／q_t の値）
        fq = collections.defaultdict(float); ft = collections.defaultdict(float)
        miss_n = 0; miss_o = 0.0; totOt = sum(v['Ot'] for v in nat.values())
        for nm, v in nat.items():
            lst = alloc[q].get(nm)
            if not lst:
                if v['Ot'] > 0: miss_n += 1; miss_o += v['Ot']
                continue
            sw = sum(w for _, w, _ in lst)
            if not (sw > 0): continue
            lost = 0.0
            for g, w, rho in lst:
                fq[g] += v['O'] * w / sw * rho; ft[g] += v['Ot'] * w / sw * rho
                if rho == 0: lost += w / sw
            if v['O'] > 0: vb_lost.append(lost)
        qt_missing.append((s, q, miss_n, miss_o / totOt if totOt > 0 else 0.0))
        k = qt / rows[0]['qIndex'] if rows[0]['qIndex'] > 0 else float('nan')
        # V0 の組み直しの自己検査：配りの行の Σ allocIndex×rhoF（今の重み）＝表の fForeignSumIndex
        v0 = collections.defaultdict(float)
        for nm, v in nat.items():
            lst = alloc_raw[q].get(nm)
            if not lst: continue
            for g, al, rho in lst: v0[g] += al * rho
        for r in rows:
            a_, b_ = v0.get(r['region'], 0.0), r['fForeignSumIndex']
            if b_ == b_:
                v0_n_[0] += 1
                rel = abs(a_ - b_) / max(abs(b_), 1e-300) if b_ != 0 else abs(a_)
                if rel > 1e-6: v0_bad_[0] += 1
                if rel > v0_rel_[0] and b_ != 0: v0_rel_[0] = rel
        c0 = c1 = c2 = c3 = 0
        for r in rows:
            nv = nat[r['nation']]
            hs = nv['H'] * r['popK'] / nv['pop'] * r['rhoNewIndex'] if nv['pop'] > 0 and r['rhoNewIndex'] > 0 else 0.0
            acore = max(0.0, r['fStateIndex'] - hs)
            b0 = best(r['fStateIndex'], r['fInterIndex'], r['fResistIndex'], r['fForeignSumIndex'], r['fUngovIndex'], r['kappaIndex'])
            if b0 != r['bestClass'] and not (b0 == 'owner' and r['bestClass'] == 'ungov'): selfchk_bad += 1
            selfchk_n += 1
            c0 += r['bestClass'] == 'foreign'
            c1 += b0 == 'foreign'
            b2 = best(r['fStateIndex'], r['fInterIndex'], r['fResistIndex'], fq.get(r['region'], 0.0), r['fUngovIndex'], r['kappaIndex'])
            c2 += b2 == 'foreign'
            hst = nv['Ht'] * r['popK'] / nv['pop'] * r['rhoNewIndex'] if nv['pop'] > 0 and r['rhoNewIndex'] > 0 else 0.0
            b3 = best(hst + acore * k, r['fInterIndex'] * k, r['fResistIndex'] * k, ft.get(r['region'], 0.0), r['fUngovIndex'] * k, r['kappaIndex'])
            c3 += b3 == 'foreign'
        qt_counts[q]['V0表'].append(c0); qt_counts[q]['V0許容差'].append(c1); qt_counts[q]['q固定Vb'].append(c2); qt_counts[q]['qtVb'].append(c3)
        # 開幕（C3）：四半期 q0 だけ
        if q == q0:
            cnt = collections.Counter()
            for r in rows:
                nv = nat[r['nation']]
                rho, rnf = r['rhoNewIndex'], r['rhoNoFreezeIndex']
                hs = nv['H'] * r['popK'] / nv['pop'] * rho if nv['pop'] > 0 and rho > 0 else 0.0
                acore = max(0.0, r['fStateIndex'] - hs)
                hsn = nv['H'] * r['popK'] / nv['pop'] * rnf if nv['pop'] > 0 and rnf > 0 else 0.0
                n, tn = r['nIndex'], r['tauNoFreezeIndex']
                fin = n * tn * r['ruleInit1Share'] * r['interRatioIndex']; fun = n * tn * r['ruleInit3Share']
                fo = fq.get(r['region'], 0.0)
                for name, args in (('V0許容差', (r['fStateIndex'], r['fInterIndex'], r['fResistIndex'], r['fForeignSumIndex'], r['fUngovIndex'])),
                                   ('Vb', (r['fStateIndex'], r['fInterIndex'], r['fResistIndex'], fo, r['fUngovIndex'])),
                                   ('Vb＋Nf', (hsn + acore, fin, r['fResistIndex'], fo, fun))):
                    b = best(*args, r['kappaIndex'])
                    cnt[(name, b)] += 1
            open_counts.append((s, len(rows), cnt))
print('# (1102) rows_m.py の出力（M＝keep_1096/base_ins）')
print(f'自己検査：写した区分（許容差なしの比べ合いの代わりに許容差つき）と表の bestClass の不一致（無統治→持ち主の許容差の割れを除く） {selfchk_bad}/{selfchk_n}')
print('\n## 問い 3 凍結の母集合')
print(f'ρ（凍結あり）と ρ（凍結を外した）が違う行 {frz_rows}・種ごと {frz_rows_by_seed}・四半期 mod 4 の内訳 {dict(sorted(frz_mod.items()))}')
print(f'違う州（種ごと） {frz_regs_seed}・そのうち ρ＝0（inf km）の行を持つ州 {inf_regs_seed}')
print(f'新しい門の今の値（凍った季節の行だけ・(ρ_t÷ρ_0)÷(ρnf_t÷ρnf_0)≠1 の行）：赤 {gate_red_rows}/{frz_rows}・比の分布 {qs([x for x in gate_ratio if x == x])}')
print(f'比の形で見えない凍った行（比が相対 1e-9 で 1＝最初の行も同じだけ凍っていた）{sum(1 for x in gate_ratio if x == x and abs(x - 1) <= TOL)}・mod 4 が 1 と 2 の行で違う行 {frz_mod.get(1, 0) + frz_mod.get(2, 0)}')
print(f'τ に凍結が焼き付いた州（最初の行で ρ≠ρnf）：{len(bake_ratio)} 州×種・τnf÷τ の分布 {qs(bake_ratio, (0.1, 0.5, 0.9, 1.0))}')
print('\n## 問い 4 i の循環')
print(f'行 {i_rows}・前の四半期の同じ（国・州）の行が無い行 {i_noprev}')
print(f'|interShare_t − iProbe_t|（今の OwnerWeight の i と設計の i の差）：{qs(i_now)}')
print(f'|interShare_(t−1) − iProbe_t|（前の四半期を読む案と今の試しの差＝C7 の見分けの力）：{qs(i_lag)}')
print(f'|interShare_t − interShare_(t−1)|（前の四半期を読む案の遅れの誤差）：{qs(i_step)}')
print(f'C7 の母集合：τ を解く行（州を最初に見た行） {i_solve_rows[0]}・それ以外で iProbe≠interShare（相対 1e-9）の行 {i_disc[0]}・同じ行 {i_same[0]}')
print(f'開幕の四半期の interShare {qs(i_first, (0.01, 0.1, 0.5))}・iProbe {qs(i_probe_first, (0.01, 0.1, 0.5))}')
print('\n## 問い 5 闘争度（圧力）の 10 分位ごとの平均（全ての種を合わせる）')
for q, L in mil.items():
    L = [x for x in L if all(v == v for v in x)]
    if not L: continue
    L.sort(key=lambda x: x[0]); n = len(L); z = sum(1 for x in L if x[0] == 0)
    print(f'四半期 {q}：行 {n}・圧力 0 の行 {z}')
    print('  分位\t圧力の上端\t今の Rule[3]\t影の無統治\t影の在地\t影の持ち主の側\t今の Rule[0]+[1]')
    for k in range(10):
        part = L[k * n // 10:(k + 1) * n // 10]
        if not part: continue
        m = lambda j: sum(x[j] for x in part) / len(part)
        print(f'  {k+1}\t{part[-1][0]:.4g}\t{m(1):.4f}\t{m(2):.4f}\t{m(3):.4f}\t{m(4):.4f}\t{m(5):.4f}')
print('\n## 問い 5 の続き：闘争度そのもの（secession_s の militancyMeanIndex）の 10 分位・開幕の RuleInit からの差の平均（全ての種）')
for q, L in milj.items():
    L = [x for x in L if all(v == v for v in x)]
    print(f'四半期 {q}：rulepower_s の行 {milj_n[q]}・secession_s と結べた行 {milj_hit[q]}')
    if not L: continue
    L.sort(key=lambda x: x[0]); n = len(L)
    print('  分位\t闘争度の上端\t今の Rule[3]−RuleInit[3]\t影の無統治−RuleInit[3]\t影の在地\t今の Rule[1]−RuleInit[1]')
    for k in range(10):
        part = L[k * n // 10:(k + 1) * n // 10]
        if not part: continue
        m = lambda j: sum(x[j] for x in part) / len(part)
        print(f'  {k+1}\t{part[-1][0]:.4g}\t{m(1):+.4f}\t{m(2):+.4f}\t{m(3):.4f}\t{m(4):+.4f}')
print('\n## 問い 2 S1 の空欄')
print(f'Vb の土台の自己検査：配りの行から組み直した Σ allocIndex×rhoF と表の fForeignSumIndex が相対 1e-6 を超えて違う行 {v0_bad_[0]}/{v0_n_[0]}・最大の相対差 {v0_rel_[0]:.3g}')
print(f'C4：配りの行で Y×(1−s)×(1−b)＞0 の行 {vb_rows}・そのうち ρ≠1（Va と Vb を見分けられる行） {vb_disc}・ρ＝0 の行 {vb_rho0}')
print(f'C4 の帰結：Vb で ρ＝0 の候補へ配られる O の割合（国×四半期・O＞0）{qs(vb_lost, (0.5, 0.9, 0.99, 1.0))}')
print('C3 開幕（四半期 q0・許容差つき）：版ごとの区分の行数（種ごと）')
names = ['V0許容差', 'Vb', 'Vb＋Nf']
for nm in names:
    non = [sum(v for (a, b), v in cnt.items() if a == nm and b != 'owner') for _, _, cnt in open_counts]
    fo = [cnt.get((nm, 'foreign'), 0) for _, _, cnt in open_counts]; un = [cnt.get((nm, 'ungov'), 0) for _, _, cnt in open_counts]; lo = [cnt.get((nm, 'local'), 0) for _, _, cnt in open_counts]
    print(f'  {nm}：非持ち主 {non}（合計 {sum(non)}・種の幅 {min(non)}〜{max(non)}）・外国 {fo}（合計 {sum(fo)}）・無統治 合計 {sum(un)}・在地 合計 {sum(lo)}')
print('q_t の下の影の外国（影の最大が外国の州の行・配りの四半期・種の合計と種の幅）：')
for q in sorted(qt_counts):
    cc = qt_counts[q]
    print(f'  四半期 {q}：' + '・'.join(f'{k} {sum(v)}（{min(v)}〜{max(v)}）' for k, v in cc.items()))
tot = collections.defaultdict(int)
for q in qt_counts:
    for k, v in qt_counts[q].items(): tot[k] += sum(v)
print('  全ての配りの四半期の和：' + '・'.join(f'{k} {v}' for k, v in tot.items()))
mm = [x for x in qt_missing if x[2] > 0]
print(f'q_t で O＞0 なのに配りの行が無い国：国×四半期 {sum(x[2] for x in qt_missing)}・その O の世界の取り分 {qs([x[3] for x in mm], (0.5, 0.9, 1.0))}')
