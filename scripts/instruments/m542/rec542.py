# (542) 合流の記録の読み手（読むだけ・採否に使わない）。使い方：python rec542.py <基線の ins> <新しい ins> [ラベル基線 ラベル新]
#  ① 英の石炭の自己費用と値段の国の比（ivf_s の coalOwnCostMain・coalPriceOverRefMain・本土）＝英÷（仏・普・墺・露の中央値）・10 種の中央値・年の最後の四半期（Q0 は四半期 0）
#  ② 建設の束の配給率（ivf_s の bottleneckRationSum÷bottleneckMonths＝律速の財の配給率の平均）・年代
#  ③ 戦争の数（wars_s の行＝始まった戦争・内戦の旗で分ける）・種の中央値・年代
#  ④ 後継の武器の割合（wp_s の newShare＝新÷(旧＋新)・小銃と火砲）・国×年・10 種の中央値
#  ⑤ 蒸気機関の解禁の年（disc_s があれば）＝国ごとの中央値
#  基線に ivf_s・wp_s が無い表は新しい側だけを出す（m533 の計器に無い列）。
import csv, glob, os, sys, statistics as st
from collections import defaultdict
EU = ["フランス", "プロイセン", "オーストリア", "ロシア"]
SIX = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "清"]
def rows(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def seeds(d, t):
    return sorted(int(os.path.basename(p)[len(t) + 2:-4]) for p in glob.glob(os.path.join(d, f'{t}_s*.tsv')))
def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')
def ivf(d):
    out = {}
    for s in seeds(d, 'ivf'):
        last = {}
        for x in rows(os.path.join(d, f'ivf_s{s}.tsv')):
            q = int(x['quarter']); y = int(x['year']); k = (x['nation'], 'Q0' if q == 0 else y)
            if k not in last or q > last[k][0]: last[k] = (q, x)
        out[s] = {k: v for k, (q, v) in last.items()}
    return out
def coal(d, lab):
    t = ivf(d)
    if not t: print(f'  {lab}: ivf_s 無し'); return
    for col in ['coalOwnCostMain', 'coalPriceOverRefMain']:
        print(f'  {lab} {col}（英の値・英÷欧州4国の中央値・10 種の中央値）')
        for y in ['Q0', 1800, 1820, 1850, 1880, 1913]:
            uk = []; rat = []
            for s, m in t.items():
                u = fl(m.get(('イギリス', y), {}).get(col, 'nan'))
                eu = med([fl(m.get((n, y), {}).get(col, 'nan')) for n in EU])
                uk.append(u); rat.append(u / eu if eu == eu and eu else float('nan'))
            print(f'    {y}\t英 {med(uk):.3f}\t英÷欧州 {med(rat):.3f}')
def ration(d, lab):
    acc = defaultdict(lambda: defaultdict(list))
    for s in seeds(d, 'ivf'):
        sm = defaultdict(lambda: [0.0, 0.0])
        for x in rows(os.path.join(d, f'ivf_s{s}.tsv')):
            n = x['nation']
            if n not in SIX: continue
            y = int(x['year']); dec = (y - 1783) // 25
            sm[(n, dec)][0] += fl(x['bottleneckRationSum']) if fl(x['bottleneckRationSum']) == fl(x['bottleneckRationSum']) else 0
            sm[(n, dec)][1] += fl(x['bottleneckMonths']) if fl(x['bottleneckMonths']) == fl(x['bottleneckMonths']) else 0
        for (n, dec), (a, b) in sm.items():
            if b > 0: acc[n][dec].append(a / b)
    if not acc: print(f'  {lab}: ivf_s 無し'); return
    print(f'  {lab} 建設の束の律速の配給率（25 年ごと・10 種の中央値）')
    print('    国\t' + '\t'.join(f'{1783 + 25 * k}〜' for k in range(7)))
    for n in SIX:
        print(f'    {n}\t' + '\t'.join(f'{med(acc[n][k]):.3f}' for k in range(7)))
def wars(d, lab):
    per = defaultdict(lambda: defaultdict(int))
    ss = seeds(d, 'wars')
    for s in ss:
        for x in rows(os.path.join(d, f'wars_s{s}.tsv')):
            y = int(x['startYear']); dec = (y - 1783) // 25
            per[('内戦' if x['civilWarFlag'] == '1' else '国家間', dec)][s] += 1
            per[('全て', 'all')][s] += 1
            per[('内戦' if x['civilWarFlag'] == '1' else '国家間', 'all')][s] += 1
    print(f'  {lab} 戦争の数（始まった戦争・種の中央値）')
    for kind in ['国家間', '内戦']:
        print(f'    {kind}\t全期間 {med([per[(kind, "all")][s] for s in ss]):.1f}\t' + '\t'.join(f'{1783 + 25 * k}〜 {med([per[(kind, k)][s] for s in ss]):.1f}' for k in range(7)))
def weapons(d, lab):
    acc = defaultdict(list)
    ss = seeds(d, 'wp')
    if not ss: print(f'  {lab}: wp_s 無し'); return
    for s in ss:
        last = {}
        for x in rows(os.path.join(d, f'wp_s{s}.tsv')):
            if x['good'] not in ('小銃', '火砲') and not x['good'].isdigit(): pass
            k = (x['nation'], x['good'], int(x['year'])); q = int(x['quarter'])
            if k not in last or q > last[k][0]: last[k] = (q, fl(x['newShare']))
        for k, (q, v) in last.items(): acc[k].append(v)
    goods = sorted({k[1] for k in acc})
    print(f'  {lab} 後継の武器の割合 newShare（新÷(旧＋新)・年の最後の四半期・10 種の中央値）')
    for g in goods:
        print(f'    財 {g}')
        print('      国\t' + '\t'.join(str(y) for y in (1850, 1870, 1880, 1890, 1900, 1913, 1950)))
        for n in SIX + ['ロシア', 'オスマン', 'オーストリア']:
            print(f'      {n}\t' + '\t'.join(f'{med(acc.get((n, g, y), [])):.3f}' for y in (1850, 1870, 1880, 1890, 1900, 1913, 1950)))
def steam(d, lab, row='蒸気機関'):
    ss = seeds(d, 'disc')
    if not ss: return
    yr = defaultdict(list); by1800 = []
    for s in ss:
        got = {}
        for x in rows(os.path.join(d, f'disc_s{s}.tsv')):
            if x['kind'] == 'E' and x['row'] == row and x['nation'] not in got: got[x['nation']] = int(x['year'])
        for n in SIX + ['ロシア', 'オスマン', 'オーストリア']: yr[n].append(got.get(n, 9999))
        by1800.append(sum(1 for v in got.values() if v <= 1800))
    print(f'  {lab} {row} の解禁の年（E の行・未解禁は 9999・10 種の中央値）・1800 年までに解禁した国の数（表に出る国のみ）の中央値 {med(by1800):.1f}')
    print('    ' + '・'.join(f'{n} {med(yr[n]):.0f}' for n in SIX + ['ロシア', 'オスマン', 'オーストリア']))
if __name__ == '__main__':
    base, new = sys.argv[1], sys.argv[2]
    lb, ln = (sys.argv[3], sys.argv[4]) if len(sys.argv) > 4 else ('基線', '新')
    print('# ① 英の石炭'); coal(base, lb); coal(new, ln)
    print('# ② 建設の束の配給率'); ration(base, lb); ration(new, ln)
    print('# ③ 戦争の数'); wars(base, lb); wars(new, ln)
    print('# ④ 後継の武器の割合'); weapons(base, lb); weapons(new, ln)
    print('# ⑤ 発見の表'); steam(base, lb); steam(new, ln)
    for rw in ('転炉鋼', '機関車', '汽船'): steam(base, lb, rw); steam(new, ln, rw)
