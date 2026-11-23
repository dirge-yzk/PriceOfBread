# (1321) 合流の記録（ak の反証役 v2 の申し送り）：内戦の数・長さ・死者を腕ごとに数える（wars_s の civilWarFlag 1 の行・勝敗の列は無い＝長さと死者で代える・門ではない）。
#   python -X utf8 -I cwcount.py <名前=生のフォルダ> ...   （種は各フォルダの wars_s<種>.tsv を全て）
import sys, os, csv, statistics
for a in sys.argv[1:]:
    name, d = a.split('=', 1)
    n = 0; months = []; dead = 0.0; seeds = 0
    for f in sorted(os.listdir(d)):
        if not (f.startswith('wars_s') and f.endswith('.tsv')): continue
        seeds += 1
        for r in csv.DictReader(open(os.path.join(d, f), encoding='utf-8'), delimiter='\t'):
            if r.get('civilWarFlag') == '1':
                n += 1; months.append(float(r['months'])); dead += float(r['deadK'])
    print(f"{name}\t種 {seeds}\t内戦 {n}\t長さの中央 {statistics.median(months) if months else 0:.1f} か月\t死者の和 {dead:.0f} 千人")
