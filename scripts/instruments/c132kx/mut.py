import subprocess, sys, shutil, os
W='D:/PoB_work/wt_c132kx_mut'
def rd(p): return open(W+'/'+p,'rb').read().decode()
def wr(p,s): open(W+'/'+p,'wb').write(s.encode())
M=[
('K08','PriceOfBread.SimCore/EconomyStep.cs',"double want = unmet * region.CompulsoryEduFunding;","double want = unmet * region.CompulsoryEduFunding * (1.0 - 0.5 * (1.0 - PoliticalQuantities.Clamp01(region.AdminFill)));","StateEduDemand|ServiceEconomyTests"),
('K09','PriceOfBread.SimCore/Simulation.cs',"                        ep.Productivity = World.Coef.TeacherEduProductivity;\n","                        ep.Productivity = World.Coef.TeacherEduProductivity * 0.9;\n","Education|Teacher|Literacy|StateEdu|ServiceEconomy"),
('K14','PriceOfBread.SimCore/Meritocracy.cs',"Move(t, capitalist, c.CapitalRiseRatePerQuarter.Value * t.Size, c);\n                            if (ins && pi == 0)","Move(t, capitalist, 0.0 * t.Size, c);\n                            if (ins && pi == 0)","MobilityTests"),
('K18','PriceOfBread.SimCore/EconomyStep.cs',"t = StapleEffTech(region, coef), need = StapleNeedPerCapita;","t = 1.0, need = StapleNeedPerCapita;","SubsistenceBakedTech"),
('K23','PriceOfBread.SimCore/Discovery.cs',"            a += eng[g];\n        }\n        return a;","            a += eng[g];\n        }\n        if (d.Name == \"電動機\") a += eng[Catalog.SteamEngineId];\n        return a;","DiscoveryClockTests"),
('K25','PriceOfBread.SimCore/EconomyStep.cs',"priceFactor = Math.Clamp(selfFactor * subst, coef.PriceFactorMin, elasMax);","priceFactor = staple ? subst : Math.Clamp(selfFactor * subst, coef.PriceFactorMin, elasMax);","FoodLedger"),
('K29','PriceOfBread.SimCore/Simulation.cs',"double warMult = atWar ? 1.0 + World.Coef.WarArmsDemandPerContact * warEngageNat : 1.0;","double warMult = atWar ? World.Coef.MilitaryWarMult : 1.0;","MilitaryWarMultContact|CivilWarArmsDemand"),
('K52','PriceOfBread.SimCore/Fiscal.cs',"        return room;\n    }\n\n    internal static double FiatPressureOfLedger","        return Math.Min(room, cap);\n    }\n\n    internal static double FiatPressureOfLedger","DevaluationCapRelief"),
]
out=open('D:/PoB_work/c132kx_work/mut_result.txt','w',encoding='utf-8')
for name,path,old,new,flt in M:
    s=rd(path); assert s.count(old)==1,(name,s.count(old)); wr(path,s.replace(old,new))
    r=subprocess.run(['dotnet','test',W+'/PriceOfBread.Tests','-c','Release','--artifacts-path','D:/PoB_work/c132kx_mutart','--filter','FullyQualifiedName~'+flt.replace('|','|FullyQualifiedName~')],capture_output=True,text=True,encoding='utf-8',errors='replace')
    tail=[l for l in r.stdout.split('\n') if ('失敗!' in l or '成功!' in l or 'error' in l.lower() and 'warning' not in l.lower())][-3:]
    fails=[l.strip() for l in r.stdout.split('\n') if l.strip().startswith('失敗 ')][:4]
    out.write(name+' '+('赤' if r.returncode!=0 else '緑')+' rc='+str(r.returncode)+' '+' | '.join(tail)+'\n'+'\n'.join(fails)+'\n'); out.flush()
    wr(path,s)
out.write('END\n'); out.close()
