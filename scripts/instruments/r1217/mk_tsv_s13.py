# 読むだけ (1217) c122rs：11-12 の S13 の 57 行を、今の木 245780b63 の錨・写しと読み直しの別で 12 列の TSV に書く（(1210) mk_tsv_s06.py の形を写した）。
#  入力：q/ledger_S13_2026-11-12.tsv（木 7bb40c77）・q/s13_cmp.tsv（cmp_rows.py）・q/s13_anchor_hunks.tsv・q/s13_readers.tsv（dl_readers.py）・q/s13_reread_m1212.txt（s13_reread.py）。
#  出力：q/ledger_S13_2026-11-18.tsv（12 列）・q/side_S13_2026-11-18.tsv（写した判定と読み直した判定の列を分ける）・q/dl_table_S13_2026-11-18.tsv（消す候補）。
#  使い方: python -X utf8 -I mk_tsv_s13.py <作業フォルダ>
import sys, os, csv
W = sys.argv[1]
NEW = '245780b63'
RAW = '（生 keep_1212/base_ins 種 1〜10・台本 r1217 の s13_reread.py・出力 q/s13_reread_m1212.txt）'
def rd(p): return list(csv.reader(open(os.path.join(W, p), encoding='utf-8'), delimiter='\t'))
cmp = {r[0]: r for r in rd('q/s13_cmp.tsv')[1:]}
near = {r[0] for r in rd('q/s13_anchor_hunks.tsv')[2:] if len(r) > 3 and r[3] not in ('0', '—')}
DELTA = {
 'S13-02': '囲む関数 Simulation の字が違う＝開幕の州の係数 CenterIsFirstProvince・CoreInitByRegionCulture の焼き込み（錨の InitialImmigration の行は字が同じ）',
 'S13-10': '係数 BaseUrbanCap はコードから消えた（(812) で天井を外した後の片付け）＝錨は文書だけ（ゲーム概要と実装機能.md:2162）',
 'S13-11': '係数 UrbanCapFollowSpeed はコードから消えた＝錨は文書だけ（ゲーム概要と実装機能.md:2167）',
 'S13-25': '錨 Crime.cs:68 の注は履歴 docs/history/Crime.cs.md:65 へ移った・係数 CrimeSatPenalty は (384) で消えた（ScalarDeletionTests.cs:40 が無いことを確かめる・Coefficients.p02 の注 :18 に名だけ残る）',
 'S13-32': '錨 Territory.cs:5 の注は書き換わった（(551) gc1 で解錠の道を消した旨は 7bb40c77 の注にあった）＝コードに解錠の道は無い',
 'S13-41': '錨の字は同じ（cmp の〈係数が消えた public〉は Coefficients の分割に伴う誤検出）・EraFactor は今も常に 1.0（Coefficients.p10_ai_silver_overrides.cs:60）',
 'S13-43': '関数 Blend の字が違う＝(1193) c119nd が国民意識の物理（Pop.NationalConsciousness）を消し、移る組の国民意識の重みの 3 行が消えた（識字・闘争度・政治意識・民族の取り分・思想・年齢・お金は今も運ぶ）',
 'S13-44': '係数 MigrationFarePerkKm はコードから消えた（(551) gc1）＝錨は文書だけ（ゲーム概要と実装機能.md:2230 の打消し）',
 'S13-57': '関数 CheckSecession の字が違う＝(1193) で SecessionConsciousnessWeight の項が消えた・PublicOrder の読みは Politics.cs:925・PowerShares.cs:20 に残る',
}
ANCH = {
 'S13-10': 'Coefficients.p04_trade.cs.md:356:BaseUrbanCap',
 'S13-11': 'Coefficients.p04_trade.cs.md:356:UrbanCapFollowSpeed',
 'S13-25': 'Crime.cs.md:65:CrimeSatPenalty',
 'S13-32': 'Territory.cs.md:9:locked',
 'S13-44': 'Coefficients.p08_audit_soldiers.cs.md:804:MigrationFarePerkKm',
 'S13-55': 'MultiNationSim.cs:1485:coreAtDest',
 'S13-05': 'SelfPlay.cs:527:w.ImmigrationRestriction != ImmigrationLaw.Closed;SelfPlay.cs:528:ImmigrationRestrictionPrefScale',
 'S13-06': 'SelfPlay.cs:526:immWant;SelfPlay.cs:531:UnequalTreatyInForce',
 'S13-37': 'MultiNationSim.cs:1433:double effFill',
}
L = {}
for l in open(os.path.join(W, 'q/s13_reread_m1212.txt'), encoding='utf-8'):
    k = l.split(' ', 1)[0]; L.setdefault(k, []).append(l.strip())
