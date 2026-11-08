# (974) c87lm 読み手：m968 の盤面で、土地税の払えない分（未払い）と、年に手放す割合の桁を数える（読むだけ・エンジンを呼ばない）。
# 使い方：python -X utf8 -I read974.py <keep_968 のフォルダ> <出力のファイル>
# 入力：<腕>/landfix_s*.tsv（課税の時点の写し：landClaimAtTaxPerMonth・landPaidOverClaimAtTax・baseRAtTaxPerMonth・landClaimPropAtTaxPerMonth・lbEngBaseRAtTaxPerMonth）
#       <腕>/admrule_s*.tsv（deliveryShare＝RuleDelivery の人口加重・四半期の末）・<腕>/fiscal_s*.tsv（govBondRatePerYear・月・列強 9 か国だけ）
# 量（国×四半期・月あたり）：
#   claim＝請求（届きを掛ける前）・paid＝実際に取れた土地税（査定×届き×払える割合）
#   U_up＝claim−paid（上界＝届きの漏れ 1−ruleMul を含む＝(918) §C-3 と同じ定義）
#   U_est＝claim×deliveryShare−paid（推定＝届きを人口加重の平均で割り戻した払えない分・確証は低い＝請求の分布と人口の分布が違えば符号も揺れる）
#   年に手放す割合 φ＝U×rate÷R（値段 P＝R×12÷rate・年の売り 12U）・純の値段（問 2）φn＝U×rate÷(R−paid)
import sys, os, glob, statistics as st

def rd(path):
    with open(path, 'rb') as f:
        lines = f.read().decode('utf-8').splitlines()
    h = lines[0].split('\t')
    ix = {k: i for i, k in enumerate(h)}
    return ix, [l.split('\t') for l in lines[1:] if l]

def fl(x):
    try:
        v = float(x)
        return v if v == v else None
    except Exception:
        return None

PER = [('1783-85', 1783, 1785), ('1800-09', 1800, 1809), ('1840-49', 1840, 1849), ('1900-09', 1900, 1909), ('全期', 1783, 1950)]

def seed_of(p):
    b = os.path.basename(p)
    return int(b.split('_s')[1].split('.')[0])

def arm(d):
    out = {}   # seed -> list of rows (nation, year, q, claim, paid, R, deliv, rate, claimProp, engR)
    for p in sorted(glob.glob(os.path.join(d, 'landfix_s*.tsv'))):
        s = seed_of(p)
        ix, rows = rd(p)
        a_ix, a_rows = rd(os.path.join(d, 'admrule_s%d.tsv' % s))
        deliv = {}
        for r in a_rows:
            deliv[(r[a_ix['quarter']], r[a_ix['nation']])] = fl(r[a_ix['deliveryShare']])
        f_ix, f_rows = rd(os.path.join(d, 'fiscal_s%d.tsv' % s))
        rate = {}
        for r in f_rows:
            m = int(r[f_ix['monthIndex']]); q = m // 3
            v = fl(r[f_ix['govBondRatePerYear']])
            if v is not None:
                rate.setdefault((q, r[f_ix['nation']]), []).append(v)
        L = []
        for r in rows:
            c = fl(r[ix['landClaimAtTaxPerMonth']]); po = fl(r[ix['landPaidOverClaimAtTax']])
            R = fl(r[ix['baseRAtTaxPerMonth']]); cp = fl(r[ix['landClaimPropAtTaxPerMonth']]); eb = fl(r[ix['lbEngBaseRAtTaxPerMonth']])
            if c is None or not (c > 0) or po is None: continue
            q = int(r[ix['quarter']]); n = r[ix['nation']]; y = int(r[ix['year']])
            rl = rate.get((q, n))
            L.append((n, y, q, c, c * po, R, deliv.get((str(q), n)), (sum(rl) / len(rl)) if rl else None, cp, eb))
        out[s] = L
    return out

