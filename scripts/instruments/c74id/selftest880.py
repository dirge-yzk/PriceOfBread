# (880) c74id holown880.py の自己試験（固定の盤面・事前登録 measurements/m880_20261106_prereg_id.txt [2]）。
#   python -X utf8 -I selftest880.py <作業フォルダ> <出力>
#   合成の region_s（州 6 つ・種 1・80 四半期）を腕と基線の 2 本作り、本物の an_h2.py -v5 で事象の行を作り、holown880.py（と突然変異の口・holclass839.py）に当てる。
#   予言の表：B1 線・B2 線・B3 弱・B4 線（A2 は弱）・B5 弱・B6 線（読めない・A2 は弱）。
import sys, os, subprocess, shutil
HERE = os.path.dirname(os.path.abspath(__file__)); INS = os.path.normpath(os.path.join(HERE, '..'))
W, OUT = sys.argv[1], sys.argv[2]
HEAD = ('quarter year nationIndex nation region tier activeFlag populationK ruralPopulationK opportunityIndex holdingFactor wageFactor landFactor employmentShare pushFactor '
        'housingFactor wagePremium arableLand migrationOutKPerQuarter migrationInKPerQuarter settlerOutKPerQuarter settlerInKPerQuarter crossBorderOutKPerQuarter crossBorderInKPerQuarter '
        'warIntensity foodFillShare').split()
NQ = 80


def traj(floor):
    """q<20 は 100・q20〜60 で直線に floor まで・その後は floor。"""
    out = []
    for q in range(NQ):
        out.append(100.0 if q < 20 else floor if q >= 60 else 100.0 + (floor - 100.0) * (q - 20) / 40.0)
    return out


def owner(split_q):
    return [('国A' if (split_q is None or q < split_q) else '国B') for q in range(NQ)]


# 州 → (腕の人口・腕の持ち主・基線の人口・基線の持ち主)
B = {
    '試験州B1': (traj(5.0), owner(None), [100.0] * NQ, owner(None)),
    '試験州B2': (traj(5.0), owner(30), [100.0] * NQ, owner(None)),
    '試験州B3': (traj(5.0), owner(None), traj(20.0), owner(None)),
    '試験州B4': (traj(5.0), owner(30), traj(20.0), owner(None)),
    '試験州B5': (traj(5.0), owner(30), traj(20.0), owner(30)),
    '試験州B6': (traj(5.0), owner(None), traj(20.0), owner(None)),
}
EXPECT = {'試験州B1': '線が原因', '試験州B2': '線が原因', '試験州B3': '元からの弱さ', '試験州B4': '線が原因', '試験州B5': '元からの弱さ', '試験州B6': '線が原因'}
EXPECT_A2 = {'試験州B1': '線が原因', '試験州B2': '線が原因', '試験州B3': '元からの弱さ', '試験州B4': '元からの弱さ', '試験州B5': '元からの弱さ', '試験州B6': '元からの弱さ'}
# 突然変異ごとの予言（違う州＝赤の出る盤面）
EXPECT_MU = {'exempt-split': {'試験州B2': '元からの弱さ', '試験州B4': '元からの弱さ'}, 'no-owner': {'試験州B4': '元からの弱さ'}}