# 読み直した行：(判定の上書き or None, 判定の型の上書き or None, 根拠)
RE = {
 'S13-02': (None, None, '〈読み直し〉monlaw 種 1 の 1783 年：日本 2・朝鮮 2・清 1・他の国は 0＝11-12 と同じ'),
 'S13-05': (None, '頻度', '〈読み直し〉締める遷移（段が上がる）0／408,987（全ての国の続いた四半期の組・種 1〜10）＝11-12 の 0／408,208 と同じ・線は流入の取りうる最大の位置（SelfPlay.cs:528〜529・ソース）'),
 'S13-06': (None, '頻度', '〈読み直し〉開く遷移 22 件・国×種 13（開幕に開放でない 3 国×10 種＝30 のうち）・同じ四半期に条約港の旗（rule_s の treatyPortInForceFlag）がある遷移 13／22＝11-12 の〈9／30・全て外圧〉より多い・残る 9 件は同じ四半期に旗が無い（前の四半期の旗か、資本の要求か＝未確かめ・近似）'),
 'S13-10': (None, None, '〈読み直し・ソース〉天井の係数はコードから消えた＝文書の 2162 行が古いまま'),
 'S13-11': (None, None, '〈読み直し・ソース〉追従の係数はコードから消えた＝文書の 2167 行が古いまま'),
 'S13-12': (None, '頻度', '〈読み直し〉urxpd（計器の国の本国の州×月 1,936,470）：飢えの門 20.9%・農奴の門 13.0%・向きの反転 17.0%・対なし 0%＝11-12 の 18〜26%・11〜17%・15〜18% の内'),
 'S13-16': (None, None, '〈読み直し〉region の wageFactor（全ての国の州×四半期 2,174,981）：1% 0.85・中央 0.992・99% 1.443＝国の中の比（国の豊かさは入らない）＝11-12・反証と同じ'),
 'S13-19': (None, '頻度', '〈読み直し〉窃盗の溜まりが正の州×四半期 53.3%（11-12 の 52.7%）・窃盗÷所得は和の比で 0.012%（11-12 の 0.031% は州ごとの平均＝数え方が違う・どちらも 0.1% 未満）'),
 'S13-20': (None, '頻度', '〈読み直し〉床 0.1 の下に入った因子 0／2,174,981（住居・賃金・押し〔押しは計器の式＝近似〕）＝入力の値域で届かない'),
 'S13-22': (None, None, '〈読み直し・ソース〉Crime.cs:11〜12 の秩序＝EffectiveCoerce÷(人口×CrimeOrderPerPop)・EffectiveCoerce は人口に比例＝人口が約分される（反証どおり）'),
 'S13-25': (None, None, '〈読み直し・ソース〉CrimeSatPenalty はコードに無い（(384) で消えた・注 1 行と消えたことを確かめる試験 1 本だけ）＝文書の削りの文が古い'),
 'S13-26': (None, None, '〈読み直し〉国内の組の移民の総量 657,151 千人（種 1〜10）'),
 'S13-27': (None, None, '〈読み直し〉上限 0.2 に届くには gMax≧6.67（RegionMobility 0.03）が要る・機会の最大 1.545・残る値打ちの最小 0.162＝gMax の上界 1.23＝届かない（ソースと生）。州の四半期の流出÷人口の最大 5.0%（種 5 清の琉球・525 四半期目）'),
 'S13-28': (None, None, '〈読み直し〉国内の移民の経路 2000km 超は量の 64.7%（11-12 の 72.3%）＝文書の距離の項の書き方が古いのは同じ'),
 'S13-29': (None, '頻度', '〈読み直し〉持ち分≦1.01 の州×四半期 6.0% が越境の流出の 65.1% を出す（11-12・反証の 5.1%・69.9%）'),
 'S13-32': (None, None, '〈読み直し・ソース〉解錠の道は (551) で消えた・文書の見出しが古い'),
 'S13-35': (None, None, '〈読み直し〉xmig（越境の組・種 1〜10）：米の流入 0.9〜12.1 千人・流出 1,512〜5,166 千人・最大の受け手は 10 種とも清＝11-12・反証と同じ向き'),
 'S13-36': (None, None, '〈読み直し〉英の本国格の州（5 州×10 種）の越境の流出の年率は最大 0.050%（四半期の流出の和÷人口の和×4・近似）＝史実の 0.5〜0.7% の約 1 桁下（11-12 の 0.002〜0.06% と同じ桁）'),
 'S13-37': (None, None, '〈読み直し・ソース〉MultiNationSim.cs:1433 の effFill＝src.FoodFill＋(r.FoodFill−src.FoodFill)＝州の充足そのもの（行が動いただけ）'),
 'S13-41': (None, None, '〈読み直し・ソース〉EraFactor は常に 1.0（Coefficients.p10_ai_silver_overrides.cs:60）・読み手は Territory.cs:70・73'),
 'S13-43': (None, None, '〈読み直し・ソース〉Blend は国民意識を運ばなくなった（(1193)）＝文書の〈国民意識を運ぶ〉が古くなった・残りは字が同じ'),
 'S13-44': (None, None, '〈読み直し・ソース〉運賃の係数はコードに無い（(551)）'),
 'S13-51': (None, '頻度', '〈読み直し〉技術者の出国禁止法が 0 でない国×四半期 0／409,752（種 1〜10）＝誰も動かせない法（反証 高-1）'),
 'S13-57': (None, None, '〈読み直し・ソース〉CheckSecession の意識の項が消えた（(1193)）・秩序の読みは 2 箇所に残る＝その設計で書き直す行のまま'),
 'S13-04': (None, None, '〈読み直し・ソース〉要求の枝は PolicyPreferences.cs の流入の率（SelfPlay.cs:522〜524 と同じ量）・要求が立ったかの列は無い＝未確かめのまま'),
}
KMAP = {'S13-20': 'K90', 'S13-27': 'K91', 'S13-50': 'K92', 'S13-51': 'K92', 'S13-41': 'K93', 'S13-38': 'K94', 'S13-23': 'K95', 'S13-24': 'K95', 'S13-05': 'K97'}
RDR = {r[0]: r for r in rd('q/s13_readers.tsv')[1:]}
def rdn(k):
    if k == 'K97': return '締める枝 SelfPlay.cs:527〜530 の 4 行＋試験 AiFirstWaveTests.cs（係数 ImmigrationReceiveCapPerQuarter は受入の上限でも読む＝消えない・係数の読み手 14）'
    r = RDR[k]; return f'読み手 {sum(int(x) for x in r[2:12])}＝エンジン {r[2]}・計器 {r[3]}・係数 {r[5]}・試験 {r[6]}・App {r[8]}・台本 {r[10]}'
