# (1133) 読む線 c110fs 問い1：外国の取り分（Rule[2]）の初出を出どころ別に数える。
# 使い方: python -X utf8 -I q1_onset.py <ins の場所> <ラベル> <出力の場所> [--rp] [--allnations]
#   --allnations：rulepower_s の nowRule2Share（全 61 国の州）で読む。無ければ rule_s の ruleForeignShare（計器の名簿 9 国だけ）。
#   --rp：rulepower_s の fForeignEngIndex（エンジンの外国の力の和）を読み、初出の四半期にエンジンの力があったかを確かめる（旗 1 の腕だけ）。
# 出どころの分け方（ソースの読み・台本は計器の行だけを読む）：
#   開幕データ  … 四半期 0 で取り分＞0 かつ world.json の州の rule[2]＞0（7 州）
#   開幕の表    … 四半期 0 で取り分＞0 かつ world.json の rule[2]＝0（旗 1 のエンジンの最初の表）
#   講和の付与  … 旗 1：四半期＞0 の初出で 1 四半期の値 ≧0.05／旗 0：四半期＞0 の全ての初出（AddForeignShare の呼び手は Diplomacy.cs:2750 の 1 つ）
#   エンジン平時… 旗 1：四半期＞0 の初出で値＜0.05・持ち主の atWarMonths＝0（②交戦の相手ではあり得ない＝③陸の隣）
#   エンジン戦時… 同じで持ち主が交戦中（②か③＝この表では分けられない）
# 跳び（jumps_）：前の四半期から 1 四半期で +0.1 以上（既に取り分のある州への講和の付与を拾う・旗 1 のエンジンの寄せは 1 四半期に 3 回×0.025 以下）。
import sys, os, json
from collections import defaultdict

ins, label, outd = sys.argv[1], sys.argv[2], sys.argv[3]
use_rp = '--rp' in sys.argv
tbl = 'rulepower' if '--allnations' in sys.argv else 'rule'
FCOL = 'nowRule2Share' if tbl == 'rulepower' else 'ruleForeignShare'
flag1 = '--flag1' in sys.argv or use_rp
if tbl == 'rulepower': label += '_all'
os.makedirs(outd, exist_ok=True)
ROOT = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore'
wd = json.load(open(ROOT + '/world.json', encoding='utf-8'))
data_foreign = {}
port = {}
for n in wd['nations']:
    for p in n['provinces']:
        r = p.get('rule') or n.get('rule')
        if r and r[2] > 0: data_foreign[p['city']] = r[2]
        port.setdefault(p['city'], bool(p.get('coastal')))
for x in json.load(open(ROOT + '/landmass.json', encoding='utf-8')):
    port.setdefault(x['name'], bool(x.get('coastal')))
TH = 0.05

def hidx(h):
    return {k: i for i, k in enumerate(h.rstrip('\n').split('\t'))}

def fnum(s):
    try: return float(s)
    except ValueError: return float('nan')

def W(path, head):
    f = open(os.path.join(outd, path), 'w', encoding='utf-8', newline='\n'); f.write(head + '\n'); return f

ons_all = W(f'onsets_{label}.tsv', 'seed\tquarter\tyear\tnation\tregion\ttier\tport\tvalue\tcount\townerAtWar\tengForceNow\tclass\tkind')
ser = W(f'series_{label}.tsv', 'seed\tquarter\tclass\tregionCount\tshareSum\tportCount\tportShareSum')
jw = W(f'jumps_{label}.tsv', 'seed\tquarter\tyear\tnation\tregion\ttier\tport\tprev\tvalue\townerAtWar\tengForceNow')
thw = W(f'thresh_{label}.tsv', 'seed\tavgCountGt0\tavgCountGe0.01\tavgCountGe0.05\tavgCountGe0.25\tjumpsGe0.1\tregionsInTable')
summ = []
for seed in range(1, 11):
    war = {}
    with open(os.path.join(ins, f'nation_s{seed}.tsv'), encoding='utf-8') as f:
        h = hidx(f.readline()); iq, inn, iw = h['quarter'], h['nation'], h['atWarMonths']
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            war[(int(c[iq]), c[inn])] = fnum(c[iw]) > 0
    eng = set()
    if use_rp:
        with open(os.path.join(ins, f'rulepower_s{seed}.tsv'), encoding='utf-8') as f:
            h = hidx(f.readline()); iq, ir, ie = h['quarter'], h['region'], h['fForeignEngIndex']
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                if fnum(c[ie]) > 0: eng.add((int(c[iq]), c[ir]))
    prev = {}; src = {}
    agg = defaultdict(lambda: [0, 0.0, 0, 0.0])
    nons = defaultdict(int); jumps = 0
    thr = defaultdict(lambda: [0, 0, 0, 0]); regs = set(); qs = set()
    with open(os.path.join(ins, f'{tbl}_s{seed}.tsv'), encoding='utf-8') as f:
        h = hidx(f.readline())
        iq, iy, inn, ir, it, iF = h['quarter'], h['year'], h['nation'], h['region'], h['tier'], h[FCOL]
        ic = h.get('foreignRulerCount')
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            q = int(c[iq]); r = c[ir]; F = fnum(c[iF]); cnt = c[ic] if ic is not None else ''
            regs.add(r); qs.add(q)
            pv = prev.get(r)
            aw = int(war.get((q, c[inn]), False)); ef = int((q, r) in eng) if use_rp else ''
            if pv is not None and pv[0] == q - 1 and F - pv[1] >= 0.1:
                jumps += 1
                jw.write('\t'.join(map(str, [seed, q, c[iy], c[inn], r, c[it], int(port.get(r, False)), pv[1], F, aw, ef])) + '\n')
            if F > 0:
                t = thr[q]; t[0] += 1; t[1] += F >= 0.01; t[2] += F >= 0.05; t[3] += F >= 0.25
            if F > 0 and (pv is None or not (pv[1] > 0)):
                if q == 0:
                    cls = '開幕データ' if r in data_foreign else '開幕の表'
                elif not flag1 or F >= TH:
                    cls = '講和の付与'
                else:
                    cls = 'エンジン戦時' if aw else 'エンジン平時'
                kind = 're' if r in src else 'first'
                src[r] = cls; nons[(cls, kind)] += 1
                ons_all.write('\t'.join(map(str, [seed, q, c[iy], c[inn], r, c[it], int(port.get(r, False)), c[iF], cnt, aw, ef, cls, kind])) + '\n')
            if F > 0:
                a = agg[(q, src.get(r, '?'))]; a[0] += 1; a[1] += F
                if port.get(r, False): a[2] += 1; a[3] += F
            prev[r] = (q, F)
    for (q, cls), a in sorted(agg.items()):
        ser.write(f'{seed}\t{q}\t{cls}\t{a[0]}\t{a[1]:.6g}\t{a[2]}\t{a[3]:.6g}\n')
    nq = max(1, len(qs))
    thw.write(f'{seed}\t' + '\t'.join(f'{sum(t[i] for t in thr.values()) / nq:.4g}' for i in range(4)) + f'\t{jumps}\t{len(regs)}\n')
    summ.append((seed, dict(nons)))
    print(seed, {k: v for k, v in sorted(nons.items())}, 'jumps', jumps, 'eng-set', len(eng), flush=True)
for f in (ons_all, ser, jw, thw): f.close()
with open(os.path.join(outd, f'onsets_summary_{label}.txt'), 'w', encoding='utf-8', newline='\n') as f:
    for seed, d in summ:
        f.write(f'{seed}\t' + '\t'.join(f'{k[0]}/{k[1]}={v}' for k, v in sorted(d.items())) + '\n')
print('done')
