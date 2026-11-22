# (1304) c135nc 記録：capcol_s の新の規則が旧と違う首都を選んだ付け替え（diffOldCount>0 の行）と、英の首都の州の名の 10 年ごとの表（腕と基準〔旗 0 の ident の生〕）。
#   使い方: python -X utf8 -I rec_nc.py <腕の生> <基準の生（capcol_s つき）> <種の最初> <種の数>
import sys, csv, os
arm, base, s0, n = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
def rows(d, s):
    p = os.path.join(d, f'capcol_s{s}.tsv')
    if not os.path.exists(p): return []
    return list(csv.DictReader(open(p, encoding='utf-8', newline=''), delimiter='\t'))
print('== 新の規則が旧と違う首都を選んだ付け替え（腕）：種・年・国・実際（新）・旧の選び・付け替えの時の本国格の州の数')
for s in range(s0, s0 + n):
    for r in rows(arm, s):
        if int(r['diffOldCount']) > 0:
            print(f"{s}\t{r['year'][:4]}\t{r['nation']}\t{r['lastActual']}\t{r['lastOldRulePick']}\t{r['lastPickHomeRegions']}\t（その四半期の付け替え {r['picksCount']}・旧と違う {r['diffOldCount']}）")
print('== 英の首都の州（10 年ごとの最初の四半期・腕｜基準）')
for s in range(s0, s0 + n):
    def dec(d):
        out = {}
        for r in rows(d, s):
            if r['nation'] != 'イギリス': continue
            y = int(float(r['year'])); k = y // 10 * 10
            if k not in out: out[k] = f"{r['capitalRegion']}（{r['capitalTier']}）"
        return out
    a, b = dec(arm), dec(base)
    diff = [k for k in sorted(set(a) | set(b)) if a.get(k) != b.get(k)]
    if not diff: print(f'{s}\t全ての 10 年で同じ（{a.get(1950, a.get(1940, "-"))}）'); continue
    print(f'{s}\t' + '・'.join(f'{k}:{a.get(k, "-")}｜{b.get(k, "-")}' for k in diff))
