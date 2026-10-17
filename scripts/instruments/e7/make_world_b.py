# -*- coding: utf-8 -*-
"""(332) 線 e7：腕 B の world.json を作る（国庫と国債を比から導く規則を入れる）。バイトで読み書きする（LF・BOM なしを保つ）。
使い方：python make_world_b.py <入力 world.json> <出力 world.json> [--only-uk]
  既定（腕 B）：全 61 国の "treasury": N を 0 にして "treasuryMonths" を足す（既定 2.5・プロイセン 28）／
                "govDebt" の 7 行を外し、18 国に "govDebtToGdpYear" を入れる。
  --only-uk（腕 B1）：イギリスの "govDebt" を外して "govDebtToGdpYear" を入れるだけ（国庫の月数・他の国は A のまま）。
値は measurements/e7_20261014_values.tsv（出典と確かさの表）。国ごとの特例のコードは書かない＝値は表の行。
"""
import sys, re, os
sp = os.path.dirname(os.path.abspath(__file__))
tsv = os.path.join(sp, '..', '..', '..', 'measurements', 'e7_20261014_values.tsv')
ratios, months, default_months = {}, {}, None
for ln in open(tsv, 'rb').read().decode('utf-8').splitlines()[1:]:
    c = ln.split('\t')
    if c[0] == 'govDebtToGdpYear': ratios[c[1]] = c[2]
    elif c[0] == 'treasuryMonths':
        if c[1].startswith('既定'): default_months = c[2]
        else: months[c[1]] = c[2]
assert default_months and ratios
src, dst = sys.argv[1], sys.argv[2]; only_uk = '--only-uk' in sys.argv
raw = open(src, 'rb').read(); assert not raw.startswith(b'\xef\xbb\xbf') and b'\r' not in raw
lines = raw.decode('utf-8').split('\n')
out, cur, seen_ratio, seen_treasury = [], None, set(), set()
for ln in lines:
    m = re.match(r'^   "name": "(.*)",$', ln)
    if m: cur = m.group(1)
    t = re.match(r'^   "treasury": [-0-9.eE+]+,$', ln)
    g = re.match(r'^   "govDebt": [-0-9.eE+]+,$', ln)
    if t and cur is not None:
        if only_uk:
            out.append(ln)
            if cur == 'イギリス': out.append('   "govDebtToGdpYear": %s,' % ratios[cur]); seen_ratio.add(cur)
        else:
            out.append('   "treasury": 0,'); out.append('   "treasuryMonths": %s,' % months.get(cur, default_months)); seen_treasury.add(cur)
            if cur in ratios: out.append('   "govDebtToGdpYear": %s,' % ratios[cur]); seen_ratio.add(cur)
        continue
    if g and cur is not None and (not only_uk or cur == 'イギリス'): continue   # 絶対額の国債は外す（比に置き換わる）
    out.append(ln)
assert seen_ratio == ({'イギリス'} if only_uk else set(ratios)), (seen_ratio, set(ratios) - seen_ratio)
assert only_uk or len(seen_treasury) == 61, len(seen_treasury)
open(dst, 'wb').write('\n'.join(out).encode('utf-8'))
print('OK', dst, 'ratios', len(seen_ratio), 'treasury', len(seen_treasury))
