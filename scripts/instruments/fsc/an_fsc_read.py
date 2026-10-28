"""(639) 線 fsc 段0（続き）：国庫の貯め込みが減らない理由の4つの読み。読むだけ。
入力＝計器つき10種の生の1フォルダ（m627_ins・N1・N2）。使い方：python an_fsc_read.py <ins フォルダ> [国名,...]
出力（種の中央値）：
 [A] 流入の内訳：(tax+tariff)÷全流入＝減税の反射が届く割合・専売÷全流入・国庫÷線が4以上の四半期の割合（減税の倍率が床 0.25 に座る）
 [B] 請求→支払い：行政と陸の請求のうち払われた割合（|台帳|÷請求）と行政の充足・官僚の覆い
 [C] 税の下げが門を通っているか：国庫÷線の帯（1未満・1〜2・2〜4・4以上）ごとの 土地税・人頭税 の 実効−目標 と 実効が目標より高い四半期の割合
 [D] 線÷年GDP と 国庫÷年GDP（十年の中央値）
 [E] 国庫が線の上にある国×四半期の割合（全国・fsx の treasuryOverLineFlag）と 国庫÷線が2以上・4以上の割合・国別（計器の9国）"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
d = sys.argv[1]
NAT = ['フランス', 'オーストリア', '日本', 'イギリス', 'アメリカ', 'プロイセン', 'ロシア', 'オスマン', '清']
if len(sys.argv) > 2: NAT = sys.argv[2].split(',')
def rows(f):
    with open(f, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r
def fl(x):
    try: return float(x)
    except: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
dec = lambda y: (int(y) // 10) * 10
files = sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv')), key=lambda s: int(re.search(r'_s(\d+)', s).group(1)))
A = {}; B = {}; C = {}; D = {}; E = {}
BANDS = [('<1', 0, 1), ('1-2', 1, 2), ('2-4', 2, 4), ('>=4', 4, 1e18)]
for ff in files:
    sd = int(re.search(r'_s(\d+)', ff).group(1))
    tl = {}; lineY = {}; trY = {}
    # fsx：全国の国庫÷線
    allq = {}   # nation -> [over, ge2, ge4, n]
    for ix, r in rows(os.path.join(d, f'fsx_s{sd}.tsv')):
        if not r[ix['time']].startswith('q'): continue
        n = r[ix['nation']]; t, l = fl(r[ix['treasuryEnd']]), fl(r[ix['lineEnd']])
        if not (l == l and l > 0 and t == t): continue
        x = allq.setdefault('*', [0, 0, 0, 0]); x[3] += 1; x[0] += (t > l); x[1] += (t >= 2 * l); x[2] += (t >= 4 * l)
        if n in NAT:
            y = allq.setdefault(n, [0, 0, 0, 0]); y[3] += 1; y[0] += (t > l); y[1] += (t >= 2 * l); y[2] += (t >= 4 * l)
            tl[(n, int(r[ix['year']]), r[ix['time']])] = t / l
            lineY.setdefault((n, dec(int(r[ix['year']]))), []).append(l)
    for k, v in allq.items(): E.setdefault(k, []).append([v[0] / v[3], v[1] / v[3], v[2] / v[3]])
    # nation：GDP・税の実効と目標・行政の充足
    gdp = {}; tq = {}
    for ix, r in rows(os.path.join(d, f'nation_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT: continue
        y = int(r[ix['year']]); g = fl(r[ix['nominalGdpPerYear']])
        if g == g: gdp.setdefault((n, dec(y)), []).append(g)
        q = r[ix['quarter']]
        tq[(n, y, q)] = (fl(r[ix['landTaxEndShare']]), fl(r[ix['landTaxTargetEndShare']]), fl(r[ix['pollTaxEndShare']]), fl(r[ix['pollTaxTargetEndShare']]),
                         fl(r[ix['adminFillMeanShare']]), fl(r[ix['adminBureaucratCoverShare']]))
    gdpD = {k: sum(v) / len(v) for k, v in gdp.items()}
    # 時刻 q... と quarter の対応：fsx の time は 'q<n>'、nation の quarter は数値。fsx は 'q'+quarter。
    acc = {}
    for (n, y, tm), v in tl.items():
        q = tm[1:]
        tx = tq.get((n, y, q))
        if tx is None: continue
        for nm, lo, hi in BANDS:
            if lo <= v < hi:
                a = C.setdefault((n, nm), {}).setdefault(sd, [0, 0, 0.0, 0.0, 0, 0.0, 0.0])
                a[0] += 1
                if tx[0] == tx[0] and tx[1] == tx[1]:
                    a[1] += (tx[0] > tx[1] + 1e-9); a[2] += tx[0]; a[3] += tx[1]
                if tx[2] == tx[2] and tx[3] == tx[3]:
                    a[4] += (tx[2] > tx[3] + 1e-9); a[5] += tx[2]; a[6] += tx[3]
    # 流入・請求→支払い
    for ix, r in rows(ff):
        n = r[ix['nation']]
        if n not in NAT: continue
        k = (n, dec(int(r[ix['year']])))
        a = acc.setdefault(k, {'m': 0, 'tax': 0.0, 'trf': 0.0, 'mono': 0.0, 'inflow': 0.0, 'cA': 0.0, 'pA': 0.0, 'cR': 0.0, 'pR': 0.0})
        a['m'] += 1
        tax, trf, mono = fl(r[ix['ledgerTaxPerMonth']]), fl(r[ix['ledgerTariffPerMonth']]), fl(r[ix['ledgerStateMonopolyPerMonth']])
        inflow = 0.0
        for c in ('ledgerTaxPerMonth', 'ledgerTariffPerMonth', 'ledgerStateMonopolyPerMonth', 'ledgerStateFactoryPerMonth', 'ledgerTributeInPerMonth', 'ledgerOtherInPerMonth'):
            v = fl(r[ix[c]]); inflow += v if v == v else 0.0
        a['tax'] += tax if tax == tax else 0.0; a['trf'] += trf if trf == trf else 0.0; a['mono'] += mono if mono == mono else 0.0; a['inflow'] += inflow
        for cn, pn, ck, pk in (('claimAdminPerMonth', 'ledgerAdminPerMonth', 'cA', 'pA'), ('claimArmyPerMonth', 'ledgerArmyPerMonth', 'cR', 'pR')):
            c_, p_ = fl(r[ix[cn]]), fl(r[ix[pn]])
            if c_ == c_ and p_ == p_: a[ck] += c_; a[pk] += -p_
    for k, a in acc.items():
        if a['inflow'] > 0:
            A.setdefault(k, []).append(((a['tax'] + a['trf']) / a['inflow'], a['mono'] / a['inflow']))
        B.setdefault(k, []).append((a['pA'] / a['cA'] if a['cA'] > 0 else float('nan'), a['pR'] / a['cR'] if a['cR'] > 0 else float('nan')))
    # D：線÷年GDP・国庫÷年GDP（fsx と nation の十年の中央値）
    trs = {}
    for ix, r in rows(os.path.join(d, f'fsx_s{sd}.tsv')):
        n = r[ix['nation']]
        if n not in NAT or not r[ix['time']].startswith('q'): continue
        k = (n, dec(int(r[ix['year']]))); g = gdpD.get(k)
        if not g: continue
        l, t = fl(r[ix['lineEnd']]), fl(r[ix['treasuryEnd']])
        if l == l and t == t: D.setdefault(k, {}).setdefault(sd, []).append((l / g, t / g))
print('# 入力', d, ' 種', len(files))
print('\n[A] (tax+tariff)÷全流入 と mono÷全流入（十年・種の中央値）')
for n in NAT:
    print(n, ' '.join('%d:%.2f/%.2f' % (y, med([x[0] for x in A.get((n, y), [])]), med([x[1] for x in A.get((n, y), [])])) for y in range(1780, 1950, 20) if (n, y) in A))
print('\n[B] 請求→支払い（払われた割合：行政／陸）')
for n in NAT:
    print(n, ' '.join('%d:%.2f/%.2f' % (y, med([x[0] for x in B.get((n, y), [])]), med([x[1] for x in B.get((n, y), [])])) for y in range(1780, 1950, 20) if (n, y) in B))
print('\n[C] 国庫÷線の帯ごと：四半期数(種の中央値)／土地税 実効>目標 の割合／土地税 実効平均・目標平均／人頭税 実効>目標 の割合／実効平均・目標平均')
for n in NAT:
    for nm, _, _ in BANDS:
        sdv = C.get((n, nm))
        if not sdv: continue
        L = list(sdv.values())
        cnt = med([a[0] for a in L])
        if not cnt or cnt < 8: continue
        print(f'{n} {nm}: n={cnt:.0f} land>tgt={med([a[1] / a[0] for a in L]):.2f} land={med([a[2] / a[0] for a in L]):.3f}/{med([a[3] / a[0] for a in L]):.3f}'
              f' poll>tgt={med([a[4] / a[0] for a in L]):.2f} poll={med([a[5] / a[0] for a in L]):.3f}/{med([a[6] / a[0] for a in L]):.3f}')
print('\n[D] 線÷年GDP と 国庫÷年GDP（十年の中央値）')
for n in NAT:
    out = []
    for y in range(1780, 1950, 20):
        v = D.get((n, y))
        if not v: continue
        lg = med([med([x[0] for x in s]) for s in v.values()]); tg = med([med([x[1] for x in s]) for s in v.values()])
        out.append('%d:%.3f/%.3f' % (y, lg, tg))
    print(n, ' '.join(out))
print('\n[E] 国庫が線の上にある国×四半期の割合／2倍以上／4倍以上（種の中央値）')
for k in ['*'] + NAT:
    v = E.get(k)
    if v: print(('全国' if k == '*' else k), '%.4f %.4f %.4f' % (med([x[0] for x in v]), med([x[1] for x in v]), med([x[2] for x in v])), ' 種別over:', ' '.join('%.3f' % x[0] for x in v))
