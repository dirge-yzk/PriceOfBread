# (452) 突然変異：EconomyStep.cs の国庫の教育の買い付けの旗の経路を 1 箇所ずつ壊し、番人（StateEduDemandTests）が赤になるかを見る。
# 使い方：python mut.py <worktree> <artifacts> <log>   ／ 各変異は適用→テスト→元へ戻す（バイトで読み書き）。
import subprocess, sys, os
wt, art, log = sys.argv[1], sys.argv[2], sys.argv[3]
only = sys.argv[4] if len(sys.argv) > 4 else None   # 例 M9＝その変異だけ回す
path = os.path.join(wt, 'PriceOfBread.SimCore', 'EconomyStep.cs')
orig = open(path, 'rb').read()
M = [
 ('M1 教育：足す行を落とす', '                    if (stateFront && want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN は足さない側。', '                    if (false && want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN は足さない側。'),
 ('M2 教育：供給が尽きた月の入口を旗で開けない', 'if (gi == Catalog.EducationId && region.CompulsoryEduFunding > Eps && (remaining > 1e-12 || stateFront || insOn))', 'if (gi == Catalog.EducationId && region.CompulsoryEduFunding > Eps && (remaining > 1e-12 || insOn))'),
 ('M3 教育：尽きた月の未充足を充足から数える（二重計上）', '                        foreach (var p in region.Pops) if (!p.IsSoldier && p.GoodNeed > Eps) unmet += Math.Max(0, p.GoodNeed - p.GoodDemand);   // 旗 1 で供給が尽きた月：予算の足りなさ（insOn', '                        foreach (var p in region.Pops) if (!p.IsSoldier && p.GoodNeed > Eps) unmet += p.GoodNeed * Math.Max(0, 1.0 - p.EducationFill);   // 旗 1 で供給が尽きた月：予算の足りなさ（insOn'),
 ('M4 教育：買えた分を引かずに want 全部を足す', 'm.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN は足さない側。', 'm.LastDemand += want;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN は足さない側。'),
 ('M5 旗を無視して常に足す', 'bool stateFront = coef.StateEduDemandInFront > 0;', 'bool stateFront = true;'),
 ('M6 高等教育：入口を旗で開けない', '(remaining > 1e-12 || stateFront || insOn) && m.Price > Eps)', '(remaining > 1e-12 || insOn) && m.Price > Eps)'),
 ('M7 高等教育：足す行を落とす', '                    if (stateFront && want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形）', '                    if (false && want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形）'),
 ('M9 計器の対称：教育の入口を計器で開けない（旗 0 の計器が尽きた月の買いたい分を出さない）', 'if (gi == Catalog.EducationId && region.CompulsoryEduFunding > Eps && (remaining > 1e-12 || stateFront || insOn))', 'if (gi == Catalog.EducationId && region.CompulsoryEduFunding > Eps && (remaining > 1e-12 || stateFront))'),
 ('M8 教育：購入を壊す（旧の take を want の半分にする）', 'double take = Math.Min(remaining, want);', 'double take = Math.Min(remaining, want * 0.5);'),
]
out = []
try:
    for name, a, b in M:
        if only and not name.startswith(only): continue
        src = orig.decode('utf-8')
        assert src.count(a) == 1, (name, src.count(a))
        open(path, 'wb').write(src.replace(a, b, 1).encode('utf-8'))
        r = subprocess.run(['dotnet', 'test', os.path.join(wt, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~StateEduDemand', '-nologo'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        tail = [l for l in r.stdout.splitlines() if ('失敗!' in l or '成功!' in l or 'error' in l.lower() and 'CS' in l)]
        verdict = 'RED' if '失敗!' in r.stdout else ('BUILD-ERR' if 'error CS' in r.stdout else 'GREEN(!)')
        out.append(f'{name}\t{verdict}\t{tail[:1]}')
        print(out[-1], flush=True)
finally:
    open(path, 'wb').write(orig)
open(log, 'w', encoding='utf-8').write('\n'.join(out) + '\n')