def write_ins(d, which):
    os.makedirs(d, exist_ok=True)
    nidx = {'国A': '0', '国B': '1'}
    with open(os.path.join(d, 'region_s1.tsv'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\t'.join(HEAD) + '\n')
        for q in range(NQ):
            for reg, t in B.items():
                pop, own = (t[0], t[1]) if which == 'arm' else (t[2], t[3])
                row = {k: '0' for k in HEAD}
                row.update(quarter=str(q), year=str(1783 + q // 4), nationIndex=nidx[own[q]], nation=own[q], region=reg, tier='Core', activeFlag='1',
                           populationK=repr(pop[q]), ruralPopulationK=repr(pop[q]), foodFillShare='1')
                f.write('\t'.join(row[k] for k in HEAD) + '\n')


def run(args, out=None):
    r = subprocess.run([sys.executable, '-X', 'utf8', '-I'] + args, capture_output=True)
    txt = r.stdout.decode('utf-8', 'replace')
    if out:
        with open(out, 'wb') as f: f.write(r.stdout)
    return r.returncode, txt


def main():
    if os.path.exists(W): shutil.rmtree(W)
    arm, base = os.path.join(W, 'arm'), os.path.join(W, 'base')
    write_ins(arm, 'arm'); write_ins(base, 'base')
    an = os.path.join(INS, 'hol', 'an_h2.py')
    arm_an, base_an = os.path.join(W, 'arm_an.txt'), os.path.join(W, 'base_an.txt')
    # an_h2 は python -I でなく PYTHONUTF8 で呼ぶ（stoplines866.sh と同じ）
    for d, o in ((arm, arm_an), (base, base_an)):
        r = subprocess.run([sys.executable, '-X', 'utf8', an, d, '-v5'], capture_output=True)
        with open(o, 'wb') as f: f.write(r.stdout)
    # B6：an_h2 の行のピークの年を書き換える＝region_s の走査と食い違う盤面
    with open(arm_an, 'rb') as f: raw = f.read()
    lines = raw.split(b'\n'); hit = 0
    for i, ln in enumerate(lines):
        if '試験州B6'.encode('utf-8') in ln and b'(1783)' in ln:
            lines[i] = ln.replace(b'(1783)', b'(1700)', 1); hit += 1
    with open(arm_an, 'wb') as f: f.write(b'\n'.join(lines))
    L = ['(880) selftest880：合成の盤面（州 6・種 1・80 四半期）・B6 の an_h2 の行のピークの年を書き換えた行 %d' % hit]
    ok_all = True
    ho = os.path.join(HERE, 'holown880.py')
    res = {}
    for mu in (None, 'exempt-split', 'no-owner'):
        o = os.path.join(W, 'hol_%s.txt' % (mu or 'ok'))
        args = [ho, arm_an, arm, o, '--base', base_an, base] + (['--mu', mu] if mu else [])
        rc, txt = run(args)
        got = {}
        for ln in open(o, encoding='utf-8'):
            for reg in B:
                if reg + '（' in ln: got[reg] = (ln.split('\t')[0], ln.split('\t')[2])
        res[mu] = got
    # 正しい規則
    for reg in B:
        lab, a2 = res[None].get(reg, ('なし', 'なし'))
        good = lab == EXPECT[reg] and a2 == 'A2=' + EXPECT_A2[reg]
        ok_all &= good
        L.append('正しい規則\t%s\t予言 %s（A2 %s）\t実際 %s（%s）\t%s' % (reg, EXPECT[reg], EXPECT_A2[reg], lab, a2, '一致' if good else '外れ'))
    # 突然変異：予言した州だけが変わる（赤）
    for mu in ('exempt-split', 'no-owner'):
        for reg in B:
            exp = EXPECT_MU[mu].get(reg, EXPECT[reg])
            lab = res[mu].get(reg, ('なし',))[0]
            red = exp != EXPECT[reg]
            good = lab == exp
            ok_all &= good
            L.append('突然変異 %s\t%s\t予言 %s%s\t実際 %s\t%s' % (mu, reg, exp, '（赤＝正しい規則と違う）' if red else '', lab, '一致' if good else '外れ'))
    # holclass839.py と A2 の写しの一致（1/10）
    hc = os.path.join(INS, 'm839', 'holclass839.py')
    rc, txt = run([hc, arm_an, base_an])
    n839 = sum(1 for ln in txt.splitlines() if ln.startswith('線が原因'))
    nA2 = sum(1 for reg in B if res[None].get(reg, ('', ''))[1] == 'A2=線が原因')
    good = n839 == nA2 == 2
    ok_all &= good
    L.append('holclass839.py の〈線が原因〉%d・A2 の写し %d（予言 2＝B1・B2）\t%s' % (n839, nA2, '一致' if good else '外れ'))
    L.append('両方の答えが出る（構造的に恒真でない）：正しい規則の〈線が原因〉%d・〈元からの弱さ〉%d' % (sum(1 for r in B if res[None].get(r, ('',))[0] == '線が原因'), sum(1 for r in B if res[None].get(r, ('',))[0] == '元からの弱さ')))
    L.append('SELFTEST %s' % ('pass' if ok_all else 'fail'))
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
    print('\n'.join(L))
    return 0 if ok_all else 1


if __name__ == '__main__':
    sys.exit(main())
