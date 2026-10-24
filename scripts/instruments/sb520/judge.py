#!/usr/bin/env python
# (520) 交渉の取り分と地代の腕の判定の読み手：事前登録 measurements/sb520_20261024_prereg.txt [5] と 追記 3 の狙いの数字 U1〜U5・R1〜R3 を合否のブロックで出し、[6] の記録を載せる（読むだけ）。
# 使い方：PYTHONUTF8=1 python scripts/instruments/sb520/judge.py <insフォルダ> <種の数> [種の起点=1] [U4 の事件の最小数=5] [基線の insフォルダ（土地改革の段の到達の比べ用・既定なし）]
# 入力：sbu_s<種>.tsv（旗 UnionBargaining の腕）・sbr_s<種>.tsv・sbrreg_s<種>.tsv（旗 LandRentResidual の腕）・rule_s<種>.tsv（土地改革・農奴解放の段の到達）。無いファイルは飛ばす。U6 と R3 の網羅は番人（SbBargainTests）が担う。
# 追記 3 ⑸：恒等の線（U1・U2・U5・R1〜R3）と機構の線（U3・U4・番人）を分けて出す。恒等の線が全て緑でも〈働いた〉とは言わない。
import sys, csv, math, os, collections

def rows(path):
    if not os.path.exists(path): return []
    with open(path, encoding='utf-8', newline='') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))

def f(x):
    try: return float(x)
    except Exception: return float('nan')

def spearman(xs, ys):
    n = len(xs)
    if n < 3: return float('nan')
    def rank(v):
        o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
        while i < n:
            j = i
            while j + 1 < n and v[o[j + 1]] == v[o[i]]: j += 1
            for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0 + 1
            i = j + 1
        return r
    rx, ry = rank(xs), rank(ys)
    mx, my = sum(rx) / n, sum(ry) / n
    sx = math.sqrt(sum((a - mx) ** 2 for a in rx)); sy = math.sqrt(sum((b - my) ** 2 for b in ry))
    if sx == 0 or sy == 0: return float('nan')
    return sum((a - mx) * (b - my) for a, b in zip(rx, ry)) / (sx * sy)

def ok(b): return '通る' if b else '外れ'

