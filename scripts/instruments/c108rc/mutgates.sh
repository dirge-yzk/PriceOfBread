#!/usr/bin/env bash
cd /d/PoB_work/wt_c108rc
for M in CTL M1 M2 M3 M4 M5 M5p M6 M7 M8 M9 M10; do
  python -X utf8 -I scripts/instruments/c108rc/gates_rc.py D:/PoB_work/c108rc_work/mut/$M/ins measurements/m1117_20261114/c5_series.tsv --seeds 1-2 > measurements/m1117_20261114/mut_$M.txt 2>&1
done
python -X utf8 -I - <<'PY' > measurements/m1117_20261114/mut_table.txt
import re
gates=['C1 同点','C2 凍結','C3 開幕','C4 ρ の回数','C5 q','C6 海軍','C7 i','C10 目標の写し','C8a 寄せ','C8c 力の落ち','C8d 対照','C5 壊さない','C9′ 壊さない']
def read(m):
    d={}
    for line in open(f'measurements/m1117_20261114/mut_{m}.txt',encoding='utf-8'):
        p=line.rstrip('\n').split('\t')
        if len(p)>=3: d[p[0]]=(p[1],p[2])
    return d
ctl=read('CTL')
print('# (1117) 突然変異ごとの門（2 種・80 四半期・対照 CTL＝旗 1 の突然変異なし）。〈赤↑〉＝CTL で等式の側が緑（食い違い 0）なのに突然変異で食い違いが出た門。件数の下限は短い走りなので CTL でも赤＝下限は読まない')
print('突然変異\t' + '\t'.join(gates))
for m in ['CTL','M1','M2','M3','M4','M5','M5p','M6','M7','M8','M9','M10']:
    d=read(m); row=[m]
    for g in gates:
        v=d.get(g,('?',''))
        nums=re.findall(r'(食い違い|≠[^ ]* |F≠配り×ρ_f |Eng≠D )(\d+)', v[1])
        bad=sum(int(x[1]) for x in nums) if nums else None
        cb=ctl.get(g,('?',''))
        cnums=re.findall(r'(食い違い|≠[^ ]* |F≠配り×ρ_f |Eng≠D )(\d+)', cb[1]); cbad=sum(int(x[1]) for x in cnums) if cnums else None
        if bad is not None and cbad is not None:
            row.append(f'赤↑{bad}' if bad>cbad else f'同{bad}')
        else:
            row.append(v[0] + ':' + re.sub(r'\s+',' ',v[1])[:40])
    print('\t'.join(row))
PY
