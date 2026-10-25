# (550) 線 fb：工場の建屋の財（建造物）の読み手（読むだけ・採否の門を出す）。
# 使い方：python -X utf8 an_fb.py <ラベル=新の出力フォルダ> [基線=<基線の出力フォルダ>] [除く=1,...]
#   除く＝門の判定から外す種（既定 1＝canary と重なる種。事前登録 追記2）。門は残りの種で判定し、全ての種の読みは [記録] で並べる。
#   新の出力フォルダ：旗 BuildingGoodBundle=1 の計器つき実行の POB_INSTRUMENTS（ivf_s<種>.tsv・fb_s<種>.tsv・disc_s<種>.tsv がある）。
#   基線：旗 0 の同じ種の ivf_s<種>.tsv（開幕の束の値段と配給率の比べる側＝腕 A の生または m542 の生）。省略時は G1・G2 の向きの比べを出さない。
#   事前登録 measurements/fb550_20261025_prereg.txt [4] ＋ 追記2 の G1〜G6b と記録 R の定義と同じ。門の合否は [門] の行（OK／NG／判定不能）。
#   主要国：英＝イギリス・仏＝フランス・普＝プロイセン・米＝アメリカ・清・日＝日本・露＝ロシア。
#   ⚠ ivf_s・fb_s の行は挙げた国（POB_INSTRUMENTS_NATIONS）だけ毎四半期・他の国は年末（四半期 %4==3）。開幕（四半期 0〜11）の読みは挙げた国を前提にし、無ければ年末の行（3・7・11）で読む。
import csv, glob, os, sys, statistics as st, math
NAT = {"英": "イギリス", "仏": "フランス", "普": "プロイセン", "米": "アメリカ", "清": "清", "日": "日本", "露": "ロシア"}
G1_NATS = ["英", "仏", "普", "米", "清"]
G2_NATS = ["英", "仏", "普", "米"]
G3_NATS = ["英", "仏", "普", "米", "日", "清", "露"]
OPEN_Q = 11   # 開幕＝四半期 0〜11（1783〜85 年）
def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def seeds(d, pat):
    return sorted(int(os.path.basename(p)[len(pat):-4]) for p in glob.glob(os.path.join(d, pat + '*.tsv')))
def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')
def mean(v):
    v = [x for x in v if x == x]
    return sum(v) / len(v) if v else float('nan')
def ok(b): return 'OK' if b else 'NG'
def spearman(xs, ys):
    n = len(xs)
    if n < 3: return float('nan')
    def rank(v):
        o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
        while i < n:
            j = i
            while j + 1 < n and v[o[j + 1]] == v[o[i]]: j += 1
            for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0
            i = j + 1
        return r
    rx, ry = rank(xs), rank(ys); mx, my = mean(rx), mean(ry)
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); sx = sum((a - mx) ** 2 for a in rx); sy = sum((b - my) ** 2 for b in ry)
    return sxy / math.sqrt(sx * sy) if sx > 0 and sy > 0 else float('nan')

def ivf_open(d, s, col):
    """国→開幕の値の平均（四半期 0〜11）と、配給率（和÷月数）。"""
    out = {}; ration = {}
    acc = {}
    for x in rows(os.path.join(d, f'ivf_s{s}.tsv')):
        q = int(x['quarter'])
        if q > OPEN_Q: continue
        n = x['nation']
        v = fl(x.get(col, 'nan'))
        out.setdefault(n, []).append(v)
        a = acc.setdefault(n, [0.0, 0.0, [], []])
        a[0] += fl(x['bottleneckRationSum']) if fl(x['bottleneckRationSum']) == fl(x['bottleneckRationSum']) else 0.0
        a[1] += fl(x['bottleneckMonths']) if fl(x['bottleneckMonths']) == fl(x['bottleneckMonths']) else 0.0
        a[2].append(fl(x['steelDemand'])); a[3].append(fl(x['steelProd']))
    for n, v in out.items(): out[n] = mean(v)
    for n, a in acc.items(): ration[n] = (a[0] / a[1] if a[1] > 0 else float('nan'), mean(a[2]), mean(a[3]))
    return out, ration

def fb_by_year(d, s):
    """国→年→(行) ＝その年の最後の四半期の行。"""
    res = {}
    for x in rows(os.path.join(d, f'fb_s{s}.tsv')):
        n = x['nation']; y = int(x['year']); q = int(x['quarter'])
        cur = res.setdefault(n, {}).get(y)
        if cur is None or q > cur[0]: res[n][y] = (q, x)
    return {n: {y: v[1] for y, v in ys.items()} for n, ys in res.items()}

