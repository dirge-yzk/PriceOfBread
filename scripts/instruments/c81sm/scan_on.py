import re,sys,glob,os
BS=chr(92)
def scan(src):
    res=[] # (name, reads)
    i=0;n=len(src)
    stack=[] # entries: ('type',) ('method',name,[count]) ('other',)
    hdr_start=0
    cur=None
    def top_method():
        for e in reversed(stack):
            if e[0]=='method': return e
        return None
    while i<n:
        ch=src[i]
        nx=src[i+1] if i+1<n else ''
        if ch=='/' and nx=='/':
            j=src.find('\n',i); i=n if j<0 else j; continue
        if ch=='/' and nx=='*':
            j=src.find('*/',i+2); i=n if j<0 else j+2; continue
        if ch=='"' or (ch in '$@' and (nx=='"' or (nx in '$@' and src[i+2:i+3]=='"'))):
            # string
            k=i; verbatim=False
            while src[k] in '$@':
                if src[k]=='@': verbatim=True
                k+=1
            k+=1
            while k<n:
                if verbatim:
                    if src[k]=='"':
                        if src[k+1:k+2]=='"': k+=2; continue
                        break
                else:
                    if src[k]==BS: k+=2; continue
                    if src[k]=='"': break
                k+=1
            i=k+1; continue
        if ch=="'":
            k=i+1
            while k<n and src[k]!="'":
                if src[k]==BS: k+=1
                k+=1
            i=k+1; continue
        if src.startswith('Instruments.On',i) and not re.match(r'\w',src[i+14:i+15] or ' ') and (i==0 or not re.match(r'[\w.]',src[i-1])):
            m=top_method()
            if m: m[2][0]+=1
            i+=14; continue
        if ch=='{':
            h=src[hdr_start:i]
            if re.search(r'\b(class|struct|namespace|record|enum|interface)\b',h): stack.append(('type',))
            elif stack and stack[-1][0]=='type' and '(' in h:
                nm=re.findall(r'(\w+)\s*(?:<[^>]*>)?\s*\(',h)
                stack.append(('method',nm[0] if nm else '?',[0]))
            else: stack.append(('other',))
            hdr_start=i+1
        elif ch=='}':
            if stack:
                e=stack.pop()
                if e[0]=='method': res.append((e[1],e[2][0]))
            hdr_start=i+1
        elif ch==';':
            hdr_start=i+1
        i+=1
    return res
if __name__=='__main__':
    for f in sorted(glob.glob(sys.argv[1]+'/*.cs')):
        src=open(f,encoding='utf-8-sig').read()
        seen={}
        for nm,c in scan(src):
            k=seen.get(nm,0); seen[nm]=k+1
            if c>=2: print(os.path.basename(f),nm,k,c)
