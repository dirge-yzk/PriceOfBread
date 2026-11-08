# (966) F の区間を挿話にまとめる：同じ (盤面,種,国) の F の区間で、間が 8 四半期以下の物を 1 つの挿話にする（数える前に固定）。
#   終わり方＝最後の区間の終わり方（r966_ef.py の fend）。〈床から上がった〉は、その後に P≧1 へ戻ったか（戻った・戻らず）で分ける。
import sys, statistics as st
for p in sys.argv[1:]:
    rows = [l.rstrip('\n').split('\t') for l in open(p, encoding='utf-8')][1:]
    by = {}
    for r in rows: by.setdefault((r[0], r[1], r[2]), []).append(r)
    res = {}
    for (b, s, n), R in by.items():
        R.sort(key=lambda r: int(r[4].split('〜')[0])); ep = [[R[0]]]
        for r in R[1:]:
            if int(r[4].split('〜')[0]) - int(ep[-1][-1][4].split('〜')[1]) - 1 <= 8: ep[-1].append(r)
            else: ep.append([r])
        for e in ep:
            last = e[-1]; how = last[6]
            if how == '床から上がった': how = '上がりP≧1へ' if last[10].isdigit() else '上がりP≧1へ戻らず'
            d = res.setdefault(b, {}); d[how] = d.get(how, 0) + 1
            d['_n'] = d.get('_n', 0) + 1; d.setdefault('_up', [])
            if last[10].isdigit(): d['_up'].append(int(last[10]))
            if e[0][3] == '分': d['_sec'] = d.get('_sec', 0) + 1
    for b in sorted(res):
        d = res[b]; up = d.pop('_up'); n = d.pop('_n'); sec = d.pop('_sec', 0)
        print(b, f'挿話 {n}（分かれた国 {sec}）', *(f'{k} {v}' for k, v in sorted(d.items())), f'P≧1 までの中央 {st.median(up) if up else "-"}', sep='\t')
