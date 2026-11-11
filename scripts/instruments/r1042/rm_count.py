# (1042) c98rm 宗教と移民の段0：国の間の移民の流れを州の札で分ける（読むだけ）
# 使い方: python -X utf8 -I rm_count.py <world.json> <出力.json>
# 盤面: B=keep_1035/base_ins・N1/N2=keep_1027/n1,n2・種 1..10（事前登録 prereg.txt）
import sys, json, os, collections, multiprocessing as mp

WORLD = sys.argv[1]; OUT = sys.argv[2]
ARMS = {'B': 'D:/PoB_work/keep_1035/base_ins', 'N1': 'D:/PoB_work/keep_1027/n1', 'N2': 'D:/PoB_work/keep_1027/n2'}
SEEDS = range(1, 11)

# Diplomacy.cs:1199 CultureGroup の写し
CG = {}
for k in ["ロシア","ウクライナ","ポーランド","チェコ","スロバキア","クロアチア","セルビア","ブルガリア"]: CG[k]="スラヴ"
for k in ["ドイツ","スウェーデン","ノルウェー","デンマーク","フラマン","オランダ"]: CG[k]="ゲルマン"
for k in ["イタリア","スペイン","ポルトガル","ルーマニア","カタルーニャ","サルデーニャ","ナポリ","シチリア","フランス","コルシカ"]: CG[k]="ラテン"
for k in ["イングランド","スコットランド","アメリカ"]: CG[k]="アングロ"
for k in ["トルコ","タタール","ウイグル","アゼルバイジャン","ウズベク"]: CG[k]="テュルク"
for k in ["ペルシア","タジク","アフガン","パシュトゥーン"]: CG[k]="イラン"
for k in ["マラーター","ヒンドゥスタン","カンナダ","テルグ","パンジャブ","ネパール"]: CG[k]="インド"
for k in ["漢","満洲","蒙古","朝鮮","日本","ベトナム","チベット"]: CG[k]="東アジア"
for k in ["ビルマ","タイ","クメール","ラオ"]: CG[k]="東南アジア"
def cgroup(c): return CG.get(c, c if c else "他")
# Diplomacy.cs:1214 ReligionFamily の写し
RF = {"カトリック":"西方キリスト","プロテスタント":"西方キリスト","正教":"東方キリスト","エチオピア正教":"東方キリスト",
      "イスラム":"イスラム","シーア派":"イスラム","ザイド派":"イスラム","ヒンドゥー教":"インド系","シク教":"インド系",
      "上座部仏教":"仏教","チベット仏教":"仏教","儒仏":"東アジア","儒":"東アジア","神仏":"東アジア","土着信仰":"民俗"}
def rfam(r): return RF.get(r, "他")

def cls_c(a, b):
    if not a or not b: return 'none'
    if a == b: return 'same'
    if cgroup(a) == cgroup(b): return 'fam'
    return 'diff'
def cls_r(a, b):
    if not a or not b: return 'none'
    if a == b: return 'same'
    if rfam(a) == rfam(b): return 'fam'
    return 'diff'

def period(y):
    return 'p1' if y < 1850 else ('p2' if y < 1900 else 'p3')

def load_world():
    w = json.load(open(WORLD, encoding='utf-8'))
    city = {}; cap = {}
    for n in w['nations']:
        pv = n['provinces']
        cap[n['name']] = (pv[0].get('culture', ''), pv[0].get('religion', ''))
        for p in pv: city[p['city']] = (p.get('culture', ''), p.get('religion', ''))
    return city, cap

