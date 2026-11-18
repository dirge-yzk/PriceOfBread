# (1201) c120fm：計器 formation_s の集計（読むだけ）。成立の件数・規則・経路（従属国の州の数・使った従属国）・取り分を種ごとに出す。
#   python -X utf8 -I formsum.py <ins（D:/…）> <種の起点> <種の終わり>
import sys, csv, os, collections
sys.stdout.reconfigure(newline='\n')
INS, SB, SE = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
tot = collections.Counter(); miss = []
for s in range(SB, SE + 1):
    p = os.path.join(INS, f'formation_s{s}.tsv')
    if not os.path.exists(p): miss.append(s); continue
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        rows = [c for c in r if len(c) >= len(h)]
    for c in rows:
        used = c[ix['usedDependents']]
        route = '従属国を使った' if used else '自分の州だけ'
        tot['成立'] += 1; tot['rule:' + c[ix['newName']]] += 1; tot[route] += 1
        if float(c[ix['natShare']]) >= 0.25: tot['取り分≧0.25'] += 1
        print(f"s{s} {c[ix['year']]} {c[ix['baseName']]}→{c[ix['newName']]} 段{c[ix['tier']]} {c[ix['ruleSource']]} 州 自分{c[ix['ownRegionsCount']]}・従属国{c[ix['depRegionsCount']]} 使った[{used}] 取り分{float(c[ix['natShare']]):.4f} 宣戦の候補[{c[ix['unificationCandidate']]}]")
    if not rows: print(f"s{s} 成立 0")
print('合計', dict(tot), '表が無い種', miss)
