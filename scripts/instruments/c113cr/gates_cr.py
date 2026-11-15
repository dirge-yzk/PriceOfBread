# (1151) c113cr 段 C′ の 2 回目の門（事前登録 measurements/m1151_20261115_prereg_cr.txt §2）。(1123) gates_rc.py の型・表を読むだけ・エンジンを呼ばない。
# 使い方: python -X utf8 -I gates_cr.py <腕の生のフォルダ> <c5_series.tsv> <thr_cr.txt> [--seeds 1-10] [--min-scale 0.2]
#   〈実際の側〉＝エンジンの列（*Eng*）・期待の側＝計器が事前登録の文から自分で数えた〈D〉の列。許容差は相対 1e-9（絶対 1e-12 の床）。
#   --min-scale：短い走り（2 種×80 四半期）で行の数の下限に掛ける倍率（腕 B は 1）。閾値（C15・G1′・C8c″）は thr_cr.txt（腕の前に固定した数）。
import sys, os, math, statistics as st, collections
sys.stdout.reconfigure(newline='\n')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import onset_cr
VALOPT = ('--seeds', '--min-scale')
args = [a for i, a in enumerate(sys.argv[1:], 1) if not a.startswith('--') and sys.argv[i - 1] not in VALOPT]
seeds = range(1, 11); scale = 1.0
for i, a in enumerate(sys.argv):
    if a == '--seeds': lo, hi = sys.argv[i + 1].split('-'); seeds = range(int(lo), int(hi) + 1)
    if a == '--min-scale': scale = float(sys.argv[i + 1])
arm, c5p, thrp = args[0], args[1], args[2]
thr = {}
with open(thrp, encoding='utf-8') as f:
    for line in f:
        if '=' in line and not line.startswith('#'):
            k, v = line.strip().split('=', 1); thr[k] = float(v)
TOL = 1e-9
fl = onset_cr.fl
def neq(a, b):
    if math.isnan(a) or math.isnan(b): return not (math.isnan(a) and math.isnan(b))
    return abs(a - b) > TOL * max(abs(a), abs(b)) + 1e-12
def finite(*v): return all(not (math.isnan(x) or math.isinf(x)) for x in v)
def need(n): return int(math.ceil(n * scale))
out = []
def P(*a): out.append(' '.join(str(x) for x in a))
def gate(name, ok, detail): P(f'{name}\t{"緑" if ok else "赤"}\t{detail}')

cnt = collections.Counter()
c3 = c3_seen = 0
c8a_in = c8a_tot = c8a_over = 0
c5_q_differs = []
nat_series = collections.defaultdict(lambda: collections.defaultdict(list))
c8e_in = c8e_tot = 0
g2_war, g2_peace = [], []
open_rows = open_bad = 0
c13_rows = c13_bad = 0; c13_keys = collections.Counter()
c14_rows = c14_bad = c14_short = 0
g1_per = []
NEED = ['bestClassEng', 'bestClassD', 'rhoEngIndex', 'rhoDIndex', 'rhoNewIndex', 'rhoNoFreezeIndex', 'qEngIndex', 'qNowIndex', 'fForeignEngIndex', 'fForeignVbIndex',
        'iEngShare', 'iDShare', 'iProbeShare', 't0EngShare', 't0DShare', 't1EngShare', 't1DShare', 't3EngShare', 't3DShare', 'nowRule0Share', 'nowRule2Share',
        'oEngNationIndex', 'lNationIndex', 'wNationIndex', 'wEngageNationIndex', 'tauEngIndex', 'tauDIndex', 'hEngNationIndex', 'hDNationIndex', 'oDNationIndex',
        'openQualFlag', 'ruleInit0Share', 'ruleInit1Share', 'ruleInit3Share', 'dataKeyD', 'dataX1DShare', 'dataX1EngShare', 'tier']
