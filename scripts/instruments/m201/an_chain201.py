"""(201) 0g 財政の正のフィードバックの鎖の読み手（読むだけ）。
python an_chain201.py <out_dir（c201・g186・f181 を含む）> [タグ] [--seeds N]
鎖の段（README §1-8「正のフィードバックの鎖は各段を計器で読む」）：
  ① 入口＝開幕の予算の外の支出（国庫の台帳 その他の支出）と開幕の切り下げ（金本位の中の切り下げ＝兌換＜1 の隙）→ 切り下げの記憶 broken
  ② 兌換停止（Fiscal の強制離脱・g186 の S 行）＝引き金（財政不足 r＜0.7／カバー率割れ）・交戦・その瞬間の broken と兌換
  ③ 兌換の割引が消える＝兌換の低下・国債の金利と平均クーポン・利払い÷歳入（c201 の C 行・10年の窓）
  ④ 借入の門が閉じる（利払い÷歳入≧0.55）→ r＜1 の張り付き → 不履行（f181 の D 行＝不履行の瞬間・直前8か月の r の平均）
  ⑤ 戻れない（broken の間は金本位へ戻れない＝通貨改革まで）＝金属本位の法のまま金本位でない期間
各国（英・仏・米・露・オスマン＋普・墺・日・西・清）について段ごとの件数と段から段へ進んだ割合を出す。"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]
maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]; TAG = args[1] if len(args) > 1 else D
NATS = ['イギリス', 'フランス', 'アメリカ', 'ロシア', 'オスマン', 'プロイセン', 'オーストリア', '日本', 'スペイン', '清']


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def med(v):
    v = [x for x in v if x == x]
    return st.median(v) if v else float('nan')


C = collections.defaultdict(list)     # (seed, name) -> rows (q, yr, dict)
S = collections.defaultdict(list)     # (seed, name) -> [(yr, dict)]
DEF = collections.defaultdict(list)   # (seed, name) -> [(yr, dict)]
seeds = []
for f in sorted(glob.glob(os.path.join(D, 'c201', 'c*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    seeds.append(sd)
    hdr = None
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] == '#C': hdr = t[5:]; continue
        if t[0] != 'C': continue
        d = {k: fl(v) for k, v in zip(hdr, t[5:])}
        C[(sd, t[4])].append((int(t[1]), int(t[2]), d))
    g = os.path.join(D, 'g186', f'g{sd}.txt')
    if os.path.exists(g):
        hs = None
        for ln in open(g, encoding='utf-8'):
            t = ln.rstrip('\n').split('|')
            if t[0] == '#S': hs = t[1:]; continue
            if t[0] == 'S' and hs:
                d = dict(zip(hs, t[1:]))
                S[(sd, d['name'])].append((int(d['yr']), {k: fl(v) for k, v in d.items() if k not in ('name',)}))
    ff = os.path.join(D, 'f181', f'f{sd}.txt')
    if os.path.exists(ff):
        hd = None
        for ln in open(ff, encoding='utf-8'):
            t = ln.rstrip('\n').split('|')
            if t[0] == '#D': hd = t[1:]; continue
            if t[0] == 'D' and hd:
                d = dict(zip(hd, t[1:]))
                DEF[(sd, d['name'])].append((int(d['yr']), {k: fl(v) for k, v in d.items() if k not in ('name',)}))

print(f'######## {TAG}（種 {len(seeds)}）＝財政の鎖（入口→兌換停止→割引の消失→借入の門→不履行・戻れない期間）')
if not seeds:
    print('# c201 の行が無い'); sys.exit(0)

# ① 入口（開幕の4四半期）
print('## ① 入口＝開幕の4四半期（1783〜84・種の中央）：その他の支出÷歳入〔予算の外の支出〕・切り下げ÷年GDP・最低の国庫÷年GDP・4四半期目の broken の種の数・金本位の種の数')
for n in NATS:
    oo, dv, tmin, brk, gold, k = [], [], [], 0, 0, 0
    for sd in seeds:
        rows = [r for r in C[(sd, n)] if r[0] < 4]
        if not rows: continue
        k += 1
        rev = sum(r[2]['dRevenue'] for r in rows); ag = rows[-1][2]['annNomGdp']
        oo.append(-sum(r[2]['dOtherOut'] for r in rows) / rev if rev > 0 else float('nan'))
        dv.append(sum(r[2]['dDevalue'] for r in rows) / ag if ag > 0 else float('nan'))
        tmin.append(min(r[2]['treasury'] for r in rows) / ag if ag > 0 else float('nan'))
        brk += 1 if rows[-1][2]['broken'] > 0.5 else 0
        gold += 1 if rows[-1][2]['onGold'] > 0.5 else 0
    if k: print(f'{n} | その他の支出÷歳入 {med(oo):.2f} | 切り下げ÷年GDP {med(dv):.4f} | 最低の国庫÷年GDP {med(tmin):+.3f} | broken {brk}/{k} | 金本位 {gold}/{k}')

# ② 兌換停止
print('## ② 兌換停止（g186 の S 行・10種の和）＝回数・引き金（財政不足／カバー率割れ）・交戦・停止の瞬間に broken・兌換の中央・年代')
for n in NATS:
    ev = [(sd, y, d) for sd in seeds for (y, d) in S[(sd, n)]]
    if not ev: print(f'{n} | 0 回'); continue
    dec = collections.Counter((y // 10) * 10 for _, y, _ in ev)
    print(f'{n} | {len(ev)} 回（種あたり {len(ev) / len(seeds):.1f}） | 財政不足 {sum(1 for *_, d in ev if d["fundTrip"] > 0.5)}・カバー率割れ {sum(1 for *_, d in ev if d["coverTrip"] > 0.5)} | 交戦 {sum(1 for *_, d in ev if d["atWar"] > 0.5)} | broken {sum(1 for *_, d in ev if d["broken"] > 0.5)} | 兌換の中央 {med([d["conv"] for *_, d in ev]):.2f} | ' + ' '.join(f'{k}s {v}' for k, v in sorted(dec.items())))

# ④ 不履行と、兌換停止からの道筋
print('## ④ 不履行（f181 の D 行・10種の和）＝回数・年・直前8か月の r の平均（0.99 以上の割合＝ほぼ払えていた月の積み重ね）・兌換・その前10年に兌換停止があったか')
for n in NATS:
    ev = [(sd, y, d) for sd in seeds for (y, d) in DEF[(sd, n)]]
    if not ev: print(f'{n} | 0 件'); continue
    pre = sum(1 for sd, y, _ in ev if any(y - 10 <= ys <= y for ys, _ in S[(sd, n)]))
    hi = sum(1 for *_, d in ev if d.get('avgR', 0) >= 0.99)
    yrs = sorted(y for _, y, _ in ev)
    print(f'{n} | {len(ev)} 件（種あたり {len(ev) / len(seeds):.1f}） | r の平均 ≥0.99 が {hi} | 兌換の中央 {med([d["conv"] for *_, d in ev]):.2f} | 前10年に兌換停止 {pre} | 年 ' + ' '.join(str(y) for y in yrs))
allS = sum(len(v) for k, v in S.items()); allD = sum(len(v) for k, v in DEF.items())
allDhi = sum(1 for v in DEF.values() for _, d in v if d.get('avgR', 0) >= 0.99)
allDpre = sum(1 for (sd, n), v in DEF.items() for y, _ in v if any(y - 10 <= ys <= y for ys, _ in S[(sd, n)]))
print(f'## 全ての国：兌換停止 {allS} 回・不履行 {allD} 件（r の平均 ≥0.99 が {allDhi}・前10年に兌換停止 {allDpre}）＝段②→④へ進んだ割合（不履行のうち前に停止）{allDpre / allD if allD else float("nan"):.2f}')
# 兌換停止の後10年に不履行へ進んだ割合
nsus = 0; ndef = 0
for (sd, n), v in S.items():
    for ys, _ in v:
        nsus += 1
        if any(ys <= yd <= ys + 10 for yd, _ in DEF[(sd, n)]): ndef += 1
print(f'## 兌換停止の後10年のうちに不履行へ進んだ割合＝{ndef}/{nsus}')

# ③⑤ 10年の窓（種の中央）
print('## ③⑤ 10年の窓（種ごとの窓の平均→種の中央）＝金本位の割合・兌換・broken の割合・累積の切り下げ÷年GDP・国債の金利・平均クーポン・利払い÷歳入・r・国債÷年GDP（窓の終わり）・国庫÷年GDP（窓の平均）')
for n in NATS:
    print(f'### {n}')
    print('窓 | 金本位 | 兌換 | broken | 累積の切り下げ÷年GDP | 国債の金利 | クーポン | 利払い÷歳入 | r | 国債÷年GDP | 国庫÷年GDP | 張り付き(四半期末の計数>0 の割合)')
    for w0 in range(1783, 1950, 10):
        w1 = w0 + 9
        per = collections.defaultdict(list)
        for sd in seeds:
            rows = [r[2] for r in C[(sd, n)] if w0 <= r[1] <= w1]
            if not rows: continue
            for key in ('onGold', 'conv', 'broken', 'cumDevalAnn', 'bondRate', 'coupon', 'intRev', 'r'):
                vals = [x[key] for x in rows if x[key] == x[key]]
                if vals: per[key].append(sum(vals) / len(vals))
            last = rows[-1]
            if last['annNomGdp'] > 0: per['debt'].append(last['debt'] / last['annNomGdp'])
            tg = [x['treasury'] / x['annNomGdp'] for x in rows if x['annNomGdp'] > 0]
            if tg: per['tre'].append(sum(tg) / len(tg))
            per['ins'].append(sum(1 for x in rows if x['insolv'] > 0) / len(rows))
        if not per: continue
        print(f'{w0}-{w1} | {med(per["onGold"]):.2f} | {med(per["conv"]):.2f} | {med(per["broken"]):.2f} | {med(per["cumDevalAnn"]):.3f} | {med(per["bondRate"]):.3f} | {med(per["coupon"]):.3f} | {med(per["intRev"]):.2f} | {med(per["r"]):.3f} | {med(per["debt"]):.3f} | {med(per["tre"]):.3f} | {med(per["ins"]):.2f}')
