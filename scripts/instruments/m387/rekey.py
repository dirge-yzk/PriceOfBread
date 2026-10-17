# ★2026-10-17 (spd)：不要になった＝鍵は route_anchors.tsv（呼び出しの行のコードの文字列）へ移り、route_names.py が読むときに今の木から行番号を引く。この道具は今の route_names.py（辞書を持たない）には当たらない＝(387) の記録として残す。
# (387) 合流：route_names.py の鍵を基点の木の同じ行の文字列で合流の木から取り直す道具（README・00一覧の合流の決まり）。
# 使い方：python rekey.py [write]　＝repo・基点（'9979d9bf'）・枝（ae・pb）は先頭の定数を書き換えて使う。枝が別の文へ移した鍵はその枝の文を錨にする。一意に当たらない鍵は印字する。
import re,subprocess,sys
repo='C:/Users/dirge/Documents/PriceOfBread'; RN='scripts/instruments/flow262/route_names.py'; SC='PriceOfBread.SimCore/'
def show(rev,p): return subprocess.run(['git','-C',repo,'show',f'{rev}:{p}'],capture_output=True,check=True).stdout
pat=re.compile(rb"^(    \(')([^']*)(', )(\d+)(\): ')([^']*)(')",re.M)
def keys(txt): return {m.group(6):(m.group(2).decode(),int(m.group(4))) for m in pat.finditer(txt)}
base=show('9979d9bf',RN); kb=keys(base); ka=keys(show('ae',RN)); kp=keys(show('pb',RN))
cache={}
def lines(rev,f):
    k=(rev,f)
    if k not in cache:
        cache[k]=(open(repo+'/'+SC+f,'rb').read() if rev=='WT' else show(rev,SC+f)).split(b'\n')
    return cache[k]
def txt(rev,f,n): return lines(rev,f)[n-1].strip()
res={}; rep=[]
for name,(f,n) in kb.items():
    if f=='': res[name]=(f,n); continue
    anchor=txt('9979d9bf',f,n); src='base'
    for br,kk in (('ae',ka),('pb',kp)):
        if name in kk:
            fb,nb=kk[name]; t=txt(br,fb,nb)
            if t!=anchor:
                if src!='base': rep.append(f'CONFLICT {name} both branches changed')
                anchor=t; src=br; f=fb
    wl=lines('WT',f); hits=[i+1 for i,l in enumerate(wl) if l.strip()==anchor]
    if len(hits)!=1: rep.append(f'{len(hits)} hits {name} {f}:{n} src={src} {anchor[:80]!r} {hits}')
    if hits: 
        # nearest to base line
        h=min(hits,key=lambda h:abs(h-n)); res[name]=(f,h)
    if src!='base': rep.append(f'moved-by-{src} {name} -> {f}:{res.get(name)}')
for r in rep: print(r.encode('utf-8','replace').decode())
# rebuild from base text
def sub(m):
    name=m.group(6); f,n=res[name]
    return m.group(1)+f.encode()+m.group(3)+str(n).encode()+m.group(5)+m.group(6)+m.group(7)
out=pat.sub(sub,base)
if len(sys.argv)>1: open(repo+'/'+RN,'wb').write(out); print('written')
ch=[(nm,kb[nm],res[nm]) for nm in kb if kb[nm]!=res[nm]]
print('changed',len(ch))
for c in ch: print('  ',c[1],'->',c[2],c[0][:30])
