# -*- coding: utf-8 -*-
"""(659) 線 fas 前半：ファシズムの思想が開く年の段0 の読み（読むだけ・コードの木は動かさない）。
使い方：PYTHONUTF8=1 python an_fas0.py <計器の出力フォルダ（idn_s・ideology_s・scalars_s・secession_s が要る）> [種の数=10]
出力：標準出力（measurements/fas659_*_read0.txt へ手で保存）。
注意：idn_s の year 列は ideology_s・scalars_s と同じ四半期番号で 1 四半期ぶんずれる（idn_s q155=1821・ideology_s q155=1822）。結合は四半期番号で行い、年は idn_s の列を使う。
開く条件の近似＝sFasNew>=0.15（技術の項が 70 に貼り付いている間は必要性の項>=30 すなわち強度>=0.15）。技術の項が 66 なら強度 0.19 が要る＝近似は緩い側。
"""
import csv, sys, collections, statistics as st

base = sys.argv[1]
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def era(y):
    return '1783-1819' if y < 1820 else '1820-49' if y < 1850 else '1850-79' if y < 1880 else '1880-1913' if y < 1914 else '1914-29' if y < 1930 else '1930-50'


ERAS = ['1783-1819', '1820-49', '1850-79', '1880-1913', '1914-29', '1930-50']


def load(name, s):
    d = {}
    with open(f'{base}/{name}_s{s}.tsv', encoding='utf-8') as f:
        for x in csv.DictReader(f, delimiter='\t'):
            d[(int(x['quarter']), x['nation'])] = x
    return d


def load_list(s):
    d = collections.defaultdict(list)
    with open(f'{base}/idn_s{s}.tsv', encoding='utf-8') as f:
        for x in csv.DictReader(f, delimiter='\t'):
            d[x['nation']].append(x)
    for l in d.values():
        l.sort(key=lambda x: int(x['quarter']))
    return d


def med(v):
    v = [a for a in v if a == a]
    return st.median(v) if v else float('nan')


idn = {s: load_list(s) for s in range(1, NS + 1)}
first = []   # (seed, nation, index, quarter, year)
for s, d in idn.items():
    for n, l in d.items():
        for i, x in enumerate(l):
            if x['knownFas'] == '1':
                first.append((s, n, i, int(x['quarter']), int(x['year'])))
                break
print(f'[A] ファシズムが開いた国×種 {len(first)} / {sum(len(d) for d in idn.values())}（国×種）')
byseed = collections.defaultdict(list)
for f in first:
    byseed[f[0]].append(f[4])
