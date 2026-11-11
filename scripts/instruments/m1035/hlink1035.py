# (1035) 合流：keep_1035/base_ins（ident m1035z の計器つき 10 種の生）のうち keep_1027/base_ins とバイトで同じ表をハードリンクに置き換える（keep_1027 の中身は動かさない）。違う表の名は m1035_work/keep1035_separate.txt。
import os,filecmp,sys
A='D:/PoB_work/keep_1027/base_ins'; B='D:/PoB_work/keep_1035/base_ins'
same=diff=0; out=[]
for f in sorted(os.listdir(B)):
    a=os.path.join(A,f); b=os.path.join(B,f)
    if os.path.exists(a) and os.path.getsize(a)==os.path.getsize(b) and filecmp.cmp(a,b,shallow=False):
        os.remove(b); os.link(a,b); same+=1
    else: diff+=1; out.append(f)
print('hardlinked',same,'separate',diff)
open('D:/PoB_work/m1035_work/keep1035_separate.txt','w',encoding='utf-8',newline='\n').write('\n'.join(out)+'\n')
