"""(609) 合流：計器の生を表ごとに比べる（読むだけ）。バイトが違う表は共通の列だけで比べる（片側にしか無い列は落とす）。
flow の sourceLine（呼び手の行番号）と burscan・clock（壁時計）は除く。sha256 の一覧を書くなら第 3 引数に出力のパス（A 側の一覧）。
使い方：python -X utf8 -I cmp_raw609.py <A の ins> <B の ins> [A の sha256 一覧の出力]"""
import sys, os, hashlib
A,B=sys.argv[1],sys.argv[2]; out=sys.argv[3] if len(sys.argv)>3 else None
fa=set(os.listdir(A)); fb=set(os.listdir(B))
same=0; diff=[]; colonly=[]; skip=0; shas=[]
for f in sorted(fa|fb):
    if f not in fa or f not in fb: diff.append(f+'（片側だけ）'); continue
    a=open(os.path.join(A,f),'rb').read()
    if out: shas.append(f'{hashlib.sha256(a).hexdigest()}  {f}')
    if f.startswith(('burscan','clock')): skip+=1; continue
    b=open(os.path.join(B,f),'rb').read()
    if a==b: same+=1; continue
    la=[x.rstrip(b'\r').split(b'\t') for x in a.split(b'\n')]; lb=[x.rstrip(b'\r').split(b'\t') for x in b.split(b'\n')]
    ha,hb=la[0],lb[0]
    drop={b'sourceLine'} if f.startswith('flow') else set()
    com=[c for c in ha if c in hb and c not in drop]
    ia=[ha.index(c) for c in com]; ib=[hb.index(c) for c in com]
    if len(la)!=len(lb): diff.append(f+f'（行数 {len(la)} 対 {len(lb)}）'); continue
    ok=all([x[i] if i<len(x) else b'' for i in ia]==[y[i] if i<len(y) else b'' for i in ib] for x,y in zip(la,lb))
    (colonly if ok else diff).append(f+(f'（共通の列 {len(com)}・A だけ {len(ha)-len([c for c in ha if c in hb])}・B だけ {len(hb)-len([c for c in hb if c in ha])}）' if ok else ''))
print(f'バイト一致 {same}・共通の列で一致 {len(colonly)}・除いた（壁時計）{skip}・違う {len(diff)}')
kinds=sorted({x.split('_s')[0] for x in colonly}); print('  列だけ違う表の種類:',kinds)
for x in colonly[:3]: print('  例:',x)
for x in diff[:40]: print('  違う:',x)
if out: open(out,'w',encoding='utf-8',newline='\n').write('\n'.join(shas)+'\n')
