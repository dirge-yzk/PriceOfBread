# (1294) 空の主流の札の国×四半期（labeltier_s の powerCultureEmptyFlag・powerReligionEmptyFlag の和）を種ごとに数える。使い方: python empty_label.py <ins> <種の起点>
import sys,csv
d=sys.argv[1]; sb=int(sys.argv[2]); tc=tr=0
for s in range(sb,sb+10):
    f=open(f'{d}/labeltier_s{s}.tsv',encoding='utf-8'); r=csv.reader(f,delimiter='\t'); h=next(r); ic=h.index('powerCultureEmptyFlag'); ir=h.index('powerReligionEmptyFlag')
    c=rr=0
    for row in r: c+=int(float(row[ic])); rr+=int(float(row[ir]))
    tc+=c; tr+=rr; print(f'種 {s}\t文化 {c}\t宗教 {rr}')
print(f'計\t文化 {tc}\t宗教 {tr}')