def main():
    d = sys.argv[1]; n = int(sys.argv[2]); s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
    minEvents = int(sys.argv[4]) if len(sys.argv) > 4 else 5
    seeds = list(range(s0, s0 + n)); verdict = {}
    U = []
    for s in seeds:
        for r in rows(f'{d}/sbu_s{s}.tsv'):
            r['_seed'] = s; U.append(r)
    if U:
        # ---- 恒等の線（作りの上で必ず通る＝配線の取り違えを見る）
        # U1 禁止の月は行を積まない（regionMonths＝0 の行は全て 0）。四半期の途中で法が変わった行（四半期末が禁止で regionMonths＞0）は件数を記録する
        zero = [r for r in U if int(f(r['regionMonths'])) == 0]
        bad = [r for r in zero if abs(f(r['uSum'])) > 1e-12 or abs(f(r['ownerLoss'])) > 1e-12 or abs(f(r['wageGain'])) > 1e-12 or int(f(r['eligN'])) != 0]
        mixedEnd = [r for r in U if int(f(r['law'])) == 0 and int(f(r['regionMonths'])) > 0]
        verdict['U1'] = len(bad) == 0
        print(f'[恒等] U1 禁止の月だけの国×四半期 {len(zero)} 行のうち上乗せ・適格な工場が 0 でない行 {len(bad)} → {ok(verdict["U1"])}（四半期末が禁止で途中まで禁止でなかった行 {len(mixedEnd)} 件は記録）')
        # U2 保存：独立に積んだ 2 本（ownerLoss・wageGain）と uSum が一致
        bad = 0; mx = 0.0
        for r in U:
            u, o, w = f(r['uSum']), f(r['ownerLoss']), f(r['wageGain']); sc = max(1.0, abs(u))
            e = max(abs(u - o), abs(u - w), abs(o - w)) / sc; mx = max(mx, e)
            if e > 1e-9: bad += 1
        verdict['U2'] = bad == 0
        print(f'[恒等] U2 上乗せの和＝持ち主の減り＝労働者の増え（相対 1e-9）：外れた行 {bad}／{len(U)}・最大の相対差 {mx:.3e} → {ok(verdict["U2"])}（leakhunt の貨幣の保存則は別に確かめる）')
        # U5 純粋な容認の四半期（統制の月が無い）で β≧罷業の力を除いた β・純粋な統制の四半期（容認の月が無い）で等しい（四半期末の law でなく月の数で見る）
        bad = 0; nT = nC = nMix = 0
        for r in U:
            tm, cm = int(f(r['tolMonths'])), int(f(r['ctrlMonths'])); a, b2 = f(r['betaVa']), f(r['betaNoStrikeVa'])
            if tm > 0 and cm == 0:
                nT += 1
                if a < b2 - 1e-9 * max(1.0, abs(b2)): bad += 1
            elif cm > 0 and tm == 0:
                nC += 1
                if abs(a - b2) > 1e-9 * max(1.0, abs(b2)): bad += 1
            elif tm > 0 and cm > 0: nMix += 1
        verdict['U5'] = bad == 0
        print(f'[恒等] U5 純粋な容認 {nT} 行で β≧罷業の力を除いた β・純粋な統制 {nC} 行で等しい（容認と統制が混ざる行 {nMix} は除く）：外れた行 {bad} → {ok(verdict["U5"])}')
        # ---- 機構の線
        # U3 純粋な容認の国×四半期（統制の月が無い）で、κ＞0・超過＞0 なら上乗せが正。S＝0 か失業率 1 の行は 0 が正当＝記録に分ける。上乗せが正の行が 1 件も無ければ配線が働いていない
        tol = [r for r in U if int(f(r['tolMonths'])) > 0 and int(f(r['ctrlMonths'])) == 0]
        prem = [r for r in tol if f(r['kappaSum']) > 0 and f(r['excessSum']) > 0]
        miss = [r for r in prem if not (f(r['uSum']) > 0)]
        missS = [r for r in miss if f(r['sSum']) > 0 and f(r['uRateSum']) / max(1.0, f(r['eligN'])) < 1.0 - 1e-9]
        miss0 = [r for r in miss if r not in missS]
        pos = [r for r in tol if f(r['uSum']) > 0]
        verdict['U3o'] = len(missS) == 0 and len(pos) > 0
        frac = len(pos) / len(tol) if tol else float('nan')
        print(f'[機構] U3（事前登録の読み） 純粋な容認の国×四半期 {len(tol)} 行：前提（κ＞0・超過＞0）が立つ行 {len(prem)}・うち上乗せが 0 の行 {len(miss)}（S＞0 かつ失業率＜1 で 0 の行 {len(missS)}＝外れ／S＝0 か失業率が 1 の行 {len(miss0)}＝記録）／上乗せが正の行 {len(pos)}（容認の行の {frac:.1%}）→ {ok(verdict["U3o"])}（事前登録の読み＝行の集計で見る粗い前提・追記 4 で U3′ に読み替え）' + ('' if verdict['U3o'] else '（門は緑だが働かない）'))
        # U4 参政権の段が上がった前後 8 四半期の S の平均が下がらない（Census→AdultMale と AdultMale→Universal の両方・事件が少なければ判定しない＝番人に委ねる）。None→Census は記録
        by = collections.defaultdict(list)
        for r in U: by[(r['_seed'], r['nation'])].append(r)
        evs = {(1, 2): [0, 0], (2, 3): [0, 0]}; recCensus = [0, 0]
        for k, rs in by.items():
            rs.sort(key=lambda r: (int(f(r['year'])), int(f(r['quarter']))))
            sv = [(f(r['sSum']) / f(r['eligN'])) if f(r['eligN']) > 0 else float('nan') for r in rs]
            for i in range(1, len(rs)):
                t = (int(f(rs[i - 1]['suffrage'])), int(f(rs[i]['suffrage'])))
                if t not in evs and t != (0, 1): continue
                bf = [x for x in sv[max(0, i - 8):i] if x == x]; af = [x for x in sv[i:i + 8] if x == x]
                if not (bf and af): continue
                dn = sum(af) / len(af) < sum(bf) / len(bf) - 1e-12
                if t == (0, 1): recCensus[0] += 1; recCensus[1] += 1 if dn else 0
                else: evs[t][0] += 1; evs[t][1] += 1 if dn else 0
        ev = sum(v[0] for v in evs.values()); down = sum(v[1] for v in evs.values())
        if ev >= minEvents:
            verdict['U4o'] = down == 0
            print(f'[機構] U4（事前登録の読み） 参政権が上がった前後 8 四半期の S：Census→AdultMale 事件 {evs[(1, 2)][0]}（下がった {evs[(1, 2)][1]}）・AdultMale→Universal 事件 {evs[(2, 3)][0]}（下がった {evs[(2, 3)][1]}） → {ok(verdict["U4o"])}（事前登録の読み＝S の平均・追記 4 で U4′ に読み替え）')
        else:
            print(f'[機構] U4 事件 {ev} 件（{minEvents} 未満）＝判定しない（番人「参政権が上がってもSは下がらず…」が担う）')
        print(f'  記録：None→Census（近代エリートの声も上がる段）の事件 {recCensus[0]}・S が下がった {recCensus[1]}')
        if 'premN' in U[0]:
            # 追記 4：U3′＝工場ごとの前提（β＞0 かつ 超過＞0）が立つ工場×月で、持ち主の減りと労働者の増えが実際に出る。前提が立つ行で 1 工場でも欠ければ外れ・前提が立つ工場が 1 件も無ければ配線が働いていない
            pn = sum(f(r['premN']) for r in U); pp = sum(f(r['premPos']) for r in U)
            badrows = [r for r in U if f(r['premN']) > 0 and f(r['premPos']) < f(r['premN'])]
            verdict['U3'] = len(badrows) == 0 and pp > 0
            print(f"[機構] U3′ 工場ごとの前提（β＞0 かつ 超過＞0）の工場×月 {pn:.0f} 件のうち 持ち主の減りと労働者の増えが出た {pp:.0f} 件・欠けた行 {len(badrows)} → {ok(verdict['U3'])}")
            # U4″（追記 5）＝票の道だけの S（sVoteSum÷eligN）の前後。**行の参政権は四半期の末の値**＝段が上がった行は月の途中まで古い法で動いている＝その行は両側の窓から外す。
            #   前の窓＝段の直前の行から遡って参政権が前の段のままの行（最大 8）／後の窓＝段の次の行から参政権が新しい段のままの行（最大 8・段が戻ったら止める）。
            #   下がった（0.02 超）事件の割合が 10% 以下・AdultMale→Universal は 0.05 超の落ちが 0 件。事件が少なければ番人に委ねる。
            ev2 = {(1, 2): [0, 0], (2, 3): [0, 0, 0]}
            for k, rs in by.items():
                rs.sort(key=lambda r: (int(f(r['year'])), int(f(r['quarter']))))
                sv = [(f(r['sVoteSum']) / f(r['eligN'])) if f(r['eligN']) > 0 else float('nan') for r in rs]
                sf = [int(f(r['suffrage'])) for r in rs]
                for i in range(1, len(rs)):
                    t = (sf[i - 1], sf[i])
                    if t not in ev2: continue
                    bf = []; j = i - 1
                    while j >= 0 and len(bf) < 8 and sf[j] == t[0]:
                        if sv[j] == sv[j]: bf.append(sv[j])
                        j -= 1
                    af = []; j = i + 1
                    while j < len(rs) and len(af) < 8 and sf[j] == t[1]:
                        if sv[j] == sv[j]: af.append(sv[j])
                        j += 1
                    if not (bf and af): continue
                    dd = sum(bf) / len(bf) - sum(af) / len(af)
                    ev2[t][0] += 1
                    if dd > 0.02: ev2[t][1] += 1
                    if t == (2, 3) and dd > 0.05: ev2[t][2] += 1
            n2 = sum(v[0] for v in ev2.values()); d2 = sum(v[1] for v in ev2.values())
            if n2 >= minEvents:
                verdict['U4'] = d2 <= 0.10 * n2 and ev2[(2, 3)][2] == 0
                print(f"[機構] U4″ 票の道だけの S の前後（段の行を除く・最大 8 四半期）：Census→AdultMale 事件 {ev2[(1, 2)][0]}（0.02 超下がった {ev2[(1, 2)][1]}）・AdultMale→Universal 事件 {ev2[(2, 3)][0]}（0.02 超 {ev2[(2, 3)][1]}・0.05 超 {ev2[(2, 3)][2]}） → {ok(verdict['U4'])}")
            else:
                print(f"[機構] U4″ 事件 {n2} 件（{minEvents} 未満）＝判定しない（番人が担う）")
        # ---- 記録（採否に使わない）
        print('---- 記録：β（付加価値加重）の国×年代・κ・S・上乗せ÷付加価値・上乗せが正の工場の割合')
        agg = collections.defaultdict(lambda: [0.0] * 9)
        for r in U:
            if int(f(r['law'])) == 0 or f(r['eligN']) <= 0: continue
            e = int(f(r['year'])) // 25 * 25
            a = agg[(r['nation'], e)]
            a[0] += f(r['betaVa']); a[1] += f(r['vaSum']); a[2] += f(r['uSum']); a[3] += f(r['kappaSum']); a[4] += f(r['regionMonths']); a[5] += f(r['eligN']); a[6] += f(r['posN']); a[7] += f(r['sSum']); a[8] += f(r['guardN'])
        def q(x, y): return x / y if y else float('nan')
        for k in sorted(agg, key=lambda k: (k[1], k[0])):
            a = agg[k]
            if k[0] in ('イギリス', 'フランス', 'プロイセン', 'ドイツ', 'アメリカ', '日本', '清', 'ロシア'):
                print(f'  {k[0]}\t{k[1]}年代\tβ={q(a[0], a[1]):.4f}\tκ={q(a[3], a[4]):.3f}\tS={q(a[7], a[5]):.3f}\t上乗せ÷付加価値={q(a[2], a[1]):.4f}\t上乗せ正の工場={q(a[6], a[5]):.1%}\t守り={a[8]:.0f}')
        lawcount = collections.Counter((int(f(r['year'])) // 25 * 25, int(f(r['law']))) for r in U)
        print('  法の数（国×四半期・年代×法〔0 禁止・1 統制・2 容認〕）：' + ' '.join(f'{k[0]}:{k[1]}={v}' for k, v in sorted(lawcount.items())))
        # 向きの記録（採否に使わない）：β＝κ(1−U)S の定義から出る相関と、独立の読み
        tolrows = [r for r in U if int(f(r['tolMonths'])) > 0 and int(f(r['eligN'])) > 0 and f(r['vaSum']) > 0]
        if tolrows:
            bt = [f(r['betaVa']) / f(r['vaSum']) for r in tolrows]
            kt = [f(r['kappaSum']) / f(r['regionMonths']) for r in tolrows]
            st = [f(r['sSum']) / f(r['eligN']) for r in tolrows]
            ut = [f(r['uRateSum']) / f(r['eligN']) for r in tolrows]
            print(f'  容認の国×四半期 {len(tolrows)} 行の順位相関：(β,κ)={spearman(bt, kt):.3f}・(β,S)={spearman(bt, st):.3f}・(β,失業率)={spearman(bt, ut):.3f}（β の定義から出る相関＝独立の読みではない）')
            print(f'  独立の読み：容認の国×四半期の上乗せが正の工場の割合={sum(f(r["posN"]) for r in tolrows) / sum(f(r["eligN"]) for r in tolrows):.1%}・上乗せ÷付加価値={sum(f(r["uSum"]) for r in tolrows) / sum(f(r["vaSum"]) for r in tolrows):.4f}・守りが効いた件数={sum(f(r["guardN"]) for r in U):.0f}')
        tm = sum(f(r['tolMonths']) for r in U); cm = sum(f(r['ctrlMonths']) for r in U); rm = sum(f(r['regionMonths']) for r in U)
        mn = sum(f(r['massN']) for r in U); um = sum(f(r['urbanMassSum']) for r in U); fs = sum(f(r['facScan']) for r in U)
        ef = sum(f(r['exportFactory']) for r in U); ea = sum(f(r['exportAll']) for r in U)
        vs = sum(f(r['vaSum']) for r in U)
        print(f'  κ の分母（州の大衆の束の人数の和）のうち都市の割合={um / mn if mn else float("nan"):.1%}（限界＝農村と兵士で薄まる）・州×月 {rm:.0f}（容認 {tm:.0f}・統制 {cm:.0f}）')
        print(f'  輸出代金（PaySales）の工場分÷全体={ef / ea if ea else float("nan"):.1%}・工場の輸出代金÷適格な工場の国内の付加価値={ef / vs if vs else float("nan"):.3f}（上乗せの入口が無い分の大きさ）')
        ns = len({r["_seed"] for r in U}) or 1
        print(f'  計算量の census（種の平均）：州×月 {rm / ns:.4g}・運動体の走査 {fs / ns:.4g} 回・見積り ≒ {(fs + 40 * rm) * 3e-9 / ns:.2f} 秒（3ns×(facScan＋40×州×月)・登録の上限 +0.3 秒）')
    # ---- 段1c
    R = []
    for s in seeds:
        for r in rows(f'{d}/sbr_s{s}.tsv'):
            r['_seed'] = s; R.append(r)
    if R:
        bad = sum(1 for r in R if f(r['selfVa']) > 0 and f(r['selfResid']) > 1e-9 * max(1.0, f(r['selfVa'])))
        verdict['R1'] = bad == 0
        print(f'R1 自作農の産地で rent＋wages−付加価値＝0：外れた行 {bad}／{len(R)} → {ok(verdict["R1"])}（所得の恒等は番人「自作農の所得は変わらず地代の記録だけが増える」）')
        bad = sum(1 for r in R if abs(f(r['rgoVa']) - f(r['rgoRent']) - f(r['rgoWages'])) > 1e-9 * max(1.0, abs(f(r['rgoVa']))))
        verdict['R2'] = bad == 0
        print(f'R2 産地の rent＋wages＝付加価値（保存）：外れた行 {bad}／{len(R)} → {ok(verdict["R2"])}（貨幣の保存則は leakhunt）')
        bad = sum(1 for r in R if int(f(r['capN'])) > 0 and f(r['capErr']) / f(r['capN']) > 1e-9)
        eff = sum(1 for r in R if int(f(r['capN'])) > 0)
        verdict['R3'] = bad == 0
        print(f'R3 法の上限が 1 未満の産地で地代の取り分＝上限：外れた行 {bad}（上限が効いた行 {eff}） → {ok(verdict["R3"])}')
        print('---- 記録：地代の年代（付加価値に占める地代・旧支配層の所得・農村の大衆の所得）')
        agg = collections.defaultdict(lambda: [0.0] * 5)
        for r in R:
            e = int(f(r['year'])) // 25 * 25; a = agg[e]
            a[0] += f(r['rgoRent']); a[1] += f(r['rgoVa']); a[2] += f(r['closedEarned']); a[3] += f(r['ruralMassEarned']); a[4] += 1
        for e in sorted(agg):
            a = agg[e]
            print(f'  {e}年代\t地代÷付加価値={a[0] / a[1] if a[1] else float("nan"):.3f}\t旧支配層の所得（国×四半期の平均）={a[2] / a[4]:.4g}\t農村の大衆の所得={a[3] / a[4]:.4g}')
        xs, ys = [], []
        for s in seeds:
            for r in rows(f'{d}/sbrreg_s{s}.tsv'):
                c, rev, rent = f(r['crowd']), f(r['rev']), f(r['rent'])
                if c == c and rev > 0:
                    xs.append(c); ys.append(rent / rev)
        print(f'  混み具合（農村人口÷耕地）と 地代÷売上 の順位相関（州×四半期 {len(xs)} 行）：{spearman(xs, ys):.3f}（記録・向きが出なければ弾力が人為の項だった証拠）')
    # ---- 土地改革・農奴解放の段の到達（追記 3 ⑵：AI の余地は旧のまま＝旗 1c の経済の効果で改革の頻度が変わったかを基線と比べる・記録）
    base = sys.argv[5] if len(sys.argv) > 5 else None
    def reach(folder):
        res = {}
        for s in seeds:
            first = {}
            for r in rows(f'{folder}/rule_s{s}.tsv'):
                nat = r['nation']; y = int(f(r['year']))
                for code, key in (('landReformLawCode', 'land'), ('serfLawCode', 'serf')):
                    c = int(f(r[code]))
                    for lv in (1, 2):
                        if c >= lv and (nat, key, lv) not in first: first[(nat, key, lv)] = y
            res[s] = first
        return res
    if base and os.path.isdir(base) and os.path.exists(f'{d}/rule_s{s0}.tsv'):
        a, bb = reach(d), reach(base)
        print('---- 記録：土地改革・農奴解放の段の到達（国×種の件数と最初に到達した年の平均・腕 vs 基線）')
        for key in ('land', 'serf'):
            for lv in (1, 2):
                ya = [v for s in seeds for (n, k, l), v in a[s].items() if k == key and l == lv]
                yb = [v for s in seeds for (n, k, l), v in bb[s].items() if k == key and l == lv]
                ma = sum(ya) / len(ya) if ya else float('nan'); mb = sum(yb) / len(yb) if yb else float('nan')
                print(f'  {"土地改革" if key == "land" else "農奴解放"} 段≧{lv}：腕 {len(ya)} 件・平均 {ma:.1f} 年／基線 {len(yb)} 件・平均 {mb:.1f} 年')
    print('---- 総合：' + ' '.join(f'{k}={"○" if v else "×"}' for k, v in sorted(verdict.items())) + '（U3o・U4o＝事前登録の最初の読み・U3・U4＝追記 4 の読み）')

if __name__ == '__main__':
    main()
