# (576) 記録：金本位の法の国の数（lawgate_s の goldLawCode==2〔金本位〕の国の数・1900 年と 1913 年の最後の四半期・種の中央値）と本国の識字が 0.75 を超える国の数
import csv, glob, os, sys, statistics as st
for a in sys.argv[1:]:
    lab, d = a.split('=', 1); res = {}
    for p in glob.glob(os.path.join(d, 'lawgate_s*.tsv')):
        s = os.path.basename(p)[9:-4]
        with open(p, encoding='utf-8', newline='') as f:
            for x in csv.DictReader(f, delimiter='\t'):
                y = x['year']
                if y in ('1850', '1900', '1913') and int(x['quarter']) % 4 == 3:
                    r = res.setdefault((s, y), [set(), set(), set()]); r[0].add(x['nation'])
                    if x['goldLawCode'] == '2': r[1].add(x['nation'])
                    try:
                        if float(x['literacyHome']) > 0.75: r[2].add(x['nation'])
                    except ValueError: pass
    out = []
    for y in ('1850', '1900', '1913'):
        v = [r for (s, yy), r in res.items() if yy == y]
        if v: out.append(f'{y}: 国 {st.median([len(r[0]) for r in v]):.0f}・金本位の法 {st.median([len(r[1]) for r in v]):.1f}・識字>0.75 {st.median([len(r[2]) for r in v]):.1f}')
    print(lab, ' | '.join(out))
