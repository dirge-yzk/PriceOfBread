#!/usr/bin/env python
"""(781) c8ii の数え（事前登録 m781_20261102_prereg_ii.txt の [4]）。計器つき 10 種の生（inheritintent_s・inheritpar_s）から：
① 再併合の行の数 ② 工事中で継いだ行の parallelStarts≧1 の件数と割合 ③ 比べる側との比（率）④ stateTargetPrereg と stateTargetUsed が違う行とその parallelStarts。
使い方：python -I measurements/m781_20261102_count_ii.py <生のフォルダ（inheritintent_s*.tsv の在る所）> [<ラベル>]"""
import csv, glob, os, re, sys
from collections import Counter, defaultdict


def read(p):
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        h = next(rd, [])
        return h, [dict(zip(h, r)) for r in rd]


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    d = sys.argv[1]; label = sys.argv[2] if len(sys.argv) > 2 else os.path.basename(d.rstrip('/\\'))
    seeds = sorted(int(re.search(r'_s(\d+)\.tsv$', p).group(1)) for p in glob.glob(os.path.join(d, 'inheritintent_s*.tsv')))
    E = {}; PAR = {}
    for s in seeds:
        h, rows = read(os.path.join(d, f'inheritintent_s{s}.tsv'))
        E[s] = [r for r in rows if r['scope'] == 'E']
        pp = os.path.join(d, f'inheritpar_s{s}.tsv')
        PAR[s] = read(pp)[1] if os.path.exists(pp) else None
    print(f'# {label}  種 {len(seeds)}：{seeds}')
    # ① 再併合の行
    how = Counter(); how_seed = defaultdict(Counter)
    for s in seeds:
        for r in E[s]:
            how[r['how']] += 1; how_seed[r['how']][s] += 1
    print('① E の行（移しの事象の生産者ごと）の種類別：' + '・'.join(f'{k}={v}' for k, v in sorted(how.items())))
    ret = how.get('Returned', 0)
    print(f'   再併合の行 {ret} 行（行のある種 {len(how_seed.get("Returned", {}))}／{len(seeds)}）  種ごと：{dict(sorted(how_seed.get("Returned", {}).items()))}')
    # 結合
    key = lambda q, how_, fr, reg, good, old, built: (q, how_, fr, reg, good, old, built)
    ipar = {}
    for s in seeds:
        if PAR[s] is None:
            continue
        for r in PAR[s]:
            ipar.setdefault(s, {}).setdefault(key(r['transferQuarter'], r['how'], r['fromNation'], r['region'], r['producer'], r['stateTargetOld'], r['builtCount']), []).append(r)
    allpar = [r for s in seeds if PAR[s] for r in PAR[s]]
    nE = sum(len(E[s]) for s in seeds)
    print(f'   inheritpar_s の行 {len(allpar)}（E の行 {nE}。差は同じ四半期の再移しなどで窓を持たない行・または E が 0 行の種）')
    # ② 工事中で継いだ行
    ip = [r for r in allpar if r['inProgressAtTransfer'] == '1']
    n_ip = len(ip)
    par_ge1 = [r for r in ip if int(r['parallelStarts']) >= 1]
    comp = [r for r in ip if int(r['completedQuarter']) >= 0]
    cens = [r for r in ip if int(r['completedQuarter']) < 0]
    print(f'② 工事中で継いだ行 {n_ip}（E の行の {100 * n_ip / max(1, nE):.1f}%）：完成 {len(comp)}・打ち切り（試合の終わりまで未完成）{len(cens)}')
    print(f'   parallelStarts≧1 の行 {len(par_ge1)}（{100 * len(par_ge1) / max(1, n_ip):.1f}%）・分布 {dict(sorted(Counter(int(r["parallelStarts"]) for r in ip).items()))}')
    for tag, rs in (('完成', comp), ('打ち切り', cens)):
        if rs:
            print(f'   {tag}：行 {len(rs)}・parallelStarts≧1 {sum(1 for r in rs if int(r["parallelStarts"]) >= 1)}・Σpar {sum(int(r["parallelStarts"]) for r in rs)}・Σ窓 {sum(int(r["windowQuarters"]) for r in rs)}')
    # ③ 率の比
    sp = sum(int(r['parallelStarts']) for r in ip); sw = sum(int(r['windowQuarters']) for r in ip)
    sb = sum(int(r['parallelStartsOwnBaseline']) for r in ip); se = sum(int(r['ownExposureRegionQuarters']) for r in ip)
    print(f'③ Σ parallelStarts={sp}・Σ windowQuarters={sw}・Σ parallelStartsOwnBaseline={sb}・Σ ownExposureRegionQuarters={se}')
    verdict = '測れない・記録'
    if sw > 0 and se > 0:
        rate_inh = sp / sw
        rate_own = (sb if sb > 0 else 0.5) / se
        ratio = rate_inh / rate_own if rate_own > 0 else float('inf')
        print(f'   率_継ぎ={rate_inh:.4f}（件/窓の四半期）・率_自国={rate_own:.4f}（件/州四半期{"・基準 0 件なので 0.5 件を置いた" if sb == 0 else ""}）・比={ratio:.2f}')
        if sp >= 10 and ratio >= 10:
            verdict = '次の波に〈事前登録の形 (a)＝工事中は units＋1〉の挙動の線を推奨'
        elif sp >= 10:
            verdict = '今の形のまま・記録'
        else:
            verdict = '測れない・記録（Σ parallelStarts＜10）'
    else:
        print('   分母が 0（窓の標本が無い）＝比は測れない')
    # 参考：窓を持つ行のうち窓の標本が 1 以上の行だけの率
    ip1 = [r for r in ip if int(r['windowQuarters']) >= 1]
    if ip1:
        print(f'   （参考）窓の標本が 1 以上の行 {len(ip1)}：Σpar={sum(int(r["parallelStarts"]) for r in ip1)}・Σ窓={sum(int(r["windowQuarters"]) for r in ip1)}・parallelStarts≧1 の行 {sum(1 for r in ip1 if int(r["parallelStarts"]) >= 1)}')
    if ip:   # （参考）窓の長さの分布：長く止まった継いだ工事（窓が 40 四半期以上）が分母の多くを占めるかを見る
        ws = sorted(int(r['windowQuarters']) for r in ip)
        long_ = [r for r in ip if int(r['windowQuarters']) >= 40]
        short = [r for r in ip if int(r['windowQuarters']) < 40]
        print(f'   （参考）窓の長さ：中央値 {ws[len(ws) // 2]}・最大 {ws[-1]}・窓が 40 四半期以上の行 {len(long_)}（Σ窓 {sum(int(r["windowQuarters"]) for r in long_)}・Σpar {sum(int(r["parallelStarts"]) for r in long_)}）'
              f'・40 未満の行 {len(short)}（Σ窓 {sum(int(r["windowQuarters"]) for r in short)}・Σpar {sum(int(r["parallelStarts"]) for r in short)}・Σ自国の基準 {sum(int(r["parallelStartsOwnBaseline"]) for r in short)}・Σ自国の曝露 {sum(int(r["ownExposureRegionQuarters"]) for r in short)}）')
    # ④ prereg と used
    diff = []
    for s in seeds:
        for r in E[s]:
            if 'stateTargetPrereg' not in r:
                continue
            if r['stateTargetPrereg'] != r['stateTargetUsed']:
                diff.append((s, r))
    ipE = sum(1 for s in seeds for r in E[s] if r.get('inProgressAtTransfer') == '1')
    print(f'④ E の行 {nE}・うち工事中 {ipE}・stateTargetPrereg≠stateTargetUsed の行 {len(diff)}（{100 * len(diff) / max(1, nE):.1f}%）  うち how：{dict(Counter(r["how"] for _, r in diff))}')
    tot = 0; n_ge1 = 0; missing = 0
    for s, r in diff:
        k = key(r['quarter'], r['how'], r['fromNation'], r['region'], r['good'], r['stateTargetOld'], r['builtCount'])
        ms = ipar.get(s, {}).get(k)
        if not ms:
            missing += 1; continue
        m = ms[0]
        v = int(m['parallelStarts'])
        tot += v; n_ge1 += 1 if v >= 1 else 0
    print(f'   その行の parallelStarts：Σ={tot}・≧1 の行 {n_ge1}・結合できなかった行 {missing}')
    print(f'判定の推奨：{verdict}')


main()
