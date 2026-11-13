// (1111) 表示の層の別アセンブリ化の IL の写し。アセンブリ（複数可）の型・メンバーごとに、署名・IL の命令の列（トークンは名前へ）・定数・属性を、アセンブリの名を除いて並べる。
//   dotnet run -- <出力> <dll>...     出力の「#」行は集計（型の数・合成の型の名簿・アセンブリの属性・資源）。型の本体は「T 名前」の下に並ぶ。
// 同名の型（<Module>・<PrivateImplementationDetails>）は複数のアセンブリで 1 つに併合する。Microsoft.CodeAnalysis.EmbeddedAttribute の付いた合成の型（アセンブリごとに持つ）は別の名簿へ。
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

static class P
{
    static readonly Dictionary<short, OpCode> Ops = new();
    static P()
    {
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (f.GetValue(null) is OpCode oc) Ops[oc.Value] = oc;
    }

    sealed class Prov : ISignatureTypeProvider<string, object>
    {
        public string GetPrimitiveType(PrimitiveTypeCode t) => t.ToString();
        public string GetTypeFromDefinition(MetadataReader m, TypeDefinitionHandle h, byte k) => TypeName(m, h);
        public string GetTypeFromReference(MetadataReader m, TypeReferenceHandle h, byte k) => TypeName(m, h);
        public string GetTypeFromSpecification(MetadataReader m, object c, TypeSpecificationHandle h, byte k) => m.GetTypeSpecification(h).DecodeSignature(this, c);
        public string GetSZArrayType(string e) => e + "[]";
        public string GetArrayType(string e, ArrayShape s) => e + "[" + new string(',', s.Rank - 1) + "]";
        public string GetByReferenceType(string e) => e + "&";
        public string GetPointerType(string e) => e + "*";
        public string GetGenericInstantiation(string g, System.Collections.Immutable.ImmutableArray<string> a) => g + "<" + string.Join(",", a) + ">";
        public string GetGenericMethodParameter(object c, int i) => "!!" + i;
        public string GetGenericTypeParameter(object c, int i) => "!" + i;
        public string GetFunctionPointerType(MethodSignature<string> s) => "fnptr(" + string.Join(",", s.ParameterTypes) + ")" + s.ReturnType;
        public string GetModifiedType(string m, string u, bool req) => u + (req ? " modreq(" : " modopt(") + m + ")";
        public string GetPinnedType(string e) => e + " pinned";
    }

    static SignatureDecoder<string, object> Dec(MetadataReader m) => new SignatureDecoder<string, object>(new Prov(), m, null);

