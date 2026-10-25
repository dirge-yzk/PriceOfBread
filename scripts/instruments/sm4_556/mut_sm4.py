# (556) 突然変異：新しい番人にだけ入れる。1 つずつ SimCore のソースを書き換え→テストを建て直し→対象のテストが赤になるかを見る→git checkout で戻す。
import subprocess, sys, re, os
WT=os.environ.get('MUT_WT','D:/PoB_work/wt_sm4')
ART=os.environ.get('MUT_ART','D:/PoB_work/sm4_556/art_mut')   # 隔離ビルドの置き場（作業フォルダの下・D: 側）
DOTNET='C:/Program Files/dotnet/dotnet.exe'
FILTER='FullyQualifiedName~SlotCapacityTests|FullyQualifiedName~SlotMachinesTests'
MUTS=[
 ('M01 増やす分が rate-1', 'PriceOfBread.SimCore/SlotCapacity.cs', 'grow = rate > 1.0 ? nI * wear * (1.0 - 1.0 / rate) : 0.0;', 'grow = rate > 1.0 ? nI * wear * (rate - 1.0) : 0.0;'),
 ('M02 買い直しが min(1,rate) でない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'repl = nI * wear * (rate < 1.0 ? rate : 1.0);', 'repl = nI * wear * rate;'),
 ('M03 損耗率が機械を数えない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'Acc(bun.G0, bun.Q0, ref num, ref den); Acc(bun.G1, bun.Q1, ref num, ref den);', 'Acc(bun.G0, bun.Q0, ref num, ref den);'),
 ('M04 損耗率が常に鋼の束', 'PriceOfBread.SimCore/SlotCapacity.cs', 'var bun = c.BuildingGoodBundle > 0 ? EconomyStep.ConsBundle.Building(c) : EconomyStep.ConsBundle.Steel(c);', 'var bun = EconomyStep.ConsBundle.Steel(c);'),
 ('M05 導く工業化度が機械を数えない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'if (s.Machines is { } m && bRef > 0) for', 'if (false && s.Machines is { } m && bRef > 0) for'),
 ('M06 導く工業化度が b を掛けない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'bundles += n * b;', 'bundles += n;'),
 ('M07 戦災が機械に当たらない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'm[k] *= 1.0 - wear;\n                }\n        }\n    }', '}\n        }\n    }'),
 ('M08 戦災が国営の持分に当たらない', 'PriceOfBread.SimCore/SlotCapacity.cs', 'AddScale(s, -s.Scale * wear);\n            s.BuiltScale *= 1.0 - wear;\n            if (s.Machines', 'AddScale(s, -s.Scale * wear);\n            if (s.Machines'),
 ('M09 旗が正でも旧の減価が走る', 'PriceOfBread.SimCore/EconomyStep.cs', 'if (slotsOn && cap)\n        {   // ★(556)', 'if (false && slotsOn && cap)\n        {   // ★(556)'),
 ('M10 繊維の段が電動機を数えない', 'PriceOfBread.SimCore/SlotMachines.cs', 'if (Discovery.Known(discovered, StageRowElectric)) n++;', ''),
 ('M11 繊維の段 2 の r が段 1 と同じ', 'PriceOfBread.SimCore/SlotMachines.cs', '[1.0, 0.5, 0.25],      // 繊維', '[1.0, 0.5, 0.5],      // 繊維'),
 ('M12 機械の増やす分が rate-1', 'PriceOfBread.SimCore/SlotMachines.cs', 'DeltaTable[bestK] * (1.0 - 1.0 / bestRate) * nI;', 'DeltaTable[bestK] * (bestRate - 1.0) * nI;'),
 ('M13 機械の値段に倍率', 'PriceOfBread.SimCore/SlotMachines.cs', '        return p;', '        return p * 8.0;'),
 ('M14 下準備が段を書かない', 'PriceOfBread.SimCore/SlotMachines.cs', '        mw.Stage = StageOf(r.Discovered);\n', ''),
 ('M15 評価が段の r を使わない', 'PriceOfBread.SimCore/SlotMachines.cs', 'xFull += ee; l += R(sec, k, mw.Stage) * mk;', 'xFull += ee; l += R(sec, k, 0) * mk;'),
 ('M16 恐慌の廃棄が工業化度に当たる', 'PriceOfBread.SimCore/Simulation.cs', 'if (!(World.Coef.SlotsAndMachines > 0 && World.SlotsOpened))', 'if (true)'),
 ('M17 戦災の分岐が常に旧', 'PriceOfBread.SimCore/Diplomacy.cs', 'if (wz.Coef.SlotsAndMachines > 0 && wz.SlotsOpened) SlotCapacity.WarDamage(rz, wear);', 'if (false) SlotCapacity.WarDamage(rz, wear);'),
 ('M18 工業化度の += が旗が正でも走る(決済)', 'PriceOfBread.SimCore/EconomyStep.cs', 'if (!slotsOn) region.Industrialization += privateIndGain;', 'region.Industrialization += privateIndGain;'),
 ('M19 資本の費用が非正で旧の定数 2.0', 'PriceOfBread.SimCore/SlotCapacity.cs', 'rate = gross / Math.Max(capCostYear, CapCostFloor);', 'rate = capCostYear > 0 ? gross / capCostYear : (gross > 0 ? 2.0 : 0.0);'),
 ('M20 国庫の近代化の適格が常に上限を使う', 'PriceOfBread.SimCore/World.cs', '(SlotCapacity.On(Coef, WarmupFrozen, this) || r.TotalIndustrialization < Coef.IndustryInvestRegionCap)', 'r.TotalIndustrialization < Coef.IndustryInvestRegionCap'),
 ('M21 国庫の近代化の重みが常に上限までの余地', 'PriceOfBread.SimCore/World.cs', 'SlotCapacity.On(Coef, WarmupFrozen, this) ? 1.0 : Math.Max(0, cap - r.TotalIndustrialization)', 'Math.Max(0, cap - r.TotalIndustrialization)'),
 ('M22 巨大建造物が常に上限で切る', 'PriceOfBread.SimCore/Megaprojects.cs', 'slotsOnMega ? add : Math.Min(add, Math.Max(0, c.IndustryInvestRegionCap - r.TotalIndustrialization));', 'Math.Min(add, Math.Max(0, c.IndustryInvestRegionCap - r.TotalIndustrialization));'),
 ('M23 国家の建設のヘッドルームが常に上限', 'PriceOfBread.SimCore/EconomyStep.cs', 'slotsOn ? double.PositiveInfinity : Math.Max(0,', 'Math.Max(0,'),
 ('M24 増えの出所が常に私的', 'PriceOfBread.SimCore/SlotCapacity.cs', 'src == 1 ? Instruments.SlotEv.AddedState : src == 2 ? Instruments.SlotEv.AddedMega : Instruments.SlotEv.Added', 'Instruments.SlotEv.Added'),
 ('M25 国家の増えが台帳 Added に入らない', 'PriceOfBread.SimCore/InstrumentsSlot.cs', 'case SlotEv.AddedState: a.Added += dN; a.AddedState += dN; break;', 'case SlotEv.AddedState: a.AddedState += dN; break;'),
]
def run(cmd):
    return subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', errors='replace', cwd=WT)
