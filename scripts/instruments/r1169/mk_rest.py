# 読むだけ (1169)：台帳（木 c3c4de36a）の S01-V01・V02 と S22 の未確かめ 9 行を、今の木で読んだ判定で 12 列に書き直す（他の列は台帳のまま）。
# 使い方: python -X utf8 -I mk_rest.py <repo> <作業フォルダ>
import sys, os, subprocess
REPO, W = sys.argv[1:3]
NEW = 'c3c4de36a'
md = subprocess.run(['git', '-C', REPO, 'show', f'{NEW}:docs/機能の台帳.md'], capture_output=True).stdout.decode('utf-8')
rows = {}
for l in md.split('\n'):
    if l.startswith('| S01-V0') or any(l.startswith(f'| S22-H0{k} ') for k in ('27', '29', '34', '35', '36', '37', '38', '40', '42')):
        c = [x.strip() for x in l.strip().strip('|').split('|')]
        rows[c[0]] = c
M = '（M＝keep_1164/base_ins・N1／N2＝keep_1164/n1・n2・台本 r1169）'
MF = '（M＝keep_1164/base_ins・全表の幅 N1f／N2f＝keep_1150/n1・n2〔木 d05c6bea9〕・台本 r1169/s22_cp.py）'
NOREAD = 'App・Display の説明台帳の読み手（PlayText.ExplainTerms の呼び出し）は Legitimacy・NationalConsciousness・Exhaustion・AdminEfficiency だけ（GameViewModel.Politics.cs:249・:256・GameViewModel.Refresh.cs:151・FiscalTables.cs:119）'
# 値＝(判定の型 or None, 錨 or None, 持ち主 or None, 判定, 根拠, 残りタスク or None, 12 列目)
O = {
 'S01-V01': (None, None, None, '働かない（逆順が 92.6%）',
   '〈読み直し〉engineer_s の E 行（解禁した四半期）で、魚の缶詰を解禁した国×種のうち缶詰より先か缶詰が未解禁のまま：M 92.6%（593 のうち先 309・未解禁 240・同じ四半期 33・缶詰が先 11）・N1 93.8%・N2 88.6%＝S01-H005e（魚の缶詰の行に After が無い・Catalog.cs:427）と同じ根。開幕の既知は E 行に出ないので数えていない' + M, None,
   '直す：S01-H005e（消す候補 ⑤＝行を畳みレシピは残す）の線で閉じる・番人は After の欠け 0'),
 'S01-V02': (None, None, None, '働かない（未解禁の国×行の 45% で必要性 0 が 50 年以上）',
   '〈読み直し〉engineer_s の T 行（8 四半期ごと・必要性を計算した標本だけ＝replaced が nan の標本は連続を切る）で、未解禁のまま needTerm 0 が連続 25 標本（50 年）以上の国×行：M 10,582／23,655（行 34）・N1 10,548／23,683（33）・N2 10,768／24,324（34）。水力発電・蒸気タービン・電動機・石油火力・窒素固定・合成ゴム・電化製品・合成染料は M で全ての種の約 61 国（611）・人絹 609・鉄筋コンクリート 606。技術力が門の 70 のまま必要性 0 は蒸気機関だけ（M 188・N1 196・N2 196）。根は 11-12 の §3 根②（新製品の比が構造として 1 を下回る・電力の鎖の錠）と同じ〈推〉' + M, None,
   '直す：S01-H004d・H004e（必要性の製法と新製品）の線で閉じる〈AI の推奨〉'),
 'S22-H027': ('頻度', 'CapitalMarket.cs:104:CapitalFlightRatePerQuarter;CapitalMarket.cs:109:divFci0', None, '働く（保存の食い違い 0）',
   '〈読み直し〉cpwdft_s 10 種：逃避が起きた国×呼び出し M 389,415（書かれた行の 96.1%・呼び出しは四半期に約 1 回＝nation_s の国×四半期の約 9 割〈推〉）・N1f 386,266・N2f 388,954。旧の式と台帳の逃避の額の食い違い 0・片側の記帳 0（逃避・利札とも・3 腕）。cpworld_s：持ち主の対外資産の減り＝受け手の負債の減り（逃避＋落とし）の相対差の最大 M 1.45e-11（N1f 1.27e-11・N2f 6.49e-12）・払えなかった利札の落としのある呼び出し M 2,817／6,880（N1f 2,744・N2f 2,785）' + MF, None, '—'),
 'S22-H029': ('効き', 'CapitalMarket.cs:63:TechEducation.AbsorptionOf;Coefficients.p03_debt_money.cs:141:CapitalCreditFloor', 'CapitalMarket.cs TechEducation.cs', '働く（ソース）・文書が古い（頻度は未確かめ＝列なし）',
   '〈読み直し・ソース〉吸収＝CapitalCreditFloor 0.5＋0.5×AbsorptionOf を伸びしろに掛ける（CapitalMarket.cs:63〜64）＝床つきの掛け算で 0 にはならない。文書の変数 CapitalAbsorptionBlend=1.0 は木に無い（文書が古い）。伸びしろ Headroom（:10）は工業化度を読む（★(665)③ の答え待ち）。cpwdft の absorbedFlight は〈旧の式では逃避があるのに台帳の差が 0〉の印（InstrumentsCpwd.cs:86）で吸収能力ではない＝(1161) の〈absorbedFlight は有る〉は取り違え（M で 43 行・N1f 42・N2f 36）。国ごとの AbsorptionOf を書く計器は無い', None,
   '—〔印〕倍率の一掃（伸びしろへの掛け算）'),
 'S22-H034': (None, 'Fiscal.cs:240:bl.Reset("BondRate")', 'Fiscal.cs Explain.cs', '働かない（読み手 0）・文書が古い',
   '〈読み直し・ソース〉書き手は Fiscal.cs:240〜248（説明台帳が付いた国だけ＝App はプレイヤー国・GameViewModel.cs:282）。' + NOREAD + '。Runner の内訳の動詞（PriceOfBread.Runner/Program.cs:2903〜2908）も BondRate を出さない＝画面にも診断にも出ない。Instruments.cs:390 は同じ式を計器で写す（説明台帳ではない）', None,
   '直す（表示）〈AI の推奨・作者に聞く〉：画面の金利の行のホバーへ結ぶか、書き手 9 行（Fiscal.cs:240〜248）を消す'),
 'S22-H035': (None, 'Credit.cs:64:exCredit.Reset("CreditTarget");PriceOfBread.Runner/Program.cs:2905:CreditTarget', 'Credit.cs Explain.cs', '一部だけ働く（画面に出ない・Runner の内訳の動詞だけ）・文書が古い',
   '〈読み直し・ソース〉書き手 Credit.cs:62〜68（説明台帳が付いた国だけ）。' + NOREAD + '。読み手は Runner の内訳の動詞 1 行（PriceOfBread.Runner/Program.cs:2905）だけ＝文書の〈表示専用〉は古い', None,
   '直す（表示）〈AI の推奨・作者に聞く〉：画面に結ぶか、診断の動詞だけと文書を直す'),
 'S22-H036': (None, 'CapitalMarket.cs:68:exCap.Reset("CapitalReturn");World.cs:1401:exFx.Reset("ExchangeRateTarget");PriceOfBread.Runner/Program.cs:2906:CapitalReturn', 'CapitalMarket.cs World.cs Explain.cs', '一部だけ働く（画面に出ない・Runner の内訳の動詞だけ）・文書が古い',
   '〈読み直し・ソース〉書き手 CapitalMarket.cs:66〜71（工業化の伸びしろ・政策金利）・World.cs:1401〜（為替の目標）。' + NOREAD + '。読み手は Runner の内訳の動詞 2 行（PriceOfBread.Runner/Program.cs:2906・:2907）だけ', None,
   '直す（表示）〈AI の推奨・作者に聞く〉：H035 と同じ'),
 'S22-H037': (None, 'FiscalTables.cs:74:Budget;FiscalTables.cs:85:Fiscal.ForecastTax', 'FiscalTables.cs GameViewModel.Fiscal.cs Fiscal.cs', '働く（表示・ソース）・文書が古い',
   '〈読み直し・ソース〉予測は PriceOfBread.Display/FiscalTables.cs の Budget（旧 App の UpdateForecast を移した）・税の予測はエンジンの査定 Fiscal.ForecastTax を目標の率で呼ぶ（:84〜85＝課税への同意・身分の免税・届く割合ほか）・行政費は距離の割増 AdminDistanceMean を掛ける（:94）＝文書の⚠〈画面が自前で計算・実収と一致しない（fis-10）〉〈行政費は距離の割増の無い旧式〉は古い。利払いの予測は現行金利×残高（:91）＝平均クーポンではない（文書のとおり）。実効の列は国庫の台帳の月の差分（FiscalTables.cs:230・:232）。画面は撮っていない', None,
   '—（文書を直す）'),
 'S22-H038': (None, 'GameViewModel.Fiscal.cs:62:SampleTreasuryLedger;FiscalTables.cs:218:LedgerOf', 'GameViewModel.Fiscal.cs FiscalTables.cs Fiscal.cs', '働く（表示・ソース）・文書が古い',
   '〈読み直し・ソース〉台帳の月の差分（GameViewModel.Fiscal.cs:62〜66・ヘッダーの収支と同じ瞬間に取る＝GameViewModel.cs:408）が予算表の実効の列と残りの行（restIn・restOut＝FiscalTables.cs:179〜180・218）に入る＝独立の台帳の表ではない（文書の見出しは古い）。〈0 の区分は行を出さない〉は画面を撮っていない＝未確かめ', None,
   '—（文書を直す）'),
 'S22-H040': (None, 'FiscalTables.cs:285:兌換カバー率;FiscalTables.cs:254:CoverVisible', 'FiscalTables.cs PlayText.cs', '働く（表示・ソース）・文書が古い',
   '〈読み直し・ソース〉出す条件＝金本位に関わるとき（加入中・志向あり MonetaryTarget≧2・兌換停止中＝FiscalTables.cs:254）・値は PlayText.CoverText（:285）。文書の出典（GameViewModel.Refresh.cs:31-38 ほか）は古い＝今は Display の財政の表。(1161) の錨の取り違え（InstrumentsCornint.cs:7）はこの錨で直した', None,
   '—（錨を直した・文書を直す）'),
 'S22-H042': (None, 'GameViewModel.Fiscal.cs:112:UpdateDebtHeader;GameViewModel.Fiscal.cs:122:デフォルトまで', 'GameViewModel.Fiscal.cs', '働く（表示・ソース）・文書が古い',
   '〈読み直し・ソース〉〈デフォルトまで○か月〉＝月で数え月で出す（GameViewModel.Fiscal.cs:122）＝文書の⚠〈四半期と出るが月を数える（fis-06）〉は直っている。出すのは残りの月が試合の残りの月に収まるときだけ（:119）', None,
   '—（文書を直す）'),
}
out = {'V': [], 'S22': []}
HDR = '\t'.join(['行', '系統', '機能', '期待する動き（出典）', '錨', '持ち主のファイル', '判定の型', '最後に確かめた', '判定', '根拠', '残りタスク', '直す／消す／残す'])
for k, (kind, anc, own, j, basis, rest, fx) in O.items():
    c = rows[k]
    if kind: c[6] = kind
    if anc: c[4] = anc
    if own: c[5] = own
    c[7] = NEW; c[8] = j; c[9] = basis
    if rest: c[10] = rest
    c[11] = fx
    c = [x.replace('|', '／') for x in c]
    out['V' if k.startswith('S01') else 'S22'].append('\t'.join(c))
open(os.path.join(W, 'ledger_S01V_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write(HDR + '\n' + '\n'.join(out['V']) + '\n')
open(os.path.join(W, 'ledger_S22u9_2026-11-16.tsv'), 'w', encoding='utf-8', newline='\n').write(HDR + '\n' + '\n'.join(out['S22']) + '\n')
print(len(out['V']), len(out['S22']))
