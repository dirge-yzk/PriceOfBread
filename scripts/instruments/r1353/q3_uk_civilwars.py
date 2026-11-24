# 問い3：英の内戦（civilWarFlag=1・sideA=イギリス）の長さ・判定・同じ四半期に反乱国へ向いた他国の戦争（入口の札）を腕ごとに並べる（読むだけ）
# verdictCode：0＝白紙（分断が固定）・1＝A（親）勝ち・2＝B（反乱）勝ち・-1＝終わっていない
import sys, os, csv, re, collections
print("腕\t種\t年\tq\t反乱国\t月数\t判定\t同じq±1に反乱国と戦った国(札)")
tally = collections.Counter()
for spec in sys.argv[1:]:
    name, d = spec.split('=', 1)
    for f in sorted(os.listdir(d)):
        m = re.match(r'wars_s(\d+)\.tsv$', os.path.basename(f))
        if not m: continue
        s = m.group(1)
        with open(os.path.join(d, f), encoding='utf-8') as fh:
            rows = list(csv.DictReader(fh, delimiter='\t'))
        for r in rows:
            if r['civilWarFlag'] == '1' and r['sideA'] == 'イギリス':
                q = int(r['q']); reb = r['sideB']
                others = [f"{x['sideA'] if x['sideB']==reb else x['sideB']}({x['entryCode']})" for x in rows if x is not r and (x['sideA'] == reb or x['sideB'] == reb) and abs(int(x['q']) - q) <= 1]
                print(f"{name}\t{s}\t{r['startYear']}\t{q}\t{reb}\t{r['months']}\t{r['verdictCode']}\t{' '.join(others) or '-'}")
                tally[(name, r['verdictCode'], int(r['months']) <= 3)] += 1
print("\n# 腕・判定・3か月以内か の件数")
for k, v in sorted(tally.items()): print(f"{k[0]}\t判定{k[1]}\t{'3か月以内' if k[2] else '4か月以上'}\t{v}")
