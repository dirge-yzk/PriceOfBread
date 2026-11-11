# (1024) c96kf 記録の列を ident の生（計器つき 10 種の <出力>/ins）から数える（読むだけ・判定ではない）。
# 使い方：python -I rec.py <ins フォルダ> [充填の秒 39.8] > 出力
# 出す物：
#  R1 fillsInvalid÷fills（全体・格×払い手別）と、C2 で削れる件数の上限を秒に直した値（秒＝充填の秒×invalid÷fills・充填の秒は .cost の推定の充填の項）
#  R2 格×払い手ごとの充填・行・最良が替わった充填
#  R3 pxstep_s の枝ごとの件数と日次の日数（全種・全年の和）
#  R4 K3：商人の行（merchantK＞1e-9）のうち、①′ を商人に写した式（労働者の 1 人あたり所得÷（商人の 1 人あたり稼ぎ×転職プレミアム）＞1）に当たる行の、資産の門（merchFallGateIn）の内と外
#     と、道の量の和・エンジンが落とした行（merchFallOut＞0）と計器の門の一致
import sys, os, glob, csv, collections

d = sys.argv[1]
fill_sec = float(sys.argv[2]) if len(sys.argv) > 2 else 39.8
csv.field_size_limit(1 << 30)

def rows(path):
    with open(path, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            yield ix, r

# R1 R2
tot = collections.defaultdict(lambda: [0, 0, 0, 0])   # (tier,payer) -> rows, fills, invalid, toBest
seeds = 0
for p in sorted(glob.glob(os.path.join(d, 'cscx_s*.tsv'))):
    seeds += 1
    for ix, r in rows(p):
        k = (r[ix['tierCode']], int(r[ix['payerCode']]))
        a = tot[k]
        a[0] += int(r[ix['rowsCount']]); a[1] += int(r[ix['fillsCount']]); a[2] += int(r[ix['fillsInvalidCount']]); a[3] += int(r[ix['fillsToBestCount']])
F = sum(a[1] for a in tot.values()); I = sum(a[2] for a in tot.values()); B = sum(a[3] for a in tot.values()); R = sum(a[0] for a in tot.values())
print(f'R1 seeds={seeds} rows={R} fills={F} invalid={I} toBest={B}')
if F:
    print(f'R1 invalid/fills={I / F:.6f}  toBest/fills={B / F:.6f}  C2 の上限（秒）＝{fill_sec}×invalid/fills＝{fill_sec * I / F:.3f}')
print('R2 tier payer rows fills invalid toBest invalid/fills')
for k in sorted(tot):
    a = tot[k]
    print(f'R2 {k[0]} {k[1]} {a[0]} {a[1]} {a[2]} {a[3]} {a[2] / a[1] if a[1] else float("nan"):.6f}')

# R3
px = collections.defaultdict(lambda: [0, 0])
for p in sorted(glob.glob(os.path.join(d, 'pxstep_s*.tsv'))):
    for ix, r in rows(p):
        a = px[r[ix['branch']]]; a[0] += int(r[ix['goodsCount']]); a[1] += int(r[ix['dailyRuns']])
print('R3 branch goodsCount dailyRuns')
for b, a in sorted(px.items(), key=lambda x: -x[1][0]):
    print(f'R3 {b} {a[0]} {a[1]}')
steps = sum(a[0] for b, a in px.items() if b not in ('dormant', 'dead'))
print(f'R3 歩に入った財の和（dormant・dead を除く）={steps}  other={px["other"][0] if "other" in px else 0}')

# R4
def fnum(s):
    return float('nan') if s == 'nan' else float(s)
cnt = collections.Counter(); route = [0.0] * 5
for p in sorted(glob.glob(os.path.join(d, 'merch_s*.tsv'))):
    for ix, r in rows(p):
        mk = fnum(r[ix['merchantK']])
        gate = r[ix['merchFallGateIn']]
        fo = fnum(r[ix['merchFallOut']])
        for i, c in enumerate(['merchPettyIn', 'artisanPettyIn', 'merchCapRiseOut', 'merchFallOut', 'artisanFallOut']):
            v = fnum(r[ix[c]])
            if v == v: route[i] += v
        cnt['rows'] += 1
        if gate != 'nan': cnt['gateEvaluated'] += 1
        if fo > 1e-9:
            cnt['engineFell'] += 1
            if gate != '1': cnt['engineFellGateNot1'] += 1
        if mk > 1e-9:
            wi = fnum(r[ix['workerIncomePerCap']]); me = fnum(r[ix['merchEarnedPerCap']]); pr = fnum(r[ix['promotionPremium']])
            if wi == wi and me == me and pr == pr:
                hit = wi / max(1e-9, me * pr) > 1.0
                key = 'hit' if hit else 'nohit'
                cnt[key] += 1
                cnt[f'{key}_gate_{gate}'] += 1
print('R4', dict(cnt))
print('R4 道の量の和（merchPettyIn artisanPettyIn merchCapRiseOut merchFallOut artisanFallOut）', [round(x, 3) for x in route])
h = cnt['hit']
if h:
    print(f'R4 ①′ を商人に写した式に当たる行={h}：資産の門の内（1）={cnt["hit_gate_1"]} ({cnt["hit_gate_1"] / h:.4f})・外（0）={cnt["hit_gate_0"]} ({cnt["hit_gate_0"] / h:.4f})・未評価（nan）={cnt["hit_gate_nan"]}')
