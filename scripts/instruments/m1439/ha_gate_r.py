# (1439) 合流：c154ha/ha_gate.py の写し（種の起点 SB だけ足した・門は同じ）
# (1436) c154ha 腕 B の門と記録の読み手（生の region_s・migcap_s・popclose_s を読むだけ・エンジンと係数を読まない）。
#   使い方: python ha_gate.py <腕の ins> <基線の ins> [種の数=10]
# 門（作る物の大きさの下限＝桁の線・★(1425)）：種ごとに国内の移民の和（region_s の migrationInKPerQuarter の全ての州×四半期の和）が基線の同じ種の 1/10 以上。
# 記録：種ごとの上限が律速した行（migcap_s の domPlanNewKPerMonth＞domCapKPerMonth の州×四半期）・西部の実÷上限（十年ごと）・
#   東部（アメリカの西部以外の州）の 農村÷耕地 の峰・見張りの州（フォートウィリアム・セントポール・ロンドン・マンチェスター・ニューヨーク・西部）の人口の峰・底・最後。
import sys, collections, os
SB = int(os.environ.get("SB", "1"))   # (1439) 合流の写し：種の起点（S11 は 11）
arm = sys.argv[1].rstrip('/') + '/'; base = sys.argv[2].rstrip('/') + '/'; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
WATCH = [('イギリス', 'フォートウィリアム'), ('アメリカ', 'セントポール'), ('イギリス', 'ロンドン'), ('イギリス', 'マンチェスター'), ('アメリカ', 'ニューヨーク'), ('アメリカ', '西部')]

def rows(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            if len(a) == len(h): yield ix, a

def region_read(d, s):
    mig = 0.0; east = collections.defaultdict(float); watch = collections.defaultdict(list); bad = 0
    for ix, a in rows(d + f'region_s{s}.tsv'):
        try:
            mi = float(a[ix['migrationInKPerQuarter']]); y = int(a[ix['year']])
            nat = a[ix['nation']]; reg = a[ix['region']]; pop = float(a[ix['populationK']]); ru = float(a[ix['ruralPopulationK']]); ar = float(a[ix['arableLand']])
        except ValueError: bad += 1; continue
        if mi == mi: mig += mi
        else: bad += 1
        if nat == 'アメリカ' and reg != '西部' and 0 < ar < 1e8: east[reg] = max(east[reg], ru / ar)
        if (nat, reg) in WATCH: watch[(nat, reg)].append((y, pop))
    return mig, east, watch, bad

def migcap_read(d, s):
    bind = 0; rowsN = 0; west = collections.defaultdict(lambda: [0.0, 0.0])
    try:
        for ix, a in rows(d + f'migcap_s{s}.tsv'):
            try: plan = float(a[ix['domPlanNewKPerMonth']]); cap = float(a[ix['domCapKPerMonth']]); y = int(a[ix['year']])
            except ValueError: continue
            if not (plan > 0): continue
            rowsN += 1; bind += plan > cap
            if a[ix['nation']] == 'アメリカ' and a[ix['region']] == '西部':
                w = west[y // 10 * 10]; w[0] += min(plan, cap); w[1] += cap
    except FileNotFoundError: return None
    return bind, rowsN, west

fail = 0
print('== 門（下限）：種 腕の国内の移民の和 基線の和 腕÷基線 判定（≧0.1）')
info = {}
for s in range(SB, SB + n):
    ma, ea, wa, ba = region_read(arm, s); mb, eb, wb, bb = region_read(base, s)
    r = ma / mb if mb > 0 else float('nan'); ok = r >= 0.1
    fail += not ok
    print(s, round(ma), round(mb), round(r, 3), 'OK' if ok else '★落ちた', f'非数の行 腕{ba} 基線{bb}')
    info[s] = (ea, eb, wa, wb)
print('門の判定:', '通った' if fail == 0 else f'落ちた {fail} 種')
print('== 記録：上限が律速した行（州×四半期・計画＞上限）：種 腕 律速/計画のある行 基線 律速/計画のある行')
for s in range(SB, SB + n):
    xa = migcap_read(arm, s); xb = migcap_read(base, s)
    print(s, f'{xa[0]}/{xa[1]}' if xa else 'na', f'{xb[0]}/{xb[1]}' if xb else 'na')
    if s == 1 and xa and xb:
        print('  種1 西部の上限の量の和（十年ごと：腕の実÷上限・腕の上限・基線の実÷上限・基線の上限）')
        for dec in sorted(set(xa[2]) | set(xb[2])):
            A = xa[2].get(dec, [0, 0]); B = xb[2].get(dec, [0, 0])
            print('  ', dec, round(A[0] / A[1], 3) if A[1] > 0 else 'na', round(A[1], 1), round(B[0] / B[1], 3) if B[1] > 0 else 'na', round(B[1], 1))
print('== 記録：東部（アメリカの西部以外）の 農村÷耕地 の峰：種 州 腕 基線')
for s in range(SB, SB + n):
    ea, eb = info[s][0], info[s][1]
    print(s, ' '.join(f'{k}:{ea.get(k, 0):.2f}/{eb.get(k, 0):.2f}' for k in sorted(set(ea) | set(eb))))
print('== 記録：見張りの州の人口（千人）：種 国 州 腕〔峰(年)・底÷峰・最後〕 基線〔同じ〕')
def summ(v):
    if not v: return 'na'
    py, pk = max(v, key=lambda t: t[1]); after = [p for y, p in v if y >= py]; return f'{pk:.1f}({py})・{min(after) / pk if pk > 0 else 0:.3f}・{v[-1][1]:.1f}'
for s in range(SB, SB + n):
    wa, wb = info[s][2], info[s][3]
    for k in WATCH: print(s, *k, summ(wa.get(k, [])), '|', summ(wb.get(k, [])))
sys.exit(1 if fail else 0)
