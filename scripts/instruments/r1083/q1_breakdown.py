# (1083) 問い 1・2：開幕の四半期の〈影で持ち主の側が最大でない〉行の内訳と、外国の行の詳細・同点の差の分布。
# 使い方: python -X utf8 -I q1_breakdown.py open_rows.tsv > q1_out.txt
# 読む列は全て rulepower_s の G10 の値（計器の書式）＝G10 で等しい＝相対差 約 1e-10 未満（それより細かい差はこの表から見えない＝限界として名乗る）。
import csv, sys, collections
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t'))
def f(x):
    try: return float(x)
    except: return float('nan')
print('開幕の行', len(rows))
cnt = collections.Counter(); cross = collections.Counter(); groups = collections.defaultdict(list)
for x in rows:
    b = x['bestClass']
    if b == 'owner': continue
    o, u, fo, lo = f(x['ownerSideShare']), f(x['ungovSideShare']), f(x['foreignSideShare']), f(x['localSideShare'])
    fs = f(x['fStateIndex'])
    if x['nowBestClass'] == 'ungov': k = 'A 今の Rule でも無統治'
    elif not (fs > 0): k = 'C F_S=0'
    elif b == 'ungov' and o == u: k = 'B 持ち主と無統治の同点（G10 で等しい）'
    elif b == 'foreign': k = 'D O の配りで外国'
    elif b == 'local': k = 'E 自立した在地'
    else: k = 'F 無統治（同点でない）'
    cnt[k] += 1; cross[(k, b, x['nowBestClass'])] += 1; groups[k].append(x)
print('非持ち主', sum(cnt.values()), '／今の最大の区分（開幕の全行）', dict(collections.Counter(x['nowBestClass'] for x in rows)), '／影の最大の区分（開幕の全行）', dict(collections.Counter(x['bestClass'] for x in rows)))
for k in sorted(cnt): print(' ', k, cnt[k])
print('区分×影の最大×今の最大'); [print('  ', k, v) for k, v in sorted(cross.items())]

# ---- 外国の行 D ----
D = groups['D O の配りで外国']
print('\n[外国の行 D] 種ごとの件数', sorted(collections.Counter(int(x['seed']) for x in D).items()))
by = collections.Counter((x['nation'], x['region'], x['tier'], x['foreignTopKey']) for x in D)
print('州（持ち主・州・格・最大の外国）×種の数（10 種のうち）')
for (n, r, t, k), v in sorted(by.items(), key=lambda kv: (-kv[1], kv[0])): print(f'  {n}\t{r}\t{t}\t{k}\t{v}')
print('最大の外国の国（延べ）', collections.Counter(x['foreignTopKey'] for x in D).most_common())
print('持ち主の国（延べ）', collections.Counter(x['nation'] for x in D).most_common())
print('格（延べ）', collections.Counter(x['tier'] for x in D).most_common())
# 外国の行の力の比：F_f(和)÷(F_S+κF_I)・ρ・配り前の値
def ratio_rows(xs):
    out = []
    for x in xs:
        own = f(x['fStateIndex']) + f(x['kappaIndex']) * f(x['fInterIndex'])
        out.append((f(x['fForeignSumIndex']) / own if own > 0 else float('inf'), f(x['fForeignRawIndex']) / own if own > 0 else float('inf'), f(x['rhoNewIndex']), f(x['nowRule2Share']), x))
    return out
rr = ratio_rows(D)
import statistics as st
print('F_f(ρ 2 回)÷持ち主の側の力：中央', round(st.median(r[0] for r in rr), 3), '最小', round(min(r[0] for r in rr), 3), '最大', round(max(r[0] for r in rr), 3))
print('今の Rule[2]（外国の取り分のストック）>0 の行', sum(1 for r in rr if r[3] > 0), '/', len(rr))

# ---- 同点 B：差の分布（G10 の表から見える範囲） ----
print('\n[同点 B] 力の比 F_S:κF_I:F_U（＝RuleInit[0]:RuleInit[1]:RuleInit[3]・今の Rule は助走で動いた値で RuleInit ではない）の延べ')
print(' ', collections.Counter(tuple(round(f(x[k]) / f(x['fUngovIndex']), 3) for k in ('fStateIndex', 'fInterIndex')) for x in groups['B 持ち主と無統治の同点（G10 で等しい）']).most_common(6))
tie_all = [x for x in rows if f(x['ownerSideShare']) > 0 and f(x['ownerSideShare']) == f(x['ungovSideShare'])]
print(' G10 で持ち主＝無統治の行の全体', len(tie_all), '＝州', len({x['region'] for x in tie_all}), '×種・影の最大の区分', dict(collections.Counter(x['bestClass'] for x in tie_all)))
print(' 州', collections.Counter((x['nation'], x['region']) for x in groups['B 持ち主と無統治の同点（G10 で等しい）']).most_common(20))
# 開幕の全ての行で、持ち主の割合と無統治の割合の相対差の分布（無統治が 2 番目以上に大きい行）
bins = collections.Counter()
for x in rows:
    o, u = f(x['ownerSideShare']), f(x['ungovSideShare'])
    if not (o > 0 and u > 0): continue
    d = abs(o - u) / max(o, u)
    lab = '0（G10 で等しい）' if d == 0 else '<1e-9' if d < 1e-9 else '<1e-6' if d < 1e-6 else '<1e-3' if d < 1e-3 else '<1e-2' if d < 1e-2 else '≧1e-2'
    bins[(lab, x['bestClass'])] += 1
print('持ち主と無統治の相対差 |o−u|÷max の分布（開幕の全行・影の最大の区分別）')
for k in ['0（G10 で等しい）', '<1e-9', '<1e-6', '<1e-3', '<1e-2', '≧1e-2']:
    print(' ', k, {b: v for (l, b), v in bins.items() if l == k})
# 許容差 tol を置いたら区分が変わる行（同点は若い番号＝持ち主へ）
for tol in (1e-12, 1e-9, 1e-6, 1e-3):
    ch = 0
    for x in rows:
        if x['bestClass'] != 'ungov': continue
        o, u = f(x['ownerSideShare']), f(x['ungovSideShare'])
        lo, fo = f(x['localSideShare']), f(x['foreignSideShare'])
        if u - o <= tol * max(o, u) and o >= lo and o >= fo: ch += 1
    print(f'  許容差 {tol:g}（相対）：無統治→持ち主へ変わる行 {ch}')
# ---- C（F_S=0）と E（自立した在地）と A（今でも無統治）の州 ----
for k in ('A 今の Rule でも無統治', 'C F_S=0', 'E 自立した在地'):
    print(f'\n[{k}] 州（持ち主・州・格）×種', collections.Counter((x['nation'], x['region'], x['tier']) for x in groups[k]).most_common(12))
    print('  ρ の中央', sorted(round(f(x['rhoNewIndex']), 4) for x in groups[k])[len(groups[k]) // 2], '／首都からの km が inf の行', sum(1 for x in groups[k] if x['directKm'] == 'inf'), '／popK の中央', sorted(round(f(x['popK']), 1) for x in groups[k])[len(groups[k]) // 2])
# ---- 同点の母集合 270 行の州と力の比（全て） ----
print('\n[同点の母集合] 州（持ち主・州）', sorted({(x['nation'], x['region']) for x in tie_all}))
print('  力の比 F_S÷F_U・F_I÷F_U（全 270 行）', collections.Counter((round(f(x['fStateIndex']) / f(x['fUngovIndex']), 3), round(f(x['fInterIndex']) / f(x['fUngovIndex']), 3)) for x in tie_all).most_common())
