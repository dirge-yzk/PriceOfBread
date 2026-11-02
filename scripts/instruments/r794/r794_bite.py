# r794: 受け皿の噛みを母集合を絞って読む（①投資家が 1 人以上の国×四半期 ②挙げた 9 国＝最初の州の名で名指す）＋世界の fx の残差（為替の換算の台帳へ積む額）。
# 引数：<r794 の表のフォルダ> <種の数>
import sys, csv, collections
d = sys.argv[1]; N = int(sys.argv[2])
NINE = {'パリ': 'フランス', '江戸': '日本', 'ロンドン': 'イギリス', 'ニューヨーク': 'アメリカ', 'モスクワ': 'ロシア', 'ベルリン': 'プロイセン', 'イスタンブール': 'オスマン', 'ウィーン': 'オーストリア', '上海': '清'}
A = collections.defaultdict(list); B = collections.defaultdict(list); FX = collections.defaultdict(float); FL = collections.defaultdict(float)
NB = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])   # 9 国×10 年：請求・払い・FCI の和・投資家の貯蓄の和（金建て）
zero_inv = collections.Counter()
for s in range(1, N + 1):
    for r in csv.DictReader(open(f'{d}/r794n_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        dec = int(r['year']) // 10 * 10
        fb = float(r['fciBefore']); sv = float(r['invSav']); dq = float(r['divReq']); dt = float(r['divTaken']); e = float(r['e'])
        if fb <= 1e-6: continue
        if float(r['investorCount']) < 1: zero_inv[dec] += 1; continue
        x = (dq / sv if sv > 1e-9 else float('inf'), fb / sv if sv > 1e-9 else float('inf'), (dq - dt) / dq if dq > 1e-12 else 0.0)
        A[dec].append(x)
        if r['nation'] in NINE:
            B[dec].append(x); nb = NB[(NINE[r['nation']], dec)]; nb[0] += dq * e / N; nb[1] += dt * e / N; nb[2] += fb * e / N; nb[3] += sv * e / N
    for r in csv.DictReader(open(f'{d}/r794w_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        dec = int(r['year']) // 10 * 10
        FX[dec] += float(r['fx']) / N; FL[dec] += (float(r['backTotalGold']) + float(r['takenSum']) * 0) / N
def qt(a, p):
    a = sorted(a); return a[min(len(a) - 1, int(p * len(a)))] if a else float('nan')
for name, S in (('①投資家が 1 人以上の国×四半期', A), ('②挙げた 9 国', B)):
    print('■' + name + '：配当の請求÷投資家の貯蓄・FCI÷投資家の貯蓄・払えなかった割合（中央値・90%）・件数（全種）')
    print('dec\t件数\t配当/貯蓄 中央\t配当/貯蓄 90%\tFCI/貯蓄 中央\tFCI/貯蓄 90%\t未払い 中央\t未払い 90%')
    for dec in sorted(S):
        a = S[dec]
        print(f'{dec}\t{len(a)}\t{qt([v[0] for v in a], .5):.4f}\t{qt([v[0] for v in a], .9):.4f}\t{qt([v[1] for v in a], .5):.3f}\t{qt([v[1] for v in a], .9):.3f}\t{qt([v[2] for v in a], .5):.3f}\t{qt([v[2] for v in a], .9):.3f}')
print('■投資家が 0 人の国×四半期（FCI>0・全種）：', dict(sorted(zero_inv.items())))
print('■9 国×10 年（種の平均・金建て）：配当の請求・払い・未払いの割合・FCI÷投資家の貯蓄（四半期の和の比）')
for (n, dec), v in sorted(NB.items()):
    if dec % 30 != 0 and dec != 1900: continue
    print(f'{n}\t{dec}\t請求 {v[0]:.3g}\t払い {v[1]:.3g}\t未払い {1 - v[1] / v[0] if v[0] > 0 else 0:.3f}\tFCI/貯蓄 {v[2] / v[3] if v[3] > 0 else float("inf"):.2f}')
print('■世界の fx（為替の換算の台帳へ積む額＝受け手が受けた額−払い手が払った額・各自の通貨の和・種の平均）と戻しの金建ての和')
for dec in sorted(FX): print(f'{dec}\tfx {FX[dec]:.4g}\t戻し(金建て) {FL[dec]:.4g}')
