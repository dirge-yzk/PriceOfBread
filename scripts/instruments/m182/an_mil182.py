"""(180) 線M の読み手（読むだけ）＝inj_mil8 の A 行（列 0〜84）と J 行。徴兵法（封建の兵から国民の兵へ）を 10 カ国で読む。
  ① 平時の志願／選抜／皆兵の割合%（10年の窓・種をまとめた平時の国×四半期）
  ② 平時に初めて皆兵になった年（種ごと・中央）｜史実の錨【推】
  ③ ゲームの中の農奴解放の年（全面になった年・限定になった年・種ごと・中央）｜史実
  ④ 戦時の法の上げ下げ（交戦中に上がった・戦争が終わった四半期に下がった・平時に上がった・平時に下がった）＝種あたり
  ⑤ 総動員（交戦中の対外の四半期のうち総動員の割合%）
  ⑥ 大戦の兵士÷人口の頂点（1914-1918 と 1939-1945 の四半期の最大・種の中央）｜史実【推】
  ⑦ 陸軍の不安の割合（列 80・平時の中央・10年の窓）と不安の全体の割合（列 54）
  ⑧ 日本の領土（本国・統合格・植民地格の州の数＝10年ごとの断面の種の中央）と統合格・植民地格の州の名前（J 行・種ごと・1873/1900/1920/1940）
python an_mil180.py <腕名=計器の出力ディレクトリ> [...] [--seeds 5] [--j 種]（(245) 旧の注入の行を標準の計器 SimCore/Instruments.cs の表へ畳んだ）"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as table_rows, num
args = sys.argv[1:]
maxsd = 99; jseeds = None
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
if '--j' in args:
    i = args.index('--j'); jseeds = [int(x) for x in args[i + 1].split(',')]; del args[i:i + 2]
ARMS = [a.split('=', 1) for a in args]
WATCH = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
DEC = [(y, y + 9) for y in range(1784, 1944, 10)] + [(1944, 1950)]
UNIV = {'プロイセン': '1814', 'ロシア': '1874', 'オーストリア': '1868', 'フランス': '1872（1798 ジュルダン法）', '日本': '1873', 'イギリス': '平時なし', 'アメリカ': '平時なし', 'オスマン': '平時なし（1843/48 籤）', 'スペイン': '1912', '清': '1933'}
SERF = {'プロイセン': '1807', 'ロシア': '1861', 'オーストリア': '1848', '日本': '1871', 'オスマン': '1839', 'フランス': '1789', 'スペイン': '1811/1837', '清': '—', 'イギリス': '開幕', 'アメリカ': '開幕'}
WWPEAK2 = {'プロイセン': '約13%（独 1943-44）', 'フランス': '約12%（1939-40）', 'イギリス': '約10%', 'ロシア': '約6%（ソ連）', 'オーストリア': '—（独に併合）', 'アメリカ': '約9%', '日本': '約8%', 'オスマン': '—', '清': '—', 'スペイン': '—'}   # ★(182) 反証役 vm2#3：第二次大戦の頂点【推・記憶の概数】
WWPEAK = {'プロイセン': '約10%（独 1914-18）', 'フランス': '約12%', 'イギリス': '約9%', 'ロシア': '約5%', 'オーストリア': '約9%', 'アメリカ': '約4%', '日本': '約0.5%（1914-18）', 'オスマン': '約4%', '清': '—', 'スペイン': '—'}


def load(D):
    out = []; jrows = []
    for sd, r in table_rows(D, 'nation', maxsd):
        q = int(r['quarter']) + 1   # 1 始まり（旧の A 行の q+1）
        out.append(dict(sd=sd, q=q, y=int(r['year']), n=r['nation'], alive=1 if num(r['totalPopulationK']) > 0 else 0, pop=num(r['populationK']), sold=num(r['soldierK']), war=num(r['atWarMonths']), cw=num(r['civilWarMonths']),
                        law=int(r['conscriptionLaw']), mob=int(r['mobilizedFlag']), share=num(r['securityThreatShare']), serf=int(r['serfEmancipation']), armsh=num(r['armyThreatShare']),
                        nc=int(r['coreRegions']), ni=int(r['integratedRegions']), ncol=int(r['colonialRegions'])))
        if q % 4 == 0:   # 年末の行（旧の J 行＝4四半期ごと）：統合格と植民地格の州の名前
            jrows.append(dict(sd=sd, q=q, y=int(r['year']), n=r['nation'], regs=[x for x in r['nonCoreRegions'].split(';') if x]))
    return out, jrows


def med(x):
    x = [v for v in x if v == v]
    return st.median(x) if x else float('nan')


def peace(r): return r['alive'] == 1 and r['war'] == 0 and r['cw'] == 0


DATA = [(name, *load(d)) for name, d in ARMS]
for name, R, J in DATA:
    print(f'######## {name}（種 {len({r["sd"] for r in R})}・行 {len(R)}）')

print('\n## ① 平時の徴兵法の割合%（志願/選抜/皆兵・10年の窓・種をまとめた平時の国×四半期）')
for name, R, J in DATA:
    print(f'#### {name}')
    print('  ' + ' '.join(f'{a:>11}' for a, _ in DEC))
    for n in WATCH:
        out = []
        for y0, y1 in DEC:
            v = [r for r in R if r['n'] == n and peace(r) and y0 <= r['y'] <= y1]
            if not v: out.append(f'{"-":>11}'); continue
            k = len(v); c = collections.Counter(r['law'] for r in v)
            out.append(f'{100 * c[0] / k:3.0f}/{100 * c[1] / k:3.0f}/{100 * c[2] / k:3.0f}')
        print(f'  {n}: ' + ' '.join(out))

def first_year(v, pred):
    for r in v:
        if pred(r): return r['y']
    return None


print('\n## ② 平時に初めて皆兵になった年（種ごと・無し＝—）｜種の中央（皆兵になった種の数）｜史実の錨【推】')
for name, R, J in DATA:
    print(f'#### {name}')
    by = collections.defaultdict(list)
    for r in R: by[(r['n'], r['sd'])].append(r)
    for n in WATCH:
        ys = []
        for sd in sorted({r['sd'] for r in R}):
            v = sorted(by.get((n, sd), []), key=lambda r: r['q'])
            ys.append(first_year(v, lambda r: peace(r) and r['law'] == 2))
        got = [y for y in ys if y is not None]
        print(f'  {n}: ' + ' '.join(str(y) if y else '—' for y in ys) + f'｜中央 {med(got) if got else "—"}（{len(got)}/{len(ys)}）｜史実 {UNIV.get(n, "")}')

print('\n## ③ ゲームの中の農奴解放の年（全面〔限定〕・種ごと・開幕から＝開）｜全面の種の中央（種の数）｜史実')
for name, R, J in DATA:
    print(f'#### {name}')
    by = collections.defaultdict(list)
    for r in R: by[(r['n'], r['sd'])].append(r)
    for n in WATCH:
        cells = []; fulls = []
        for sd in sorted({r['sd'] for r in R}):
            v = sorted(by.get((n, sd), []), key=lambda r: r['q'])
            if not v: cells.append('-'); continue
            if v[0]['serf'] == 2: cells.append('開'); continue
            yf = first_year(v, lambda r: r['serf'] == 2); yl = first_year(v, lambda r: r['serf'] >= 1) if v[0]['serf'] == 0 else None
            if yf: fulls.append(yf)
            cells.append((str(yf) if yf else '—') + (f'〔{yl}〕' if yl else ''))
        print(f'  {n}: ' + ' '.join(cells) + f'｜中央 {med(fulls) if fulls else "—"}（{len(fulls)}）｜史実 {SERF.get(n, "")}')

print('\n## ④ 戦時の法の上げ下げ（種あたり）＝交戦中に上がった｜戦争が終わった四半期に下がった｜平時に上がった｜平時に下がった｜交戦中に下がった')
for name, R, J in DATA:
    nsd = len({r['sd'] for r in R}) or 1
    by = collections.defaultdict(list)
    for r in R: by[(r['sd'], r['n'])].append(r)
    tot = collections.Counter(); per = collections.defaultdict(collections.Counter)
    for (sd, n), v in by.items():
        v.sort(key=lambda r: r['q'])
        for a, b in zip(v, v[1:]):
            if b['alive'] != 1 or a['alive'] != 1 or b['law'] == a['law']: continue
            bw = b['war'] > 0 or b['cw'] > 0; aw = a['war'] > 0 or a['cw'] > 0
            k = ('戦時に上' if b['law'] > a['law'] and bw else
                 '戦後に下' if b['law'] < a['law'] and aw and not bw else
                 '平時に上' if b['law'] > a['law'] else '平時に下' if not bw else '戦時に下')
            tot[k] += 1
            if n in WATCH: per[n][k] += 1
    keys = ['戦時に上', '戦後に下', '平時に上', '平時に下', '戦時に下']
    print(f'#### {name}')
    print('  全ての国（種あたり）: ' + '・'.join(f'{k} {tot[k] / nsd:.1f}' for k in keys))
    for n in WATCH:
        if per[n]: print(f'  {n}（種あたり）: ' + '・'.join(f'{k} {per[n][k] / nsd:.1f}' for k in keys if per[n][k]))

print('\n## ⑤ 総動員（交戦中の対外の四半期のうち総動員の割合%・括弧は国×四半期）')
for name, R, J in DATA:
    out = []
    for n in WATCH + ['全ての国']:
        v = [r for r in R if r['alive'] == 1 and r['war'] > 0 and r['cw'] == 0 and (n == '全ての国' or r['n'] == n)]
        if v: out.append(f'{n} {100 * sum(1 for r in v if r["mob"] == 1) / len(v):.0f}（{len(v)}）')
    print(f'  {name}: ' + '・'.join(out))

print('\n## ⑥ 大戦の兵士÷人口の頂点%（四半期の最大・種の中央〔最小〜最大〕）｜史実【推】')
for name, R, J in DATA:
    print(f'#### {name}')
    for (y0, y1) in [(1914, 1918), (1939, 1945)]:
        cells = []
        for n in WATCH:
            per = []
            for sd in sorted({r['sd'] for r in R}):
                v = [100 * r['sold'] / r['pop'] for r in R if r['sd'] == sd and r['n'] == n and r['alive'] == 1 and y0 <= r['y'] <= y1 and r['pop'] > 0]
                if v: per.append(max(v))
            if per: cells.append(f'{n} {med(per):.2f}〔{min(per):.2f}〜{max(per):.2f}〕' + (f'（史実 {WWPEAK[n]}）' if y0 == 1914 and n in WWPEAK else f'（史実 {WWPEAK2[n]}）' if y0 == 1939 and n in WWPEAK2 else ''))
        print(f'  {y0}-{y1}: ' + '・'.join(cells))

print('\n## ⑦ 陸軍の不安の割合（列 80・平時の中央）／不安の全体の割合（列 54）（10年の窓）')
for name, R, J in DATA:
    print(f'#### {name}')
    print('  ' + ' '.join(f'{a:>11}' for a, _ in DEC))
    for n in WATCH:
        out = []
        for y0, y1 in DEC:
            v = [r for r in R if r['n'] == n and peace(r) and y0 <= r['y'] <= y1]
            out.append(f'{med([r["armsh"] for r in v]):5.2f}/{med([r["share"] for r in v]):5.2f}' if v else f'{"-":>11}')
        print(f'  {n}: ' + ' '.join(out))

print('\n## ⑧ 日本の領土＝本国・統合格・植民地格の州の数（年末の断面・種の中央）と陸軍の不安の割合・徴兵法（種の中央）')
for name, R, J in DATA:
    print(f'#### {name}')
    for y in [1790, 1820, 1850, 1873, 1885, 1900, 1910, 1920, 1930, 1940, 1949]:
        v = [r for r in R if r['n'] == '日本' and r['y'] == y and r['alive'] == 1]
        last = {}
        for r in sorted(v, key=lambda r: r['q']): last[r['sd']] = r
        if not last: print(f'  {y}: -'); continue
        L = list(last.values())
        print(f'  {y}: 本国 {med([r["nc"] for r in L]):.0f}・統合 {med([r["ni"] for r in L]):.0f}・植民地 {med([r["ncol"] for r in L]):.0f}｜陸軍の不安 {med([r["armsh"] for r in L]):.2f}・全体 {med([r["share"] for r in L]):.2f}｜徴兵法 {med([r["law"] for r in L]):.0f}｜統合格がある種 {sum(1 for r in L if r["ni"] > 0)}/{len(L)}')
    seeds = jseeds or sorted({j['sd'] for j in J})[:3]
    for sd in seeds:
        for y in [1873, 1900, 1920, 1940]:
            v = [j for j in J if j['sd'] == sd and j['n'] == '日本' and j['y'] == y]
            if v: print(f'  種{sd} {y}: ' + ' '.join(v[-1]['regs']) if v[-1]['regs'] else f'  種{sd} {y}: （本国だけ）')
