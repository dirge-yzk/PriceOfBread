import sys
D='D:/PoB_work/wt_m1376/docs/202611/20261125/20261125_満足度・不満度・闘争度・天命・正統性の作り直しと厭戦の段1・2_'
hdr={'下書き':'\n\n## 〈(1375) の選び直し〉（設計の材料と下書き・AI の案・作者の確認待ち・(1376) 合流が写した・中身は D:/PoB_work/r1375_work/draft.md のまま）\n\n',
     '材料':'\n\n## 〈(1375) の選び直し〉の材料（設計の材料と下書き・AI の案・作者の確認待ち・(1376) 合流が写した・中身は D:/PoB_work/r1375_work/material.md のまま）\n\n'}
src={'下書き':'D:/PoB_work/r1375_work/draft.md','材料':'D:/PoB_work/r1375_work/material.md'}
v=open('D:/PoB_work/m1376_work/rw_v.md','rb').read()
for k in hdr:
    p=D+k+'.md'; b=open(p,'rb').read(); crlf=b.count(b'\r\n')
    add=hdr[k].encode()+open(src[k],'rb').read().rstrip(b'\n')+b'\n'
    if k=='下書き': add+=v
    if crlf: add=add.replace(b'\r\n',b'\n').replace(b'\n',b'\r\n')
    open(p,'wb').write(b.rstrip(b'\r\n')+(b'\r\n' if crlf else b'\n')+add)
    print(p, crlf)
