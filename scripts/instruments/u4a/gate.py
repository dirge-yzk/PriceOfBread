# (714) 線 u4a：資本の U4（私的な建設の予算を流れへ・旗 InvestBudgetFromFlow）の門の読み手。読むだけ（consf_s＝InstrumentsU4.cs を読む）。
# 設計書 docs/202610/20261030/20261030_資本の在庫と投資_設計.md §11-4 と §11-7 の式を 1 本で持つ（腕の前にこの読み手ごとコミットする＝腕の後に式を変えない）。
# 使い方：
#   pin   python -X utf8 -I scripts/instruments/u4a/gate.py pin <基線の生> <N1 の生> <N2 の生> <出力 json> [種の数=10] [種の起点=1]
#         ＝腕の前：国×十年ごとの bind・fbind・W・G-U4-1 の必要値と、G-U4-2 の 2 組（縛る組・緩む組）・W の比・F1 の rho の W を機械的に取って json に固定する。
#   nulls python -X utf8 -I scripts/instruments/u4a/gate.py nulls <基線の生> <N1 の生> <N2 の生> <pin の json>（ヌル腕の一つ抜き・記録）／report <pin の json>（表）
#   judge python -X utf8 -I scripts/instruments/u4a/gate.py judge <腕の生> <pin の json> [種の数=10] [種の起点=1] [同じ種の基線の生]
#         ＝腕の後：pin に固定した式で G-U4-1・G-U4-2・F1 を読む（pin の式・閾値は変えない）。
# 定義（値は全て 国×十年・本国格＝scope home・種の中央値。十年の区切りは年の下一桁を落とす〔1783〜1789 は 1783〕）：
#   bind＝budgetBoundRegionMonthsCount÷desiringRegionMonthsCount／fbind＝flowBoundRegionMonthsCount÷flowDesiringRegionMonthsCount／fp＝flowPlannedMoneySum÷paidMoneySum（fplan/paid）／paid＝paidMoneySum／rho＝oscFirstDiffLagProductSum÷√(oscFirstDiffSquareSum×oscFirstDiffLagSquareSum)
#   G-U4-1：窓＝1790〜1850 年代の 7 つの十年・国＝英仏普墺露オスマン清日（アメリカは窓の外）。通る＝腕の bind − 3 本（基線・N1・N2）の bind の最大 ≧ max(W, ½×(基線の fbind − 基線の bind))。W＝3 本の bind の最大−最小。
#            門＝通る十年が 4 つ以上の国が 6 国以上。
#   G-U4-2：1800〜1913 年（その年の四半期だけ足す・1910 年代の区は 1910〜1913）の国×十年を、基線の fp で 2 組に分ける：縛る組＝fp ≦ 0.8／緩む組＝fp ≧ 1.25。R＝腕の paid の種の中央値÷基線の paid の種の中央値。
#            W の比＝3 本の paid の種の中央値の最大−最小÷基線の paid の種の中央値。門＝①縛る組の R の中央値 ≦ 1 − ½×(1 − 縛る組の fp の中央値) かつ ②緩む組の R の中央値 ≧ 1 − 緩む組の W の比の中央値。
#   F1：1800〜1913 年の国×十年で、腕の rho ≦ −0.7 かつ 腕の rho ≦ 3 本の rho の最小 − W_rho（W_rho＝3 本の rho の最大−最小）。該当の十年が 1800〜1913 年の十年の 1/3 以上の国が 3 国以上なら〈線の不採用〉。
import csv, sys, json, math, statistics as st, collections as C
sys.stdout.reconfigure(encoding='utf-8')
NAT = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "清", "日本", "アメリカ"]
G1_NAT = NAT[:8]
G1_DEC = [1790, 1800, 1810, 1820, 1830, 1840, 1850]
G2_LO, G2_HI = 1800, 1913
K = ["desiringRegionMonthsCount", "budgetBoundRegionMonthsCount", "flowDesiringRegionMonthsCount", "flowBoundRegionMonthsCount",
     "flowPlannedMoneySum", "paidMoneySum", "oscFirstDiffLagProductSum", "oscFirstDiffSquareSum", "oscFirstDiffLagSquareSum"]