    static string TypeName(MetadataReader m, EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.TypeDefinition:
            {
                var t = m.GetTypeDefinition((TypeDefinitionHandle)h);
                var decl = t.GetDeclaringType();
                string n = m.GetString(t.Name);
                if (!decl.IsNil) return TypeName(m, decl) + "+" + n;
                string ns = m.GetString(t.Namespace);
                return ns.Length == 0 ? n : ns + "." + n;
            }
            case HandleKind.TypeReference:
            {
                var t = m.GetTypeReference((TypeReferenceHandle)h);
                string n = m.GetString(t.Name);
                if (t.ResolutionScope.Kind == HandleKind.TypeReference) return TypeName(m, (TypeReferenceHandle)t.ResolutionScope) + "+" + n;
                string ns = m.GetString(t.Namespace);
                return ns.Length == 0 ? n : ns + "." + n;
            }
            case HandleKind.TypeSpecification:
                return m.GetTypeSpecification((TypeSpecificationHandle)h).DecodeSignature(new Prov(), null);
        }
        return "?" + h.Kind;
    }

    static string MethodSig(MetadataReader m, BlobHandle b, string name)
    {
        var s = m.GetBlobReader(b);
        var sig = Dec(m).DecodeMethodSignature(ref s);
        return $"{sig.ReturnType} {name}{(sig.GenericParameterCount > 0 ? "`" + sig.GenericParameterCount : "")}({string.Join(",", sig.ParameterTypes)})" + (sig.Header.IsInstance ? " inst" : " static");
    }

    static string MemberName(MetadataReader m, EntityHandle h)
    {
        switch (h.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var d = m.GetMethodDefinition((MethodDefinitionHandle)h);
                return TypeName(m, d.GetDeclaringType()) + "::" + MethodSig(m, d.Signature, m.GetString(d.Name));
            }
            case HandleKind.MemberReference:
            {
                var d = m.GetMemberReference((MemberReferenceHandle)h);
                string parent = d.Parent.Kind == HandleKind.MethodDefinition ? MemberName(m, d.Parent) : TypeName(m, d.Parent);
                var br = m.GetBlobReader(d.Signature);
                if (br.ReadSignatureHeader().Kind == SignatureKind.Field)
                {
                    var br2 = m.GetBlobReader(d.Signature);
                    return parent + "::" + m.GetString(d.Name) + " : " + Dec(m).DecodeFieldSignature(ref br2);
                }
                return parent + "::" + MethodSig(m, d.Signature, m.GetString(d.Name));
            }
            case HandleKind.FieldDefinition:
            {
                var d = m.GetFieldDefinition((FieldDefinitionHandle)h);
                var br = m.GetBlobReader(d.Signature);
                return TypeName(m, d.GetDeclaringType()) + "::" + m.GetString(d.Name) + " : " + Dec(m).DecodeFieldSignature(ref br);
            }
            case HandleKind.MethodSpecification:
            {
                var d = m.GetMethodSpecification((MethodSpecificationHandle)h);
                var br = m.GetBlobReader(d.Signature);
                br.ReadSignatureHeader(); int n = br.ReadCompressedInteger();
                var args = new List<string>();
                var dec = Dec(m);
                for (int i = 0; i < n; i++) args.Add(dec.DecodeType(ref br));
                return MemberName(m, d.Method) + "<" + string.Join(",", args) + ">";
            }
            case HandleKind.TypeDefinition: case HandleKind.TypeReference: case HandleKind.TypeSpecification:
                return TypeName(m, h);
            case HandleKind.StandaloneSignature:
            {
                var d = m.GetStandaloneSignature((StandaloneSignatureHandle)h);
                return "standalone:" + Convert.ToHexString(m.GetBlobBytes(d.Signature));
            }
        }
        return "?" + h.Kind;
    }

    static string Attr(MetadataReader m, CustomAttributeHandle h)
    {
        var a = m.GetCustomAttribute(h);
        return $"attr {MemberName(m, a.Constructor)} [{Convert.ToHexString(m.GetBlobBytes(a.Value))}]";
    }

    static IEnumerable<string> Attrs(MetadataReader m, CustomAttributeHandleCollection c) => c.Select(h => Attr(m, h));

    static string Const(MetadataReader m, ConstantHandle h)
    {
        if (h.IsNil) return "";
        var c = m.GetConstant(h);
        var br = m.GetBlobReader(c.Value);
        object v = c.TypeCode switch
        {
            ConstantTypeCode.String => "\"" + br.ReadUTF16(br.Length) + "\"",
            ConstantTypeCode.NullReference => "null",
            _ => br.ReadConstant(c.TypeCode)
        };
        return $" const {c.TypeCode}={v}";
    }

    static void DumpIl(MetadataReader m, MethodBodyBlock body, List<string> o)
    {
        o.Add($"    body maxstack={body.MaxStack} initlocals={body.LocalVariablesInitialized}");
        if (!body.LocalSignature.IsNil)
        {
            var sa = m.GetStandaloneSignature(body.LocalSignature);
            var br = m.GetBlobReader(sa.Signature);
            o.Add("    locals " + string.Join(",", Dec(m).DecodeLocalSignature(ref br)));
        }
        foreach (var e in body.ExceptionRegions)
            o.Add($"    eh {e.Kind} try {e.TryOffset}+{e.TryLength} handler {e.HandlerOffset}+{e.HandlerLength} " + (e.CatchType.IsNil ? "" : TypeName(m, e.CatchType)));
        var il = body.GetILReader();
        int start = il.Offset;
        while (il.RemainingBytes > 0)
        {
            int off = il.Offset - start;
            int b = il.ReadByte();
            short code = (short)b;
            if (b == 0xFE) code = (short)(0xFE00 | il.ReadByte());
            if (!Ops.TryGetValue(code, out var op)) { o.Add($"    {off:X4} ??? {code:X}"); continue; }
            string arg = "";
            switch (op.OperandType)
            {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget: { int d = il.ReadSByte(); arg = (il.Offset - start + d).ToString("X4"); break; }
                case OperandType.InlineBrTarget: { int d = il.ReadInt32(); arg = (il.Offset - start + d).ToString("X4"); break; }
                case OperandType.ShortInlineI: arg = il.ReadSByte().ToString(); break;
                case OperandType.ShortInlineVar: arg = il.ReadByte().ToString(); break;
                case OperandType.InlineVar: arg = il.ReadInt16().ToString(); break;
                case OperandType.InlineI: arg = il.ReadInt32().ToString(); break;
                case OperandType.InlineI8: arg = il.ReadInt64().ToString(); break;
                case OperandType.ShortInlineR: arg = BitConverter.SingleToInt32Bits(il.ReadSingle()).ToString("X8"); break;
                case OperandType.InlineR: arg = BitConverter.DoubleToInt64Bits(il.ReadDouble()).ToString("X16"); break;
                case OperandType.InlineString: arg = "\"" + m.GetUserString(MetadataTokens.UserStringHandle(il.ReadInt32() & 0xFFFFFF)) + "\""; break;
                case OperandType.InlineSwitch:
                {
                    int n = il.ReadInt32(); var ts = new int[n]; for (int i = 0; i < n; i++) ts[i] = il.ReadInt32();
                    int basePos = il.Offset - start;
                    arg = string.Join(",", ts.Select(t => (basePos + t).ToString("X4"))); break;
                }
                case OperandType.InlineMethod: case OperandType.InlineField: case OperandType.InlineType: case OperandType.InlineTok: case OperandType.InlineSig:
                    arg = MemberName(m, MetadataTokens.EntityHandle(il.ReadInt32())); break;
                default: throw new Exception("operand " + op.OperandType);
            }
            o.Add($"    {off:X4} {op.Name} {arg}");
        }
    }

    static bool IsEmbedded(MetadataReader m, TypeDefinition t) => Attrs(m, t.GetCustomAttributes()).Any(a => a.Contains("Microsoft.CodeAnalysis.EmbeddedAttribute"));

    static bool InEmbedded(MetadataReader m, TypeDefinition t)
    {
        while (true)
        {
            if (IsEmbedded(m, t)) return true;
            var d = t.GetDeclaringType();
            if (d.IsNil) return false;
            t = m.GetTypeDefinition(d);
        }
    }

    static int Main(string[] args)
    {
        var types = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        var embedded = new SortedSet<string>(StringComparer.Ordinal);
        var asmAttrs = new List<string>(); var resources = new List<string>();
        foreach (var path in args.Skip(1))
        {
            using var fs = File.OpenRead(path);
            using var pe = new PEReader(fs);
            var m = pe.GetMetadataReader();
            foreach (var l in Attrs(m, m.GetAssemblyDefinition().GetCustomAttributes())) asmAttrs.Add(l);
            foreach (var rh in m.ManifestResources)
            {
                var r = m.GetManifestResource(rh);
                var br = pe.GetSectionData(pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress + (int)r.Offset).GetReader();
                int len = br.ReadInt32(); var bytes = br.ReadBytes(len);
                resources.Add(m.GetString(r.Name) + " " + len + " " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..16]);
            }
            foreach (var th in m.TypeDefinitions)
            {
                var t = m.GetTypeDefinition(th);
                string name = TypeName(m, th);
                if (InEmbedded(m, t)) { embedded.Add(name); continue; }
                if (!types.TryGetValue(name, out var o)) types[name] = o = new List<string>();
                var tag = new List<string> { "  type " + t.Attributes };
                if (!t.BaseType.IsNil) tag.Add("base " + TypeName(m, t.BaseType));
                foreach (var ih in t.GetInterfaceImplementations()) tag.Add("impl " + TypeName(m, m.GetInterfaceImplementation(ih).Interface));
                foreach (var gh in t.GetGenericParameters()) tag.Add("gp " + m.GetString(m.GetGenericParameter(gh).Name));
                o.Add(string.Join(" ", tag));
                foreach (var a in Attrs(m, t.GetCustomAttributes())) o.Add("  " + a);
                foreach (var fh in t.GetFields())
                {
                    var f = m.GetFieldDefinition(fh);
                    var fb = m.GetBlobReader(f.Signature);
                    string ft = Dec(m).DecodeFieldSignature(ref fb);
                    string line = $"  field {f.Attributes} {m.GetString(f.Name)} : {ft}{Const(m, f.GetDefaultValue())}";
                    int rva = f.GetRelativeVirtualAddress();
                    if (rva != 0)
                    {
                        // 静的な配列の初期値：型の名に大きさ（Size=N）が入る＝データの中身のハッシュを足す
                        var tm = System.Text.RegularExpressions.Regex.Match(ft, @"Size=(\d+)");
                        int size = tm.Success ? int.Parse(tm.Groups[1].Value) : 0;
                        if (size > 0) line += " data " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pe.GetSectionData(rva).GetContent(0, size).ToArray()))[..16];
                    }
                    o.Add(line);
                    foreach (var a in Attrs(m, f.GetCustomAttributes())) o.Add("    " + a);
                }
                foreach (var mh in t.GetMethods())
                {
                    var d = m.GetMethodDefinition(mh);
                    o.Add($"  method {d.Attributes} {d.ImplAttributes} {MethodSig(m, d.Signature, m.GetString(d.Name))}");
                    foreach (var a in Attrs(m, d.GetCustomAttributes())) o.Add("    " + a);
                    foreach (var gh in d.GetGenericParameters()) o.Add("    gp " + m.GetString(m.GetGenericParameter(gh).Name));
                    foreach (var ph in d.GetParameters())
                    {
                        var p = m.GetParameter(ph);
                        o.Add($"    param {p.SequenceNumber} {m.GetString(p.Name)} {p.Attributes}{Const(m, p.GetDefaultValue())}");
                        foreach (var a in Attrs(m, p.GetCustomAttributes())) o.Add("      " + a);
                    }
                    if (d.RelativeVirtualAddress != 0) DumpIl(m, pe.GetMethodBody(d.RelativeVirtualAddress), o);
                }
                foreach (var ph in t.GetProperties())
                {
                    var p = m.GetPropertyDefinition(ph); var acc = p.GetAccessors();
                    o.Add($"  property {m.GetString(p.Name)} get={(acc.Getter.IsNil ? "" : m.GetString(m.GetMethodDefinition(acc.Getter).Name))} set={(acc.Setter.IsNil ? "" : m.GetString(m.GetMethodDefinition(acc.Setter).Name))}{Const(m, p.GetDefaultValue())}");
                    foreach (var a in Attrs(m, p.GetCustomAttributes())) o.Add("    " + a);
                }
                foreach (var eh in t.GetEvents()) o.Add($"  event {m.GetString(m.GetEventDefinition(eh).Name)}");
            }
        }
        var sb = new StringBuilder();
        sb.AppendLine("# types " + types.Count);
        foreach (var e in embedded) sb.AppendLine("# embedded " + e);
        foreach (var a in asmAttrs.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine("# asmattr " + a);
        foreach (var r in resources.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine("# resource " + r);
        foreach (var (name, lines) in types)
        {
            sb.AppendLine("T " + name);
            if (name == "<Module>" || name == "<PrivateImplementationDetails>")
            {
                // 併合した型：塊（field/method/attr の先頭行から次の先頭行まで）で並べ、型の行は捨てる
                var blocks = new List<string>(); var cur = new StringBuilder();
                foreach (var l in lines)
                {
                    if (l.StartsWith("  field") || l.StartsWith("  method") || l.StartsWith("  type") || l.StartsWith("  attr")) { if (cur.Length > 0) blocks.Add(cur.ToString()); cur.Clear(); }
                    cur.AppendLine(l);
                }
                if (cur.Length > 0) blocks.Add(cur.ToString());
                foreach (var b in blocks.Where(x => !x.StartsWith("  type")).OrderBy(x => x, StringComparer.Ordinal)) sb.Append(b);
            }
            else foreach (var l in lines) sb.AppendLine(l);
        }
        File.WriteAllText(args[0], sb.ToString(), new UTF8Encoding(false));
        return 0;
    }
}
