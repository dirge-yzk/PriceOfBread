# (638) 止める線 S2（発散）と記録の人口の倍率（読むだけ）。
# 使い方：python -X utf8 -I s2pop.py <計器の出力ディレクトリ>...
#   S2＝価格水準の四半期の比の最大（≥100・非有限・≥1e10 倍で止める）。記録＝主要国の人口の倍率（1913／1783 の四半期 0 と 520）の種の中央値。
import csv, sys, math, collections, statistics as st

KEY = ['イギリス', 'フランス', '清', 'アメリカ', '日本', 'プロイセン', 'ロシア', 'オーストリア', 'ネーデルラント']
for d in sys.argv[1:]:
    mx = (0.0, '', 0, 0); nonfin = 0; big = 0
    pop = collections.defaultdict(dict)   # nation -> seed -> (q0, q520)
    for s in range(1, 11):
        last = {}
        with open(f'{d}/nation_s{s}.tsv', encoding='utf-8') as fh:
            rdr = csv.reader(fh, delimiter='\t'); head = next(rdr); ix = {h: i for i, h in enumerate(head)}
            ip, iq, inat, ipop = ix['priceLevel'], ix['quarter'], ix['nation'], ix.get('populationK', ix.get('population'))
            for row in rdr:
                try:
                    p = float(row[ip])
                except Exception:
                    continue
                n = row[inat]; q = int(row[iq])
                if not math.isfinite(p):
                    nonfin += 1; continue
                if n in last and last[n] > 0:
                    r = p / last[n]
                    r = max(r, 1 / r) if r > 0 else float('inf')
                    if r > mx[0]:
                        mx = (r, n, s, q)
                    if r >= 100:
                        big += 1
                last[n] = p
                if ipop is not None and n in KEY and q in (0, 520):
                    pop[n].setdefault(s, {})[q] = float(row[ipop])
    print(d.split('/')[-2], '価格水準の四半期の比の最大', mx, '・非有限', nonfin, '・比≥100 の行', big)
    out = []
    for n in KEY:
        v = [x[520] / x[0] for x in pop[n].values() if 0 in x and 520 in x and x[0] > 0]
        if v:
            out.append(f'{n} {st.median(v):.2f}')
    print('  人口の倍率（q520／q0 の種の中央値）：' + '・'.join(out))