def dec(y): return 1783 if y < 1790 else (y // 10) * 10
def div(a, b): return a / b if b > 0 else float('nan')
def ok(x): return x == x
def med(v):
    v = [x for x in v if ok(x)]; return st.median(v) if v else float('nan')

def load(root, ns, sb):
    """(nation, decade) -> 指標 -> 種ごとの値（scope home）。g2＝1800〜1913 年の四半期だけ足した版（1910 年代の区が 1913 で切れる）。"""
    res = C.defaultdict(lambda: C.defaultdict(list)); g2 = C.defaultdict(lambda: C.defaultdict(list))
    for s in range(sb, sb + ns):
        a = C.defaultdict(lambda: C.defaultdict(float)); b = C.defaultdict(lambda: C.defaultdict(float))
        with open(f"{root}/consf_s{s}.tsv", encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='\t'):
                n = x['nation']
                if n not in NAT or x['scope'] != 'home': continue
                if any(x.get(k) in (None, '') for k in K): continue
                y = int(x['year']); d = dec(y)
                for k in K:
                    a[(n, d)][k] += float(x[k])
                    if G2_LO <= y <= G2_HI: b[(n, d)][k] += float(x[k])
        for tgt, src in ((res, a), (g2, b)):
            for key, t in src.items():
                sq = t['oscFirstDiffSquareSum'] * t['oscFirstDiffLagSquareSum']
                m = {'bind': div(t['budgetBoundRegionMonthsCount'], t['desiringRegionMonthsCount']),
                     'fbind': div(t['flowBoundRegionMonthsCount'], t['flowDesiringRegionMonthsCount']),
                     'fp': div(t['flowPlannedMoneySum'], t['paidMoneySum']),
                     'paid': t['paidMoneySum'],
                     'rho': t['oscFirstDiffLagProductSum'] / math.sqrt(sq) if sq > 0 else float('nan')}
                for k, v in m.items(): tgt[key][k].append(v)
    return res, g2

def cell(res, key, m): return med(res[key][m]) if key in res else float('nan')

def pin(roots, out, ns, sb):
    arms = [load(r, ns, sb) for r in roots]            # 基線・N1・N2
    base1, base2 = arms[0]
    P = {'g1': {}, 'g2_cells': {}, 'defs': 'gate.py の頭の定義'}
    for n in G1_NAT:
        for d in G1_DEC:
            key = (n, d)
            bs = [cell(a[0], key, 'bind') for a in arms]; fb = cell(base1, key, 'fbind')
            if not all(ok(x) for x in bs) or not ok(fb): P['g1'][f'{n}|{d}'] = None; continue
            w = max(bs) - min(bs); floor = 0.5 * (fb - bs[0]); need = max(bs) + max(w, floor)
            P['g1'][f'{n}|{d}'] = {'bind': bs, 'fbind': fb, 'W': w, 'floor': floor, 'nullmax': max(bs), 'need': need, 'decidable': need <= 1.0}
    for n in NAT:
        for d in range(1800, 1920, 10):
            key = (n, d)
            paids = [cell(a[1], key, 'paid') for a in arms]; fp = cell(base2, key, 'fp')
            if not all(ok(x) and x > 0 for x in paids) or not ok(fp): continue
            grp = 'tight' if fp <= 0.8 else ('loose' if fp >= 1.25 else None)
            rhos = [cell(a[1], key, 'rho') for a in arms]
            P['g2_cells'][f'{n}|{d}'] = {'fp': fp, 'group': grp, 'paid': paids, 'wratio': (max(paids) - min(paids)) / paids[0],
                                          'rho': rhos, 'wrho': (max(rhos) - min(rhos)) if all(ok(x) for x in rhos) else None}
    t = [c for c in P['g2_cells'].values() if c['group'] == 'tight']; l = [c for c in P['g2_cells'].values() if c['group'] == 'loose']
    P['g2'] = {'n_tight': len(t), 'n_loose': len(l), 'tight_fp_median': med([c['fp'] for c in t]), 'loose_wratio_median': med([c['wratio'] for c in l]),
               'gate1_limit': 1 - 0.5 * (1 - med([c['fp'] for c in t])) if t else float('nan'), 'gate2_limit': 1 - med([c['wratio'] for c in l]) if l else float('nan')}
    json.dump(P, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    return P

def judge(root, pinf, ns, sb, base=None):
    """base＝同じ種の基線の生（種 11〜20 の最終の判定では a11）。R の分母だけを替える＝門の式・限界・必要値は種 1〜10 で固定した pin のまま。"""
    P = json.load(open(pinf, encoding='utf-8')); res, g2 = load(root, ns, sb); out = []
    bg2 = load(base, ns, sb)[1] if base else None
    out.append('## G-U4-1（配線の門）：国ごとの通る十年の数')
    nat_pass = 0
    for n in G1_NAT:
        row = []; k = 0
        for d in G1_DEC:
            p = P['g1'].get(f'{n}|{d}'); b = cell(res, (n, d), 'bind')
            if p is None or not ok(b): row.append(f'{d}:NA'); continue
            ok1 = b >= p['need']; k += ok1
            row.append(f"{d}:{b:.3f}/{p['need']:.3f}{'○' if ok1 else '×'}{'' if p['decidable'] else '(判定不能)'}")
        nat_pass += k >= 4
        out.append(f'{n}\t通る {k}/7\t' + ' '.join(row))
    out.append(f'G-U4-1：通る十年が 4 つ以上の国 {nat_pass}/8（門＝6 国以上）→ {"通る" if nat_pass >= 6 else "外れ"}')
    out.append('## G-U4-2（対比の門）')
    tr, lr = [], []
    for key, c in P['g2_cells'].items():
        if c['group'] is None: continue
        n, d = key.split('|'); a = cell(g2, (n, int(d)), 'paid')
        if not ok(a): continue
        den = cell(bg2, (n, int(d)), 'paid') if bg2 is not None else c['paid'][0]
        if not (ok(den) and den > 0): continue
        r = a / den
        (tr if c['group'] == 'tight' else lr).append((key, r, c))
    mt, ml = med([r for _, r, _ in tr]), med([r for _, r, _ in lr])
    g1l, g2l = P['g2']['gate1_limit'], P['g2']['gate2_limit']
    out.append(f"縛る組 {len(tr)} 件：R の中央 {mt:.3f}（限界 {g1l:.3f}）→ ①{'通る' if ok(mt) and mt <= g1l else '外れ'}／縛る組の 2/3 以上で R ≦ 1−½(1−fp)：{sum(r <= 1 - 0.5 * (1 - c['fp']) for _, r, c in tr)}/{len(tr)}（記録）")
    out.append(f"緩む組 {len(lr)} 件：R の中央 {ml:.3f}（限界 {g2l:.3f}）→ ②{'通る' if ok(ml) and ml >= g2l else '外れ'}")
    # ②′（腕の前の追記・反証役の中③・設計書 §11-8）：1880 年代以前の緩む組（お金が足りる国×十年＝今の配給で計画の 0.6 前後しか払えていない側）。限界は pin の W の比から機械的に取る（1 − その中央値）。
    le = [(k, r, c) for k, r, c in lr if int(k.split('|')[1]) <= 1880]
    wle = med([c['wratio'] for _, _, c in le]); l2 = 1 - wle; mle = med([r for _, r, _ in le])
    out.append(f"早い緩む組（1880 年代以前）{len(le)} 件：R の中央 {mle:.3f}（限界 {l2:.3f}＝1 − W の比の中央 {wle:.3f}）→ ②′{'通る' if ok(mle) and mle >= l2 else '外れ'}")
    g2ok = ok(mt) and ok(ml) and ok(mle) and mt <= g1l and ml >= g2l and mle >= l2
    out.append(f"G-U4-2（①かつ②かつ②′）：{'通る' if g2ok else '外れ'}")
    out.append('## F1（周期 2 の振動・分母は rho が有限な十年だけ＝建設の差が 0 の十年は分母から落ちる）')
    per = C.defaultdict(lambda: [0, 0])
    for key, c in P['g2_cells'].items():
        n, d = key.split('|'); r = cell(g2, (n, int(d)), 'rho')
        if not ok(r) or c['wrho'] is None: continue
        per[n][1] += 1
        if r <= -0.7 and r <= min(c['rho']) - c['wrho']: per[n][0] += 1
    nf = 0
    for n in NAT:
        if per[n][1]:
            f = per[n][0] / per[n][1] >= 1 / 3; nf += f
            out.append(f'{n}\t該当 {per[n][0]}/{per[n][1]}\t{"該当の国" if f else ""}')
    out.append(f'F1：該当の国 {nf}（3 国以上で〈線の不採用〉）→ {"不採用" if nf >= 3 else "出ない"}')
    return '\n'.join(out)

def nullcheck(roots, pinf, ns, sb):
    """ヌル腕の一つ抜き（記録）：3 本（基線・N1・N2）のうち 1 本を腕と見なし、固定した式の門を通るかを数える（通れば門は空振りする＝判定力が無い）。"""
    P = json.load(open(pinf, encoding='utf-8')); arms = [load(r, ns, sb) for r in roots]; out = ['## ヌル腕の一つ抜き（記録）']
    for i, nm in enumerate(['基線', 'N1', 'N2']):
        others = [j for j in range(3) if j != i]; npass = 0; cells = 0
        for n in G1_NAT:
            k = 0
            for d in G1_DEC:
                p = P['g1'].get(f'{n}|{d}')
                if p is None: continue
                b = cell(arms[i][0], (n, d), 'bind'); nm_ = max(cell(arms[j][0], (n, d), 'bind') for j in others)
                if ok(b) and b - nm_ >= max(p['W'], p['floor']): k += 1
            npass += k >= 4
        tr = [c for c in P['g2_cells'].values() if c['group'] == 'tight']; lo = [c for c in P['g2_cells'].values() if c['group'] == 'loose']
        rt = med([c['paid'][i] / c['paid'][0] for c in tr]); rl = med([c['paid'][i] / c['paid'][0] for c in lo])
        out.append(f'{nm}を腕と見なす：G-U4-1 を通る国 {npass}/8・縛る組の R 中央 {rt:.3f}（限界 {P["g2"]["gate1_limit"]:.3f}）・緩む組の R 中央 {rl:.3f}（限界 {P["g2"]["gate2_limit"]:.3f}）')
    early = [c for k, c in P['g2_cells'].items() if c['group'] == 'loose' and int(k.split('|')[1]) <= 1880]
    late = [c for k, c in P['g2_cells'].items() if c['group'] == 'loose' and int(k.split('|')[1]) > 1880]
    out.append(f'参考（門にしない）：緩む組 {len(early)+len(late)} 件のうち 1880 年代以前 {len(early)} 件の W の比の中央 {med([c["wratio"] for c in early]):.3f}・1890 年代以後 {len(late)} 件の中央 {med([c["wratio"] for c in late]):.3f}')
    return chr(10).join(out)

def report(pinf):
    """pin の json を 1 枚の表（measurements に残す・設計書 §11-7 が写す）にする。"""
    P = json.load(open(pinf, encoding='utf-8')); o = []
    o.append('# (714) u4a 腕の前の W の追記（読み手 scripts/instruments/u4a/gate.py・生＝D:/PoB_work/keep_711 の base_ins／n1／n2 の consf_s・種 1〜10・本国格）')
    o.append('## G-U4-1：国×十年の bind（基線／N1／N2）・W・基線の fbind・必要値 need＝3 本の最大＋max(W, ½×(fbind−基線の bind))（腕の bind が need 以上で通る・need が 1 を超えれば判定不能）')
    o.append('国	十年	基線	N1	N2	W	fbind	床	need	判定')
    for k, c in P['g1'].items():
        n, d = k.split('|')
        if c is None: o.append(f'{n}	{d}	NA'); continue
        o.append(f"{n}	{d}	{c['bind'][0]:.3f}	{c['bind'][1]:.3f}	{c['bind'][2]:.3f}	{c['W']:.3f}	{c['fbind']:.3f}	{c['floor']:.3f}	{c['need']:.3f}	{'可' if c['decidable'] else '判定不能'}")
    g = P['g2']
    o.append('## G-U4-2：国×十年（1800〜1913 年・十年の区は 1910 年代が 1910〜1913）の基線の fplan/paid で 2 組に分ける（縛る組＝0.8 以下・緩む組＝1.25 以上）')
    o.append(f"縛る組 {g['n_tight']} 件・fplan/paid の中央 {g['tight_fp_median']:.4f} → ①の限界 R ≦ {g['gate1_limit']:.4f}")
    o.append(f"緩む組 {g['n_loose']} 件・W の比の中央 {g['loose_wratio_median']:.4f} → ②の限界 R ≧ {g['gate2_limit']:.4f}")
    o.append('国	十年	基線の fplan/paid	組	paid 基線	N1	N2	W の比	rho 基線	N1	N2	W_rho')
    for k, c in P['g2_cells'].items():
        n, d = k.split('|')
        o.append(f"{n}	{d}	{c['fp']:.3f}	{ {'tight': '縛る', 'loose': '緩む', None: '-'}[c['group']] }	" + '	'.join(f'{x:.0f}' for x in c['paid']) + f"	{c['wratio']:.3f}	" + '	'.join(f'{x:.2f}' for x in c['rho']) + f"	{(c['wrho'] if c['wrho'] is not None else float('nan')):.2f}")
    return chr(10).join(o)

def _series(root, ns, sb):
    """money_s から 国×年 の（outstandingLoansStock・cumulativeFiatIssuedStock）の年末の値を種ごとに（記録用）。"""
    out = C.defaultdict(lambda: C.defaultdict(list))
    for s in range(sb, sb + ns):
        last = {}
        with open(f"{root}/money_s{s}.tsv", encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='	'):
                n = x['nation']
                if n not in NAT: continue
                last[(n, int(x['year']))] = (float(x['outstandingLoansStock']), float(x['cumulativeFiatIssuedStock']))
        for (n, y), v in last.items():
            out[(n, y)]['loans'].append(v[0]); out[(n, y)]['fiat'].append(v[1])
    return out

def record(arm, roots, ns, sb, base=None):
    """記録（採否に使わない・反証役の高②②・中④）。①貯蓄を埋める与信の増え：国ごとの年末の与信残高と不換の累計の種の中央値（基線・N1・N2・腕）。
    ②貯蓄の上限が建設の予算を切った割合：budget÷flowBudget（本国格・植民地・国×十年）。③植民地の建設の支払いと bind。"""
    out = ['## 記録 ①：与信の残高と不換の累計（money_s・種の中央値・年末）：基線／N1／N2／腕']
    ser = [_series(r, ns, sb) for r in roots + [arm]]
    for n in NAT:
        for y in (1850, 1900, 1949):
            row = []
            for sr in ser:
                k = (n, y)
                row.append(f"{med(sr[k]['loans']):.4g}／{med(sr[k]['fiat']):.4g}" if k in sr else 'NA')
            out.append(f"{n}	{y}	与信／不換の累計	" + '	'.join(row))
    out.append('## 記録 ②③：consf_s の budgetMoneySum÷flowBudgetMoneySum（貯蓄の上限が効いた度合い・1 で上限なし）と paid・bind（国×十年・種の中央値・基線／腕）')
    def agg(root, scope):
        res = C.defaultdict(lambda: C.defaultdict(list))
        for s in range(sb, sb + ns):
            a = C.defaultdict(lambda: C.defaultdict(float))
            with open(f"{root}/consf_s{s}.tsv", encoding='utf-8') as f:
                for x in csv.DictReader(f, delimiter='	'):
                    n = x['nation']
                    if n not in NAT or x['scope'] != scope: continue
                    if any(x.get(k) in (None, '') for k in ('budgetMoneySum', 'flowBudgetMoneySum', 'paidMoneySum', 'desiringRegionMonthsCount', 'budgetBoundRegionMonthsCount')): continue
                    d = dec(int(x['year']))
                    for k in ('budgetMoneySum', 'flowBudgetMoneySum', 'paidMoneySum', 'desiringRegionMonthsCount', 'budgetBoundRegionMonthsCount'): a[(n, d)][k] += float(x[k])
            for key, t in a.items():
                res[key]['ratio'].append(div(t['budgetMoneySum'], t['flowBudgetMoneySum'])); res[key]['paid'].append(t['paidMoneySum'])
                res[key]['bind'].append(div(t['budgetBoundRegionMonthsCount'], t['desiringRegionMonthsCount']))
        return res
    for scope in ('home', 'Colonial'):
        bs = agg(roots[0], scope); ar = agg(arm, scope)
        for n in NAT:
            for d in sorted({k[1] for k in ar if k[0] == n}):
                k = (n, d)
                if k not in bs: continue
                out.append(f"{scope}	{n}	{d}	上限の度合い 基線 {med(bs[k]['ratio']):.3f}／腕 {med(ar[k]['ratio']):.3f}	paid 基線 {med(bs[k]['paid']):.4g}／腕 {med(ar[k]['paid']):.4g}	bind 基線 {med(bs[k]['bind']):.3f}／腕 {med(ar[k]['bind']):.3f}")
    return chr(10).join(out)


if __name__ == '__main__':
    a = sys.argv
    if a[1] == 'pin':
        ns = int(a[6]) if len(a) > 6 else 10; sb = int(a[7]) if len(a) > 7 else 1
        P = pin([a[2], a[3], a[4]], a[5], ns, sb); print(json.dumps(P['g2'], ensure_ascii=False, indent=1))
    elif a[1] == 'nulls':
        ns = int(a[6]) if len(a) > 6 else 10; sb = int(a[7]) if len(a) > 7 else 1
        print(nullcheck([a[2], a[3], a[4]], a[5], ns, sb))
    elif a[1] == 'report':
        print(report(a[2]))
    elif a[1] == 'record':   # record <腕の生> <基線の生> <N1 の生> <N2 の生> [種の数] [種の起点]
        ns = int(a[6]) if len(a) > 6 else 10; sb = int(a[7]) if len(a) > 7 else 1
        print(record(a[2], [a[3], a[4], a[5]], ns, sb))
    elif a[1] == 'judge':
        ns = int(a[4]) if len(a) > 4 else 10; sb = int(a[5]) if len(a) > 5 else 1
        print(judge(a[2], a[3], ns, sb, a[6] if len(a) > 6 else None))