res=[]
sel=sys.argv[1:] 
for name,f,old,new in MUTS:
    if sel and name.split()[0] not in sel: continue
    p=os.path.join(WT,f)
    raw=open(p,'rb').read()
    s=raw.decode('utf-8')
    crlf = '\r\n' in s
    o=old.replace('\n','\r\n') if crlf else old
    n=new.replace('\n','\r\n') if crlf else new
    if s.count(o)!=1:
        res.append((name,'ANCHOR-MISS',s.count(o))); print(name,'ANCHOR-MISS',s.count(o),flush=True); continue
    open(p,'wb').write(s.replace(o,n).encode('utf-8'))
    try:
        b=run([DOTNET,'build','PriceOfBread.Tests','-c','Release','--artifacts-path',ART,'-v','q','-nologo'])
        if 'エラー' in b.stdout and ' 0 エラー' not in b.stdout:
            res.append((name,'BUILD-FAIL','')); print(name,'BUILD-FAIL',flush=True); continue
        t=run([DOTNET,'test','PriceOfBread.Tests','-c','Release','--artifacts-path',ART,'--no-build','--filter',FILTER,'-nologo'])
        m=re.search(r'失敗:\s*(\d+)、合格:\s*(\d+)',t.stdout)
        failed=int(m.group(1)) if m else -1
        res.append((name,'KILLED' if failed>0 else 'SURVIVED',failed)); print(name,'KILLED' if failed>0 else 'SURVIVED',failed,flush=True)
    finally:
        open(p,'wb').write(raw)
print('---');
for r in res: print(*r)
