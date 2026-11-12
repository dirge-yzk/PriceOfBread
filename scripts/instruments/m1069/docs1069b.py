# (1069) 合流の docs の書き足し 3)〜5)（docs1069.py の 1 回目が 3) の検査で止まった＝1)・2) は入った。2 回目は 3)・4) が入り 5) の検査で止まった＝DO34 で 3)・4) を飛ばす）
#   python -X utf8 docs1069b.py
R = 'D:/PoB_work/wt_m1069/'
def rd(p): return open(R + p, 'rb').read()
def wr(p, b): open(R + p, 'wb').write(b)
DO34 = False

# 3) CURRENT：最終更新の行を差し替え、前の行を〈前の更新（(1060)）〉へ（古い節にも同じ頭の行が残る＝最初の 1 行）
if DO34:
    C = 'docs/CURRENT.md'
    cl = rd(C).decode('utf-8').split('\n')
    k = [i for i, l in enumerate(cl) if l.startswith('| ★ **最終更新（main へ合流済み）**')]
    cl[k[0]] = cl[k[0]].replace('| ★ **最終更新（main へ合流済み）**', '| 前の更新（(1060)）', 1)
    cl.insert(k[0], '| ★ **最終更新（main へ合流済み）** | ⚠ **2026-11-12（(1069) 合流（波101）＝挙動の線 c101vs〔(1064) 宗主と属国の輪を切る・旗 DependencyCycleBreak 既定 1・計器 dep_s〕を採った＝新しい基線 m1069**。…（実際の文は docs/CURRENT.md の 14 行）')
    wr(C, '\n'.join(cl).encode('utf-8'))

# 4) README：★(1069) を ★(1060) の後に（実際の文は docs/README.md の ★(1069)）
if DO34:
    pass

# 5) 機能の台帳：線の持ち主の行（(1060) の〈矯正講和の束ね〉の行の直後＝〈判定は線の担当と…〉の頭の行は 4 本ある）
T = 'docs/機能の台帳.md'
tl = rd(T).decode('utf-8').split('\n')
k = [i for i, l in enumerate(tl) if l.startswith('- 矯正講和の束ね〈崩壊 1 回に矯正 1 回〉（(1060)・採らない）')]
assert len(k) == 1, len(k)
NEW = [
    '- 出す表を選ぶ口と二段の判定（(1069)）：(1061) c101sel で POB_INSTRUMENTS_TABLES（Instruments.cs:47〜:59 と Open・未設定・空は全表）・大きい 5 表の計算の切り・scripts/instruments/ident/stage.py（段 1＝SMALL 29 表／段 2＝全表）・prune の --allow 必須と PRUNE の既定 1。穴（反証役 v）：--cfg で STAGE_BASE を替えると段 1・エンジン側の計器の欄だけの差分は段 1 のまま・TABLES= が名乗られない（00一覧 次の波）。',
    '- 枝 uifix a7d854dd3 の取り込み（(1069)）：(1062) c101uf（App 78・SimCore/Display 15・Tests 16 ファイル・エンジン 0）。撮影の規則 CivilianToSoldier・BudgetVsLedger は実際の画面で空振り（画面の本線へ）。uifix の a7d854dd3 の後の 4 本は未取り込み。',
    '- 宗主と属国の輪を切る（(1069)）：(1064) c101vs で旗 DependencyCycleBreak を 1 に点火（Coefficients.cs:1612・Diplomacy.cs:450〜:466 SetDependency・計器 dep_s〔InstrumentsDep.cs・Instruments.cs:1945〕・DependencyCycleTests）。切る経路は年代記の行と緊張の残りを残さない（材料）。',
    '- 形 Y 国家の調達を販売へ（(1069)・採らない）：(1063) c101py は門 G1 が通ったが手続きで採らない（MilitaryTests の書き換え・procx_s が Open を通らない）。旗 ProcureStateAsSale・計器 procx_s は main に無い（枝 origin/c101py）。',
]
for j, line in enumerate(NEW):
    tl.insert(k[0] + 1 + j, line)
wr(T, '\n'.join(tl).encode('utf-8'))
print('docs ok')