def fixdec(s):
    return (s.replace('〈作者「全て推奨で」で決定（2026-11-12）〉', '〈AI の推奨（作者「全て推奨で」の下の候補）〉')
             .replace('作者「全て推奨で」で決定', 'AI の推奨（作者「全て推奨で」の下の候補）')
             .replace('〈作者裁定を上書き・推奨で〉', '〈AI の推奨で作者の裁定を上書きする候補〉'))
def act_of(r):
    t = r[11]; j = r[8]
    if t and t != '—':
        for k in ('消す', '直す', '残す'):
            if t.startswith(k): return k, ''
        if 'その設計で書き直す' in t: return '直す', ''
        return '残す', ''
    if j == '働く': return '残す', '働く'
    if j == '未確かめ': return '残す', '計器が無い＝§5 の計器が埋めるまで判定しない'
    if j == '文書が古い': return '直す', '文書だけ（行の数を変えない overview の直し）'
    return '残す', j
ACT_OVR = {
 'S13-25': ('直す', '文書だけ（係数はコードに無い）＋Coefficients.p02 の注 :18 の名を消す（恒等の注の直し）'),
 'S13-10': ('直す', '文書だけ（天井はコードに無い）'),
 'S13-11': ('直す', '文書だけ（追従はコードに無い）'),
 'S13-44': ('直す', '文書だけ（運賃はコードに無い）'),
 'S13-32': ('直す', '文書だけ（解錠の道はコードに無い）'),
 'S13-06': ('残す', '開く遷移 22 件のうち同じ四半期に条約港の旗 13 件・残る 9 件の入口は未確かめ＝記録。S13-05 の消す線の後は両向きとも要求の 1 本から'),
 'S13-43': ('直す', '文書だけ（国民意識を運ぶ文を消す）＋その設計で書き直す（11-12 の 12 列目）'),
}
rows = rd('q/ledger_S13_2026-11-12.tsv')
hdr, body = rows[0], rows[1:]
out, side = [hdr], [['行', '錨の関数の差（cmp_rows.py・anchor_hunks.py）', '写した判定（11-12・木 7bb40c77）', '読み直した判定（245780b63・keep_1212）', '確証', '処置', '理由（1 行）']]
for r in body:
    rid = r[0]; c = cmp.get(rid); old = r[8]
    anchors = ANCH.get(rid) or (c[2] if c and c[2] and '?' not in c[2] else r[4])
    if c and c[1] == '変わった': delta = DELTA.get(rid, '囲む関数の字が違う（未読）')
    else: delta = '錨を囲む関数の字が同じ'
    j, typ, basis = RE.get(rid, (None, None, None))
    nj = j or old
    if basis is None: basis = f'〈写し〉11-12 の判定と根拠（木 7bb40c77）を写す。{delta}。11-12 の根拠：' + r[9]
    else: basis = basis + RAW * (('生' in basis[:12]) or ('〈読み直し〉' in basis)) + f'。{delta}'
    r2 = r[:]; r2[4] = anchors; r2[7] = NEW; r2[8] = nj; r2[9] = basis
    if typ: r2[6] = typ
    act, why = ACT_OVR.get(rid) or act_of(r)
    t12 = fixdec(r[11])
    if t12 in ('—', ''): t12 = f'{act}：{why}' if why else act
    if rid in ACT_OVR and (not t12.startswith(act) or rid == 'S13-06'): t12 = f'{act}：{why}（11-12：' + t12 + '）'
    if rid in KMAP and t12.startswith('消す'): t12 = '消す〔' + KMAP[rid] + '・' + rdn(KMAP[rid]) + '・245780b63 の dl_readers.py〕' + t12[2:]
    r2[11] = t12
    out.append(r2)
    re_ = rid in RE
    conf = ('中（生で数えた・種 1〜10）' if '〈読み直し〉' in (basis or '') else '高（ソースの grep）') if re_ else '写し（11-12 の生で数えた物・今の生では数えていない）'
    side.append([rid, delta, old, nj if re_ else '—', conf, act, why or t12[:60]])
