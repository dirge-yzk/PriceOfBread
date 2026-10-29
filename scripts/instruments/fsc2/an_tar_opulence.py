"""(647) 線 fsc2 段0：潤沢の減税の倍率が関税の目標にも掛かっている（SelfPlay.RelieveOpulence の p.TariffTarget *= mul）ことの実測。読むだけ。
入力＝計器つき10種の生（fsx_s・scalars_s）。使い方：python -I an_tar_opulence.py <ins フォルダ> [国名,...]
出力：国ごとに、国庫÷線の帯（<1・1〜2・2〜4・4以上）別の 関税の目標 p.TariffTarget（scalars_s の tariffTargetIndex）の平均と行数（種をまとめた行の平均）。
帯が上がるほど目標が下がるなら〈国庫が潤沢だと関税の目標が下がる〉＝国内の利害と無関係な結合。"""
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
BANDS = [('<1', 0, 1), ('1-2', 1, 2), ('2-4', 2, 4), ('>=4', 4, 1e18)]
acc = {}
for ff in sorted(glob.glob(os.path.join(d, 'fsx_s*.tsv'))):
    sd = re.search(r'_s(\d+)', ff).group(1)
    tl = {}
    for ix, r in rows(ff):
        if not r[ix['time']].startswith('q'): continue
        t, l = fl(r[ix['treasuryEnd']]), fl(r[ix['lineEnd']])
        if l == l and l > 0 and t == t: tl[(r[ix['nation']], int(r[ix['time']][1:]))] = t / l
    sf = os.path.join(d, f'scalars_s{sd}.tsv')
    for ix, r in rows(sf):
        n = r[ix['nation']]
        if n not in NAT: continue
        k = (n, int(r[ix['quarter']]))
        if k not in tl: continue
        v = fl(r[ix['tariffTargetIndex']])
        if v != v: continue
        for nm, a, b in BANDS:
            if a <= tl[k] < b: acc.setdefault((n, nm), []).append(v); break
print('# 入力', d)
print('国 帯: 関税の目標の平均(行数)  ※関税の目標＝p.TariffTarget（四半期末）')
for n in NAT:
    print(n, ' '.join(f"{nm}:{st.mean(acc[(n, nm)]):.3f}({len(acc[(n, nm)])})" if (n, nm) in acc and acc[(n, nm)] else f'{nm}:-' for nm, _, _ in BANDS))
