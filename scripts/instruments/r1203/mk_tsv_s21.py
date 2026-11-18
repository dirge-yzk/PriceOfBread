# 読むだけ (1203) c120rh：11-12 の S21 の 32 行を、今の木 c68e09bfb の錨・写しと読み直しの別で 12 列の TSV に書き、3 行（英の不履行の入口・⑩・⑪）を足す
#  （(1196) mk_tsv_s09.py の形を写した）。入力：ledger_S21_2026-11-12.tsv（木 7bb40c77）・out/s21_cmp.tsv（cmp_rows.py）。
#  出力：q3/ledger_S21_2026-11-18.tsv（12 列）・q3/side_S21_2026-11-18.tsv（写した判定と読み直した判定の列を分ける）。
#  12 列目の〈作者「全て推奨で」で決定〉は〈消すと決まった〉と読めるので〈AI の推奨（作者「全て推奨で」の下の候補）〉へ直す（(1196) と同じ）。
#  使い方: python -X utf8 -I mk_tsv_s21.py <作業フォルダ>
import sys, os, csv
W = sys.argv[1]
NEW = 'c68e09bf'
RAW = '（生 keep_1198/base_ins 種 1〜10＝M・keep_1198/a11 種 11〜20＝S11・keep_1198/n1＝N1・keep_1191/base_ins＝m1191・台本 r1203 の s21_recount.py・s21_repay.py・ukdefault.py）'
FN = {  # 錨のファイル → 11-12 から c68e09bf までのコードの差（注と空白を除く・codediff.py）
 'Fiscal.cs': 'Fiscal.Step は国民意識の NationBuildingSpending の塊の削除（(1193)）だけ',
 'Simulation.cs': 'MonthSettle は駐屯の調達 GarrisonProcurement・巨大建造物の外国の利回りの請求・記念碑の積み・戦争の恐れの張力の削除だけ（借入・切り下げ・不換の行は字が同じ）',
 'MultiNationSim.cs': 'StepDay は巨大建造物の段と国民意識の説明の削除だけ',
 'CrownParliament.cs': 'Step は国民意識の漸化式の削除と政体の名の表示だけ',
 'Politics.cs': 'ExecuteRegimeChange は RevolutionAwakensNation の呼び出しの削除だけ',
 'SelfPlay.cs': 'DecideQuarter は StartMegaproject の削除だけ',
 'Credit.cs': 'Credit.Step は退蔵の比の Clamp を否定形へ・金の裏付けの貸出の床 GoldCoverCreditFloor の削除（錨の行 Credit.cs:29 は同じ字）',
 'CapitalMarket.cs': 'CapitalMarket.cs の錨の関数は字が同じ',
 'World.cs': 'World.cs の錨の欄は字が同じ',
 'Government.cs': 'Government.cs の錨の関数は字が同じ',
}
# 読み直した行：(判定の上書き or None, 根拠, 12 列目の上書き or None)
RE = {
 'S21-05': ('一部だけ働く（他国の投資家の貯蓄で切られた月 M 1・プールが縛った月 M 526）', '〈読み直し〉プールが縛った月 M 526・他国の投資家の貯蓄で切られた月 M 1・S11 1・N1 4・m1191 0（nation_s の 2 列の和）。国債の金利の最小 M 9 国 0.0162＝倍率 2.47＜3（fiscal_s）＝上限 3.0 は届かない（11-12 の式の読みと一致）' + RAW, None),
 'S21-07': ('働く（上限に届いた月 M 5.49%）', '〈読み直し〉上限に届いた月（interestAtLimitFlag＝Fiscal.cs:13 の underLimit の否定と同じ不等式）M 9,888/180,270（5.49%）・m1191 5.28%・S11 4.15%・N1 4.38%＝両側が出る。11-12 の 2.1% からの増えは m1191 と同じ幅＝nd の効きではない（11-12〜11-18 の他の線の軌道）。反証 2-4：〈届いた月の 100% で借入を止めた〉は作りから必ず 100%＝証拠にしない。反証 2-2：英は開幕から上限の縁（1787〜93 年に月の 27〜44%・反証 f3.py）' + RAW, None),
 'S21-14': (None, '〈読み直し〉返した額のうち床（年 1%）を超えた分 M 43.8%（英 48.4%・仏 83.9%・墺 64.6%・西 73.1%・清 28.1%・露 26.0%）・m1191 46.2%（fsx_s の repayAboveFloorSum÷repayDoneSum）。反証 2-3：開幕の国債を返した原資の 87〜99% は (616) の〈線を超えた国庫〉（11-12 の数・読み直していない）' + RAW, None),
 'S21-16': ('働く（上限 0.30 に当たった月 M 2・S11 844・N1 258）', '〈読み直し〉金利が BondRateMaxPerYear 0.30 に当たった月 M 2・m1191 13・S11 844（英 種 16 が 838 か月・1820 年から）・N1 258（英 種 7 が 253 か月）。英 S11 種 16 は 1822〜1900 年に利払い÷歳入 1.1〜3.2・払えた率 1・国債は動かない＝利払いが歳入を超えたまま切り下げで全額を払い続ける形。利払い＞歳入で払えた率 1 が 120 か月以上続く国×種は S11 英 1,226・N1 英 507・M オスマン 390・露 388・清 300 ほか（全ての腕に出る＝nd ではない）' + RAW,
            '残す（理由を直す）：上限は GDP が 0 の時の番兵だけではない＝届く行は〈利払い＞歳入を切り下げで払い続ける錠〉の中。根は借入の上限（S21-07）・切り下げ（S21-19）・破産の線（S21-26）の入口の側＝上限だけを消す線は起こさない'),
 'S21-19': ('働く（余地のある月の 99.8%）', '〈読み直し〉借入の後に払えない額が残った月（unpaidBeforeDevaluationShare＞0）M 9,611・うち切り下げた月 9,596（99.8%）・m1191 99.7%・S11 99.7%・N1 99.9%' + RAW, None),
 'S21-22': ('働く（不換の台帳が減った四半期 M 15）', '〈読み直し〉不換の台帳（money_s cumulativeFiatIssuedStock）が減った四半期 M 15＝英 6・墺 4・露 3・オスマン 1・仏 1・m1191 14（S11・N1 は money_s が無い＝数えない）。反証：画面（Display/FiscalTables.cs）は支払い率を見る（〈見ない〉は半分だけ正しい）' + RAW, None),
 'S21-26': ('働く（不履行 M 12・S11 12・N1 19・m1191 15）', '〈読み直し〉不履行（defaultCount の増え）M 12＝墺 5・英 3・露 2・オスマン 1・仏 1。前の月の onGold で分けると群 A（金本位のまま）M 8・群 B（離脱の後）M 4＝英 3 件は全て群 B（離脱から 2〜3 か月）。累計は利払い÷歳入が上限 0.67 以上の月だけ積む（Fiscal.cs:193・underLimit の否定）・DefaultArrearsMonths 1.0' + RAW,
            None),
 'S21-30': (None, None, None),
 'S21-31': ('その設計で書き直す', None, None),
}
ADD = [
 ['S21-33', 'S21', '英の国債の不履行の入口（(1198) の記録：M 3/10・S11 2/10・N1 5/10）', '史実の英は 1797 年に兌換を止め（銀行制限法）、不履行はしない。上限は利払い÷歳入 0.67（借入の門）・累計 1 か月分で不履行', 'Fiscal.cs:13:underLimit;Fiscal.cs:193:InsolvencyMonths;Coefficients.p02_borrow_polity_epidemic.cs:10:DefaultArrearsMonths', 'Fiscal.cs Coefficients.p02_borrow_polity_epidemic.cs', '両側の門・頻度', NEW, '働く（史実と違う向き＝記録）',
  '〈読み直し〉英の不履行 M 3 件（種 2・6＝1810・種 8＝1793）は m1191 と同じ月（月 333・129）＝nd・us の分かれ（1857〜1883 年）より前の同じ軌道。S11 3 件（1794・1821・1949）・N1 5 件（1796〜1861）。11 件のうち 10 件は 1793〜1861 年。前の 12 か月：金本位の月 10〜12・離脱は不履行の 1〜3 か月前・払えた率の最小 0.54〜0.80・上限に届いた月 1〜12。入口＝利払い÷歳入が 0.67 を超えた月に借入の門が閉じ、切り下げで払い切れない月（払えた率＜1）の 1−率 を累計し 1.0 に届くと不履行（2〜3 か月）。兌換の停止（離脱）は累計を止めない' + RAW,
  '挙動の線の素案（次の波以後）：不履行の累計を〈離脱の前の月の未払い〉だけでなく、離脱で開いた切り下げの余地の後に読む（史実＝兌換の停止が不履行の代わり）。新しい係数なし・形は群 B にだけ当たる＝群 A（金本位のまま 1919〜49）は S22-06 の根の線', '直す（挙動の線・前に読む線は済み）：AI の推奨（作者「しばらくの間、全て推奨で」の下の候補）'],
 ['S21-34', 'S21', '⑩ 利札の残り（§1 の残りタスクの列）', '外資の逃避の預けに付く利札の未払いで請求権を落とす（(819) c65fl・規則 C の拡張）', 'CapitalMarket.cs:111:CapitalCouponUnpaidWriteDown;Coefficients.p03_debt_money.cs:137:CapitalCouponUnpaidWriteDown', 'CapitalMarket.cs Coefficients.p03_debt_money.cs', '効き', NEW, '文書が古い',
  '〈読み直し・ソース〉CapitalCouponUnpaidWriteDown＝1（Coefficients.p03_debt_money.cs:137）で CapitalMarket.cs:111 の枝が動く＝利札の残りの線は入っている。この行は国債（S21）ではなく資本の逃避（S22）の側＝11-12 の担当の読み（ledger_S21_2026-11-12.md:34）と一致', '—', '残す（§1 の S21 の残りタスクの列から外し S22 の列だけに置く＝文書の直し）'],
 ['S21-35', 'S21', '⑪ InsolvencyQuarters→Months（§1 の残りタスクの列）', '破産の累計を月で数える（(843) で改名）', 'Fiscal.cs:193:InsolvencyMonths;World.cs:866:InsolvencyMonths', 'Fiscal.cs World.cs', '読まれない欄', NEW, '文書が古い',
  '〈読み直し・ソース〉c68e09bf で git grep InsolvencyQuarters 0 件（SimCore・App・Runner）＝エンジン・App は InsolvencyMonths・11-12 の〈Fiscal.cs:349 の注の旧名だけ残る〉も今は無い＝済み', '—', '残す（§1 の S21 の残りタスクの列から ⑪ を外す＝文書の直し）'],
]
cmp = {r['行']: r for r in csv.DictReader(open(os.path.join(W, 'out', 's21_cmp.tsv'), encoding='utf-8'), delimiter='\t')}
rows = list(csv.reader(open(os.path.join(W, 'ledger_S21_2026-11-12.tsv'), encoding='utf-8'), delimiter='\t'))
hdr, body = rows[0], rows[1:]
out, side = [hdr], [['行', '錨の関数の差', '写した判定（11-12・木 7bb40c77）', '読み直した判定（c68e09bf・keep_1198）', '確証', '処置', '理由（1 行）']]
def fixdec(s): return s.replace('〈作者「全て推奨で」で決定（2026-11-12）〉', '〈AI の推奨（作者「全て推奨で」の下の候補）〉').replace('作者「全て推奨で」で決定', 'AI の推奨（作者「全て推奨で」の下の候補）')
for r in body:
    rid = r[0]; c = cmp.get(rid); old_judge = r[8]
    anchors = c['新しい錨'] if c and c['新しい錨'] else r[4]
    files = sorted({a.split(':')[0] for a in anchors.split(';') if a})
    delta = '；'.join(FN.get(f, f + ' の錨の関数は字が同じ') for f in files) if c and c['判定'] == '変わった' else '錨の関数の字が同じ（cmp_rows.py）'
    j, basis, fix = RE.get(rid, (None, None, None))
    new_judge = j or old_judge
    if basis is None:
        basis = f'〈写し〉11-12 の判定と根拠（木 7bb40c77）を写す。錨の関数の差は〈{delta}〉＝S21 の式に触れない。11-12 の根拠：' + r[9]
    r2 = r[:]; r2[4] = anchors; r2[7] = NEW; r2[8] = new_judge; r2[9] = basis; r2[11] = fixdec(fix if fix else r[11])
    RD = {'S21-05': '読み手 2＝エンジン 2・K77', 'S21-09': '読み手 5＝エンジン 3・係数 1・試験 1・K78', 'S21-20': '読み手 18＝エンジン 2・計器 1・係数 2・試験 13・K79', 'S21-23': '読み手 6＝エンジン 5・係数 1・K80', 'S21-24': '読み手 2＝エンジン 2・K81', 'S21-32': '読み手 18＝エンジン 2・試験 8・App 4・Runner 4・K82'}
    if rid in RD and r2[11].startswith('消す'): r2[11] = '消す〔' + RD[rid] + '・c68e09bf の dl_readers.py〕' + r2[11][2:]
    if r2[11] in ('—', ''): r2[11] = '残す：' + ('未確かめ（数える計器が無い＝直す物を言えない）' if r2[8] == '未確かめ' else ('その設計で書き直す（大きな設計の側）' if r2[8].startswith('その設計') else '働く・直す物なし（c68e09bf）'))
    if rid == 'S21-31': r2[9] = '〈写し＋反証 2-6〉発火の口（政体の移り）は王権と議会の作り直しで書き直す＝判定を〈その設計で書き直す〉へ。11-12 の根拠：' + r[9]
    if rid == 'S21-30': r2[9] = '〈写し＋反証〉App の召集の表示（GameViewModel.Politics.cs）は旗 0 の死んだ枝を出す（反証 §3）。' + f'錨の関数の差は〈{delta}〉。11-12 の根拠：' + r[9]
    out.append(r2)
    reread = rid in RE and (RE[rid][0] is not None or RE[rid][1] is not None)
    side.append([rid, delta, old_judge, new_judge if reread else '—', ('写し＋反証（生で数えていない）' if rid == 'S21-31' else '中（生で数えた・M は種 1〜10）') if reread else '写し（錨の関数の差が S21 の式に触れない）', r2[11].split('：')[0].split('（')[0] or '—', (r2[11][:80] if r2[11] != '—' else '—')])
for a in ADD:
    out.append(a); side.append([a[0], '新しい行', '—', a[8], '中' if a[0] == 'S21-33' else '高（ソースの grep）', a[11].split('（')[0], a[11][:80]])
with open(os.path.join(W, 'q3', 'ledger_S21_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    for r in out: f.write('\t'.join(x.replace('\t', ' ').replace('\n', ' ').replace('|', '／') for x in r) + '\n')
with open(os.path.join(W, 'q3', 'side_S21_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    for r in side: f.write('\t'.join(x.replace('\t', ' ').replace('\n', ' ').replace('|', '／') for x in r) + '\n')
print('行', len(out) - 1, '読み直し', sum(1 for s in side[1:] if s[3] != '—'))
