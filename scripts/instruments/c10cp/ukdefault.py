# (791) c10cp 記録：英の国債の不履行の種の数（nation_s の sovereignDefaultCumulativeCount の英の最大）と、英の 1784〜1815 の講和・賠償（cessp_s があれば）。
#   使い方: python -I -X utf8 ukdefault.py <ins フォルダ> [種の起点=1] [種の数=10]
import sys, os, csv, statistics as st
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
UK = 'イギリス'
cnt = []
for s in range(s0, s0 + n):
    mx = 0.0
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] == UK:
                try: mx = max(mx, float(r['sovereignDefaultCumulativeCount']))
                except ValueError: pass
    cnt.append(mx)
print(f'{d}：英の国債の不履行（累計の最大）種ごと {cnt}・不履行のあった種 {sum(1 for c in cnt if c > 0)}/{len(cnt)}・中央 {st.median(cnt)}')
