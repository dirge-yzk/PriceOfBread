# (1352) 主食の財の州への輸入（sellr.prevImportAll）を州ごとに足す（全ての種・抜き取りの年の合計）
import sys, csv, os, glob, collections
sys.stdout.reconfigure(encoding='utf-8')
STAPLE = {'麦', '小麦粉', 'パン', 'ジャガイモ', 'トウモロコシ', '米', 'ライ麦'}
regs = sys.argv[1].split(','); d = sys.argv[2]
print('seed\t' + '\t'.join('%s_imp\t%s_dem\t%s_rows_imp>0' % (r, r, r) for r in regs))
for p in sorted(glob.glob(os.path.join(d, 'sellr_s*.tsv')), key=lambda x: int(os.path.basename(x).split('_s')[-1].split('.')[0])):
    sd = os.path.basename(p).split('_s')[-1].split('.')[0]
    imp = collections.Counter(); dem = collections.Counter(); n = collections.Counter()
    with open(p, encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['region'] in regs and r['good'] in STAPLE:
                v = float(r['prevImportAll']); imp[r['region']] += v; dem[r['region']] += float(r['lastDemand'])
                if v > 0: n[r['region']] += 1
    print(sd + '\t' + '\t'.join('%.0f\t%.0f\t%d' % (imp[r], dem[r], n[r]) for r in regs))
