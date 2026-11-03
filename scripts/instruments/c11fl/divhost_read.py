# (801) c11fl：divhost_s<種>.tsv（外資の配当の旧新の表・全ての国×四半期）を読む。読むだけ。
# 使い方：python divhost_read.py <ins フォルダ> <種の数> [--nine] [--ng-other <inv_income の tsv>]
#   出力：①配置の検査（行の数・rentMonths の分布・call の重複）②レントの近似（rentQSum÷(3×rentMonth3)）
#         ③門 U（上限）：paid ≦ 3×rentMonth3×(1+1e-9) の違反の数  ④門 L（下限）：rentMonth3>0 かつ fci>0 かつ 分母>1e-9 の行で paid÷分母 ≧0.999 の違反の数（分母＝min(divNewIndex, invSav)）
#         ⑤同じ盤面の旧新（旗 0 の盤面では反実仮想の新・旗 1 の盤面では実際）：Σ金建ての paid・Σ金建ての divOld・Σ金建ての divNew 予測（min(divNewIndex, invSav)）を 10 年ごと
#         ⑥持ち分→1 の数（fci÷(fci＋invSav) ≧0.99 の行）・払いが貯蓄の半分以上を取る行の数・未払いの割合（1−paid÷divReq）
# 同じ四半期の同じ国に 2 回以上行がある（助走の呼び出し）ときは最後の行だけ読む。
# 額の単位：各国の通貨→金建て（×fxRate）→種の平均。四半期の額（月平均×3 ではない＝divhost_s は四半期の額そのもの）。
import sys, csv, collections, math, os
d = sys.argv[1]; N = int(sys.argv[2])
NINE = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
TOL_U = 1e-9; TOL_L = 0.999
rows_total = 0; mon_hist = collections.Counter(); dup_calls = 0
ratio_q = []                # rentQSum ÷ (3×rentMonth3)
anymis = 0; rentpos = 0     # rentAnyPop≠rentMonth3 の行数／rentMonth3>0 の行数
U_viol = 0; U_n = 0; L_viol = 0; L_n = 0; L_min = float('inf'); U_max = 0.0
share99 = 0; fci_pos = 0; halfS = 0; unpaid_num = 0.0; unpaid_den = 0.0; zero_den = 0
dec = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])   # (dec,all/nine) → paid, divOld, newPred, divReq（金建て・種の平均）
nine = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])
worst = []; eq_new = eq_old = eq_none = 0; eq_rows = 0
for s in range(1, N + 1):
    p = os.path.join(d, f'divhost_s{s}.tsv')
    if not os.path.exists(p): print('欠け', p); continue
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        last = {}
        for row in r:
            rows_total += 1
            k0 = (row[ix['quarter']], row[ix['nation']])
            if k0 in last: dup_calls += 1
            last[k0] = row
        for row in last.values():
            q = int(row[ix['quarter']]); call = int(row[ix['call']]); y = int(row[ix['year']]); nat = row[ix['nation']]
            e = float(row[ix['fxRate']]); rent3 = float(row[ix['rentMonth3']]); qs = float(row[ix['rentQSum']]); mo = int(float(row[ix['rentMonths']]))
            anyp = float(row[ix['rentAnyPop']]); fci = float(row[ix['fci']]); sav = float(row[ix['invSav']])
            dold = float(row[ix['divOldIndex']]); dnew = float(row[ix['divNewIndex']]); req = float(row[ix['divReqIndex']]); paid = float(row[ix['paidIndex']])
            mon_hist[mo] += 1
            if mo != 3: continue
            if rent3 > 0:
                rentpos += 1; ratio_q.append(qs / (3.0 * rent3))
                if abs(anyp - rent3) > 1e-9 * max(1.0, abs(rent3)): anymis += 1
            if fci > 0:
                fci_pos += 1
                if fci / (fci + sav) >= 0.99: share99 += 1
                if sav > 0 and paid >= 0.5 * sav: halfS += 1
            if req > 1e-9: unpaid_num += max(0.0, req - paid); unpaid_den += req
            if req > 0 or dold > 0 or dnew > 0:
                eq_rows += 1
                if req == dnew: eq_new += 1
                if req == dold: eq_old += 1
                if req != dnew and req != dold: eq_none += 1
            # 門 U
            if paid > 0:
                U_n += 1
                lim = 3.0 * rent3
                if paid > lim * (1 + TOL_U) + 0.0: U_viol += 1; worst.append((paid / max(lim, 1e-300), s, q, nat, paid, lim))
                U_max = max(U_max, paid / lim if lim > 0 else float('inf'))
            # 門 L
            if rent3 > 0 and fci > 0:
                den = min(dnew, sav)
                if den > 1e-9:
                    L_n += 1; v = paid / den
                    L_min = min(L_min, v)
                    if v < TOL_L: L_viol += 1
                else: zero_den += 1
            # 旧新（金建て・種の平均）
            pred = min(dnew, sav)
            dd = y // 10 * 10
            for key in (dd, ):
                a = dec[key]; a[0] += paid * e / N; a[1] += dold * e / N; a[2] += pred * e / N; a[3] += req * e / N
                if nat in NINE and 1850 <= y <= 1949:
                    b = nine[key]; b[0] += paid * e / N; b[1] += dold * e / N; b[2] += pred * e / N; b[3] += req * e / N
