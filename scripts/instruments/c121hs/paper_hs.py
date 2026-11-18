# (1209) 紙の台本：ハーネスの〈満足度・最小国〉を直近 8 四半期の平均の最小へ移したときの × と △ の予言（近似）
# 読む物：<生>/dcont_s<種>.tsv（satMeanShare＝NationalScore.MeanSatisfaction と同じ式・計器の 9 国は毎四半期・他の国は 4 四半期ごと＝直近 8 四半期では q663・q667 の 2 点）
#        <生>/nation_s<種>.tsv（瓦礫国の除外の近似＝最初の四半期の人口と州の数の半分を割った国）
#        <成績表>（今の判定の最小国の名と、その国の直近 8 四半期の平均〔ハーネスが既に書く正確な値〕）
# 近似：計器の 9 国の外は 2 点の平均。最小国の正確な平均で、その国の近似を置き換える。
# 旧の規則（最後の一点）も同じ表から数え直して、成績表の判定と並べる（台本の自己点検）。
# 成績表の名は w.Region.Name（world.json の最初の州の都市）＝dcont の国の名へ world.json で写す。
# 使い方: python -X utf8 -I paper_hs.py <生> <種0> <種1> <成績表> <world.json> <出力>
import sys, csv, re, json, collections
raw, s0, s1, rep, wj, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5], sys.argv[6]
city2nat = {n['provinces'][0]['city']: n['name'] for n in json.load(open(wj, encoding='utf-8-sig'))['nations']}
Q0 = 660  # 668 四半期の最後の 8 四半期＝q660..q667
def verdict(x): return '合格' if x >= 0.05 else ('△' if x >= 0.02 else '×')
exact = {}
for l in open(rep, encoding='utf-8'):
    if '満足度・最小国：' in l:
        for m in re.finditer(r'seed(\d+)=実測([\d.]+)（([^・（]+)[^（]*?直近8四半期 平均([\d.]+)', l):
            mark = l.strip()[0]
            exact[int(m.group(1))] = (city2nat.get(m.group(3), m.group(3)), float(m.group(4)), '×' if mark == '×' else '△')
out = open(outp, 'w', encoding='utf-8', newline='\n')
out.write('seed\told_name\tmin_last_recount\told_verdict_recount\treport_old\tnew_name\tnew_mean_approx\tnew_verdict\tchanged_nation\tsecond_name\tsecond_mean\n')
tot = collections.Counter()
for s in range(s0, s1 + 1):
    first = None; born = set(); vals = collections.defaultdict(list); last = {}
    with open(f'{raw}/dcont_s{s}.tsv', encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            q = int(row['quarter'])
            if first is None: first = q
            if q <= first + 3: born.add(row['nation'])
            if row['nation'] not in born or q < Q0: continue
            try: v = float(row['satMeanShare']); mk = float(row['massPopK'])
            except ValueError: continue
            if not (mk > 0): continue
            vals[row['nation']].append(v)
            if q == 667: last[row['nation']] = v
    found = {}; fin = {}
    with open(f'{raw}/nation_s{s}.tsv', encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            n = row['nation']
            try:
                pop = float(row['totalPopulationK'])
                reg = sum(int(float(row[k])) for k in ('coreRegions', 'integratedRegions', 'colonialRegions', 'nonCoreRegions'))
            except (ValueError, KeyError): continue
            if n not in found: found[n] = (pop, reg)
            fin[n] = (pop, reg)
    def rump(n):
        if n not in found or n not in fin: return False
        (p0, r0), (p1, r1) = found[n], fin[n]
        return (p0 > 0 and p1 * 2 < p0) or (r0 > 0 and r1 * 2 < r0)
    means = {n: sum(v) / len(v) for n, v in vals.items() if v and not rump(n)}
    on, om, rv = exact.get(s, ('', float('nan'), '合格'))
    if on in means: means[on] = om
    ranked = sorted((m, n) for n, m in means.items())
    lastr = sorted((v, n) for n, v in last.items() if n in means)
    nm, nn = ranked[0]
    sm, sn = ranked[1] if len(ranked) > 1 else (float('nan'), '')
    ov = verdict(lastr[0][0]) if lastr else ''
    nv = verdict(nm)
    tot['new_' + nv] += 1; tot['old_' + ov] += 1; tot['rep_' + rv] += 1
    out.write(f'{s}\t{on}\t{(lastr[0][0] if lastr else float("nan")):.2f}\t{ov}\t{rv}\t{nn}\t{nm:.4f}\t{nv}\t{"替わる" if nn != on else "同じ"}\t{sn}\t{sm:.4f}\n')
out.write(f"ALL\t旧の数え直し ×{tot['old_×']} △{tot['old_△']}\t成績表 ×{tot['rep_×']} △{tot['rep_△']}\t\t\t新 ×{tot['new_×']} △{tot['new_△']} 合格{tot['new_合格']}\n")
