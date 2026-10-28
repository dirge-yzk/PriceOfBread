"""(639) 線 fsc 後半：腕の採る条件 ②③④⑤⑧⑨⑪ と記録 R1・R2・R7・R10 を fsc 表（fsc_s<種>.tsv）から出す。読むだけ。
使い方：python an_fsc_arm.py <ins フォルダ> [種の数=10]
 行＝計器の9国は毎四半期・他の国は年末。連続する四半期の比べ（⑧⑨）は9国だけ（四半期が連続する行）。
 ② 処分の向き（旗 OpulenceUseByInterest が正の腕）／③ 関税の書き手の向き（旗 AiTariffFromInterest が正の腕）は書き手が読んだ一致度（used*）で数える＝配管の確認。
 ⑧ ⑨ 実効の量：四半期末の一致度（agree*）で条件を作る（旗 0 の腕でも同じ定義で出る＝比べる側 Ains0 と並べる）。
事前登録 追記2 §14-3 の定義。出力の数は種の中央値（④(a) は種ごとの値の中央値・②③⑧⑨⑪ は全種の和の割合）。"""
import csv, sys, os, io, math, statistics as st, collections
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
d = sys.argv[1]; N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
NAT = ['アメリカ', 'イギリス', 'オスマン', 'オーストリア', 'フランス', 'プロイセン', 'ロシア', '日本', '清']
def fl(x):
    try: return float(x)
    except: return float('nan')
