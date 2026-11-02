# r794: r794n_s<種>.tsv・r794w_s<種>.tsv（使い捨ての木の読むだけの計器）を読む。引数：<ins フォルダ> <種の数>
# 出力：①世界×10 年（種の平均・金建て）②国×10 年（9 国＋負担の上位）③保存の検査（四半期ごとの差の最大）
import sys, csv, collections, math
d = sys.argv[1]; N = int(sys.argv[2])
F = lambda x: float(x)
W = collections.defaultdict(float)        # (dec, key)
Wend = collections.defaultdict(float)     # 10 年の最後の四半期のストック (dec,key)
NAT = collections.defaultdict(float)      # (nation, dec, key)
NATend = {}
chk = collections.defaultdict(float)      # 検査の最大
maxcnt = collections.Counter()
HB = collections.defaultdict(list); HBc = collections.defaultdict(float); UNP = collections.defaultdict(float); DQ = collections.defaultdict(float)
for s in range(1, N + 1):
    rows = list(csv.DictReader(open(f'{d}/r794n_s{s}.tsv', encoding='utf-8'), delimiter='\t'))
    wrows = list(csv.DictReader(open(f'{d}/r794w_s{s}.tsv', encoding='utf-8'), delimiter='\t'))
    bycall = collections.defaultdict(list)
    for r in rows: bycall[int(r['call'])].append(r)
    lastcall_dec = {}
    for wr in wrows:
        c = int(wr['call']); y = int(wr['year']); dec = y // 10 * 10
        lastcall_dec[dec] = c
        rs = bycall[c]
        g = lambda r, k: F(r[k]) * F(r['e'])
        domGot = sum(g(r, 'domGot') for r in rs); domIn = sum(g(r, 'domIn') for r in rs)
        tFl = sum(g(r, 'flightTaken') for r in rs); tDv = sum(g(r, 'divTaken') for r in rs)
        dvReq = sum(g(r, 'divReq') for r in rs); flReq = sum(g(r, 'flightReq') for r in rs)
        back = sum(g(r, 'backK') for r in rs); refund = sum(g(r, 'refund') for r in rs)
        sent = sum(g(r, 'sentK') for r in rs); recv = sum(g(r, 'recvK') for r in rs)
        ngdpQ = sum(F(r['ngdp']) * 3 * F(r['e']) for r in rs)
        W[(dec, 'invSavMeanQ')] += sum(F(r['invSav']) * F(r['e']) for r in rs) / N
        W[(dec, 'fciMeanQ')] += sum(F(r['fciAfter']) * F(r['e']) for r in rs) / N
        W[(dec, 'gdpYearMeanQ')] += ngdpQ * 4 / N
        W[(dec, 'top1div')] += max((g(r, 'divTaken') for r in rs), default=0) / N
        for k, v in (('domGot', domGot), ('domIn', domIn), ('flightTaken', tFl), ('divTaken', tDv), ('divReq', dvReq), ('flightReq', flReq), ('back', back), ('refund', refund), ('fdiSent', sent), ('fdiRecv', recv), ('ngdpQ', ngdpQ), ('fx', F(wr['fx'])), ('max0', F(wr['max0BindCount'])), ('quarters', 1)):
            W[(dec, k)] += v / N
        # 検査：金建ての保存（各四半期）
        bt = F(wr['backTotalGold'])
        if back + refund > 0 or tFl + tDv > 0:
            chk['|Σ取った金建て−Σ戻した金建て|/Σ取った'] = max(chk['|Σ取った金建て−Σ戻した金建て|/Σ取った'], abs((tFl + tDv) - (back + refund)) / max(1e-12, tFl + tDv))
        if domGot > 0:
            chk['|Σ逃避の金建て−Σ受け皿の金建て|/Σ逃避'] = max(chk['|Σ逃避の金建て−Σ受け皿の金建て|/Σ逃避'], abs(domGot - domIn) / domGot)
        if recv > 0 or sent > 0:
            chk['|Σ送り−Σ受け|/Σ送り'] = max(chk['|Σ送り−Σ受け|/Σ送り'], abs(sent - recv) / max(1e-12, sent))
        maxcnt['droppedQuarters'] = max(maxcnt['droppedQuarters'], int(wr['droppedQuarters']))
        for r in rs:
            nm = r['nation']
            fb = F(r['fciBefore']); sv = F(r['invSav']); dq = F(r['divReq']); dt = F(r['divTaken'])
            if fb > 1e-6:
                HB[dec].append((dq / sv if sv > 1e-9 else float('inf'), fb / sv if sv > 1e-9 else float('inf'), (dq - dt) / dq if dq > 1e-12 else 0.0))
                if fb > sv: HBc[(dec, 'fci>sav')] += 1 / N
                HBc[(dec, 'hostQ')] += 1 / N
                UNP[nm] += (dq - dt) * F(r['e']) / N; DQ[nm] += dq * F(r['e']) / N
            NAT[(nm, dec, 'net')] += (g(r, 'backK') + g(r, 'refund') - g(r, 'domGot') - g(r, 'flightTaken') - g(r, 'divTaken') + g(r, 'domIn')) / N
            for k in ('domGot', 'domIn', 'flightTaken', 'divTaken', 'divReq'):
                NAT[(nm, dec, k)] += g(r, k) / N
            NAT[(nm, dec, 'back')] += g(r, 'backK') / N
            NAT[(nm, dec, 'fdiRecv')] += g(r, 'recvK') / N
            NAT[(nm, dec, 'ngdpQ')] += F(r['ngdp']) * 3 * F(r['e']) / N
            NAT[(nm, dec, 'invSavQ')] += F(r['invSav']) * F(r['e']) / N
            NAT[(nm, dec, 'fciQ')] += F(r['fciAfter']) * F(r['e']) / N
            NAT[(nm, dec, 'quarters')] += 1 / N
    for dec, c in lastcall_dec.items():
        wr = [x for x in wrows if int(x['call']) == c][0]
        for k in ('fciSumGoldAfter', 'caSumGoldAfter', 'fShadowSumGoldAfter'):
            Wend[(dec, k)] += F(wr[k]) / N
