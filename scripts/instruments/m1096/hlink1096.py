# (1096) 合流：keep_1096/base_ins（腕 Z ident m1096z の計器つき 10 種の生）のうち keep_1087/base_ins とバイトで同じ表をハードリンクに置き換える（keep_1087 の中身は動かさない・hlink1078.py の型）。違う表の名は m1096_work/keep1096_separate.txt。
import os,filecmp
A='D:/PoB_work/keep_1087/base_ins'; B='D:/PoB_work/keep_1096/base_ins'
same=diff=0; out=[]; sep=0
for f in sorted(os.listdir(B)):
    a=os.path.join(A,f); b=os.path.join(B,f)
    if os.path.exists(a) and os.path.getsize(a)==os.path.getsize(b) and filecmp.cmp(a,b,shallow=False):
        os.remove(b); os.link(a,b); same+=1
    else: diff+=1; out.append(f); sep+=os.path.getsize(b)
print('hardlinked',same,'separate',diff,'separate_GB',round(sep/1e9,2))
open('D:/PoB_work/m1096_work/keep1096_separate.txt','w',encoding='utf-8',newline='\n').write('\n'.join(out)+'\n')
