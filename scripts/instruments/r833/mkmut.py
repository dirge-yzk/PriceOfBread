# (833) 突然変異の写しをバイトで作る（元の台本は変えない）
import sys, os
src, out = sys.argv[1], sys.argv[2]
b = open(src, 'rb').read()
M = {'mu0': (b'elif thin(rp):', b'elif False:'),               # 旗 0（痩せを常に偽）
     'mu1': (b'POP_RATIO_THIN = 10.0', b'POP_RATIO_THIN = 2.0'),  # 閾値 10→2
     'mu2': (b'rp = pratio(pop, ppop[u])', b'rp = ra'),          # 人口の比→GDP の比
     'mu3': (b'K = 4 ', b'K = 0 '),                              # K 4→0
     'mu4': (b'POP_RATIO_THIN = 10.0', b'POP_RATIO_THIN = 100.0')} # 閾値 10→100（余白の読み）
for k, (a, c) in M.items():
    assert b.count(a) == 1, k
    open(os.path.join(out, f'gdp_jump833_{k}.py'), 'wb').write(b.replace(a, c))