def share(x, k):
    tot = fl(x['outTotal'])
    return fl(x[k]) / tot if tot > 1e-9 else float('nan')

def rnd(x): return int(math.floor(x + 0.5))

def run(lab, d, ss, base, gate):
    """gate=True＝門として読む（[門] の行）。False＝全ての種を [記録] として並べる（門の語は出さない）。"""
    ns = len(ss); T = '門' if gate else '記録'
    need_g1 = rnd(0.9 * ns); need_g2 = rnd(0.8 * ns); need_g3e = rnd(0.8 * ns); max_g4 = rnd(0.3 * ns); max_g4b = rnd(0.4 * ns)
    print(f'## {lab}（種 {ss}・{"門の判定" if gate else "全ての種の記録"}・n={ns}）')
    # ---- G1・G2：開幕の束の値段と配給率
    g1 = {}; g2 = {}; g5 = []; bd = {}
    for s in ss:
        nb, rn = ivf_open(d, s, 'bundleBuildingOverRefMain')
        bb, rb = ivf_open(base, s, 'bundleSteelOverRefMain') if base and os.path.exists(os.path.join(base, f'ivf_s{s}.tsv')) else ({}, {})
        for k in set(G1_NATS) | set(G2_NATS):
            n = NAT[k]
            g1.setdefault(k, []).append((nb.get(n, float('nan')), bb.get(n, float('nan'))))
            g2.setdefault(k, []).append((rn.get(n, (float('nan'),))[0], rb.get(n, (float('nan'),))[0]))
        uk = rn.get(NAT['英'])
        if uk: g5.append(uk[1] - 1.3 * uk[2] if uk[2] == uk[2] and uk[1] == uk[1] else float('nan'))
    print('  [G1 開幕の束÷(参照×物価水準)] 種ごと（新／基線）')
    g1ok = True
    for k in G1_NATS:
        v = g1[k]; low = sum(1 for a, b in v if a == a and b == b and a < b)
        red = med([(1 - a / b) for a, b in v if a == a and b == b and b > 0])
        line = f'    {k}：新の中央値 {med([a for a, b in v]):.2f}・基線の中央値 {med([b for a, b in v]):.2f}・基線より低い種 {low}/{ns}・減り（中央値）{red * 100:.0f}%' if base else f'    {k}：新の中央値 {med([a for a, b in v]):.2f}'
        print(line)
        if base: g1ok &= (low >= need_g1 and red >= 0.25)
    uk_abs = med([a for a, b in g1['英']]); g1ok &= uk_abs <= 1.6
    print(f'  [{T}] G1 {ok(g1ok)}（5 国とも基線より低い種 ≥{need_g1}/{ns} かつ減りの中央値 ≥25% かつ 英の中央値 {uk_abs:.2f}≤1.6）' + ('' if base else '　※基線なし＝向きは読んでいない'))
    print('  [G2 開幕の建設の律速の配給率]')
    g2ok = True
    for k in G2_NATS:
        v = g2[k]; hi = sum(1 for a, b in v if a == a and b == b and a > b)
        print(f'    {k}：新の中央値 {med([a for a, b in v]):.2f}・基線の中央値 {med([b for a, b in v]):.2f}・基線より高い種 {hi}/{ns}')
        if base: g2ok &= hi >= need_g2
    print(f'  [{T}] G2 {ok(g2ok)}（英仏普米とも基線より高い種 ≥{need_g2}/{ns}）　[記録] 英の水準 {med([a for a, b in g2["英"]]):.2f}（目標 ≥0.8 は記録）')
    # ---- G3：段の産出の割合
    viol = 0; viol_rows = []
    mv = {}   # (段, 国, 種) → 門が開いた後の 10 年の動きの最大
    cast1830 = {}; late1913 = {}; rho = []; ratio_px = {}; px_all_bad = []
    STAGES = (('outCast', 'gateCast'), ('outSteel', 'gateSteel'), ('outRC', 'gateRC'))
    for s in ss:
        by = fb_by_year(d, s)
        for n, ys in by.items():
            for y, x in ys.items():
                for key, g_ in STAGES:
                    if fl(x[key]) > 1e-6 and fl(x[g_]) < 0.5:
                        viol += 1; viol_rows.append((s, n, y, key))
        for k in G3_NATS:
            n = NAT[k]; ys = by.get(n, {})
            for stage, g_ in STAGES:
                oy = next((y for y in sorted(ys) if fl(ys[y][g_]) > 0.5), None)
                if oy is None: continue
                best = None
                for y in sorted(ys):
                    if y >= oy and y + 10 in ys:
                        a, b = share(ys[y], stage), share(ys[y + 10], stage)
                        if a == a and b == b: best = abs(b - a) if best is None else max(best, abs(b - a))
                if best is not None: mv[(stage, k, s)] = best
            if 1830 in ys: cast1830[(k, s)] = share(ys[1830], 'outCast')
            if 1913 in ys:
                a, b = share(ys[1913], 'outSteel'), share(ys[1913], 'outRC')
                late1913[(k, s)] = (a if a == a else 0) + (b if b == b else 0)
            for y, x in ys.items():
                v = fl(x['buildPxOverRefMain'])
                if v == v and (v > 100 or v < 0.01): px_all_bad.append((s, k, y, v))
        for k in ('英', '仏', '普', '米'):
            vs = [fl(x['buildPxOverRefMain']) for y, x in sorted(by.get(NAT[k], {}).items()) if 1783 <= y <= 1800]
            vs = [v for v in vs if v == v and v > 0]
            if len(vs) >= 3: ratio_px[(k, s)] = max(vs) / min(vs)
        xs, ys_ = [], []
        for x in rows(os.path.join(d, f'fb_s{s}.tsv')):
            if 'costWood' not in x: break
            costs = [fl(x[c]) for c in ('costWood', 'costBrick', 'costCast', 'costSteel', 'costRC')]
            outs = [fl(x[c]) for c in ('outWood', 'outBrick', 'outCast', 'outSteel', 'outRC')]
            tot = fl(x['outTotal'])
            opened = [i for i, c in enumerate(costs) if c == c and c > 0]
            if len(opened) < 3 or not tot > 1.0: continue
            cmin = min(costs[i] for i in opened)
            for i in opened:
                xs.append(-(costs[i] / cmin)); ys_.append(outs[i] / tot)
        rho.append(spearman(xs, ys_) if len(xs) >= 10 else float('nan'))
    print(f'  [G3a 門の番人] 段の産出 >0 なのに門が閉じている国×年: {viol} 件' + ('' if viol == 0 else f' 例 {viol_rows[:3]}'))
    print(f'  [{T}] G3a {ok(viol == 0)}')
    # G3b：段ごと・門が開いた国×種だけを母数にする（開かない要素を 0 と数えない）
    g3b_ok = True; judged = 0
    for stage, nm in (('outCast', '鋳鉄の骨組み'), ('outSteel', '鋼の骨組み'), ('outRC', '鉄筋コンクリート造')):
        vals = [v for (st_, k, s), v in mv.items() if st_ == stage]
        if len(vals) >= 3:
            m = med(vals); judged += 1; g3b_ok &= (m <= 0.6)
            print(f'  [G3b {nm}] 門が開いた国×種 {len(vals)} 件・10 年の動きの最大の中央値 {m:.3f}（最大 {max(vals):.3f}）')
        else:
            print(f'  [G3b {nm}] 門が開いた国×種 {len(vals)} 件＝判定不能（3 件未満）' + (f'・最大 {max(vals):.3f}' if vals else ''))
    print(f'  [{T}] G3b {ok(g3b_ok) if judged >= 1 else "判定不能"}（判定できた段 {judged}・各段の中央値 ≤0.6）')
    # G3c：鋼の骨組みの門が開いた主要 7 国×種が 20 四半期後に採るか（開いた母数が 5 件未満は判定不能）
    opened = 0; taken = 0; never = 0
    for s_ in ss:
        rowsq = {}
        for x in rows(os.path.join(d, f'fb_s{s_}.tsv')):
            if x['nation'] in [NAT[k] for k in G3_NATS]: rowsq.setdefault(x['nation'], []).append((int(x['quarter']), x))
        for k in G3_NATS:
            lst = sorted(rowsq.get(NAT[k], []), key=lambda t: t[0])
            q_open = next((q for q, x in lst if fl(x['gateSteel']) > 0.5), None)
            if q_open is None: never += 1; continue
            later = [x for q, x in lst if q >= q_open + 20]
            if not later: continue
            opened += 1
            sh = share(later[0], 'outSteel')
            if sh == sh and sh > 0.05: taken += 1
    frac = taken / opened if opened else float('nan')
    l13 = [late1913.get((k, s_), float('nan')) for k in ('英', '米', '普') for s_ in ss]
    print(f'  [G3c 鋼の骨組みの門が開いた主要 7 国で 20 四半期後に割合>0.05] {taken}/{opened}（門が出力の期間内に開かなかった国×種 {never}）　[記録] 1913 の 鋼＋RC の割合 英米普×種の中央値 {med(l13):.2f}')
    print(f'  [{T}] G3c {"判定不能（開いた国×種が 5 件未満＝開かないことでは落とさない）" if opened < 5 else ok(frac >= 0.8)}（採られた割合 {frac:.2f}≥0.8）')
    uk30 = [cast1830.get(('英', s_), float('nan')) for s_ in ss]; cn30 = [cast1830.get(('清', s_), float('nan')) for s_ in ss]
    gt = sum(1 for a, b in zip(uk30, cn30) if a == a and b == b and a > b)
    cn_lead = sum(1 for a, b in zip(uk30, cn30) if a == a and b == b and b - a >= 0.15)
    print(f'  [G3d 1830 の鋳鉄の骨組みの割合] 英 ' + ' '.join(f'{v:.2f}' for v in uk30) + '　清 ' + ' '.join(f'{v:.2f}' for v in cn30) + f'　[記録] 英＞清 の種 {gt}/{ns}')
    print(f'  [{T}] G3d {ok(med(uk30) >= 0.15)}（英の中央値 {med(uk30):.2f}≥0.15＝門が開いた段が採られる）')
    print(f'  [{T}] G4b {ok(cn_lead <= max_g4b)}（1830 年の鋳鉄の骨組みの割合で清が英を 0.15 以上上回る種 {cn_lead}/{ns}≤{max_g4b}）')
    pos = sum(1 for r in rho if r == r and r > 0)
    print(f'  [G3e 費用と割合の順位相関 ρ] 種ごと ' + ' '.join(f'{r:+.2f}' for r in rho))
    print(f'  [{T}] G3e {ok(pos >= need_g3e)}（ρ＞0 の種 {pos}/{ns}≥{need_g3e}）')
    # ---- G4：鋳鉄の骨組みの最初の発見者
    first_cn = 0; firsts = []; tie_cn = 0
    for s_ in ss:
        got = {}
        for x in rows(os.path.join(d, f'disc_s{s_}.tsv')):
            if x['kind'] == 'E' and x['row'] == '鋳鉄の骨組み' and x['nation'] not in got: got[x['nation']] = int(x['quarter'])
        if got:
            q0 = min(got.values()); fs = sorted(n for n, q in got.items() if q == q0)
            firsts.append((s_, q0, fs))
            if '清' in fs: tie_cn += 1
            cq = got.get('清'); others = [got.get(NAT[k], 9999) for k in ('英', '仏', '普', '米')]
            if cq is not None and all(cq + 4 <= o for o in others): first_cn += 1
    print('  [G4 鋳鉄の骨組みの最初の発見者] ' + ' / '.join(f'種{s_}：Q{q}→{"・".join(f)}' for s_, q, f in firsts))
    print(f'  [{T}] G4 {ok(first_cn <= max_g4)}（清が英仏普米のどれよりも 4 四半期以上先の種 {first_cn}/{ns}≤{max_g4}）　[記録] 最初の四半期に清を含む種 {tie_cn}/{ns}')
    # ---- 転炉鋼：G3f′（進行不能の番人）と R5（1913 年までの数は記録）
    yrs = []; rc_yrs = []
    for s_ in ss:
        y = 9999; yh = 9999
        for x in rows(os.path.join(d, f'disc_s{s_}.tsv')):
            if x['kind'] == 'E' and x['nation'] == NAT['英']:
                if x['row'] == '転炉鋼' and y == 9999: y = int(x['year'])
                if x['row'] == '鉄筋コンクリート' and yh == 9999: yh = int(x['year'])
        yrs.append(y); rc_yrs.append(yh)
    never_b = sum(1 for y in yrs if y >= 9999)
    print('  [R5 英の転炉鋼の発見の年（9999＝出力の期間内に未発見）] ' + ' '.join(str(y) for y in yrs) + f'　1913 年までの種 {sum(1 for y in yrs if y <= 1913)}/{ns}（記録）／英の鉄筋コンクリート（住居の行）の年 ' + ' '.join(str(y) for y in rc_yrs))
    print(f'  [{T}] G3f′ {ok(never_b == 0)}（英が転炉鋼を出力の期間内〔〜1950〕に一度も発見しない種 {never_b}/{ns}＝0 が要る・進行不能の番人）')
    print(f'  [記録] G5（旧・英の鋼の 需要−1.3×産出 開幕・月あたり）種ごと ' + ' '.join(f'{v:.2f}' for v in g5) + f'・中央値 {med(g5):.2f}')
    # ---- G6・G6b・S2b
    px = []
    for s in ss:
        by = fb_by_year(d, s)
        for k in ('英', '仏', '普', '米'):
            for y, x in by.get(NAT[k], {}).items():
                if 1783 <= y <= 1800: px.append(fl(x['buildPxOverRefMain']))
    print(f'  [G6 建造物の値段÷(参照×物価水準) 1783〜1800・英仏普米の年末] 中央値 {med(px):.2f}（範囲 {min(px) if px else float("nan"):.2f}〜{max(px) if px else float("nan"):.2f}）')
    print(f'  [{T}] G6 {ok(0.7 <= med(px) <= 1.5)}（0.7〜1.5）')
    rv = list(ratio_px.values())
    print(f'  [{T}] G6b {ok(med(rv) <= 15)}（英仏普米×種の 1783〜1800 年末の値段の 最大÷最小 の中央値 {med(rv):.1f}≤15・最大 {max(rv) if rv else float("nan"):.1f}）')
    print(f'  [{"S2b 発散" if gate else "記録"}] 主要 7 国の年末の建造物の値段÷参照が 100 倍超または 1/100 未満の国×年: {len(px_all_bad)} 件' + ('' if not px_all_bad else f' 例 {px_all_bad[:3]}'))
    # ---- 記録
    print('  [R1 段の割合（英・仏・清）の年代]')
    for k in ('英', '仏', '清'):
        for s in ss[:3]:
            ys = fb_by_year(d, s).get(NAT[k], {})
            line = ' '.join(f'{y}:' + '/'.join(f'{share(ys[y], c):.2f}' for c in ('outBrick', 'outCast', 'outSteel', 'outRC')) for y in (1790, 1800, 1830, 1850, 1873, 1900, 1913) if y in ys)
            print(f'    {k} 種{s}（煉瓦/鋳鉄/鋼/RC）{line}')
    steel = {}
    for x in rows(os.path.join(d, f'ivf_s{ss[0]}.tsv')):
        if x['nation'] == NAT['英'] and int(x['quarter']) % 4 == 3: steel[int(x['year'])] = (fl(x['steelDemand']), fl(x['steelProd']))
    print('  [R2 英の鋼（需要/産出・種 ' + str(ss[0]) + '）] ' + ' '.join(f'{y}:{steel[y][0]:.1f}/{steel[y][1]:.1f}' for y in (1784, 1790, 1800, 1850, 1873, 1900, 1913) if y in steel))
    # R3：建造物の市場の過剰（開幕の需要÷産出・在庫の平均）
    print('  [R3 建造物の市場（開幕 Q0〜11 の 需要÷産出〔月あたりの和の比〕／在庫の州平均 の種の中央値）]')
    for k in ('英', '仏', '普', '米', '清'):
        dp = []; stk = []
        for s in ss:
            sd = sp = 0.0; sm = []
            for x in rows(os.path.join(d, f'fb_s{s}.tsv')):
                if x['nation'] == NAT[k] and int(x['quarter']) <= OPEN_Q:
                    sd += fl(x['buildDemand']); sp += fl(x['buildProd']); sm.append(fl(x['buildStockMean']))
            dp.append(sd / sp if sp > 0 else float('nan')); stk.append(mean(sm))
        print(f'    {k}：需要÷産出 {med(dp):.2f}・在庫（州平均）{med(stk):.0f}')
    # R4：材料の需給（開幕・英）
    mats = {'製材': ('lumDemand', 'lumProd'), '石灰石': ('limDemand', 'limProd'), '粘土': ('clayDemand', 'clayProd'), 'セメント': ('cementDemand', 'cementProd')}
    out = []
    for nm, (dc, pc) in mats.items():
        vv = []
        for s in ss:
            sd = sp = 0.0
            for x in rows(os.path.join(d, f'fb_s{s}.tsv')):
                if x['nation'] == NAT['英'] and int(x['quarter']) <= OPEN_Q: sd += fl(x[dc]); sp += fl(x[pc])
            vv.append(sd / sp if sp > 0 else float('nan'))
        out.append(f'{nm} {med(vv):.2f}')
    print('  [R4 英の材料の需要÷産出（開幕 Q0〜11・種の中央値）] ' + '・'.join(out))

def main():
    args = sys.argv[1:]
    base = None; labs = []; excl = {1}
    for a in args:
        if a.startswith('基線='): base = a.split('=', 1)[1]
        elif a.startswith('除く='): excl = {int(t) for t in a.split('=', 1)[1].split(',') if t}
        elif '=' in a: labs.append(a.split('=', 1))
    for lab, d in labs:
        allss = seeds(d, 'fb_s')
        gs = [s for s in allss if s not in excl]
        run(lab, d, gs, base, True)
        if len(gs) != len(allss): run(lab + '〔全ての種〕', d, allss, base, False)
main()
