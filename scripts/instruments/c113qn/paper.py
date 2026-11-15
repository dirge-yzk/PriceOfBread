# (1153) c113qn 紙の台本：植民地の毎月のキニーネ（Colonial.cs:24・:43）の旧の規則の量を、今の盤面の生（region_s・fiscal_s・goods_s）から数える（読むだけ・近似）。
# 使い方: python -X utf8 -I paper.py <リポジトリ> <ラベル=生のフォルダ> ...
# 旧の規則＝植民地格（tier=Colonial）でマラリア地（world.json の abstractTargets の quinine の標的の植民地の名）の州 1 つあたり月 6.0（p04 の旧の既定・計器と同じく台本が自分で持つ）。
# 近似：Colonial.Step の早い戻り（植民地 0・ColonialResources なし）は region_s では見えない＝州を持つ四半期は全て数える。調達の量（afford・資金の比・在庫）は生に無い＝請求の側（fiscal_s）で読む。
import sys, os, json
sys.stdout.reconfigure(newline='\n')
REPO = sys.argv[1]
OLD_PER_COLONY = 6.0
W = json.load(open(os.path.join(REPO, 'PriceOfBread.SimCore', 'world.json'), encoding='utf-8'))
QN = {c['name'] for t in W['abstractTargets'] if t.get('quinine') for c in t['colonies']}


def fl(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def arm(lab, d):
    seeds = [s for s in range(1, 21) if os.path.exists(os.path.join(d, f'region_s{s}.tsv'))]
    tot_nq = tot_mal = 0; tot_want = 0.0; first = []; per_seed = []
    claim_m = claim_pos = 0; qn_share = []; nat_mal = {}
    for s in seeds:
        f = open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8')
        h = f.readline().rstrip('\n').split('\t')
        iq, it, ir, inat, iact = (h.index(k) for k in ('quarter', 'tier', 'region', 'nation', 'activeFlag'))
        nq = set(); cnt = {}
        for l in f:
            x = l.rstrip('\n').split('\t')
            if x[iact] not in ('1', 'True', 'true'):
                continue
            k = (int(x[iq]), x[inat]); nq.add(k)
            if x[it] == 'Colonial' and x[ir] in QN:
                cnt[k] = cnt.get(k, 0) + 1
        f.close()
        want = sum(OLD_PER_COLONY * n * 3 for n in cnt.values())
        tot_nq += len(nq); tot_mal += len(cnt); tot_want += want
        if cnt:
            first.append(min(q for q, _ in cnt))
        for (_, nat) in cnt:
            nat_mal[nat] = nat_mal.get(nat, 0) + 1
        per_seed.append('%d:%d/%d' % (s, len(cnt), len(nq)))
        # 請求の側：月の植民地の維持費の請求（燃料とキニーネの和）と、キニーネの値段×旧の規則の量（goods_s の値段の近似）
        fp = os.path.join(d, f'fiscal_s{s}.tsv')
        if os.path.exists(fp):
            f = open(fp, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); icl = h.index('claimColonialUpkeepPerMonth')
            for l in f:
                x = l.rstrip('\n').split('\t'); claim_m += 1; claim_pos += fl(x[icl]) > 0
            f.close()
        gp = os.path.join(d, f'goods_s{s}.tsv')
        if os.path.exists(gp):
            f = open(gp, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t')
            igq, ign, inn, idm = (h.index(k) for k in ('quarter', 'goodName', 'nation', 'lastDemandQty'))
            for l in f:
                x = l.rstrip('\n').split('\t')
                if x[ign] != 'キニーネ':
                    continue
                k = (int(x[igq]), x[inn])
                if k in cnt and fl(x[idm]) > 0:
                    qn_share.append(OLD_PER_COLONY * cnt[k] / fl(x[idm]))
            f.close()
    qs = sorted(qn_share)
    med = qs[len(qs) // 2] if qs else float('nan')
    top = sorted(nat_mal.items(), key=lambda kv: -kv[1])[:6]
    print(f'{lab}\tseeds={len(seeds)}\tP1_malNQ={tot_mal}\tactiveNQ={tot_nq}\tshare={tot_mal / max(1, tot_nq):.4f}\tP2_oldRuleWantSum={tot_want:.1f}'
          f'\tP3_firstQ_min={min(first) if first else "-"}\tP4_fiscalMonths={claim_m}\tclaimPos={claim_pos}'
          f'\tP5_oldWant/nationQnDemand_n={len(qs)}_median={med:.4g}\tP6_topNations={",".join("%s:%d" % kv for kv in top)}')
    print(f'{lab}\tper_seed\t' + ' '.join(per_seed))


for a in sys.argv[2:]:
    lab, d = a.split('=', 1)
    arm(lab, d)