def run(job):
    arm, seed = job
    d = ARMS[arm]
    city, capl = load_world()
    # region_s: 札（近似を含む）・人口・因子
    lab = {}; approx = set(); nolab = set()
    R = {}   # (q, region) -> dict
    popq = collections.defaultdict(float)
    popq_lab_c = collections.defaultdict(float); popq_lab_r = collections.defaultdict(float)
    popq_grp = collections.defaultdict(float); popq_fam = collections.defaultdict(float)
    popqn = collections.defaultdict(float)
    popqn_lab_c = collections.defaultdict(float); popqn_lab_r = collections.defaultdict(float)
    popqn_grp = collections.defaultdict(float); popqn_fam = collections.defaultdict(float)
    year_of = {}
    with open(f'{d}/region_s{seed}.tsv', 'rb') as f:
        hdr = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t')
            q = int(x[ix['quarter']]); rg = x[ix['region']]; nat = x[ix['nation']]
            year_of[q] = int(x[ix['year']])
            if rg not in lab:
                if rg in city: lab[rg] = city[rg]
                elif nat in capl: lab[rg] = capl[nat]; approx.add(rg)
                else: lab[rg] = ('', ''); nolab.add(rg)
            act = x[ix['activeFlag']] == '1'
            pk = float(x[ix['populationK']])
            def g(k):
                try: return float(x[ix[k]])
                except: return float('nan')
            R[(q, rg)] = (nat, act, pk, g('opportunityIndex'), g('holdingFactor'), g('wageFactor'), g('landFactor'), g('employmentShare'),
                          g('pushFactor'), g('housingFactor'), g('foodFillShare'), g('crossBorderInKPerQuarter'), x[ix['tier']])
            if act and pk > 0:
                c, r = lab[rg]
                popq[q] += pk; popqn[(q, nat)] += pk
                if c: popq_lab_c[(q, c)] += pk; popqn_lab_c[(q, nat, c)] += pk; popq_grp[(q, cgroup(c))] += pk; popqn_grp[(q, nat, cgroup(c))] += pk
                if r: popq_lab_r[(q, r)] += pk; popqn_lab_r[(q, nat, r)] += pk; popq_fam[(q, rfam(r))] += pk; popqn_fam[(q, nat, rfam(r))] += pk
    # 国の多数派の宗教（州の札・四半期ごと）
    natrel = collections.defaultdict(lambda: collections.defaultdict(float))
    for (q, nat, r), v in popqn_lab_r.items(): natrel[(q, nat)][r] += v
    majrel = {k: max(v.items(), key=lambda t: t[1])[0] for k, v in natrel.items()}
    # monlaw: 移民の法・国教の法（全ての国・四半期）
    imm = {}; srl = {}
    with open(f'{d}/monlaw_s{seed}.tsv', 'rb') as f:
        hdr = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t')
            k = (int(x[ix['quarter']]), x[ix['nation']])
            imm[k] = int(x[ix['ImmigrationRestriction']]); srl[k] = int(x[ix['StateReligion']])
    out = collections.defaultdict(float)
    # 流れ
    src_flows = collections.defaultdict(list)   # (q, fromRegion) -> [(toRegion, toNation, moved)]
    pair_year = collections.defaultdict(float)
    inflow_q = collections.defaultdict(float)
    km_w = []   # (km, moved)
    with open(f'{d}/xmig_s{seed}.tsv', 'rb') as f:
        hdr = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t')
            q = int(x[0]); fr = x[1]; to = x[2]; fn = x[3]; tn = x[4]; mv = float(x[5]); km = float(x[6])
            if not (mv > 0): continue
            y = year_of.get(q, 1783 + q // 4); p = period(y)
            fc, frl = lab.get(fr, ('', '')); tc, trl = lab.get(to, ('', ''))
            cc = cls_c(fc, tc); rc = cls_r(frl, trl)
            for pp in ('all', p):
                out[f'tot|{pp}'] += mv
                out[f'c|{cc}|{pp}'] += mv; out[f'r|{rc}|{pp}'] += mv; out[f'cr|{cc}{rc}|{pp}'] += mv
                if fr in approx or to in approx: out[f'approx|{pp}'] += mv
                if fr in nolab or to in nolab: out[f'nolab|{pp}'] += mv
            clean = fr not in approx and to not in approx and fc and tc and frl and trl
            if clean:
                out['cl|tot'] += mv; out[f'cl|c|{cc}'] += mv; out[f'cl|r|{rc}'] += mv
            if rc == 'same' or cc == 'same': out[f'samepair|{fn}>{tn}|{"cl" if clean else "ap"}'] += mv
            src_flows[(q, fr, fn)].append((to, tn, mv))
            pair_year[(y, fn, tn)] += mv
            inflow_q[(q, to)] += mv
            km_w.append((km, mv))
            # 行き先の国教の法ごとの宗教の分類
            s = srl.get((q, tn)); im = imm.get((q, tn))
            if s is not None: out[f'srl|{s}|{rc}'] += mv
            if im is not None: out[f'imm|{im}|{rc}'] += mv; out[f'immc|{im}|{cc}'] += mv
            # 出身の国の多数派と違う宗教の州からの流れ（ユグノー型の州の札の代用）
            mr = majrel.get((q, fn))
            if frl and mr: out[f'minor|{"min" if frl != mr else "maj"}|{p}'] += mv
            # 米への流入の構成
            if tn == 'アメリカ':
                pu = 'u1' if y < 1850 else ('u2' if y < 1882 else ('u3' if y < 1924 else 'u4'))
                out[f'us|{pu}|c|{cc}'] += mv; out[f'us|{pu}|r|{rc}'] += mv; out[f'us|{pu}|tot'] += mv
                out[f'usfrom|{pu}|{fn}'] += mv
            # 行き先の因子（流量で重み）
            rr = R.get((q, to)); rs = R.get((q, fr))
            if rr and rs:
                gap = rr[3] - rs[3] * rs[4] * rs[10] - 0.5
                out[f'gapw'] += mv * gap
    # 参照 A（他国の全ての活きた州へ人口比）と参照 B（実際に受けた州の中で人口比）
    for (q, fr, fn), lst in src_flows.items():
        F = sum(m for _, _, m in lst)
        y = year_of.get(q, 1783 + q // 4); p = period(y)
        fc, frl = lab.get(fr, ('', ''))
        tot = popq[q] - popqn[(q, fn)]
        if tot > 0 and fc:
            same = popq_lab_c[(q, fc)] - popqn_lab_c[(q, fn, fc)]
            grp = popq_grp[(q, cgroup(fc))] - popqn_grp[(q, fn, cgroup(fc))]
            for pp in ('all', p):
                out[f'refA|c|same|{pp}'] += F * same / tot; out[f'refA|c|fam|{pp}'] += F * (grp - same) / tot; out[f'refA|c|base|{pp}'] += F
        if tot > 0 and frl:
            same = popq_lab_r[(q, frl)] - popqn_lab_r[(q, fn, frl)]
            fam = popq_fam[(q, rfam(frl))] - popqn_fam[(q, fn, rfam(frl))]
            for pp in ('all', p):
                out[f'refA|r|same|{pp}'] += F * same / tot; out[f'refA|r|fam|{pp}'] += F * (fam - same) / tot; out[f'refA|r|base|{pp}'] += F
        # 参照 B
        ws = [];
        for to, tn, m in lst:
            rr = R.get((q, to)); ws.append((to, rr[2] if rr else 0.0))
        wt = sum(v for _, v in ws)
        if wt > 0:
            for to, v in ws:
                tc, trl = lab.get(to, ('', ''))
                cc = cls_c(fc, tc); rc = cls_r(frl, trl)
                for pp in ('all', p):
                    out[f'refB|c|{cc}|{pp}'] += F * v / wt; out[f'refB|r|{rc}|{pp}'] += F * v / wt
            for pp in ('all', p): out[f'refB|base|{pp}'] += F
        # 参照 B の clean（出る州も行き先も札が world.json の実の札で空でない組だけ）
        if fr not in approx and fc and frl:
            cl = [(to, m) for to, tn, m in lst if to not in approx and lab.get(to, ('', ''))[0] and lab.get(to, ('', ''))[1]]
            Fc = sum(m for _, m in cl)
            wc = [(to, R[(q, to)][2] if (q, to) in R else 0.0) for to, _ in cl]; wt2 = sum(v for _, v in wc)
            if Fc > 0 and wt2 > 0:
                out['clB|base'] += Fc
                for to, v in wc:
                    tc, trl = lab[to]
                    out[f'clB|c|{cls_c(fc, tc)}'] += Fc * v / wt2; out[f'clB|r|{cls_r(frl, trl)}'] += Fc * v / wt2
    # 受入の上限が効いたか（近似）：cap≈0.012×人口×法の率×(食料×住居×土地)^2
    lawf = {0: 1.0, 1: 0.5, 2: 0.2}
    for (q, to), inf in inflow_q.items():
        rr = R.get((q, to))
        if not rr: continue
        nat = rr[0]; pk = rr[2]; land = rr[6]; hf = 1 - 2 * (1 - rr[9]); ff = rr[10]
        cl = lambda v: min(1.0, max(0.0, v)) if v == v else 0.0
        capk = 0.012 * pk * lawf.get(imm.get((q, nat), 0), 1.0) * (cl(ff) * cl(hf) * (land if land == land else 0)) ** 2
        out['cap|inflow'] += inf
        if capk > 0 and inf >= 0.95 * capk: out['cap|bind'] += inf
        out['cap|ratio_w'] += inf * (inf / capk if capk > 0 else 0)
    # 距離（流量の重みの分位）
    km_w.sort(); T = sum(m for _, m in km_w); acc = 0; qs = {}
    for km, m in km_w:
        acc += m
        for qq in (0.1, 0.5, 0.9):
            if qq not in qs and acc >= qq * T: qs[qq] = km
    for qq, v in qs.items(): out[f'km|{qq}'] = v
    # 連鎖：国の組の年の流れの割合と、それより前の累計の割合の順位相関（年ごと）
    years = sorted({y for y, _, _ in pair_year})
    cum = collections.defaultdict(float); rhos = []
    for y in years:
        cur = {(a, b): v for (yy, a, b), v in pair_year.items() if yy == y}
        keys = [k for k in cur if cum.get(k, 0) > 0]
        if len(keys) >= 8:
            def rank(vals):
                o = sorted(range(len(vals)), key=lambda i: vals[i]); r = [0] * len(vals)
                for k, i in enumerate(o): r[i] = k
                return r
            a = rank([cur[k] for k in keys]); b = rank([cum[k] for k in keys]); n = len(keys)
            ma = sum(a) / n; mb = sum(b) / n
            num = sum((a[i] - ma) * (b[i] - mb) for i in range(n)); den = (sum((v - ma) ** 2 for v in a) * sum((v - mb) ** 2 for v in b)) ** 0.5
            if den > 0: rhos.append((y, num / den, n))
        for k, v in cur.items(): cum[k] += v
    for y, r, n in rhos: out[f'rho|{period(y)}|sum'] += r; out[f'rho|{period(y)}|n'] += 1
    # 札の表（近似の州の名）
    out['n_approx'] = len(approx); out['n_nolab'] = len(nolab)
    return arm, seed, dict(out), sorted(approx), sorted(nolab)

if __name__ == '__main__':
    jobs = [(a, s) for a in ARMS for s in SEEDS]
    with mp.Pool(6) as pool:
        res = pool.map(run, jobs)
    json.dump([{'arm': a, 'seed': s, 'v': v, 'approx': ap, 'nolab': nl} for a, s, v, ap, nl in res], open(OUT, 'w', encoding='utf-8'), ensure_ascii=False)
    print('done', len(res))