decs = sorted({k[0] for k in W})
print('■世界×10 年（種の平均・金建て・10 年の和・ストックは 10 年の最後の四半期）')
cols = ['domGot', 'domIn', 'flightTaken', 'divReq', 'divTaken', 'back', 'refund', 'fdiSent', 'fdiRecv', 'ngdpQ', 'max0', 'quarters']
print('dec\t' + '\t'.join(cols) + '\tΣFCI\tΣ影(逃避由来)\t影/FCI\tΣCA\t(CA−FCI)/FCI\t配当/GDP\t配当/ΣFCI年')
for dec in decs:
    fci = Wend[(dec, 'fciSumGoldAfter')]; ca = Wend[(dec, 'caSumGoldAfter')]; sh = Wend[(dec, 'fShadowSumGoldAfter')]
    q = W[(dec, 'quarters')]
    print(f'{dec}\t' + '\t'.join(f'{W[(dec, k)]:.4g}' for k in cols)
          + f'\t{fci:.4g}\t{sh:.4g}\t{(sh / fci if fci > 0 else float("nan")):.4f}\t{ca:.4g}\t{((ca - fci) / fci if fci > 0 else float("nan")):.4f}'
          + f'\t{(W[(dec, "divTaken")] / W[(dec, "ngdpQ")] if W[(dec, "ngdpQ")] > 0 else float("nan")):.5f}\t{(W[(dec, "divTaken")] / (q / 4) / fci if fci > 0 and q > 0 else float("nan")):.4f}')
print('■世界×10 年の平均の比（四半期の平均）：ΣFCI÷年の世界GDP・ΣFCI÷Σ投資家の貯蓄')
for dec in decs:
    q = W[(dec, 'quarters')]
    if q <= 0: continue
    print(f'{dec}\tFCI/年GDP={W[(dec, "fciMeanQ")] / W[(dec, "gdpYearMeanQ")]:.3f}\tFCI/投資家の貯蓄={W[(dec, "fciMeanQ")] / W[(dec, "invSavMeanQ")]:.3f}\t投資家の貯蓄/年GDP={W[(dec, "invSavMeanQ")] / W[(dec, "gdpYearMeanQ")]:.3f}')
