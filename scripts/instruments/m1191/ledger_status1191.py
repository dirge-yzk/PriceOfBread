# (1191) 合流：挙動を変えた線（c118nc・c118ur）と恒等の線 c118hc の台帳の行の最後の列の末尾に〈(1191) …〉を足した（中身は書き換えない・1 回だけ回した記録）。
#   python -X utf8 -I ledger_status1191.py <docs/機能の台帳.md>
import sys
p = sys.argv[1]; b = open(p, 'rb').read(); nl = '\r\n' if b'\r\n' in b else '\n'; s = b.decode('utf-8')
ST = {
 'S15-40': '(1191) c118nc で RevolutionConsciousness 0.25→0（国民意識の廃止の 1 段目）＝革命は国民意識を上げない・状態の削除は波119',
 'S25-59': '(1191) c118ur で UnificationAiPursueChance 0.7→1.0・UnificationAiWarGapYears 3→0（AI の待ちと見送りを外した・動詞は共通のまま）＝種 1〜20 で効く四半期 0・同盟の門は AI に無い（前からの非対称・波119）',
 'S24-H017': '(1191) c118ur で待ちと 30% の見送りを外した（S25-59 と同じ）',
 'S23-H013': '(1191) c118nc で NationBuildMobilizationWeight 0.5→0＝戦時の徴兵を国民意識で削らない（戦時×総動員の兵÷人口 ×1.48）・SkillMobilizationRelief は読まれない＝波119 で消す',
 'S27-H006': '(1191) c118nc で SecessionConsciousnessWeight 1.0→0＝分離の圧に国民意識の割引が掛からない（分離の圧 p90 ×1.24）',
 'S27-H021': '(1191) c118nc で SecessionConsciousnessWeight 1.0→0（S27-H006 と同じ）',
 'S02-H004a': '(1191) c118hc で枝 StockpileFactor・欄 WarScareTension・係数 3 本を消した（恒等）',
}
L = s.split(nl); n = 0
for i, l in enumerate(L):
    for k, v in ST.items():
        if l.startswith('| ' + k + ' |'):
            c = l.rstrip().rstrip('|').rstrip(); L[i] = c + '〈' + v + '〉 |'; n += 1
print(n)
open(p, 'wb').write(nl.join(L).encode('utf-8'))
