# (951) c84lb 紙の台本（読むだけ）：計器の写し LbFrozen が〈州が国を出て戻る〉で生き返る型の件数を、州の表 region_s から数える。
#   名簿の国（world.json の landTaxBasis が "Fixed"＝イギリス・清）の州が、最初の四半期の行で持っていた国の region_s の行から消え、後で同じ名の国の行に戻る事象を
#   〈戻り〉と数える（州の名で引く・国の名で引く＝同じ名の別の国は区別できない＝上限の見積もり。inheritintent_s の E 行は全ての移転を書かない＝使わない）。戻った四半期 q の移転は四半期の締めの後の共有層＝
#   計器が最初に見るのは q+1 の月の締め＝landfix_s の q+1 の行に旧の規則の写しの増えが出る（写しのある RGO を持つ州だけ＝上限）。
#   同じ四半期の締めの中で出て戻る（送り出しと戻りが同じ q）＝月の締めの口では見えない事象（限界）＝別に数える。
#   landfix_s のフォルダを渡すと、lbFrozenCopyRgo が前の行より増えた（国×四半期）を数え、予言の（国・q+1）と突き合わせる。
#   使い方: python -X utf8 paper951.py <region_s のある ins フォルダ> [<landfix_s のある ins フォルダ>]
import csv, glob, os, re, sys, collections
FIXED = {'イギリス', '清'}
I = sys.argv[1]; L = sys.argv[2] if len(sys.argv) > 2 else None
tot_ret = tot_same = 0; pred = {}
for f in sorted(glob.glob(os.path.join(I, 'region_s*.tsv')), key=lambda p: int(re.findall(r'(\d+)\.tsv$', p)[0])):
    seed = int(re.findall(r'(\d+)\.tsv$', f)[0])
    pres = collections.defaultdict(set); q0 = None
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='	'):
        n = r['nation']
        if n not in FIXED: continue
        q = int(r['quarter'])
        if q0 is None or q < q0: q0 = q
        pres[(n, r['region'])].add(q)
    rets = []
    for (n, reg), qs in pres.items():
        if q0 not in qs: continue   # 最初の四半期に持っていない州＝助走の持ち主ではない（写しなし）
        s = sorted(qs)
        for a, b2 in zip(s, s[1:]):
            if b2 > a + 1: rets.append((n, b2, reg))   # a の後に出て b2 の行で戻っている＝計器が最初に見るのは b2+1 の月の締め
    p = collections.Counter((n, q + 1) for n, q, _ in rets)
    pred[seed] = p
    same = 0
    ef = os.path.join(I, f'inheritintent_s{seed}.tsv')
    if os.path.exists(ef):
        outq = collections.defaultdict(set); inq = collections.defaultdict(set)
        for r in csv.DictReader(open(ef, encoding='utf-8'), delimiter='	'):
            if r['scope'] != 'E': continue
            if r['fromNation'] in FIXED: outq[(r['fromNation'], r['region'])].add(int(r['quarter']))
            if r['toNation'] in FIXED: inq[(r['toNation'], r['region'])].add(int(r['quarter']))
        same = sum(1 for k, v in outq.items() if v & inq.get(k, set()))
    tot_ret += len(rets); tot_same += same
    print(f's{seed}: 戻り {len(rets)}（国×四半期 {len(p)}）・同じ四半期の締めの中で出て戻る（E 行で見える物だけ＝下限） {same}', '; '.join(f'{n} q{q} {reg}' for n, q, reg in sorted(rets, key=lambda x: x[1])[:8]))
print(f'計 戻り {tot_ret}・国×四半期 {sum(len(v) for v in pred.values())}・同じ四半期の締めの中で出て戻る（下限） {tot_same}')
if L:
    rise = hit = 0; miss = []
    for f in sorted(glob.glob(os.path.join(L, 'landfix_s*.tsv'))):
        seed = int(re.findall(r'(\d+)\.tsv$', f)[0]); prev = {}
        for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
            n = r['nation']; v = r['lbFrozenCopyRgo']
            if v in ('', 'nan'): continue
            cp = int(float(v)); q = int(r['quarter'])
            if n in prev and cp > prev[n]:
                rise += 1
                if (n, q) in pred.get(seed, {}): hit += 1
                else: miss.append((seed, n, q, prev[n], cp))
            prev[n] = cp
    print(f'landfix_s：写しの数が前の行より増えた（国×四半期） {rise}・うち予言の（国・q+1）に当たる {hit}・外れ {len(miss)}', miss[:10])
