# (1056) c100ic 紙の台本：imp_s と cpeace_s から、同じ月×同じ敗者の 2 本目以降の矯正を、崩壊の集合（cg）を通った物と通らない物に分けて数える。
# 使い方: python paper_ic.py <計器の出力フォルダ> [seeds=1-10]
# 新しい列（groupIdIndex・skippedByGroupFlag）がある表ではその列で読む（腕 B）。無い表（基線）では cpeace の nGrouped と shares の勝者の名で近似する。
import sys, os, collections
d = sys.argv[1]
seeds = range(1, 11) if len(sys.argv) < 3 else range(int(sys.argv[2].split('-')[0]), int(sys.argv[2].split('-')[1]) + 1)
UK = 'イギリス'
tot = collections.Counter(); per_seed = []
for s in seeds:
    pi = os.path.join(d, f'imp_s{s}.tsv'); pc = os.path.join(d, f'cpeace_s{s}.tsv')
    if not os.path.exists(pi): print(f'seed {s}: imp なし'); continue
    L = open(pi, encoding='utf-8').read().splitlines(); h = L[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
    rows = [r.split('\t') for r in L[1:] if r]
    grp = {}  # (month, loser) -> set(winners in cg)
    if os.path.exists(pc):
        C = open(pc, encoding='utf-8').read().splitlines(); ch = C[0].split('\t'); cx = {k: i for i, k in enumerate(ch)}
        for r in C[1:]:
            f = r.split('\t')
            if int(f[cx['nGrouped']]) >= 2:
                ws = set(x.split(':')[0] for x in f[cx['shares']].split('/') if x)
                grp[(f[cx['month']], f[cx['loser']])] = ws
    has_new = 'groupIdIndex' in ix
    c = collections.Counter()
    seen = collections.Counter(); seen_g = collections.Counter(); ev = set(); ev_uk = set()
    for f in rows:
        if has_new and f[ix['skippedByGroupFlag']] == '1': c['skipped'] += 1; continue
        m, lo, wi = f[ix['month']], f[ix['loser']], f[ix['winner']]
        key = (m, lo)
        c['imp'] += 1; ev.add(key)
        if lo == UK: c['imp_uk'] += 1; ev_uk.add(key)
        if has_new: incg = f[ix['groupIdIndex']] != '0'
        else: incg = key in grp and wi in grp[key]
        k2 = (m, lo, f[ix['groupIdIndex']]) if has_new and incg else key
        seen[key] += 1
        if seen[key] == 1 and incg: c['events_cg'] += 1
        if incg:
            seen_g[k2] += 1
            if seen_g[k2] >= 2: c['second_cg'] += 1; c['second_cg_uk'] += (lo == UK)
        if seen[key] >= 2:
            c['second_all'] += 1
            if not incg: c['second_nocg'] += 1
    c['events'] = len(ev); c['events_uk'] = len(ev_uk); c['seed_uk'] = 1 if ev_uk else 0
    per_seed.append((s, dict(c))); tot.update(c)
for s, c in per_seed: print(f'seed {s}: ' + ' '.join(f'{k}={c.get(k,0)}' for k in ['imp','events','second_all','second_cg','second_nocg','skipped','imp_uk','events_uk','second_cg_uk']))
print('合計: ' + ' '.join(f'{k}={tot.get(k,0)}' for k in ['imp','events','second_all','second_cg','second_nocg','skipped','imp_uk','events_uk','second_cg_uk','seed_uk','events_cg']))