print(f'■①配置：行 {rows_total}・同じ四半期の 2 回目以降の行 {dup_calls}・rentMonths の分布 {dict(sorted(mon_hist.items()))}')
if ratio_q:
    ratio_q.sort(); m = len(ratio_q)
    print(f'■②レントの近似 rentQSum÷(3×rentMonth3)（rentMonth3>0 の行 {m}）：p10 {ratio_q[m//10]:.3f}・中央 {ratio_q[m//2]:.3f}・p90 {ratio_q[m*9//10]:.3f}・最小 {ratio_q[0]:.3f}・最大 {ratio_q[-1]:.3f}')
    print(f'   rentAnyPop≠rentMonth3（投資家でない POP にレントが書かれている）の行：{anymis} / {rentpos}')
print(f'■③門 U（paid ≦ 3×rentMonth3×(1+1e-9)）：違反 {U_viol} / 払いのある行 {U_n}・paid÷(3×rentMonth3) の最大 {U_max:.6g}')
for w in sorted(worst, reverse=True)[:5]: print('   違反の例（比・種・四半期・国・paid・上限）', w)
print(f'■④門 L（rentMonth3>0 かつ fci>0 かつ 分母>1e-9 の行で paid÷min(divNewIndex,invSav) ≧{TOL_L}）：違反 {L_viol} / {L_n}・最小 {L_min:.6g}・分母が 1e-9 以下で除いた行 {zero_den}')
print(f'■⑦計器と実装の形の一致：divReqIndex==divNewIndex の行 {eq_new}・divReqIndex==divOldIndex の行 {eq_old}・どちらでもない行 {eq_none}（旗 0 なら old、旗 1 なら new に全て一致するはず）')
print(f'■⑥持ち分 fci÷(fci+invSav) ≧0.99 の行 {share99} / fci>0 の行 {fci_pos}・払いが投資家の貯蓄の半分以上を取る行 {halfS}・未払いの割合 {unpaid_num / unpaid_den if unpaid_den > 0 else float("nan"):.4f}')
print('■⑤同じ盤面の旧新（金建て・種の平均・10 年ごと）：全ての国 paid／divOld／min(divNew,invSav)／divReq')
for k in sorted(dec): print(f'  {k}\t' + '\t'.join(f'{x:.5g}' for x in dec[k]))
print('■⑤′9 国・1850〜1949：paid／divOld／min(divNew,invSav)／divReq')
tp = [0.0] * 4
for k in sorted(nine):
    print(f'  {k}\t' + '\t'.join(f'{x:.5g}' for x in nine[k]))
    for i in range(4): tp[i] += nine[k][i]
print('  合計\t' + '\t'.join(f'{x:.6g}' for x in tp))
# 観測の門 O の一次の予言：9 国・1850〜1949 の Σ min(divNew,invSav) ÷ Σ otherPos（inv_income.py の出力の 9国計・1850〜1940 の otherPos の和＝配当に依らない受け取り）。
#   ⚠divhost_s は IsInvestor の全ての階級（flow_s の読み手は階級 3・10・4・9 だけ）＝分子の絶対値は flow_s より約 15% 大きい（旗 0 の paid の和 ÷ 基線の flow_s の配当の和で割って両方を出す）。
# (805) O の予言は旗 0 の腕（divReqIndex が全ての行で divOldIndex に一致する）のときだけ出す。旗 1 の腕では divNew が実際なので、旗 0 の盤面を前提にした予言は意味を持たない。
flag0 = eq_rows > 0 and eq_none == 0 and eq_old == eq_rows
if '--other' in sys.argv and not flag0:
    print('■O 観測の一次の予言：省略（旗 1 の腕＝divReqIndex が divOldIndex に全行は一致しない・旗 0 の盤面の予言なので出さない）')
if '--other' in sys.argv and flag0:
    op = 0.0; dv = 0.0
    with open(sys.argv[sys.argv.index('--other') + 1], encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['nation'] == '9国計' and 1850 <= int(r['dec']) <= 1940: op += float(r['otherPos']); dv += -float(r['div'])
    k = dv / tp[0] if tp[0] > 0 else float('nan')
    print(f'■O 観測の一次の予言（9 国・1850〜1949・otherPos の和 {op:.6g}・基線 flow_s の配当の和 {dv:.6g}）：')
    print(f'   計器の paid÷otherPos = {tp[0] / op:.4f}（旗 0 の実際）・計器の Σmin(divNew,invSav)÷otherPos = {tp[2] / op:.4f}（旗 1 の一次の予言・上側の見積り）')
    print(f'   flow_s の階級（3・10・4・9）に合わせて ×{k:.4f}：旗 1 の予言 {tp[2] * k / op:.4f}（旗 0 の実際は基線の flow_s で {dv / op:.4f}）')
