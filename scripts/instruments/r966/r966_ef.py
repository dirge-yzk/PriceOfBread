# (966) c86ef 読むだけ：元からの弱さ E∧F〈投資家の貯蓄が配当で尽き、価格水準が床 0.4 に張り付く〉を、全ての組・全ての盤面で同じ規則で数える。
#   エンジンの関数を呼ばない・旗を読まない。定義は数える前にここで固定した（結果を見て変えない）。
#   E＝divhost_s で 配当の請求 divReqIndex ＞0 かつ 払えた額 paidIndex ＜請求の 1/2 の四半期が 4 つ以上続く区間（r950 と同じ）
#   F＝nation_s の priceLevel ≦0.4＋1e-6 が 4 四半期以上続く区間（r950 と同じ）
#   G＝nation_s の名目 GDP が直前 40 四半期の最大の 1/100 未満（最初の 4 行は除く・r950 と同じ）
#   組＝E の区間と F の区間が ±m 四半期で重なる。m は 0・8・16 の 3 つを全て書く（主は 8＝r950 と同じ）。
#   区間の書き方は 1 つ：lo＝min(e0,f0)−8・hi＝max(e1,f1)+8（全ての組に同じ・ef[0] だけを読まない）。G はこの区間の内。
#   助走の行：divhost_s と cpwd_s は同じ quarter に call が複数ある＝(国,quarter) ごとに call が最大の行だけを使う（runs が重複で切れない）。
#   分かれた国＝cederule_s の how＝Seceded・tierBefore＝Colonial の receiver。
#   python -X utf8 -I r966_ef.py <出力の頭> <名>=<ins フォルダ>:<種の始め>:<種の終わり> ...
import sys, os, math
FLOOR = 0.4 + 1e-6
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def runs(qs, k=4):
    qs = sorted(set(qs)); out = []; st = None; pv = None
    for q in qs:
        if pv is not None and q == pv + 1: pv = q; continue
        if st is not None and pv - st + 1 >= k: out.append((st, pv))
        st = pv = q
    if st is not None and pv - st + 1 >= k: out.append((st, pv))
    return out
def near(a, b, m): return a[0] - m <= b[1] and b[0] - m <= a[1]
def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f: yield ix, line.rstrip('\n').split('\t')
def lastcall(p, cols):
    d = {}
    if not os.path.exists(p): return d
    for ix, x in rd(p):
        k = (x[ix['nation']], int(x[ix['quarter']])); c = int(x[ix['call']])
        if k not in d or c >= d[k][0]: d[k] = (c, [fl(x[ix[cn]]) for cn in cols])
    return {k: v[1] for k, v in d.items()}
def g(v, f='.3g'): return format(v, f) if isinstance(v, float) and v == v else str(v)
out = sys.argv[1]
fp = open(out + '_pairs.tsv', 'w', encoding='utf-8', newline='')
ff = open(out + '_frun.tsv', 'w', encoding='utf-8', newline='')
fg = open(out + '_g.tsv', 'w', encoding='utf-8', newline='')
fs = open(out + '_sum.txt', 'w', encoding='utf-8', newline='')
fp.write('\t'.join(['盤面', '種', '国', '分', '人口の最大', 'E', 'F', '区間', 'G', 'fciの山の四半期', 'fci(lo-40)', 'fciの山', '流入の和(lo-40〜e0)', '逃避の和(同)', '落としの和(同)',
    'invSavの山の四半期', 'invSavの山', 'invSav<山の1/100の最初', 'e0', 'f0', '山→尽き', '尽き→e0', 'e0→f0', 'fci(e0)', 'fci(e1)', 'fci(e1)/fci(e0)',
    '請求の和(区間)', '払いの和(区間)', '貯蓄の最小の四半期', '貯蓄', 'outCapital', 'inOther', 'outImport', 'inExport', 'heirSet', 'inCredit',
    '純資本÷貯蓄', '純輸入÷貯蓄', '総(heir+outImport)÷貯蓄', '区間の純資本の和÷貯蓄(lo)', '区間の純輸入の和÷貯蓄(lo)', '貯蓄(lo)', 'Fの終わり方', 'Fの中のcycの最小', 'f1の後にP≧1までの四半期']) + '\n')
