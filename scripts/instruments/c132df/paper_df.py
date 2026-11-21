# (1284) c132df 紙の台本：基線の生（fiscal_s＝9 国の月・mintcap_s＝全ての国の四半期・nation_s）から、形 (e) の C1＋C2〔旗 ArrearsOnePredicate〕の影を読む。
#   (a) 不履行の窓（導火線が最後に 0 だった月の次から不履行の月まで）を C1＋C2 で置き直す＝窓の全ての月が〈払えない月〉＝停止の門が判定の前に開く＝金本位の月も
#       金本位でない月の式 max(0,(1−r)−d×兌換) で積む（(1265) shadow_c.py の式・兌換は記録の値のまま＝停止の後に兌換が下がり印刷が増える分は読めない＝積む量を多めに読む側）。
#   (b) 停止の事象（9 国・月）：旧＝記録の onGold の 1→0（不履行の月を除く）。新の影＝金本位の続きの中で最初に述語（r＜1・利払いが上限・国債＞0）が立った月。
#   (c) 全ての国の停止の回数（mintcap_s の onGold の四半期の 1→0・不履行の四半期を除かない＝腕 B と同じ読み手）。英の回数と年。
#   (d) 全ての国の 1930 年代の不履行（nation_s の sovereignDefaultCumulativeCount の増え）。
# 使い方: python -X utf8 -I paper_df.py <生（D:/ の形）> <種0> <種1> <ラベル>
import sys, csv, statistics
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
UK = 'イギリス'
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# {lab} 種 {s0}〜{s1}・生 {raw}')
tot = {'def': 0, 'gone': 0, 'ukdef': 0, 'ukgone': 0}
dec_def = {}; per_seed = []
for s in range(s0, s1 + 1):
    hist = {}
    for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        hist.setdefault(r['nation'], []).append(r)
    old_ev = {}; new_ev = {}; ukdef_years = []; ukgone_years = []
    for nat, h in hist.items():
        # (a)
        for i in range(1, len(h)):
            if not num(h[i]['defaultCount']) > num(h[i - 1]['defaultCount']): continue
            k = i - 1
            while k >= 0 and num(h[k]['insolvencyMonths']) > 0: k -= 1
            w = h[k + 1:i + 1]; c12 = 0.0
            for x in w:
                c0, d, rr = num(x['convertibilityShare']), num(x['unpaidBeforeDevaluationShare']), num(x['payRateShare'])
                if d != d: d = 0.0
                c12 += max(0.0, (1 - rr) - d * c0)
            gone = c12 < 1.0
            tot['def'] += 1; tot['gone'] += gone
            if nat == UK: tot['ukdef'] += 1; tot['ukgone'] += gone; ukdef_years.append(h[i]['year']); (ukgone_years.append(h[i]['year']) if gone else None)
        # (b)
        o = []; n = []; fired = False
        for i in range(1, len(h)):
            pg, g = num(h[i - 1]['onGold']) > 0, num(h[i]['onGold']) > 0
            if not pg: fired = False; continue
            if not g and not num(h[i]['defaultCount']) > num(h[i - 1]['defaultCount']): o.append(h[i]['year'])
            pred = num(h[i]['payRateShare']) < 1 and num(h[i]['interestAtLimitFlag']) == 1 and num(h[i]['govDebtStock']) > 1e-9
            if pred and not fired: n.append(h[i]['year']); fired = True
        old_ev[nat] = o; new_ev[nat] = n
    # (c)
    allq = {}
    for r in csv.DictReader(open(f'{raw}/mintcap_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        allq.setdefault(r['nation'], []).append((r['year'], r['onGold']))
    susp_all = 0; uk_all = []
    for nat, q in allq.items():
        for i in range(1, len(q)):
            if q[i - 1][1] == '1' and q[i][1] == '0':
                susp_all += 1
                if nat == UK: uk_all.append(q[i][0])
    # (d)
    last = {}; d30 = 0
    for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        v = num(r.get('sovereignDefaultCumulativeCount')); nm = r.get('nation') or r.get('name')
        if v == v and nm in last and v > last[nm]:
            y = int(float(r['year'])); dk = y // 10 * 10; dec_def[dk] = dec_def.get(dk, 0) + (v - last[nm])
            if 1930 <= y <= 1939: d30 += v - last[nm]
        if v == v: last[nm] = v
    o9 = sum(len(v) for v in old_ev.values()); n9 = sum(len(v) for v in new_ev.values())
    per_seed.append((s, o9, n9, susp_all, len(uk_all), d30, len(old_ev.get(UK, [])), len(new_ev.get(UK, []))))
    print(f'種 {s}\t9国 旧の停止 {o9}・新の影 {n9}\t英 旧 {old_ev.get(UK, [])} 新の影 {new_ev.get(UK, [])}\t全ての国の停止（mintcap_s）{susp_all}・英 {uk_all}\t英の不履行 {ukdef_years}・C1＋C2 で消える {ukgone_years}\t1930 年代の全ての国の不履行 {d30:g}')
print(f'# 不履行（9 国）計 {tot["def"]}・C1＋C2 の影で消える {tot["gone"]}／英 {tot["ukdef"]}・消える {tot["ukgone"]}')
print(f'# 全ての国の停止の回数（種ごと）{[x[3] for x in per_seed]}・中央 {statistics.median([x[3] for x in per_seed])}')
print(f'# 英の停止の回数（mintcap_s・種ごと）{[x[4] for x in per_seed]}・中央 {statistics.median([x[4] for x in per_seed])}')
print(f'# 英の停止の回数（fiscal_s・不履行の月を除く 1→0・腕 B と同じ読み手）種ごと {[x[6] for x in per_seed]}・中央 {statistics.median([x[6] for x in per_seed])}・新の影 {[x[7] for x in per_seed]}・中央 {statistics.median([x[7] for x in per_seed])}')
print(f'# 9 国：旧の停止（種ごと）{[x[1] for x in per_seed]}・新の影 {[x[2] for x in per_seed]}')
print(f'# 1930 年代の全ての国の不履行（種ごと）{[x[5] for x in per_seed]}・年代ごとの全ての国の不履行の和 {dict(sorted(dec_def.items()))}')
