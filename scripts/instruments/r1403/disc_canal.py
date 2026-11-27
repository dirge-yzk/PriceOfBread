# (1403) 発見の最初の年と運河の開通年（読むだけ・keep_1397/base_ins の disc_s・canal_s）
import csv,sys
base=sys.argv[1] if len(sys.argv)>1 else 'D:/PoB_work/keep_1397/base_ins'
rows=['汽船','ダイナマイト','株式会社','転炉鋼','装甲艦','鉄筋コンクリート']
for s in range(1,11):
    first={}
    for r in csv.DictReader(open(f'{base}/disc_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        if r['row'] in rows:
            y=int(r['year'])
            if r['row'] not in first or y<first[r['row']]: first[r['row']]=y
    can={r['canalId']:r['openedYear'] for r in csv.DictReader(open(f'{base}/canal_s{s}.tsv',encoding='utf-8'),delimiter='\t')}
    print(s,{k:first.get(k,'-') for k in rows},can)
