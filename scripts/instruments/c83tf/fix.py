import sys
p=sys.argv[1]
b=open(p,'rb').read()
def rep(a,c):
    global b
    assert b.count(a)==1,a
    b=b.replace(a,c)
rep(b'var start = new Dictionary<(string, string), (double Cum, double Ipc)>();',
    'var start = new Dictionary<(string, string), (double Cum, double Ipc, int Q)>();   // (941) Q＝その値を書いた四半期（州が国の一覧から外れて戻った行は累積が複数の四半期にまたがる＝数えない）'.encode())
rep(b'if (q >= 0 && start.TryGetValue((nn, r.Name), out var s))',
    b'if (q >= 0 && start.TryGetValue((nn, r.Name), out var s) && s.Q == q - 1)')
rep(b'start[(nn, r.Name)] = (cum, ipc);', b'start[(nn, r.Name)] = (cum, ipc, q);')
open(p,'wb').write(b)