ff.write('\t'.join(['盤面', '種', '国', '分', 'F', '長さ', '終わり方', 'cycの最小', '床の行の数', 'Eと±8で組', 'f1の後にP≧1までの四半期']) + '\n')
fg.write('\t'.join(['盤面', '種', '国', '分', '人口の最大', 'Gの四半期の数', 'Gの最初', 'Eの区間の数', 'Fの区間の数', 'Gに最も近いE∧F組(±8)の距離', 'E∧F組(±16)の内', 'E∧F組(±0)の内']) + '\n')
for arg in sys.argv[2:]:
    name, rest = arg.split('=', 1); d, a0, a1 = rest.rsplit(':', 2)
    T = dict(nat=0, sec=0, E=0, F=0, G=0, EF0=0, EF8=0, EF16=0, EFG8=0, secEF8=0, secEFG8=0, pairs8=0, Fruns=0, Gsec=0)
    endc = {}
    for s in range(int(a0), int(a1) + 1):
        pn = os.path.join(d, f'nation_s{s}.tsv')
        if not os.path.exists(pn): continue
        sec = {}
        for ix, x in rd(os.path.join(d, f'cederule_s{s}.tsv')):
            if x[ix['how']] == 'Seceded' and x[ix['tierBefore']] == 'Colonial': sec.setdefault(x[ix['receiver']], int(x[ix['quarter']]))
        ser = {}
        for ix, x in rd(pn):
            ser.setdefault(x[ix['nation']], []).append((int(x[ix['quarter']]), fl(x[ix['populationK']]), fl(x[ix['nominalGdpPerYear']]), fl(x[ix['priceLevel']])))
        lastq = max(q for xs in ser.values() for q, *_ in xs)
        dv = lastcall(os.path.join(d, f'divhost_s{s}.tsv'), ['fci', 'invSav', 'divReqIndex', 'paidIndex'])
        cp = lastcall(os.path.join(d, f'cpwd_s{s}.tsv'), ['fciBeforeFlight', 'fciAtCoupon', 'fciAfterWriteDown'])
        pcl = {}
        # pclamp_s の time は国ごとの試合の四半期の通し番号（Instruments.cs の PcGameN＝国の状態ごと）＝途中で生まれた国は誕生からの番号。
        #   開幕からいる国は quarter と同じ。途中で生まれた国は nation_s の最初の行＋1＋番号（最初の呼び出しは趨勢の基準を取るだけ＝行を書かない）。
        #   名前が同じ国が死んで生まれ直すと、生き続けた区間（nation_s の続いた四半期）ごとに番号が 0 から始まる＝区間ごとに写し、床に当たる区間を選ぶ（n2_947 種 5 で 608/608）。
        q00 = min(xs[0][0] for xs in ser.values())
        SEG = {}
        for nn, xs in ser.items():
            qs = [y[0] for y in xs]; sg = []; st0 = qs[0]
            for i in range(1, len(qs)):
                if qs[i] != qs[i - 1] + 1: sg.append((st0, qs[i - 1])); st0 = qs[i]
            sg.append((st0, qs[-1])); SEG[nn] = sg
        Pq = {(nn, y[0]): y[3] for nn, xs in ser.items() for y in xs}
        def MAPQ(nn, c):
            cand = [(0 if a == q00 else a + 1) + c for a, b in SEG[nn]]
            cand = [q for q, (a, b) in zip(cand, SEG[nn]) if a <= q <= b]
            hit = [q for q in cand if Pq.get((nn, q), 9) <= FLOOR]
            return hit[-1] if hit else (cand[-1] if cand else None)
        pp = os.path.join(d, f'pclamp_s{s}.tsv')
        if os.path.exists(pp):
            for ix, x in rd(pp):
                t = x[ix['time']]
                nn = x[ix['nation']]
                if t.startswith('q') and x[ix['kind']] == 'floor' and nn in ser:
                    mq = MAPQ(nn, int(t[1:]))
                    if mq is not None: pcl[(nn, mq)] = fl(x[ix['cyclicalRatio']])
        # cearn：E∧F の国だけを後で読む
        info = {}
        for n, xs in ser.items():
            T['nat'] += 1; isS = n in sec; T['sec'] += isS
            ex = [q for (nn, q), v in dv.items() if nn == n and v[2] > 0 and not (v[3] >= 0.5 * v[2])]
            Er = runs(ex); Fr = runs([q for q, pk, gg, l in xs if l <= FLOOR])
            Gq = []
            for i, (q, pk, gg, l) in enumerate(xs):
                if i < 4: continue
                mx = max(y[2] for y in xs[max(0, i - 40):i])
                if mx > 0 and gg < mx / 100: Gq.append(q)
            P = {q: l for q, pk, gg, l in xs}; qs_n = [q for q, *_ in xs]
            if Er: T['E'] += 1
            if Fr: T['F'] += 1
            if Gq: T['G'] += 1; T['Gsec'] += isS
            pk = max(y[1] for y in xs)
            prs = {m: [(e, f_) for e in Er for f_ in Fr if near(e, f_, m)] for m in (0, 8, 16)}
            for m in (0, 8, 16):
                if prs[m]: T[f'EF{m}'] += 1
            if prs[8]: T['secEF8'] += isS
            def win(e, f_): return (min(e[0], f_[0]) - 8, max(e[1], f_[1]) + 8)
            efg = [(e, f_) for e, f_ in prs[8] if any(win(e, f_)[0] <= q <= win(e, f_)[1] for q in Gq)]
            if efg: T['EFG8'] += 1; T['secEFG8'] += isS
            T['pairs8'] += len(prs[8])
            def fend(f_):
                after = [q for q in qs_n if q > f_[1]]
                if f_[1] >= lastq: return '試合の終わり', ''
                if not after: return '国が消えた', ''
                up = [q for q in after if P[q] >= 1.0]
                return '床から上がった', (up[0] - f_[1] if up else '戻らず')
            for f_ in Fr:
                T['Fruns'] += 1; how, up = fend(f_); endc[how] = endc.get(how, 0) + 1
                cyc = [pcl[(n, q)] for q in range(f_[0], f_[1] + 1) if (n, q) in pcl]
                ff.write('\t'.join(map(str, [name, s, n, '分' if isS else '-', f'{f_[0]}〜{f_[1]}', f_[1] - f_[0] + 1, how, g(min(cyc)) if cyc else 'なし', len(cyc),
                    '組' if any(near(e, f_, 8) for e in Er) else '-', up])) + '\n')
            if Gq:
                gq0 = Gq[0]
                dist = min((max(0, win(e, f_)[0] - q, q - win(e, f_)[1]) for e, f_ in prs[8] for q in Gq), default='組なし')
                in16 = any(min(e[0], f_[0]) - 8 <= q <= max(e[1], f_[1]) + 8 for e, f_ in prs[16] for q in Gq)
                in0 = any(min(e[0], f_[0]) - 8 <= q <= max(e[1], f_[1]) + 8 for e, f_ in prs[0] for q in Gq)
                fg.write('\t'.join(map(str, [name, s, n, '分' if isS else '-', round(pk), len(Gq), gq0, len(Er), len(Fr), dist, '内' if in16 else '-', '内' if in0 else '-'])) + '\n')
            if prs[8]: info[n] = (isS, pk, prs[8], Gq, fend)
        # 写しの確かめ：床の行（pFiat＜床）を写した四半期で nation_s の priceLevel が床（≦0.4＋1e-6）か
        Pm = {(nn, q): l for nn, xs in ser.items() for q, pk, gg, l in xs}
        T['pclN'] = T.get('pclN', 0) + len(pcl); T['pclOK'] = T.get('pclOK', 0) + sum(1 for k in pcl if k in Pm and Pm[k] <= FLOOR)
        # cearn を 1 回だけ読む（E∧F の国だけ）
        agg = {}
        pc_ = os.path.join(d, f'cearn_s{s}.tsv')
        has_cearn = os.path.exists(pc_) and info
        if has_cearn:
            cols = ['savings', 'outCapitalPerQuarter', 'inOtherPerQuarter', 'outImportPerQuarter', 'inExportPerQuarter', 'heirDebtSettledPerQuarter', 'inCreditPerQuarter']
            for ix, x in rd(pc_):
                nn = x[ix['nation']]
                if nn not in info: continue
                a = agg.setdefault((nn, int(x[ix['quarter']])), [0.0] * len(cols))
                for j, cn in enumerate(cols):
                    v = fl(x[ix[cn]]); a[j] += v if v == v else 0.0
        for n, (isS, pk, prs8, Gq, fend) in info.items():
            for e, f_ in prs8:
                lo, hi = min(e[0], f_[0]) - 8, max(e[1], f_[1]) + 8
                G = 'G' if any(lo <= q <= hi for q in Gq) else '-'
                fq = [(q, dv[(n, q)][0]) for q in range(lo - 40, hi + 1) if (n, q) in dv]
                qpk, fpk = max(fq, key=lambda t: t[1]) if fq else ('', float('nan'))
                f40 = dv[(n, lo - 40)][0] if (n, lo - 40) in dv else (fq[0][1] if fq else float('nan'))
                infl = fli = wo = 0.0
                for q in range(lo - 40, e[0]):
                    if (n, q) in cp and (n, q + 1) in cp: infl += cp[(n, q + 1)][0] - cp[(n, q)][2]
                    if (n, q) in cp: fli += cp[(n, q)][0] - cp[(n, q)][1]; wo += cp[(n, q)][1] - cp[(n, q)][2]
                sq = [(q, dv[(n, q)][1]) for q in range(lo - 40, hi + 1) if (n, q) in dv]
                qsp, spk = max(sq, key=lambda t: t[1]) if sq else ('', float('nan'))
                dry = next((q for q, v in sq if q > qsp and v < spk / 100), '') if sq else ''
                fe0 = dv.get((n, e[0]), [float('nan')])[0]; fe1 = dv.get((n, e[1]), [float('nan')])[0]
                req = sum(dv[(n, q)][2] for q in range(lo, hi + 1) if (n, q) in dv); paid = sum(dv[(n, q)][3] for q in range(lo, hi + 1) if (n, q) in dv)
                how, up = fend(f_)
                cyc = [pcl[(n, q)] for q in range(f_[0], f_[1] + 1) if (n, q) in pcl]
                row = [name, s, n, '分' if isS else '-', round(pk), f'{e[0]}〜{e[1]}', f'{f_[0]}〜{f_[1]}', f'Q{lo}〜Q{hi}', G, qpk, g(f40), g(fpk), g(infl), g(fli), g(wo),
                       qsp, g(spk), dry, e[0], f_[0], (dry - qsp) if dry != '' else '', (e[0] - dry) if dry != '' else '', f_[0] - e[0], g(fe0), g(fe1), g(fe1 / fe0) if fe0 > 0 else '',
                       g(req), g(paid)]
                cq = [(q, agg[(n, q)]) for q in range(lo, hi + 1) if (n, q) in agg]
                if cq:
                    q, a = min(cq, key=lambda t: t[1][0]); sv = a[0]
                    a0_ = agg.get((n, lo), cq[0][1])[0]
                    nc = sum(v[1] - v[2] for _, v in cq); ni = sum(v[3] - v[4] for _, v in cq)
                    row += [q, g(sv), g(a[1]), g(a[2]), g(a[3]), g(a[4]), g(a[5]), g(a[6]),
                            g((a[1] - a[2]) / sv) if sv > 0 else 'inf', g((a[3] - a[4]) / sv) if sv > 0 else 'inf', g((a[5] + a[3]) / sv) if sv > 0 else 'inf',
                            g(nc / a0_) if a0_ > 0 else 'inf', g(ni / a0_) if a0_ > 0 else 'inf', g(a0_)]
                else: row += ['cearn なし'] + [''] * 13
                row += [how, g(min(cyc)) if cyc else 'なし', up]
                fp.write('\t'.join(map(str, row)) + '\n')
    fs.write(f'# {name} {d} 種 {a0}〜{a1}\n')
    fs.write(f'{name}\t国×種 {T["nat"]}（分 {T["sec"]}）\tE {T["E"]}\tF {T["F"]}\tG {T["G"]}（分 {T["Gsec"]}）\tE∧F(±0) {T["EF0"]}\tE∧F(±8) {T["EF8"]}（分 {T["secEF8"]}・組 {T["pairs8"]}）\tE∧F(±16) {T["EF16"]}\tE∧F∧G(±8) {T["EFG8"]}（分 {T["secEFG8"]}）\tFの区間 {T["Fruns"]}\t終わり方 {endc}\t床の行の写し {T.get("pclOK", 0)}/{T.get("pclN", 0)}\n')
    fs.flush(); fp.flush(); ff.flush(); fg.flush()
