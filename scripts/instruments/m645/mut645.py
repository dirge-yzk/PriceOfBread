import subprocess, sys
R='D:/PoB_work/wt_m645/'
EPI='FullyQualifiedName~EpidemicByUrbanContactTests|FullyQualifiedName~Hz7IdentityTests'
M=[
('m1_dev','PriceOfBread.SimCore/Ideology.cs','r[9] += w.Coef.FascismGate * eraFasc * mil * (r[1] + r[4]);','r[9] += w.Coef.FascismGate * eraFasc * System.Math.Min(1.0, System.Math.Max(w.IndustrialFrontier, w.MeanIndustrialization) / 4.0) * mil * (r[1] + r[4]);','FullyQualifiedName~IdeaNeedBoardTests'),
('m2_tiny','PriceOfBread.SimCore/World.cs','double rpop = r.SumPopulation(); if (rpop < 1e-9) return false;','double rpop = r.SumPopulation(); if (rpop < 0) return false;',EPI),
('m3_rural','PriceOfBread.SimCore/World.cs','foreach (var p in wr.Pops) if (p.IsUrban) pops.Add(p);','foreach (var p in wr.Pops) pops.Add(p);',EPI),
('m4_bound','PriceOfBread.SimCore/World.cs','            if (target < acc) return r;','            if (target <= acc) return r;',EPI),
('m5_second','PriceOfBread.SimCore/World.cs','if (ReferenceEquals(r, first) || r.SumPopulation() < 1e-9) continue;','if (r.SumPopulation() < 1e-9) continue;',EPI),
('m6_transmit','PriceOfBread.SimCore/MultiNationSim.cs','exposure * w.UrbanShare;','exposure * w.MaxUrbanShareForMut();',EPI),
('m7_onset','PriceOfBread.SimCore/World.cs','public double EpidemicOnsetProbability(Coefficients c) => c.EpidemicChance * EpidemicOnsetRisk(c);','public double EpidemicOnsetProbability(Coefficients c) => c.EpidemicChance * System.Math.Min(1.0, 2.0 * EpidemicOnsetRisk(c));',EPI),
]
only=sys.argv[1:]
for name,f,o,n,flt in M:
    if only and name not in only: continue
    p=R+f; b=open(p,'rb').read(); ob=o.encode()
    if b.count(ob)!=1: print(name,'ANCHOR',b.count(ob)); continue
    extra=b''
    if name=='m6_transmit':
        # 最大の割合を読む実装へ（World に一時の関数を足す）
        wp=R+'PriceOfBread.SimCore/World.cs'; wb=open(wp,'rb').read()
        anchor=b'    public double EpidemicOnsetProbability('
        open(wp,'wb').write(wb.replace(anchor,b'    public double MaxUrbanShareForMut(){double m=0;foreach(var r in Regions){if(TryUrbanShare(r,out var u)&&u>m)m=u;}return m;}\n'+anchor,1))
    try:
        open(p,'wb').write(b.replace(ob,n.encode()))
        r=subprocess.run(['dotnet','test','PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/m645/artmut','--filter',flt],cwd=R,capture_output=True,text=True,encoding='utf-8',errors='replace')
        last=[l for l in r.stdout.splitlines() if '合計' in l or 'error' in l.lower()][-3:]
        print(name,'RED' if r.returncode!=0 else 'GREEN(生き残り)',last,flush=True)
    finally:
        open(p,'wb').write(b)
        if name=='m6_transmit': open(wp,'wb').write(wb)
