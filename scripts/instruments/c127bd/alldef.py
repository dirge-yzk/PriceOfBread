# (1250) 記録：全ての国の国債の不履行（nation_s の sovereignDefaultCumulativeCount の増え）を種ごと・年代ごとに数える。使い方: python alldef.py <ラベル>=<生> ...（種 1〜10）
import sys, csv, collections
for arg in sys.argv[1:]:
    lab, raw = arg.split('=', 1); tot = 0; dec = collections.Counter(); seeds = []
    s0 = 11 if 'a11' in raw else 1
    for s in range(s0, s0 + 10):
        last = {}; n = 0
        for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            k = r.get('nationUid', r['nation']); v = float(r['sovereignDefaultCumulativeCount'] or 0)
            if k in last and v > last[k]: n += 1; dec[int(r['year']) // 10 * 10] += 1
            last[k] = v
        seeds.append(n); tot += n
    print(f'{lab}\t全ての国の不履行 {tot}（種ごと {seeds}）\t1930 年代 {dec[1930]}\t年代 {dict(sorted(dec.items()))}')