print('■検査（四半期ごとの最大）')
for k, v in chk.items(): print(f'{k}\t{v:.3e}')
print('捨てた四半期（Diplomacy の参照の不一致）\t', maxcnt['droppedQuarters'])
print('■国×10 年（種の平均・金建て）：受け皿の負担＝(逃避の払い＋配当の払い)÷受け入れ・配当の払い÷GDP・持ち主の戻り÷逃避')
nats = sorted({k[0] for k in NAT})
print('nation\tdec\tdomIn\tflightTaken\tdivTaken\tdomGot\tback\tfdiRecv\t負担/受入\t配当/GDP\t戻り/逃避\tFCI/年GDP(平均)')
rank = []
for nm in nats:
    tot_div = sum(NAT[(nm, dd, 'divTaken')] for dd in decs); tot_gdp = sum(NAT[(nm, dd, 'ngdpQ')] for dd in decs)
    rank.append((tot_div / tot_gdp if tot_gdp > 0 else 0, nm))
    for dd in decs:
        if NAT[(nm, dd, 'quarters')] == 0: continue
        di = NAT[(nm, dd, 'domIn')]; ft = NAT[(nm, dd, 'flightTaken')]; dv = NAT[(nm, dd, 'divTaken')]; dg = NAT[(nm, dd, 'domGot')]; bk = NAT[(nm, dd, 'back')]; gq = NAT[(nm, dd, 'ngdpQ')]
        qn = NAT[(nm, dd, 'quarters')]
        print(f'{nm}\t{dd}\t{di:.4g}\t{ft:.4g}\t{dv:.4g}\t{dg:.4g}\t{bk:.4g}\t{NAT[(nm, dd, "fdiRecv")]:.4g}\t{((ft + dv) / di if di > 0 else float("nan")):.3f}\t{(dv / gq if gq > 0 else float("nan")):.5f}\t{(bk / dg if dg > 0 else float("nan")):.3f}\t{(NAT[(nm, dd, "fciQ")] / qn / (gq / qn * 4) if gq > 0 else float("nan")):.4f}')
print('■配当の払い÷GDP（全期間）の上位 15 国')
for v, nm in sorted(rank, reverse=True)[:15]: print(f'{nm}\t{v:.5f}')
print('■受け皿の噛み（外資の負債がある国×四半期・全種）：配当の請求÷投資家の貯蓄（四半期）・FCI÷投資家の貯蓄・払えなかった割合の分位（中央値・90%）と FCI>貯蓄 の件数（種の平均）')
def qt(a, p):
    a = sorted(a); return a[min(len(a) - 1, int(p * len(a)))] if a else float('nan')
print('dec\t国×四半期(種平均)\tFCI>貯蓄(種平均)\t配当/貯蓄 中央\t配当/貯蓄 90%\tFCI/貯蓄 中央\tFCI/貯蓄 90%\t未払い 中央\t未払い 90%')
for dec in decs:
    a = HB[dec]
    if not a: continue
    print(f'{dec}\t{HBc[(dec, "hostQ")]:.0f}\t{HBc[(dec, "fci>sav")]:.0f}\t{qt([x[0] for x in a], .5):.4f}\t{qt([x[0] for x in a], .9):.4f}\t{qt([x[1] for x in a], .5):.3f}\t{qt([x[1] for x in a], .9):.3f}\t{qt([x[2] for x in a], .5):.3f}\t{qt([x[2] for x in a], .9):.3f}')
print('■払えなかった配当の割合の上位 10 国（全期間・金建て）')
for nm in sorted(UNP, key=lambda k: -UNP[k])[:10]: print(f'{nm}\t未払い {UNP[nm]:.4g}\t請求 {DQ[nm]:.4g}\t割合 {UNP[nm] / DQ[nm] if DQ[nm] > 0 else 0:.3f}')
print('■国の純の位置（全期間・金建て・種の平均）：戻り＋返金＋受け皿の受け入れ−逃避−外資の逃避の払い−配当の払い（正＝受け取り超）')
netv = {nm: sum(NAT[(nm, dd, 'net')] for dd in decs) for nm in nats}
gd = {nm: sum(NAT[(nm, dd, 'ngdpQ')] for dd in decs) for nm in nats}
for nm in sorted(netv, key=lambda k: netv[k])[:10] + ['…'] + sorted(netv, key=lambda k: netv[k])[-8:]:
    if nm == '…': print('…'); continue
    print(f'{nm}\t純 {netv[nm]:.4g}\t÷全期間のGDP {netv[nm] / gd[nm] if gd[nm] > 0 else float("nan"):.4f}')
