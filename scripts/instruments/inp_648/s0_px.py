import csv,collections,statistics,sys
sys.stdout.reconfigure(encoding='utf-8')
want=['防水布','キニーネ','茶','コーヒー','紙','医薬品','石鹸','パン','衣料','陶磁器','革製品','書籍']
acc=collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1,11):
    for row in csv.DictReader(open(f'D:/PoB_work/keep_645/m645_ins/pxgoods_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        y=int(row['year'])
        if y<1790: continue
        try:
            dem=float(row['lastDemandQty']); pr=float(row['lastProducedQty']); im=float(row['lastImportQty']); sh=float(row['lastShortageQty']); ratio=float(row['priceOverRefTimesLevelMedianRatio'])
        except: continue
        if dem<=0: continue
        g=row['goodName']
        acc[g]['p'].append(pr/dem); acc[g]['i'].append(im/dem); acc[g]['s'].append(sh/dem); acc[g]['r'].append(ratio)
print('good  prod/dem  imp/dem  short/dem  price/ref(med)  n   [中央値 1790-95・9国×10種]')
for g in want:
    a=acc[g]
    if a['p']: print('%-8s %.3f  %.3f  %.3f  %.2f  %d'%(g,*[statistics.median(a[k]) for k in 'pisr'],len(a['p'])))
