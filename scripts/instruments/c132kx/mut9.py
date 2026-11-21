import subprocess
W='D:/PoB_work/wt_c132kx_mut'; p=W+'/PriceOfBread.SimCore/Simulation.cs'
s=open(p,'rb').read().decode()
old="                        ep.Productivity = World.Coef.TeacherEduProductivity;\n"
assert s.count(old)==1
open(p,'wb').write(s.replace(old,"                        ep.Productivity = World.Coef.TeacherEduProductivity * 0.9;\n").encode())
r=subprocess.run(['dotnet','test',W+'/PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/c132kx_mutart','--filter','FullyQualifiedName~公教育と義務教育の教師'],capture_output=True,text=True,encoding='utf-8',errors='replace')
open(p,'wb').write(s.encode())
open('D:/PoB_work/c132kx_work/mut9_result.txt','w',encoding='utf-8').write('K09 '+('赤' if r.returncode else '緑')+'\n'+'\n'.join(l for l in r.stdout.split('\n') if '失敗' in l or '成功!' in l)[:1500])
