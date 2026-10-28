"""(631) 〈0 に張り付く〉の数え方：主要 9 国（計器つきの NATIONS）×十年（1800〜1949 の 15 個）×種の格子で、十年の最後の四半期の 国債÷年名目GDP が EPS（既定 0.02）未満の割合。
読むだけ。使い方：python zero_stick.py <ins フォルダ> [EPS]。国別の割合と、全体の割合を出す。nation_s の govDebtStock・nominalGdpPerYear を読む（NaN と GDP 0 の行は数えない）。"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
d = sys.argv[1]; EPS = float(sys.argv[2]) if len(sys.argv) > 2 else 0.02
NAT = ['アメリカ', 'イギリス', 'オスマン', 'オーストリア', 'フランス', 'プロイセン', 'ロシア', '日本', '清']
cell = {}   # (nation, decade) -> list of ratios (per seed)
for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda s: int(re.search(r'_s(\d+)', s).group(1))):
    last = {}
    with open(f, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd); ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) != len(h): continue
            n = r[ix['nation']]
            if n not in NAT: continue
            try: y = int(r[ix['year']]); q = int(r[ix['quarter']]); dbt = float(r[ix['govDebtStock']]); g = float(r[ix['nominalGdpPerYear']])
            except: continue
            if not (g > 0 and dbt == dbt and g == g): continue
            D = (y // 10) * 10
            if D < 1800 or D > 1940: continue
            if (n, D) not in last or q >= last[(n, D)][0]: last[(n, D)] = (q, dbt / g)
    for k, (q, v) in last.items(): cell.setdefault(k, []).append(v)
tot = stick = 0
print(f'EPS={EPS}  国\t割合（十年の終わりに 国債÷年GDP＜EPS の 種×十年 の割合）\t中央値（国債÷年GDP の全 種×十年）')
for n in NAT:
    v = [x for D in range(1800, 1950, 10) for x in cell.get((n, D), [])]
    if not v: continue
    s = sum(1 for x in v if x < EPS); tot += len(v); stick += s
    print(f'{n}\t{s / len(v):.3f}\t{st.median(v):.3f}')
print(f'全体\t{stick / tot:.3f}\t(n={tot})')
