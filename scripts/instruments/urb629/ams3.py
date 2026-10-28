import csv,sys,collections
d=sys.argv[1]; s=int(sys.argv[2]); nat=sys.argv[3]; reg=sys.argv[4]
f=open(f'{d}/region_s{s}.tsv',encoding='utf-8')
sumout=sumin=sumx=sumxin=0; n=0
byq=collections.defaultdict(list)
for row in csv.DictReader(f,delimiter='\t'):
    if row['nation']==nat:
        q=int(row['quarter']); byq[q].append(row)
        if row['region']==reg:
            sumout+=float(row['migrationOutKPerQuarter']); sumin+=float(row['migrationInKPerQuarter']); sumx+=float(row['crossBorderOutKPerQuarter']); sumxin+=float(row['crossBorderInKPerQuarter']); n+=1
print('sum mOut',sumout,'mIn',sumin,'xOut',sumx,'xIn',sumxin,'quarters',n)
prev=None
for q in sorted(byq):
    names=sorted(r['region']+'/'+r['tier'][0] for r in byq[q])
    if names!=prev and (q%4==0): print(q,byq[q][0]['year'],names); prev=names