for s in seeds:
    p = os.path.join(arm, f'rulepower_s{s}.tsv')
    if not os.path.exists(p): P('種', s, 'の rulepower_s が無い'); continue
    first_q = None; q0 = None; qdiff = set(); seen_reg = set(); seen_first = set()
    prev = {}; natq = collections.defaultdict(dict)
    natrows = collections.Counter(); natc8 = collections.defaultdict(list); natcap = {}
    c14_seen = set(); g1q = collections.Counter()
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        miss = [k for k in NEED if k not in ix]
        if miss: P('種', s, '列が無い', miss); continue
        g = {k: ix[k] for k in NEED}; iq, ir, inat = ix['quarter'], ix['region'], ix['nation']
        for line in f:
            c = line.rstrip('\n').split('\t')
            q = int(c[iq]); rg = c[ir]; nat = c[inat]
            be, bd = c[g['bestClassEng']], c[g['bestClassD']]
            if be != 'none' or bd != 'none':
                cnt['C1_rows'] += 1
                if be != bd: cnt['C1_bad'] += 1
            if c[g['rhoNewIndex']] != c[g['rhoNoFreezeIndex']]: cnt['C2_pop'] += 1
            if neq(fl(c[g['rhoEngIndex']]), fl(c[g['rhoDIndex']])): cnt['C2_bad'] += 1
            if be != 'none':
                if first_q is None: first_q = q
                if q == first_q:
                    c3_seen += 1
                    if be != 'owner': c3 += 1
            qe, qd = fl(c[g['qEngIndex']]), fl(c[g['qNowIndex']])
            if neq(qe, qd): cnt['C5_bad'] += 1
            if finite(qe):
                if q0 is None: q0 = qe
                if neq(qe, q0): qdiff.add(q)
            fe, fd = fl(c[g['fForeignEngIndex']]), fl(c[g['fForeignVbIndex']])
            if neq(fe, fd): cnt['C6_bad'] += 1
            if fd > 0: cnt['C6_pop'] += 1
            ie, idd, ip = fl(c[g['iEngShare']]), fl(c[g['iDShare']]), fl(c[g['iProbeShare']])
            solve_row = rg not in seen_reg; seen_reg.add(rg)
            if not solve_row and neq(idd, ip) and finite(idd, ip):
                cnt['C7_pop'] += 1
                if neq(ie, idd): cnt['C7_bad'] += 1
            elif neq(ie, idd): cnt['C7_bad_other'] += 1
            cnt['C10_rows'] += 1
            if any(neq(fl(c[g[t + 'EngShare']]), fl(c[g[t + 'DShare']])) for t in ('t0', 't1', 't3')): cnt['C10_bad'] += 1
            te, td = fl(c[g['tauEngIndex']]), fl(c[g['tauDIndex']])
            if finite(te) or finite(td):
                cnt['C12_rows'] += 1
                if neq(te, td): cnt['C12_bad'] += 1
            if (q, nat) not in c14_seen:
                c14_seen.add((q, nat))
                he, hd, oe, od, L = fl(c[g['hEngNationIndex']]), fl(c[g['hDNationIndex']]), fl(c[g['oEngNationIndex']]), fl(c[g['oDNationIndex']]), fl(c[g['lNationIndex']])
                if finite(he) or finite(hd):
                    c14_rows += 1
                    if neq(he, hd) or neq(oe, od): c14_bad += 1
                    if finite(he, oe, L) and oe == 0 and he < L * (1 - 1e-12): c14_short += 1
            if rg not in seen_first:
                seen_first.add(rg)
                if c[g['openQualFlag']] == '1':
                    t0, t1, t3 = fl(c[g['t0EngShare']]), fl(c[g['t1EngShare']]), fl(c[g['t3EngShare']])
                    r0, r1, r3 = fl(c[g['ruleInit0Share']]), fl(c[g['ruleInit1Share']]), fl(c[g['ruleInit3Share']])
                    open_rows += 1
                    if not finite(t0, t1, t3) or max(abs(t0 - r0), abs(t1 - r1), abs(t3 - r3)) > 1e-9: open_bad += 1
            if c[g['dataKeyD']] not in ('', 'nan'):
                c13_rows += 1; c13_keys[(rg, c[g['dataKeyD']])] += 1
                if neq(fl(c[g['dataX1DShare']]), fl(c[g['dataX1EngShare']])): c13_bad += 1
            t0 = fl(c[g['t0EngShare']]); now0 = fl(c[g['nowRule0Share']])
            pv = prev.get(rg)
            if pv is not None and pv[0] == q - 1 and finite(t0, now0, pv[1]):
                gap = t0 - pv[1]
                if abs(gap) > 1e-3:
                    want = 0.025 * gap; got = now0 - pv[1]
                    c8a_tot += 1
                    okin = abs(got - want) <= 0.5 * abs(want)
                    if okin: c8a_in += 1
                    if (got / want if want != 0 else 0) > 2: c8a_over += 1
                    natc8[(q, nat)].append(okin)
            prev[rg] = (q, now0)
            natrows[(q, nat)] += 1
            if (q, nat) not in natcap: natcap[(q, nat)] = (t0, fl(c[g['wNationIndex']]), fl(c[g['wEngageNationIndex']]))
            if nat not in natq[q]: natq[q][nat] = [fl(c[g['lNationIndex']]), fl(c[g['oEngNationIndex']])]
            if c[g['tier']] == 'Core' and fl(c[g['wNationIndex']]) == 0 and fl(c[g['wEngageNationIndex']]) == 0 and fl(c[g['nowRule2Share']]) >= 0.25: g1q[q] += 1
    c5_q_differs.append(len(qdiff))
    g1_per.append(max(g1q.values()) if g1q else 0)
    for key, lst in natc8.items():
        if natrows[key] == 1: c8e_tot += len(lst); c8e_in += sum(lst)
    for key, (t0c, wn, we) in natcap.items():
        if not finite(t0c): continue
        if wn > 0: g2_war.append(t0c)
        elif wn == 0 and we == 0: g2_peace.append(t0c)
    for q in sorted(natq):
        L = sum(v[0] for v in natq[q].values() if finite(v[0])); Os = [v[1] for v in natq[q].values() if finite(v[1])]
        if Os and L > 0:
            nat_series[q]['sumOsumL'].append(sum(Os) / L)
            so = sum(Os); nat_series[q]['maxOshare'].append(max(Os) / so if so > 0 else 0.0)

