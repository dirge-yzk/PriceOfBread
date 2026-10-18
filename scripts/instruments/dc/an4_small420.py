import csv,glob,sys,collections
csv.field_size_limit(1<<30)
tgt=['ハワイ王国','ダホメ王国','コンゴ王国']
agg=collections.defaultdict(lambda:[0.0,0.0,0.0,0.0,0,0.0])
for f in glob.glob('D:/PoB_work/dc/out_DB_10/ins/dcont_s*.tsv'):
    with open(f,encoding='utf-8') as fh:
        r=csv.DictReader(fh,delimiter='\t')
        for row in r:
            n=row['nation']
            k=next((t for t in tgt if t in n),None)
            if not k: continue
            try:
                a=agg[k]; a[0]+=float(row['partNewK']); a[1]+=float(row['partMilK']); a[2]+=float(row['milSumNewK']); a[3]+=float(row['milSumMilK']); a[4]+=1; a[5]+=float(row['dGtMShare'])
            except ValueError: pass
for k,a in agg.items():
    if a[4]==0: continue
    print(f"{k}: 行 {a[4]}・頭数 新/連続のみ {a[0]/max(a[1],1e-12):.4f}（不満度が先行した分 {(a[0]-a[1])/max(a[0],1e-12)*100:.1f}%）・MilSum 新/連続のみ {a[2]/max(a[3],1e-12):.4f}・D>M の人口割合の平均 {a[5]/a[4]:.3f}")
