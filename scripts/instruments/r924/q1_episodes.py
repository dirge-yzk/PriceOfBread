# (924) 問い1・3：政体の札ごとの四半期と、専制（polityCode 1）の挿話の入口・出口・項の和
# 使い方: python -X utf8 -I q1_episodes.py <out.txt>
import csv, sys, os, math
csv.field_size_limit(10**8)
ROOT = 'D:/PoB_work/keep_919'
ARMS = [('base', 'base_ins', range(1, 11)), ('n1', 'n1', range(1, 11)), ('n2', 'n2', range(1, 11)), ('a11', 'a11', range(11, 21))]
POL = {0: '絶対', 1: '専制', 2: '幕府', 3: '立憲', 4: '共和', 5: 'ブル独', 6: 'プロ独', 7: '軍政', 8: '無政府'}
CROWN = {0: '無', 1: '絶対', 2: '専制', 3: '幕府'}
NATS = ['イギリス', '清', 'ロシア', 'プロイセン', '日本', 'フランス', 'オーストリア', 'オスマン']
TERMS = ['summonTermShare', 'rollbackTermShare', 'term1Share', 'term2Share', 'term2pShare', 'residualShare']

def f(x):
    try:
        v = float(x)
        return v
    except Exception:
        return math.nan

def ci(x):
    v = f(x)
    return -1 if math.isnan(v) else int(v)

def nz(v):
    return 0.0 if math.isnan(v) else v

out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
def P(*a):
    print(*a, file=out)

counts = {}   # (arm, nat) -> {pol: n}
desp_seed = {}  # (arm,nat) -> {seed: n}
for arm, sub, seeds in ARMS:
    for s in seeds:
        p = os.path.join(ROOT, sub, f'crownpc_s{s}.tsv')
        if not os.path.exists(p):
            P('欠け', p); continue
        rows = {}
        with open(p, encoding='utf-8', newline='') as fh:
            r = csv.DictReader(fh, delimiter='\t')
            for row in r:
                n = row['nation']
                if n in NATS:
                    rows.setdefault(n, []).append(row)
        for n, rs in rows.items():
            d = counts.setdefault((arm, n), {})
            for row in rs:
                pc = ci(row['polityCode'])
                d[pc] = d.get(pc, 0) + 1
            desp_seed.setdefault((arm, n), {})[s] = sum(1 for row in rs if ci(row['polityCode']) == 1)
            if n != 'イギリス':
                continue
            # 挿話
            i = 0
            while i < len(rs):
                if ci(rs[i]['polityCode']) != 1:
                    i += 1; continue
                j = i
                while j + 1 < len(rs) and ci(rs[j + 1]['polityCode']) == 1:
                    j += 1
                e = rs[i]; prev = rs[i - 1] if i > 0 else None
                sums = {t: sum(nz(f(rs[k][t])) for k in range(i, j + 1)) for t in TERMS}
                ext = rs[j + 1] if j + 1 < len(rs) else None
                P(f'{arm}\ts{s}\t入口{e["year"]}q{e["quarter"]}\t長さ{j - i + 1}\t'
                  f'前の札={POL.get(ci(e["polityPrevCode"]), e["polityPrevCode"])}\t前の王冠={CROWN.get(ci(e["crownPrevCode"]), e["crownPrevCode"])}\t'
                  f'前の奪取={e["seizurePrevCode"]}\t王冠={CROWN.get(ci(e["crownCode"]))}\t'
                  f'pcPrev={f(e["pcPrevShare"]):.4f}\tpcNow={f(e["pcNowShare"]):.4f}\t'
                  f'政変印={e["regimeEventFlag"]}\t再ラベル印={e["relabelFlag"]}\t下賜印={e["grantFlag"]}\texplain={e["explainCode"]}\t'
                  f'正統性={f(e["legitimacyIndex"]):.3f}\t'
                  f'出口={(POL.get(ci(ext["polityCode"])) + str(ext["year"])) if ext else "終わり"}\t'
                  + '\t'.join(f'{t[:-5]}Σ={sums[t]:.3f}' for t in TERMS))
                i = j + 1

P('\n== 札ごとの四半期（種の和）')
for n in NATS:
    for arm, _, _ in ARMS:
        d = counts.get((arm, n), {})
        P(n, arm, '\t'.join(f'{POL[k]}{d.get(k, 0)}' for k in sorted(POL)))
P('\n== 専制の四半期の種ごと')
for n in NATS:
    for arm, _, _ in ARMS:
        d = desp_seed.get((arm, n), {})
        P(n, arm, ' '.join(f's{k}:{v}' for k, v in sorted(d.items())), '種の数(>0)=', sum(1 for v in d.values() if v > 0))
out.close()