P('# (1151) gates_cr.py', arm, 'seeds', f'{seeds.start}-{seeds.stop - 1}', 'min-scale', scale)
gate('C1 同点', cnt['C1_rows'] >= need(100000) and cnt['C1_bad'] == 0, f"比べた行 {cnt['C1_rows']}（≧{need(100000)}）・食い違い {cnt['C1_bad']}（＝0）")
gate('C2 凍結', cnt['C2_pop'] >= need(10000) and cnt['C2_bad'] == 0, f"凍結で ρ が違う行 {cnt['C2_pop']}（≧{need(10000)}）・rhoEng≠rhoD {cnt['C2_bad']}（＝0）")
P(f'C3 開幕\t記録\t最初の表の四半期で bestClassEng≠owner {c3}・見た州 {c3_seen}')
gate('C5 q', cnt['C5_bad'] == 0 and bool(c5_q_differs) and min(c5_q_differs) >= need(600), f"qEng≠qNow {cnt['C5_bad']}（＝0）・q が最初と違う四半期の種の最小 {min(c5_q_differs) if c5_q_differs else 'なし'}（≧{need(600)}）")
gate('C6 海軍', cnt['C6_bad'] == 0 and cnt['C6_pop'] >= need(1000), f"fForeignEng≠fForeignVb {cnt['C6_bad']}（＝0）・外国の力＞0 の行 {cnt['C6_pop']}（≧{need(1000)}）")
gate('C7 i', cnt['C7_pop'] >= need(100000) and cnt['C7_bad'] == 0, f"解く行の外で iD≠iProbe {cnt['C7_pop']}（≧{need(100000)}）・iEng≠iD {cnt['C7_bad']}（＝0）・その他の食い違い {cnt['C7_bad_other']}（記録）")
gate('C10 目標の写し', cnt['C10_rows'] >= need(100000) and cnt['C10_bad'] == 0, f"行 {cnt['C10_rows']}・t0/t1/t3 の Eng≠D {cnt['C10_bad']}（＝0）")
gate('C12 τ の並び順', cnt['C12_rows'] >= need(1000) and cnt['C12_bad'] == 0, f"τ のある行 {cnt['C12_rows']}（≧{need(1000)}）・tauEng≠tauD（〈D〉は国を逆の順）{cnt['C12_bad']}（＝0）")
gate('C13 開幕のデータ', c13_rows >= need(60) and c13_bad == 0, f"データの州の最初の表の行 {c13_rows}（≧{need(60)}）・寄せの 1 歩の後の取り分の Eng≠D {c13_bad}（＝0）・州と名 {dict(c13_keys)}")
gate('C14 外から先に削る', c14_rows >= need(10000) and c14_short >= need(100) and c14_bad == 0, f"国×四半期 {c14_rows}（≧{need(10000)}）・O＝0 かつ H＜L {c14_short}（≧{need(100)}）・H か O の Eng≠D {c14_bad}（＝0）")
gate('開幕 目標−RuleInit', open_rows >= need(800) and open_bad == 0, f"最初の表で openQualFlag＝1 の行 {open_rows}（≧{need(800)}）・|t−RuleInit|＞1e-9 {open_bad}（＝0）")
fr_in = c8a_in / c8a_tot if c8a_tot else float('nan'); fr_over = c8a_over / c8a_tot if c8a_tot else float('nan')
gate('C8a 寄せ', c8a_tot >= need(10000) and fr_in >= 0.9 and fr_over <= 0.02, f"行 {c8a_tot}（≧{need(10000)}）・±50% の内 {fr_in:.4g}（≧0.9）・2 倍を超える {fr_over:.4g}（≦0.02）")
ser = collections.defaultdict(dict)
with open(c5p, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t')
    for line in f:
        c = line.rstrip('\n').split('\t'); ser[int(c[0])] = {h[j]: fl(c[j]) for j in range(1, len(h))}
pairs = [('N1', 'N2'), ('M', 'N2'), ('M', 'N1')]
for k, name, th in (('sumOsumL', 'C5 壊さない', 1), ('maxOshare', 'C9′ 壊さない', 2)):
    worst = 0; per_ = []
    for pr in pairs:
        n = 0
        for q, v in nat_series.items():
            if q not in ser or not v[k]: continue
            m = st.median(v[k]); lo = min(ser[q][f'{b}_{k}_min'] for b in pr); hi = max(ser[q][f'{b}_{k}_max'] for b in pr)
            n += not (lo <= m <= hi)
        per_.append(n); worst = max(worst, n)
    gate(name, worst <= th and len(nat_series) > 0, f"幅の外の四半期（2 本の組 {pairs}）{per_}・最大 {worst}（≦{th}）・四半期 {len(nat_series)}")
c4pop = c4bad = c4up = c4rows = 0; c11_rows = c11_bad = 0; c11_kind = collections.Counter(); alloc_missing = False
for s in seeds:
    p = os.path.join(arm, f'rulepoweralloc_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        if 'wEngIndex' not in ix or 'candDKind' not in ix: alloc_missing = True; break
        for line in f:
            c = line.rstrip('\n').split('\t')
            we, wd, ae, rf, fe = fl(c[ix['wEngIndex']]), fl(c[ix['wDIndex']]), fl(c[ix['allocEngIndex']]), fl(c[ix['rhoFDIndex']]), fl(c[ix['fForeignEngIndex']])
            dk, ef = c[ix['candDKind']], c[ix['candEngFlag']]
            if dk != '0' or ef == '1':
                c11_rows += 1; c11_kind[dk] += 1
                if (dk != '0') != (ef == '1'): c11_bad += 1
            if math.isnan(we) and math.isnan(wd): continue
            c4rows += 1
            if neq(we, wd): c4bad += 1
            if ae > 0 and finite(rf) and neq(rf, 1.0): c4pop += 1
            if ae > 0 and neq(fe, ae * rf): c4up += 1
gate('C4 ρ の回数と重み', not alloc_missing and c4pop >= need(1000) and c4bad == 0 and c4up == 0, f"配り＞0 かつ ρ_f≠1 {c4pop}（≧{need(1000)}）・wEng≠wD {c4bad}（＝0）・F≠配り×ρ_f {c4up}（＝0）・行 {c4rows}・表の列 {'無い' if alloc_missing else 'あり'}")
gate('C11 候補の集合', not alloc_missing and c11_rows >= need(10000) and c11_bad == 0, f"〈D〉かエンジンの候補の (国, 州) {c11_rows}（≧{need(10000)}）・食い違い {c11_bad}（＝0）・〈D〉の区分 {dict(c11_kind)}")
c8c, c8d, c8n = onset_cr.c8c(arm, seeds)
gate('C8c″ 力の落ち', finite(c8c) and c8c <= thr['C8c_hi'], f"O＝0 の国・同じ州の集合・国の中の対照の種の中央 {c8c:.4g}（≦{thr['C8c_hi']:.4g}）・種 {c8n}")
P(f'C8d″ 対照\t記録\tO＞0 の国の同じ定義の種の中央 {c8d:.4g}（原因は未確認）')
fr_e = c8e_in / c8e_tot if c8e_tot else float('nan')
gate('C8e 州が1つの国の寄せ', c8e_tot >= need(1000) and fr_e >= 0.9, f"行 {c8e_tot}（≧{need(1000)}）・±50% の内 {fr_e:.4g}（≧0.9）")
mw = st.median(g2_war) if g2_war else float('nan'); mp = st.median(g2_peace) if g2_peace else float('nan')
gate('G2 交戦中の首都', len(g2_war) >= need(100) and finite(mw, mp) and mp > 0 and mw >= 0.5 * mp, f"交戦中の首都の t0Eng の中央 {mw:.4g}（行 {len(g2_war)}・≧{need(100)}）・平時 {mp:.4g}（行 {len(g2_peace)}）・比 {mw / mp if finite(mw, mp) and mp > 0 else float('nan'):.4g}（≧0.5）")
on = onset_cr.onset(arm, seeds)
gate('C15 尾の質量', on['seeds'] > 0 and finite(on['C15']) and on['C15'] <= thr['C15_hi'], f"①だけの 0＜s＜0.01 の取り分の和（四半期の平均・種の中央・データの州を除く）{on['C15']:.6g}（≦{thr['C15_hi']:.6g}）・種 {on['seeds']}・記録：データの州 {on['C15_data']:.6g}・講和の付与 {on['C15_grant']:.6g}")
gate('G1′ 遠い漂い', on['seeds'] > 0 and on['G1p_sum'] <= thr['G1p_hi'], f"交戦していない持ち主の Core で①だけの外国 ≧0.25 の州（種×四半期の最大の種ごとの和）{on['G1p_sum']}（≦{thr['G1p_hi']:.4g}）・種ごと {on['G1p_per']}")
P(f'G1 総数\t記録\t交戦していない国の Core で nowRule2Share ≧0.25 の州（種×四半期の最大の種ごとの和）{sum(g1_per)}・種ごと {g1_per}')
print('\n'.join(out))