def main():
    base, outp = sys.argv[1], sys.argv[2]
    arms = [('M（base_ins・種 1〜10）', 'base_ins'), ('N1', 'n1'), ('N2', 'n2'), ('S11（a11・種 11〜20）', 'a11')]
    o = []
    o.append('# read974：m968 の土地税の未払いと年に手放す割合（読むだけ）・入力 %s' % base)
    o.append('# U_up＝請求−取れた額（上界・届きの漏れを含む）・U_est＝請求×届き（人口加重）−取れた額（推定・確証は低い）・φ＝U×金利÷R（年）・φn＝純の値段（R−取れた土地税）で割った φ')
    keep = {}
    for lab, sub in arms:
        d = os.path.join(base, sub)
        if not os.path.isdir(d): o.append('## %s：無い' % lab); continue
        A = arm(d); keep[lab] = A
        seeds = sorted(A)
        o.append('')
        o.append('## %s（種 %s）' % (lab, ','.join(map(str, seeds))))
        # (a) 件数：種あたりの国×四半期（請求＞0）と、U_est/claim が 1% 超・10% 超の件数・U_est の和
        tot = []; e1 = []; e10 = []; neg = []; sumU = []; sumUup = []; sumC = []
        for s in seeds:
            L = A[s]
            tot.append(len(L))
            k1 = k10 = kn = 0; su = suu = sc = 0.0
            for (n, y, q, c, pd, R, dv, rt, cp, eb) in L:
                if dv is None: continue
                ue = c * dv - pd
                if ue > 0.01 * c: k1 += 1
                if ue > 0.10 * c: k10 += 1
                if ue < -0.01 * c: kn += 1
                su += max(0.0, ue); suu += c - pd; sc += c
            e1.append(k1); e10.append(k10); neg.append(kn); sumU.append(su); sumUup.append(suu); sumC.append(sc)
        o.append('- 国×四半期（請求＞0）の数・種の中央：%d・U_est が請求の 1%% 超：%d（%.1f%%）・10%% 超：%d・U_est が −1%% 未満（推定の符号の揺れ）：%d' % (
            st.median(tot), st.median(e1), 100.0 * st.median(e1) / max(1, st.median(tot)), st.median(e10), st.median(neg)))
        o.append('- 全期の和（月あたりの和を四半期の行で足した量・種の中央）：請求 %.4g・U_up %.4g（請求の %.2f%%）・U_est の正の部 %.4g（請求の %.2f%%）' % (
            st.median(sumC), st.median(sumUup), 100.0 * st.median(sumUup) / st.median(sumC), st.median(sumU), 100.0 * st.median(sumU) / st.median(sumC)))
        # (b) 国ごとの U_est の正の件数（種の和の上位）
        cnt = {}
        for s in seeds:
            for (n, y, q, c, pd, R, dv, rt, cp, eb) in A[s]:
                if dv is not None and c * dv - pd > 0.01 * c: cnt[n] = cnt.get(n, 0) + 1
        top = sorted(cnt.items(), key=lambda t: -t[1])[:12]
        o.append('- U_est が請求の 1%% 超の国×四半期（%d 種の和の上位 12）：%s' % (len(seeds), '・'.join('%s %d' % t for t in top)))
        # (c) 列強 9 か国の φ（期間ごとの平均の種の中央）
        nations = ['イギリス', '清', 'プロイセン', 'ロシア', 'フランス', 'オーストリア', '日本', 'オスマン', 'アメリカ']
        o.append('- 年に手放す割合（%/年・期間の国×四半期の平均の種の中央）φ_up／φ_est／φn_est（純の値段）・定額の国は請求の新÷旧（landClaim÷landClaimProp）')
        for n in nations:
            parts = []
            for (pl, y0, y1) in PER:
                vu = []; ve = []; vn = []; rr = []
                for s in seeds:
                    au = []; ae = []; an = []; ar = []
                    for (nn, y, q, c, pd, R, dv, rt, cp, eb) in A[s]:
                        if nn != n or y < y0 or y > y1 or rt is None or R is None or not (R > 0) or dv is None: continue
                        au.append((c - pd) * rt / R)
                        ue = max(0.0, c * dv - pd)
                        ae.append(ue * rt / R)
                        if R - pd > 0: an.append(ue * rt / (R - pd))
                        if cp and cp > 0: ar.append(c / cp)
                    if au: vu.append(sum(au) / len(au)); ve.append(sum(ae) / len(ae))
                    if an: vn.append(sum(an) / len(an))
                    if ar: rr.append(sum(ar) / len(ar))
                if not vu: parts.append('%s —' % pl); continue
                s_ = '%s %.3f／%.3f／%.3f' % (pl, 100 * st.median(vu), 100 * st.median(ve), 100 * st.median(vn) if vn else float('nan'))
                if rr and abs(st.median(rr) - 1) > 1e-9: s_ += '（新÷旧 %.3f）' % st.median(rr)
                parts.append(s_)
            o.append('  - %s：%s' % (n, '・'.join(parts)))
    # (d) 英・清の未払いの推定の比：M÷N1・M÷N2 の幅の読み（全期の U_est の和）
    o.append('')
    o.append('## 英・清・普の全期の U_est の和（種の中央・月あたりを四半期の行で足した量）と請求の比')
    for n in ['イギリス', '清', 'プロイセン', '日本', 'フランス']:
        row = []
        for lab, A in keep.items():
            vs = []; cs = []
            for s in sorted(A):
                su = sc = 0.0
                for (nn, y, q, c, pd, R, dv, rt, cp, eb) in A[s]:
                    if nn != n or dv is None: continue
                    su += max(0.0, c * dv - pd); sc += c
                if sc > 0: vs.append(su); cs.append(sc)
            if vs: row.append('%s %.4g（請求の %.2f%%）' % (lab.split('（')[0], st.median(vs), 100 * st.median(vs) / st.median(cs)))
        o.append('- %s：%s' % (n, '・'.join(row)))
    # (e) 定額の国（英・清）で定額の請求が旧の規則の請求を上回る四半期（凶作・デフレの月＝A-2 が払えない分を立てうる所）の件数と、上回る分の和
    o.append('')
    o.append('## 定額の国の〈新÷旧＞1〉の国×四半期（種の中央）・上回る分（claim−claimProp）の和÷全期の請求・paid÷(claim×届き) の分位 10/50/90%')
    for n in ['イギリス', '清']:
        row = []
        for lab, A in keep.items():
            ks = []; ex = []; qs = []
            for s in sorted(A):
                k = 0; e = 0.0; sc = 0.0
                for (nn, y, q, c, pd, R, dv, rt, cp, eb) in A[s]:
                    if nn != n: continue
                    sc += c
                    if cp and c > cp: k += 1; e += c - cp
                    if dv and c * dv > 0: qs.append(pd / (c * dv))
                ks.append(k); ex.append(e / sc if sc > 0 else 0.0)
            qs.sort()
            qv = [qs[int(len(qs) * t)] for t in (0.1, 0.5, 0.9)] if qs else [float('nan')] * 3
            row.append('%s %d 件（%.2f%%）・分位 %.3f/%.3f/%.3f' % (lab.split('（')[0], st.median(ks), 100 * st.median(ex), *qv))
        o.append('- %s：%s' % (n, '・'.join(row)))
    with open(outp, 'wb') as f:
        f.write(('\n'.join(o) + '\n').encode('utf-8'))

main()
