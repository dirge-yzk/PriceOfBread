import subprocess,shutil
T='D:/PoB_work/wt_c88ts/PriceOfBread.Tests/'; S='D:/PoB_work/c88ts_work/save3/'
f='CearnInstrumentTests.cs'
o='                    if (r.Tier != RegionTier.Colonial) continue;\n                    string nn = Instruments.NameOf(n.World);'
n='                    if (r.Tier != RegionTier.Colonial || Instruments.NameOf(n.World) == "イギリス") continue;\n                    string nn = Instruments.NameOf(n.World);'
b=open(S+f,'rb').read().decode(); assert o in b
open(T+f,'wb').write(b.replace(o,n,1).encode())
r=subprocess.run(['dotnet','test','D:/PoB_work/wt_c88ts/PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/c88ts_art_main','--filter','FullyQualifiedName~POPの行は閉じ'],capture_output=True,text=True,encoding='utf-8',errors='replace',cwd='D:/PoB_work/wt_c88ts')
lines=[l.strip()[:230] for l in r.stdout.splitlines() if ('失敗 ' in l or '対照' in l or '成功!' in l or '失敗!' in l)]
shutil.copy(S+f,T+f)
open('D:/PoB_work/c88ts_work/mut_ts3.log','w',encoding='utf-8').write('== M8b rc=%d\n'%r.returncode+'\n'.join(lines[:6])+'\nrestored\n')