def wr(p, rr):
    with open(os.path.join(W, 'q', p), 'w', encoding='utf-8', newline='\n') as f:
        for x in rr: f.write('\t'.join(y.replace('\t', ' ').replace('\n', ' ').replace('|', '／') for y in x) + '\n')
wr('ledger_S13_2026-11-18.tsv', out); wr('side_S13_2026-11-18.tsv', side)
DL = [
 ('K90', '機会の因子の床 0.1 OpportunityFactorFloor（S13-20）', '否', '床（入力の値域で届かない＝0／2,174,981）', '恒等（値域の番人を付ける）', '否', '無い', '導く必要なし', '線 3（M11）'),
 ('K91', '州と州の移民の 1 期の上限 0.2 MaxMigrationFraction（S13-27）', '否', '天井（gMax の上界 1.23・届くには 6.67）', '恒等の候補（盤面で届かない＝浮動小数の順は変わらない・NaN の退避は否定形で残す）', '否', '理屈の上の暴走の止め', '受入の上限（S13-30）と出る側の持ち分（S13-29）', '線 3（M11）'),
 ('K92', '技術者の出国禁止法（法の軸ごと・S13-50・51）', '否', '誰も動かせない法（0／409,752）', '恒等（全部の表のバイト一致）', '弱（英の職工移住禁止法 1719〜1825）', '英の職工移住禁止法の表現だけ', '招聘の割増（MultiNationSim の招聘）が技師の流出を細らせる', '線 1・法の軸 PolicyAxis の全ての読み手'),
 ('K93', '時代の距離 EraMigrationScale・EraFactor（S13-41）', '否', '常に 1.0', '恒等', '否', '無い', 'R6（経路の km×汽船の速さ）', 'S11-34 か線 3'),
 ('K94', '坩堝と差別の重み EthnicDraw 0.7・DiscriminationLiteracy 0.45（S13-38）', '一部（受け手の豊かさと重なる・R1 の後）', '識字 0.45 の閾値の錠（Clamp01）', '挙動（R6 に含める・R1 より前に消さない）', '弱〜中（米の坩堝・排斥）', '統一度の低い国が移民を引く・識字の低い国が阻む', '受け手の 1 人あたりの実の所得（R1）と移民の費用（R6）', 'R1→R6'),
 ('K95', '犯罪の旧い式と配り CrimeBase・CrimeMilitancyWeight・CrimeUrbanWeight（S13-23・24）', '一部（困窮×不満は不満度と重なる＝思想⑨）', '上限 0.016', '挙動（W13）', '否', '困窮大衆への闇の再分配', 'W13 の犯罪者 POP と κ 1 つ', 'W13'),
 ('K96', '計器の定数の列 capBound・wTargetCapped・wCap（urxpd・S13-10）', '否', '—', '恒等（計器・--removed-cols）', '否', '無い', '—', '計器の線（線 4）'),
 ('K97', '移民制限法を AI が締める閾値の線（SelfPlay.cs:527〜530 の締める枝＝係数は消えない）', '否', '閾値（流入の取りうる最大＝0／408,987）', '挙動の線（盤面で届かない＝AiFirstWaveTests.cs の試験 2 本）', '中（排華移民法 1882・1924 年法は要求の側から出す）', '締める向きの AI の目標（一度も立たない）', '要求の 1 本（段B2 の POP の好み）', '線 2'),
 ('K98', '越境の閾値 0.5 CrossBorderOppGapThreshold（S13-34・35）', '否', '閾値（差が 0.5 未満の組を止める）', '挙動（R1 で比例へ置き換え）', '否', '差が小さい組の止め', '差に比例する流れ（R1）', 'R1'),
]
with open(os.path.join(W, 'q', 'dl_table_S13_2026-11-18.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('id\t名\t読み手（245780b63）\t(b) 他の量と重なる\t(c) 閾値・錠・床\t消し方\t史実で重要か\t失う物\t大きな機構から導けるか\t前提の線\n')
    for k, name, b, cc, how, hist, lose, derive, pre in DL:
        f.write('\t'.join([k, name, rdn(k), b, cc, how, hist, lose, derive, pre]) + '\n')
print('行', len(out) - 1, '・読み直し', sum(1 for r in body if r[0] in RE), '・写し', sum(1 for r in body if r[0] not in RE))
