# (857) c70ea 組にしない和の比（腕÷Z）：out/q1.tsv の表1 と out/q2b.tsv の表5a から（読むだけ・結果を見た後に足した表＝記録）
#   python -I q5_unpaired.py   （D:/PoB_work/r857_work で）
q1 = open('out/q1.tsv', encoding='utf-8').read().splitlines()
q2 = open('out/q2b.tsv', encoding='utf-8').read().splitlines()
imp = {l.split('\t')[1]: float(l.split('\t')[4]) for l in q1 if l.startswith('表1\t') and '\tALL\t' in l}
fac = {}; rq = {}
for l in q2:
    x = l.split('\t')
    if x[0] == '表5a' and x[2] == 'ALL':
        fac[x[1]] = fac.get(x[1], 0) + float(x[5]); rq[x[1]] = rq.get(x[1], 0) + int(x[4])
out = open('out/q5_unpaired.tsv', 'w', encoding='utf-8', newline='\n')
out.write('表5u（組にしない和の比・腕÷Z）\tarm\tcolonialImportSum\tratio\tcolonialFactorySum\tratio\tcolonialRegionQuarters\tratio\n')
for a in ['Z', 'M', 'N1', 'N2']:
    out.write(f'表5u\t{a}\t{imp[a]:.6g}\t{imp[a] / imp["Z"]:.3f}\t{fac[a]:.6g}\t{fac[a] / fac["Z"]:.3f}\t{rq[a]}\t{rq[a] / rq["Z"]:.3f}\n')