def rows(path):
    with open(path, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r
ERA = [(1783, 1819), (1820, 1859), (1860, 1899), (1900, 1929), (1930, 1950)]
def era(y):
    for i, (a, b) in enumerate(ERA):
        if a <= y <= b: return i
    return len(ERA) - 1
TAX = [('所得', 0, 'agreeIncome', 'incomeEff'), ('人頭', 1, 'agreePoll', 'pollEff'), ('土地', 2, 'agreeLand', 'landEff'), ('累進', 3, 'agreeProg', 'progEff')]
c2 = collections.Counter(); c3 = collections.Counter(); c8 = collections.defaultdict(collections.Counter); c9 = collections.Counter(); c11 = collections.Counter()
c10 = collections.Counter()
cells4 = []; ends = collections.Counter(); runmax = []
dispose_taxdown = 0; dispose_spendup = 0
parl_n = 0; tot_n = 0
sign_flip = collections.Counter()
xrows = 0
price_ratio = []
for s in range(1, N + 1):
    p = os.path.join(d, f'fsc_s{s}.tsv')
    if not os.path.exists(p): print(f'# 種{s}: fsc_s{s}.tsv が無い'); continue
    series = collections.defaultdict(list)
    cell = collections.defaultdict(lambda: [False, False])
    for ix, r in rows(p):
        q = int(r[ix['quarter']]); y = int(r[ix['year']]); n = r[ix['nation']]
        g = lambda k: fl(r[ix[k]]) if k in ix else float('nan')
        tot_n += 1; parl_n += r[ix['parliamentBranchFlag']] == '1'
        te = g('tariffEnd')
        if te == te:
            ends['n'] += 1; ends['hi'] += te >= 0.9; ends['lo'] += te <= 0.01
        x = g('xExcess')
        # ② 処分の向き（書き手が読んだ一致度）
        if x > 0 and not math.isnan(g('beforeIncome')) and r[ix['crisisOverwroteFlag']] == '0':
            xrows += 1
            for nm, k, ag, eff in TAX:
                u = g('usedAgree' + ag[5:]); b = g('before' + ag[5:]); a_ = g('after' + ag[5:])
                if u == u and u != 0 and b > 0:
                    c2['tax_den'] += 1
                    ok = (a_ < b - 1e-15) if u < 0 else (abs(a_ - b) <= 1e-15)
                    c2['tax_ok'] += ok
                    if a_ < b - 1e-15: dispose_taxdown += 1
            for nm, key in (('Social', 'Social'), ('Edu', 'Edu')):
                u = g('usedAgree' + key); b = g('before' + key); a_ = g('after' + key)
                if u == u and u != 0:
                    c2['sp_den'] += 1
                    ok = (a_ >= b - 1e-15) if u > 0 else (abs(a_ - b) <= 1e-15)
                    c2['sp_ok'] += ok
                    if a_ > b + 1e-15: dispose_spendup += 1
        # ③ 関税の書き手の向き
        wt = g('tariffWriterTarget'); wa = g('tariffWriterAnchor'); ut = g('usedAgreeTariff')
        if wt == wt and wa == wa and ut == ut and ut != 0:
            if wt <= -0.25 + 1e-12 or wt >= 1.0 - 1e-12: c3['clamped'] += 1
            else:
                c3['den'] += 1
                dsg = (wt > wa + 1e-12) - (wt < wa - 1e-12)
                c3['ok'] += dsg == (1 if ut > 0 else -1)
            c3['over'] += r[ix['tariffOverriddenFlag']] == '1'; c3['all'] += 1
        # ⑪ 社会保障の目標が法を超えて残った
        st_ = g('socialTargetEnd'); lw = g('socialLawStage')
        if st_ == st_ and lw == lw:
            c11['den'] += 1
            tgt = round(round(st_ / 0.05) * 0.05 * 3)
            c11['refused'] += tgt > lw
        series[n].append((q, y, r, ix))
    # 9国の連続行
    for n in NAT:
        L = sorted(series.get(n, []), key=lambda t: t[0])
        for i in range(len(L)):
            q, y, r, ix = L[i]
            g = lambda k, r=r, ix=ix: fl(r[ix[k]]) if k in ix else float('nan')
            if i > 0 and L[i - 1][0] == q - 1:
                a = fl(L[i - 1][2][L[i - 1][3]['tariffEnd']]); b = g('tariffEnd')
                if a == a and b == b:
                    if b > a + 1e-9: cell[(n, era(y))][0] = True
                    if b < a - 1e-9: cell[(n, era(y))][1] = True
            # 4 四半期先
            j = i + 4
            if j < len(L) and L[j][0] == q + 4:
                rj, ixj = L[j][2], L[j][3]
                gj = lambda k, rj=rj, ixj=ixj: fl(rj[ixj[k]]) if k in ixj else float('nan')
                x = g('xExcess')
                if x > 0:
                    for nm, k, ag, eff in TAX:
                        a_ = g(ag)
                        if a_ == a_ and a_ < 0:
                            c8[nm]['den'] += 1; c8[nm]['down'] += gj(eff) < g(eff) - 1e-12
                            c8['全']['den'] += 1; c8['全']['down'] += gj(eff) < g(eff) - 1e-12
                at = g('agreeTariff'); dt = gj('tariffEnd') - g('tariffEnd')
                if at == at and at != 0 and abs(dt) > 1e-9:
                    c9['den'] += 1; c9['ok'] += (dt > 0) == (at > 0)
        # 符号の連続（四半期ごとの変化の符号）
        run = 0; best = 0; lastsg = 0
        for i in range(1, len(L)):
            if L[i - 1][0] != L[i][0] - 1: run = 0; lastsg = 0; continue
            dv = fl(L[i][2][L[i][3]['tariffEnd']]) - fl(L[i - 1][2][L[i - 1][3]['tariffEnd']])
            sg = 1 if dv > 1e-9 else -1 if dv < -1e-9 else 0
            if sg != 0 and sg == lastsg: run += 1
            elif sg != 0: run = 1
            else: run = 0
            if sg != 0: lastsg = sg
            best = max(best, run)
        runmax.append(best)
    cells4.append(sum(1 for v in cell.values() if v[0] and v[1]))
print('# 入力', d, '種', N, '行の数', tot_n, '議会の枝の割合 %.3f' % (parl_n / max(1, tot_n)))
def fr(a, b): return '%.4f (%d/%d)' % (a / b if b else float('nan'), a, b)
print('② 処分の向き：税 ', fr(c2['tax_ok'], c2['tax_den']), '／支出', fr(c2['sp_ok'], c2['sp_den']), '（x>0 の行', xrows, '）')
print('④(b) 処分の書き込み：税の下げ', dispose_taxdown, '件・支出の引き上げ', dispose_spendup, '件（10種の合計）')
print('③ 関税の書き手の向き', fr(c3['ok'], c3['den']), '／端のクランプ', c3['clamped'], '／上書きされた行', fr(c3['over'], c3['all']))
print('④(a) 9国×5時代の 上げと下げの両方がある数（種の中央値）', st.median(cells4) if cells4 else float('nan'), '種ごと', cells4)
print('⑤ 端：tariffEnd≥0.9 の割合', fr(ends['hi'], ends['n']), '・≤0.01 の割合', fr(ends['lo'], ends['n']))
print('⑧ 実効の税の向き（x>0 かつ 一致度<0 の次の 4 四半期に実効が下がった割合）：' + '／'.join(f"{k} {fr(v['down'], v['den'])}" for k, v in c8.items()))
print('⑨ 実効の関税の向き：符号一致', fr(c9['ok'], c9['den']), '／関税の同じ符号の連続の最大（国×種の中央値・最大）', st.median(runmax) if runmax else float('nan'), max(runmax) if runmax else float('nan'))
print('⑪ 社会保障の目標が法の段を超えて残った行の割合', fr(c11['refused'], c11['den']))