mins = {s: min(v) for s, v in byseed.items()}
print('    種ごとの世界で最初の年', dict(sorted(mins.items())), '中央', med(list(mins.values())), '／ 開かなかった種', [s for s in range(1, NS + 1) if s not in mins])
print('    国×種の開いた年の中央', med([f[4] for f in first]), '最小', min(f[4] for f in first), '最大', max(f[4] for f in first))
MAJ = ('イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'オスマン', 'アメリカ', '日本', '清')
print('    主要 9 国の開いた年（種, 国, 年）', sorted(((s, n, y) for s, n, i, q, y in first if n in MAJ), key=lambda t: t[2]))
print('    開いた国×種の一覧（年順・種, 国, 年）', [(s, n, y) for s, n, i, q, y in sorted(first, key=lambda t: t[4])])
_ys = sorted(f[4] for f in first)
print('    開いた年の分布：1850 年より前', sum(1 for y in _ys if y < 1850), '／1850-79', sum(1 for y in _ys if 1850 <= y < 1880), '／1880-99', sum(1 for y in _ys if 1880 <= y < 1900), '／1900-19', sum(1 for y in _ys if 1900 <= y < 1920), '／1920-39', sum(1 for y in _ys if 1920 <= y < 1940), '／1940 以後', sum(1 for y in _ys if y >= 1940), '／p25', _ys[len(_ys)//4], '／p75', _ys[3*len(_ys)//4])
print('    1919 年より前に開いた国×種', sum(1 for y in _ys if y < 1919), '／', len(_ys), '・世界で最初の年が 1919 年より前の種', sum(1 for v in mins.values() if v < 1919), '／', len(mins), '（史実の最初の年＝伊 1919〜22 との比べ・分母は全国で史実は 2 国＝比べ方に注意）')
cnt = collections.Counter()
for s, n, i, q, y in first:
    x = idn[s][n][i - 1]
    cnt[('need>=30' if fl(x['needFas']) >= 30 else 'need<30', 'trans>0' if fl(x['transFas']) > 0 else 'trans=0')] += 1
print('    開く直前の四半期の内訳（必要性の項×伝わりの項）', dict(cnt))
print('[B] 開く直前の四半期（q-1）の各項の中央［p10,p90］')
for c in ['crisis', 'popMil', 'decline', 'sFasNew', 'needFas', 'techFas', 'transFas', 'knownSoc', 'knownCom', 'knownImp']:
    v = sorted(a for a in (fl(idn[s][n][i - 1][c]) for s, n, i, q, y in first) if a == a)
    print(f'    {c:9s} {st.median(v):8.3f} [{v[len(v)//10]:.3f}, {v[9*len(v)//10]:.3f}]')

print('[C] 社会主義を知っている・まだ開いていない国×四半期の時代別の平均（left＝(社会主義+共産主義の国の割合)÷0.30 を 1 で頭打ち・max(敗戦,恐慌)＝crisis÷left）')
agg = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1, NS + 1):
    ide = load('ideology', s)
    sc = load('scalars', s)
    for n, l in idn[s].items():
        for x in l:
            if x['knownFas'] == '1' or x['knownSoc'] != '1':
                continue
            q = int(x['quarter'])
            i = ide.get((q, n))
            c = sc.get((q, n))
            if i is None or c is None:
                continue
            left = min(1.0, max(0.0, (fl(i['ideoNationalSocialismShare']) + fl(i['ideoNationalCommunismShare'])) / 0.30))
            cr = fl(x['crisis'])
            a = agg[era(int(x['year']))]
            a['left'].append(left)
            a['defeat'].append(min(1, max(0, fl(c['defeatShock']))))
            a['maxDS'].append(cr / left if left > 1e-9 else float('nan'))
            a['crisis'].append(cr)
            a['popMil'].append(fl(x['popMil']))
            a['decline'].append(fl(x['decline']))
            a['sFas'].append(fl(x['sFasNew']))
            a['pass'].append(1.0 if fl(x['sFasNew']) >= 0.15 else 0.0)
            a['left50'].append(1.0 if left >= 0.5 else 0.0)
            a['suff2'].append(1.0 if int(c['suffrage']) >= 2 else 0.0)
print('    時代          n   left defeat maxDS crisis popMil decline  sFas | 開く条件を満たす割合 | left>=0.5 の割合 | 参政権 段2 以上の割合')
for e in ERAS:
    a = agg[e]
    if not a['left']:
        continue

    def m(k):
        return st.mean(v for v in a[k] if v == v)
    print(f'    {e:10s} {len(a["left"]):5d} ' + ' '.join(f'{m(k):6.3f}' for k in ['left', 'defeat', 'maxDS', 'crisis', 'popMil', 'decline', 'sFas']) + f' | {m("pass"):.4f} | {m("left50"):.3f} | {m("suff2"):.3f}')

print('[D] 国力の在庫と今（decline の信号性）')
for s in (1, 5, 8):
    byq = collections.defaultdict(lambda: [0.0, 0.0])
    dl = []
    for n, l in idn[s].items():
        for x in l:
            a, b = fl(x['standing']), fl(x['scoreNow'])
            if a == a and b == b:
                r = byq[int(x['quarter'])]
                r[0] += a
                r[1] += b
                if a > 0.05:
                    dl.append(max(0.0, (a - b) / a))
    dl.sort()
    sums = [(q, round(byq[q][0], 2), round(byq[q][1], 2)) for q in (40, 200, 400, 660) if q in byq]
    print(f'    種{s}: 全国の和（q,在庫,今）{sums} ／ decline（在庫>0.05）中央 {dl[len(dl)//2]:.3f}・p75 {dl[3*len(dl)//4]:.3f}・0 の割合 {sum(1 for v in dl if v == 0)/len(dl):.3f}・0.3 超の割合 {sum(1 for v in dl if v > 0.3)/len(dl):.3f}')

print('[E] 静的な前検査（基線の軌道の上・開く条件の近似＝sFasNew>=0.15・動的な戻りは見ない・⚠基線の行と K1・K2・K4 は sFasNew>=0.15 だけ／前提の辺 K3 の行だけ技術の項>=60 を足す＝K3 の比べる元は『前提の辺 なし』の 1888 で『基線』の 1895 とは別）')
tot = 0
totc = 0
p0 = 0
p_cens = 0
p_am = 0
pc = 0
f0 = {}
fc = {}
fam = {}
fcons = {}
edge = {k: {} for k in ('base', 'com', 'imp', 'comimp')}
for s in range(1, NS + 1):
    sc = load('scalars', s)
    agg2 = collections.defaultdict(lambda: [0.0, 0.0])
    try:
        with open(f'{base}/secession_s{s}.tsv', encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='\t'):
                p, m_, c_ = fl(x['popK']), fl(x['militancyMeanIndex']), fl(x['consciousnessMeanIndex'])
                if p == p and m_ == m_ and c_ == c_:
                    a = agg2[(int(x['quarter']), x['nation'])]
                    a[0] += p
                    a[1] += p * m_ * c_
    except FileNotFoundError:
        pass
    b = {k: None for k in ('base', 'cens', 'am', 'cons')}
    eb = {k: None for k in edge}
    for n, l in idn[s].items():
        for x in l:
            if x['knownFas'] == '1':
                break
            q = int(x['quarter'])
            y = int(x['year'])
            sf = sc.get((q, n))
            if sf is None:
                continue
            tot += 1
            sg = int(sf['suffrage'])
            if fl(x['sFasNew']) >= 0.15:
                p0 += 1
                b['base'] = y if b['base'] is None else min(b['base'], y)
                if fl(x['techFas']) >= 60:
                    for k, cond in (('base', True), ('com', x['knownCom'] == '1'), ('imp', x['knownImp'] == '1'), ('comimp', x['knownCom'] == '1' and x['knownImp'] == '1')):
                        if cond:
                            eb[k] = y if eb[k] is None else min(eb[k], y)
                if sg >= 1:
                    p_cens += 1
                    b['cens'] = y if b['cens'] is None else min(b['cens'], y)
                if sg >= 2:
                    p_am += 1
                    b['am'] = y if b['am'] is None else min(b['am'], y)
            a2 = agg2.get((q, n))
            if a2 and a2[0] > 0:
                totc += 1
            if a2 and a2[0] > 0 and fl(x['crisis']) * (a2[1] / a2[0]) * fl(x['decline']) >= 0.15:
                pc += 1
                b['cons'] = y if b['cons'] is None else min(b['cons'], y)
    f0[s] = b['base']
    fc[s] = b['cens']
    fam[s] = b['am']
    fcons[s] = b['cons']
    for k in edge:
        edge[k][s] = eb[k]
print(f'    行 {tot}：開く条件を満たす {p0}／参政権 段1 以上でも {p_cens}／段2 以上でも {p_am}／意識で重みを付けた闘争度（popMil の置き換え）で {pc}／{totc}（意識の表が載る行だけが分母）')
for nm, d in (('基線', f0), ('参政権 段1 以上の門', fc), ('参政権 段2 以上の門', fam), ('意識つきの闘争度で置き換え', fcons)):
    v = [x for x in d.values() if x]
    print(f'    {nm:22s} 種ごとの最初の年 {dict(sorted(d.items()))} 中央 {med(v) if v else None}（開いた種 {len(v)}）')
for k, nm in (('base', '前提の辺 なし'), ('com', '前提の辺 共産主義'), ('imp', '前提の辺 帝国主義'), ('comimp', '前提の辺 共産主義＋帝国主義')):
    v = [x for x in edge[k].values() if x]
    print(f'    {nm:22s} 中央 {med(v) if v else None}（開いた種 {len(v)}）')

print('[F] 記録（ファシズムの政体・思想の割合・参政権）')
mx = []
poly = collections.Counter()
suffc = collections.defaultdict(collections.Counter)
for s in range(1, NS + 1):
    best = collections.defaultdict(float)
    with open(f'{base}/ideology_s{s}.tsv', encoding='utf-8') as f:
        for x in csv.DictReader(f, delimiter='\t'):
            v = fl(x['ideoNationalFascismShare'])
            if v > best[x['nation']]:
                best[x['nation']] = v
    mx.append(best)
    with open(f'{base}/scalars_s{s}.tsv', encoding='utf-8') as f:
        for x in csv.DictReader(f, delimiter='\t'):
            poly[x['polityCode']] += 1
            suffc[era(int(x['year']))][x['suffrage']] += 1
for th in (0.01, 0.05, 0.10):
    print(f'    ファシズムの国の割合の最大が {th} 以上の国×種 {sum(1 for b in mx for v in b.values() if v >= th)}／{sum(len(b) for b in mx)}')
print('    政体コード（0 絶対君主・1 専制・2 幕府・3 立憲君主・4 共和・5 ブルジョア独裁・6 プロレタリア独裁・7 軍事独裁・8 無政府）の国×四半期数', dict(sorted(poly.items())))
for e in ERAS:
    t = sum(suffc[e].values())
    print(f'    参政権の段の割合 {e}', {k: round(v / t, 3) for k, v in sorted(suffc[e].items())})

print('[G] 追記（反証役の点検の反映・読むだけ）')
# G1: 前提の辺の候補 K7＝ファシズムの前提にラジオ（大衆媒体・Catalog にあり After 電化製品）を置く形の静的な前検査＝ラジオが開いた国×種の数（disc_s の E 行）
rowc = collections.Counter()
for s_ in range(1, NS + 1):
    try:
        with open(f'{base}/disc_s{s_}.tsv', encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='	'):
                if x['kind'] == 'E':
                    rowc[x['row']] += 1
    except FileNotFoundError:
        pass
print('    K7 前提の辺 ラジオ（After 電化製品）：ラジオが開いた国×種', rowc.get('ラジオ', 0), '／電化製品', rowc.get('電化製品', 0), '／送電網', rowc.get('送電網', 0), '／自動車', rowc.get('自動車', 0), '／内燃機関', rowc.get('内燃機関', 0), '／機関銃', rowc.get('機関銃', 0), '（ファシズムが開いた国×種', len(first), 'に対し辺の親が開いている国は無い＝空の門）')
# G2: 意識（consciousnessMeanIndex）が時代で動くか＝K1 が空の理由が作りか信号かを分ける（popK 加重の平均・全州）
ce = collections.defaultdict(lambda: [0.0, 0.0])
for s_ in range(1, NS + 1):
    try:
        with open(f'{base}/secession_s{s_}.tsv', encoding='utf-8') as f:
            for x in csv.DictReader(f, delimiter='	'):
                p_, c_ = fl(x['popK']), fl(x['consciousnessMeanIndex'])
                if p_ == p_ and c_ == c_:
                    e_ = ce[era(int(x['year']))]
                    e_[0] += p_
                    e_[1] += p_ * c_
    except FileNotFoundError:
        pass
print('    意識の popK 加重平均（時代別）', {e: round(ce[e][1] / ce[e][0], 3) for e in ERAS if ce[e][0] > 0})
