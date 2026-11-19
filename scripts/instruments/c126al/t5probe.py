p='D:/PoB_work/c126al_work/mut/PriceOfBread.Tests/CorncfInstrumentTests.cs'
b=open(p,'rb').read()
old=b'''        var (dir, sim) = Run(3);
        var rows = Last(dir, 3); int pos = 0;'''
new=b'''        var log = new System.Text.StringBuilder();
        foreach (ulong sd in new ulong[] { 1791, 1792, 1793, 1794, 1795, 1796, 1797, 1798 })
        {
        var (dir, sim) = Run(3, null, sd);
        var rows = Last(dir, 3, sd); int pos = 0;'''
assert b.count(old)==1; b=b.replace(old,new)
old2=b'''        Assert.True(pos > 0, "\xe8\xbc\xb8\xe5\x85\xa5\xe3\x81\xae\xe5\x89\xb2\xe5\x90\x88\xe3\x81\x8c\xe5\x85\xa8\xe3\x81\xa6 0\xef\xbc\x9d\xe8\xa9\xa6\xe9\xa8\x93\xe3\x81\xab\xe3\x81\xaa\xe3\x82\x89\xe3\x81\xaa\xe3\x81\x84");'''
assert b.count(old2)==1,'old2'
b=b.replace(old2,b'        log.Append($"seed {sd} pos {pos}; ");\n        }\n        Assert.Fail("T5PROBE " + log);')
open(p,'wb').write(b)
