using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TypeGen;

class Program
{
    // ==================== Configuration ====================

    record HostObjectDef(string JsName, string CSharpTypeName);
    record HostTypeDef(string JsName, string CSharpTypeName, string? Extends = null);
    record GlobalFuncDef(string JsName, string CSharpMethodName);

    static readonly HostObjectDef[] HostObjects =
    [
        new("keyMouseScript", "BetterGenshinImpact.Core.Script.Dependence.KeyMouseScript"),
        new("pathingScript", "BetterGenshinImpact.Core.Script.Dependence.AutoPathingScript"),
        new("genshin", "BetterGenshinImpact.Core.Script.Dependence.Genshin"),
        new("log", "BetterGenshinImpact.Core.Script.Dependence.Log"),
        new("file", "BetterGenshinImpact.Core.Script.Dependence.LimitedFile"),
        new("http", "BetterGenshinImpact.Core.Script.Dependence.Http"),
        new("notification", "BetterGenshinImpact.Core.Script.Dependence.Notification"),
        new("dispatcher", "BetterGenshinImpact.Core.Script.Dependence.Dispatcher"),
    ];

    static readonly HostTypeDef[] HostTypes =
    [
        new("RealtimeTimer", "BetterGenshinImpact.Core.Script.Dependence.Model.RealtimeTimer"),
        new("SoloTask", "BetterGenshinImpact.Core.Script.Dependence.Model.SoloTask"),
        new("AutoSkipConfig", "BetterGenshinImpact.GameTask.AutoSkip.AutoSkipConfig"),
        new("CancellationTokenSource", "System.Threading.CancellationTokenSource"),
        new("CancellationToken", "System.Threading.CancellationToken"),
        new("PostMessage", "BetterGenshinImpact.Core.Script.Dependence.Simulator.PostMessage"),
        new("ServerTime", "BetterGenshinImpact.Core.Script.Dependence.ServerTime"),
        new("Mat", "OpenCvSharp.Mat"),
        new("Point2f", "OpenCvSharp.Point2f"),
        new("Rect", "OpenCvSharp.Rect"),
        new("Scalar", "OpenCvSharp.Scalar"),
        new("Color", "System.Drawing.Color"),
        new("RecognitionObject", "BetterGenshinImpact.Core.Recognition.RecognitionObject"),
        new("Region", "BetterGenshinImpact.GameTask.Model.Area.Region"),
        new("ImageRegion", "BetterGenshinImpact.GameTask.Model.Area.ImageRegion", "Region"),
        new("GameCaptureRegion", "BetterGenshinImpact.GameTask.Model.Area.GameCaptureRegion", "ImageRegion"),
        new("DesktopRegion", "BetterGenshinImpact.GameTask.Model.Area.DesktopRegion", "Region"),
        new("CombatScenes", "BetterGenshinImpact.GameTask.AutoFight.Model.CombatScenes"),
        new("Avatar", "BetterGenshinImpact.GameTask.AutoFight.Model.Avatar"),
        new("AutoDomainParam", "BetterGenshinImpact.GameTask.AutoDomain.AutoDomainParam"),
        new("AutoFightParam", "BetterGenshinImpact.GameTask.AutoFight.AutoFightParam"),
        new("AutoLeyLineOutcropParam", "BetterGenshinImpact.GameTask.AutoLeyLineOutcrop.AutoLeyLineOutcropParam"),
        new("AutoStygianOnslaughtParam", "BetterGenshinImpact.GameTask.AutoStygianOnslaught.AutoStygianOnslaughtParam"),
        new("KeyMouseHook", "BetterGenshinImpact.Core.Script.Dependence.KeyMouseHook"),
        new("BvPage", "BetterGenshinImpact.Core.BgiVision.BvPage"),
        new("BvLocator", "BetterGenshinImpact.Core.BgiVision.BvLocator"),
        new("BvImage", "BetterGenshinImpact.Core.BgiVision.BvImage"),
    ];

    static readonly GlobalFuncDef[] GlobalFunctions =
    [
        new("sleep", "Sleep"),
        new("getVersion", "GetVersion"),
        new("keyDown", "KeyDown"),
        new("keyUp", "KeyUp"),
        new("keyPress", "KeyPress"),
        new("setGameMetrics", "SetGameMetrics"),
        new("getGameMetrics", "GetGameMetrics"),
        new("moveMouseBy", "MoveMouseBy"),
        new("moveMouseTo", "MoveMouseTo"),
        new("click", "Click"),
        new("leftButtonClick", "LeftButtonClick"),
        new("leftButtonDown", "LeftButtonDown"),
        new("leftButtonUp", "LeftButtonUp"),
        new("rightButtonClick", "RightButtonClick"),
        new("rightButtonDown", "RightButtonDown"),
        new("rightButtonUp", "RightButtonUp"),
        new("middleButtonClick", "MiddleButtonClick"),
        new("middleButtonDown", "MiddleButtonDown"),
        new("middleButtonUp", "MiddleButtonUp"),
        new("verticalScroll", "VerticalScroll"),
        new("captureGameRegion", "CaptureGameRegion"),
        new("getAvatars", "GetAvatars"),
        new("inputText", "InputText"),
    ];

    static readonly HashSet<string> SkipBaseMembers =
    [
        "ToString", "Equals", "GetHashCode", "GetType",
        "ReferenceEquals", "MemberwiseClone", "Finalize",
        ".ctor", ".cctor",
    ];

    static readonly HashSet<string> OpaqueTypeNames =
    [
        "ILogger",
        "IStringLocalizer",
        "CultureInfo",
        "IServiceProvider",
        "IKeyboardSimulator",
        "IMouseSimulator",
        "INodeConverter",
        "DrawContent",
        "RectDrawable",
        "LineDrawable",
        "ISystemInfo",
        "ElementAssets",
        "BgiYoloPredictor",
        "AutoFightAssets",
        "Image",
        "Size",
        "Pen",
        "Point",
        "ScriptObject",
        "MultiGameStatus",
        "AvatarActiveCheckContext",
        "CombatAvatar",
        "AutoLeyLineOutcropFightConfig",
        "AutoLeyLineOutcropConfig",
        "AutoStygianOnslaughtConfig",
        "DateTime",
        "Exception",
    ];

    // Type name patterns that should map to `any`
    static readonly HashSet<string> OpaqueTypeNamePatterns =
    [
        "Lazy`",
        "NavigationInstance",
        "ActionScheduler",
        "AutoFightConfig",
    ];

    static readonly HashSet<string> SkipBaseTypes =
    [
        "CommunityToolkit.Mvvm.ComponentModel.ObservableObject",
        "System.ComponentModel.INotifyPropertyChanged",
        "System.ComponentModel.PropertyChangedEventHandler",
    ];

    // Types that use hardcoded declarations instead of reflection
    static readonly HashSet<string> HardcodedTypes =
    [
        "System.Threading.CancellationTokenSource",
        "System.Threading.CancellationToken",
        "OpenCvSharp.Mat",
        "OpenCvSharp.Point2f",
        "OpenCvSharp.Rect",
        "OpenCvSharp.Scalar",
        "System.Drawing.Color",
        "BetterGenshinImpact.GameTask.Model.Area.Region",
        "BetterGenshinImpact.GameTask.Model.Area.ImageRegion",
        "BetterGenshinImpact.GameTask.Model.Area.GameCaptureRegion",
        "BetterGenshinImpact.GameTask.Model.Area.DesktopRegion",
    ];

    // ==================== Helpers ====================

    static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    static string PascalToAlias(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToUpperInvariant(name[0]) + name[1..];

    static readonly HashSet<string> PrimitiveNames =
    [
        "Int32", "Single", "Double", "Int64", "Int16", "Byte",
        "UInt32", "UInt64", "Decimal", "String", "Char", "Boolean",
        "Void", "Object",
    ];

    static string? GetPrimitiveTsName(string typeName) => typeName switch
    {
        "Int32" or "Single" or "Double" or "Int64" or "Int16" or "Byte"
            or "UInt32" or "UInt64" or "Decimal" => "number",
        "String" or "Char" => "string",
        "Boolean" => "boolean",
        "Void" => "void",
        "Object" => "any",
        _ => null,
    };

    // C# Type → TypeScript type string
    static string ToTsType(Type type, bool isReturnType = false)
    {
        if (type == null) return "any";

        // Nullable<T> / T?
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
            return $"{ToTsType(underlying, isReturnType)} | null";

        // Primitives
        var primitive = GetPrimitiveTsName(type.Name);
        if (primitive != null) return primitive;

        // Task / Task<T>
        if (type.Name == "Task" && !type.IsGenericType ||
            type.Name == "ValueTask" && !type.IsGenericType)
            return "Promise<void>";
        if (type.IsGenericType)
        {
            var genDefName = type.GetGenericTypeDefinition().Name;
            if (genDefName == "Task`1" || genDefName == "ValueTask`1")
            {
                var innerType = type.GetGenericArguments()[0];
                var innerTs = ToTsType(innerType, true);
                return $"Promise<{innerTs}>";
            }
        }

        // Arrays
        if (type.IsArray)
            return $"{ToTsType(type.GetElementType()!)}[]";

        // Generic collections
        if (type.IsGenericType)
        {
            var genDefName = type.GetGenericTypeDefinition().Name;
            var genArgs = type.GetGenericArguments();
            if (genDefName is "List`1" or "IList`1" or "IEnumerable`1" or "ICollection`1"
                or "IReadOnlyCollection`1" or "IReadOnlyList`1" or "ReadOnlyCollection`1"
                or "HashSet`1" or "ObservableCollection`1")
                return $"{ToTsType(genArgs[0])}[]";
            if (genDefName is "Dictionary`2" or "IDictionary`2")
                return $"Record<string, {ToTsType(genArgs[1])}>";
            if (genDefName == "KeyValuePair`2")
                return $"[{ToTsType(genArgs[0])}, {ToTsType(genArgs[1])}]";
        }

        // Enums
        if (type.IsEnum)
            return string.Join(" | ", Enum.GetNames(type).Select(n => $"\"{n}\""));

        // Delegates (Func<>, Action<>)
        if (type.BaseType?.Name == "MulticastDelegate" || type.BaseType?.Name == "Delegate")
            return "any";

        // Opaque types
        if (OpaqueTypeNames.Contains(type.Name))
            return "any";

        // Opaque type name patterns (e.g., Lazy`1, NavigationInstance)
        if (OpaqueTypeNamePatterns.Any(p => type.Name.StartsWith(p)))
            return "any";

        // Nullable reference types (ref type with ?)
        if (type.IsGenericType && type.GetGenericTypeDefinition().Name == "Nullable`1")
            return $"{ToTsType(type.GetGenericArguments()[0])} | null";

        // Unresolved generic types (name contains backtick like Image`1, ValueTuple`2)
        if (type.Name.Contains('`'))
            return "any";

        // Simple type name
        return type.Name;
    }

    static bool ShouldSkipMember(MemberInfo member, Type? declaringType = null)
    {
        if (member is not PropertyInfo and not MethodInfo and not FieldInfo and not ConstructorInfo)
            return true;

        // Skip Obsolete
        if (member.GetCustomAttribute<ObsoleteAttribute>() != null)
            return true;

        // Skip non-public
        if (member is MethodInfo mi && !mi.IsPublic) return true;
        if (member is PropertyInfo pi && (pi.GetMethod == null || !pi.GetMethod.IsPublic) &&
            (pi.SetMethod == null || !pi.SetMethod.IsPublic)) return true;
        if (member is FieldInfo fi && !fi.IsPublic) return true;

        // Skip Object base members
        if (SkipBaseMembers.Contains(member.Name)) return true;

        // Skip property backing methods (get_/set_)
        if (member is MethodInfo && (member.Name.StartsWith("get_") || member.Name.StartsWith("set_")))
            return true;

        // Skip properties with opaque types
        if (member is PropertyInfo prop)
        {
            var propType = prop.PropertyType;
            if (OpaqueTypeNames.Contains(propType.Name)) return true;
            // Skip event-like properties
            if (propType.Name.Contains("EventHandler")) return true;
            // Skip ILogger properties
            if (propType.Name == "ILogger" || (propType.IsGenericType && propType.GetGenericTypeDefinition().Name.StartsWith("ILogger")))
                return true;
            if (propType.Name == "IStringLocalizer") return true;
            if (propType.Name == "CultureInfo") return true;
        }

        // Skip methods from ObservableObject base
        if (declaringType != null)
        {
            if (member.DeclaringType != null &&
                SkipBaseTypes.Contains(member.DeclaringType.FullName!))
                return true;
        }

        // Skip Dispose unless it's the type's own
        if (member.Name == "Dispose" && member.DeclaringType?.Name != "KeyMouseHook" &&
            member.DeclaringType?.FullName != "BetterGenshinImpact.Core.Script.Dependence.KeyMouseHook" &&
            member.DeclaringType?.Name != "CombatScenes")
        {
            if (member.DeclaringType?.GetInterfaces().Any(i => i.Name == "IDisposable") == true &&
                member.DeclaringType.GetMethod("Dispose",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
                return true;
        }

        // Skip event members
        if (member is EventInfo) return true;

        return false;
    }

    static bool IsPropertyReadOnly(PropertyInfo prop) => prop.SetMethod == null;

    static bool IsFieldReadOnly(FieldInfo field) => field.IsInitOnly;

    // ==================== XML Doc Reader ====================

    class XmlDocReader
    {
        readonly string _sourceRoot;
        readonly Dictionary<string, string> _typeToFilePath = new();

        public XmlDocReader(string sourceRoot)
        {
            _sourceRoot = Path.GetFullPath(sourceRoot);
            BuildTypeIndex();
        }

        void BuildTypeIndex()
        {
            if (!Directory.Exists(_sourceRoot)) return;
            var csFiles = Directory.GetFiles(_sourceRoot, "*.cs", SearchOption.AllDirectories);
            foreach (var file in csFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var matches = Regex.Matches(content,
                        @"(?:public\s+)?(?:static\s+)?(?:partial\s+)?(?:sealed\s+)?(?:abstract\s+)?class\s+(\w+)");
                    foreach (Match m in matches)
                    {
                        _typeToFilePath[m.Groups[1].Value] = file;
                    }
                }
                catch { }
            }
        }

        public (string? Summary, string? SeePath, int? SeeLine) GetDocForType(Type type)
        {
            if (!_typeToFilePath.TryGetValue(type.Name, out var filePath)) return (null, null, null);
            if (!File.Exists(filePath)) return (null, null, null);
            var lines = File.ReadAllLines(filePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (!Regex.IsMatch(trimmed, $@"\bclass\s+{type.Name}\b")) continue;

                var summaryLines = new List<string>();
                for (var j = i - 1; j >= Math.Max(0, i - 10); j--)
                {
                    var prev = lines[j].Trim();
                    if (prev.StartsWith("///"))
                        summaryLines.Insert(0, prev);
                    else break;
                }
                if (summaryLines.Count > 0)
                {
                    var (summary, _, _) = ParseXmlDocLines(summaryLines);
                    return (summary, null, null);
                }
                break;
            }
            return (null, null, null);
        }

        public (string? Summary, string? SeePath, int? SeeLine, Dictionary<string, string> Params, string? Returns)
            GetDocForMember(Type type, string memberName)
        {
            if (!_typeToFilePath.TryGetValue(type.Name, out var filePath))
                return (null, null, null, new(), null);

            // Find the member declaration
            if (!File.Exists(filePath)) return (null, null, null, new(), null);
            var lines = File.ReadAllLines(filePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                // Match the member name in a declaration context
                if (!trimmed.Contains(memberName)) continue;
                if (!trimmed.StartsWith("public") && !trimmed.StartsWith("public") &&
                    !Regex.IsMatch(trimmed, $@"\b{memberName}\b\s*[\(<]") &&
                    !Regex.IsMatch(trimmed, $@"\b{memberName}\b\s*[\{{=:]"))
                    continue;

                // Check if this is the right member (not just a reference to it)
                if (Regex.IsMatch(trimmed, $@"\bpublic\s+(?:static\s+)?(?:readonly\s+)?(?:virtual\s+)?(?:async\s+)?(?:override\s+)?(?:abstract\s+)?(?:new\s+)?[\w<>\[\],\s\?]+\s+{memberName}\b") ||
                    Regex.IsMatch(trimmed, $@"\b{memberName}\b\s*\(") ||
                    Regex.IsMatch(trimmed, $@"\b{memberName}\b\s*\{{") ||
                    Regex.IsMatch(trimmed, $@"\b{memberName}\b\s*[=;]"))
                {
                    // Look backward for XML doc
                    var summaryLines = new List<string>();
                    for (var j = i - 1; j >= Math.Max(0, i - 20); j--)
                    {
                        var prev = lines[j].Trim();
                        if (prev.StartsWith("///"))
                            summaryLines.Insert(0, prev);
                        else if (prev.StartsWith("[") || prev.StartsWith("public") || string.IsNullOrEmpty(prev))
                            break;
                        else
                            break;
                    }

                    if (summaryLines.Count > 0)
                    {
                        var (summary, paramDict, returns) = ParseXmlDocLines(summaryLines);
                        var relPath = Path.GetRelativePath(
                            Path.GetDirectoryName(_sourceRoot)!,
                            filePath).Replace('\\', '/');
                        return (summary, relPath, i + 1, paramDict, returns);
                    }
                    else
                    {
                        var relPath = Path.GetRelativePath(
                            Path.GetDirectoryName(_sourceRoot)!,
                            filePath).Replace('\\', '/');
                        return (null, relPath, i + 1, new(), null);
                    }
                }
            }

            return (null, null, null, new(), null);
        }

        static (string? Summary, Dictionary<string, string> Params, string? Returns) ParseXmlDocLines(
            List<string> lines)
        {
            var summary = new StringBuilder();
            var paramDict = new Dictionary<string, string>();
            string? returns = null;
            var current = "summary";

            foreach (var line in lines)
            {
                var text = line.TrimStart('/').Trim();
                if (text.StartsWith("<summary>"))
                {
                    text = Regex.Replace(text, @"<summary>\s*", "");
                    if (text.EndsWith("</summary>"))
                        text = text[..^"</summary>".Length].Trim();
                    if (text.Length > 0)
                        summary.Append(text);
                    current = "summary";
                    continue;
                }
                if (text.StartsWith("</summary>"))
                {
                    current = "none";
                    continue;
                }
                if (text.StartsWith("<param"))
                {
                    current = "param";
                    var nameMatch = Regex.Match(text, @"name=""(\w+)""");
                    var contentMatch = Regex.Match(text, @">([^<]*)");
                    if (nameMatch.Success && contentMatch.Success)
                    {
                        var content = contentMatch.Groups[1].Value.Trim();
                        if (content.Length > 0)
                            paramDict[nameMatch.Groups[1].Value] = content;
                    }
                    continue;
                }
                if (text.StartsWith("<returns>"))
                {
                    current = "returns";
                    text = Regex.Replace(text, @"<returns>\s*", "");
                    if (text.EndsWith("</returns>"))
                        returns = text[..^"</returns>".Length].Trim();
                    else
                        returns = text;
                    continue;
                }
                if (text.StartsWith("</returns>"))
                {
                    current = "none";
                    continue;
                }
                if (text.StartsWith("<remarks>") || text.StartsWith("<value>"))
                {
                    current = "skip";
                    continue;
                }
                if (text.StartsWith("</remarks>") || text.StartsWith("</value>"))
                {
                    current = "none";
                    continue;
                }

                // Clean XML tags
                text = Regex.Replace(text, @"<[^>]+>", "").Trim();

                if (current == "summary" && text.Length > 0)
                {
                    if (summary.Length > 0) summary.Append(' ');
                    summary.Append(text);
                }
                else if (current == "returns" && text.Length > 0)
                {
                    returns = (returns ?? "") + " " + text;
                }
            }

            var summaryStr = summary.ToString().Trim();
            if (summaryStr.Length == 0) summaryStr = null;
            if (returns != null) returns = returns.Trim();
            if (returns?.Length == 0) returns = null;

            return (summaryStr, paramDict, returns);
        }
    }

    // ==================== JSDoc Generation ====================

    static string Indent(string text, string indent)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return indent + text.Replace("\n", "\n" + indent);
    }

    static string FormatJsDoc(string? summary, string? seePath, int? seeLine,
        Dictionary<string, string>? paramDocs = null, string? returnsDoc = null)
    {
        if (summary != null || (paramDocs != null && paramDocs.Count > 0) || returnsDoc != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/**");
            if (summary != null)
            {
                sb.Append(" * ");
                sb.AppendLine(summary);
            }
            if (paramDocs != null)
            {
                foreach (var (name, desc) in paramDocs)
                {
                    sb.Append($" * @param {ToCamelCase(name)} {desc}");
                    sb.AppendLine();
                }
            }
            if (returnsDoc != null)
            {
                sb.Append($" * @returns {returnsDoc}");
                sb.AppendLine();
            }
            sb.Append(" */");
            return sb.ToString();
        }
        else if (seePath != null && seeLine != null)
        {
            return $"/** @see {seePath}:{seeLine} */";
        }
        return "";
    }

    static string FormatSingleLineJsDoc(string? summary, string? seePath, int? seeLine)
    {
        if (summary != null)
            return $"/** {summary} */";
        if (seePath != null && seeLine != null)
            return $"/** @see {seePath}:{seeLine} */";
        return "";
    }

    // ==================== Emitter ====================

    class TsEmitter
    {
        readonly XmlDocReader _docReader;
        readonly Assembly _assembly;
        readonly Dictionary<string, Type> _resolvedTypes = new();

        public TsEmitter(XmlDocReader docReader, Assembly assembly)
        {
            _docReader = docReader;
            _assembly = assembly;
        }

        public string Generate()
        {
            var sb = new StringBuilder();

            EmitHeader(sb);
            EmitGlobalFunctions(sb);
            sb.AppendLine();
            sb.AppendLine("// ==================== 全局对象 ====================");
            sb.AppendLine();
            EmitGlobalObjects(sb);
            sb.AppendLine();
            sb.AppendLine("// ==================== 类型定义 ====================");
            sb.AppendLine();
            EmitInterfaces(sb);
            EmitHostTypes(sb);

            return sb.ToString();
        }

        void EmitHeader(StringBuilder sb)
        {
            sb.AppendLine("// AUTO-GENERATED FILE. DO NOT EDIT.");
            sb.AppendLine("// Generated by Tools/TypeGen");
            sb.AppendLine();
            sb.AppendLine("/**");
            sb.AppendLine(" * BetterGenshinImpact JavaScript API 类型声明文件");
            sb.AppendLine(" * 此文件定义了在脚本中可用的所有全局对象和方法");
            sb.AppendLine(" * 注意: ClearScript 绑定后类型和方法的首字母会变为小写");
            sb.AppendLine(" */");
        }

        void EmitGlobalFunctions(StringBuilder sb)
        {
            sb.AppendLine("// ==================== 全局方法 ====================");
            sb.AppendLine();

            Type? globalMethodType = null;
            try
            {
                globalMethodType = _assembly.GetType(
                    "BetterGenshinImpact.Core.Script.Dependence.GlobalMethod");
            }
            catch { }

            if (globalMethodType == null)
            {
                Console.Error.WriteLine("Warning: GlobalMethod type not found");
                return;
            }

            foreach (var funcDef in GlobalFunctions)
            {
                MethodInfo? method = null;
                try
                {
                    method = globalMethodType.GetMethod(funcDef.CSharpMethodName,
                        BindingFlags.Public | BindingFlags.Static);
                }
                catch { }

                if (method == null)
                {
                    Console.Error.WriteLine($"Warning: GlobalMethod.{funcDef.CSharpMethodName} not found");
                    continue;
                }

                try
                {
                    var jsName = funcDef.JsName;
                    var parameters = method.GetParameters();
                    var returnType = ToTsType(method.ReturnType);
                    var (summary, seePath, seeLine, paramDocs, returnsDoc) =
                        _docReader.GetDocForMember(globalMethodType, funcDef.CSharpMethodName);

                    var jsDoc = FormatJsDoc(summary, seePath, seeLine, paramDocs, returnsDoc);
                    if (jsDoc.Length > 0) sb.AppendLine(jsDoc);

                    var paramStr = FormatParameters(parameters);
                    sb.AppendLine($"declare function {jsName}({paramStr}): {returnType};");
                    sb.AppendLine();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Failed to emit global function {funcDef.JsName}: {ex.Message}");
                    sb.AppendLine($"// Error generating {funcDef.JsName}: {ex.Message}");
                    sb.AppendLine();
                }
            }
        }

        void EmitGlobalObjects(StringBuilder sb)
        {
            foreach (var hostDef in HostObjects)
            {
                var type = ResolveType(hostDef.CSharpTypeName);
                if (type == null)
                {
                    Console.Error.WriteLine($"Warning: Type not found: {hostDef.CSharpTypeName}");
                    continue;
                }

                try
                {
                    EmitSingleGlobalObject(sb, type, hostDef);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Failed to emit {hostDef.JsName}: {ex.Message}");
                    sb.AppendLine($"// Error generating {hostDef.JsName}: {ex.Message}");
                    sb.AppendLine();
                }
            }
        }

        void EmitSingleGlobalObject(StringBuilder sb, Type type, HostObjectDef hostDef)
        {
            var (typeSummary, _, _) = _docReader.GetDocForType(type);
            if (typeSummary != null)
            {
                sb.AppendLine($"/**");
                sb.AppendLine($" * {typeSummary}");
                sb.AppendLine($" */");
            }

            sb.AppendLine($"declare const {hostDef.JsName}: {{");

            // Emit instance properties
            EmitInstanceProperties(sb, type, hostDef.JsName, isObject: true);

            // Emit instance methods
            EmitInstanceMethods(sb, type, hostDef.JsName, isObject: true);

            // Emit aliases
            EmitObjectAliases(sb, type, hostDef.JsName);

            sb.AppendLine("};");
            sb.AppendLine();
        }

        void EmitInstanceProperties(StringBuilder sb, Type type, string parentName, bool isObject = false)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => !ShouldSkipMember(p, type))
                .ToList();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !ShouldSkipMember(f, type))
                .ToList();

            // Also include inherited ObservableObject properties
            if (type.BaseType?.FullName?.Contains("ObservableObject") == true)
            {
                var propChanged = type.GetProperty("PropertyChanged",
                    BindingFlags.Public | BindingFlags.Instance);
                if (propChanged != null && !props.Any(p => p.Name == "PropertyChanged"))
                    props.Insert(0, propChanged);

                var propChanging = type.GetProperty("PropertyChanging",
                    BindingFlags.Public | BindingFlags.Instance);
                if (propChanging != null && !props.Any(p => p.Name == "PropertyChanging"))
                    props.Insert(1, propChanging);
            }

            foreach (var prop in props)
            {
                var isReadOnly = IsPropertyReadOnly(prop);
                var camelName = ToCamelCase(prop.Name);
                var tsType = ToTsType(prop.PropertyType);

                // Handle nullable reference types
                if (!prop.PropertyType.IsValueType && prop.PropertyType.Name != "String" &&
                    prop.GetCustomAttribute<System.Runtime.CompilerServices.NullableAttribute>()?.NullableFlags
                        .FirstOrDefault() == 2)
                    tsType += " | null";

                // Check for nullability from NullabilityInfoContext (simple heuristic)
                if (tsType != "any" && !tsType.Contains("| null") &&
                    !prop.PropertyType.IsValueType && !prop.Name.EndsWith("Config"))
                {
                    // Don't auto-add nullability, rely on explicit ? or source analysis
                }

                var (summary, seePath, seeLine, _, _) = _docReader.GetDocForMember(type, prop.Name);
                var jsDoc = FormatSingleLineJsDoc(summary, seePath, seeLine);
                if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));

                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  {readonlyStr}{camelName}: {tsType};");
                sb.AppendLine();
            }

            foreach (var field in fields)
            {
                var isReadOnly = IsFieldReadOnly(field);
                var camelName = ToCamelCase(field.Name);
                var tsType = ToTsType(field.FieldType);

                // Check nullable reference types for fields
                if (!field.FieldType.IsValueType && field.FieldType.IsClass)
                {
                    // Check if field can be null
                    if (field.FieldType.Name != "String")
                    {
                        var declaringType = field.DeclaringType;
                        var fieldInfo = declaringType?.GetField(field.Name,
                            BindingFlags.Public | BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            // Simple heuristic: if field is initialized to null or default
                            // We don't add null automatically
                        }
                    }
                }

                var (summary, seePath, seeLine, _, _) = _docReader.GetDocForMember(type, field.Name);
                var jsDoc = FormatSingleLineJsDoc(summary, seePath, seeLine);
                if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));

                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  {readonlyStr}{camelName}: {tsType};");
                sb.AppendLine();
            }
        }

        void EmitInstanceMethods(StringBuilder sb, Type type, string parentName, bool isObject = false)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !ShouldSkipMember(m, type))
                .ToList();

            // Group by name for overloads
            var groups = methods.GroupBy(m => m.Name);

            foreach (var group in groups)
            {
                foreach (var method in group)
                {
                    var camelName = ToCamelCase(method.Name);
                    var parameters = method.GetParameters();
                    var returnType = ToTsType(method.ReturnType);
                    var (summary, seePath, seeLine, paramDocs, returnsDoc) =
                        _docReader.GetDocForMember(type, method.Name);

                    var jsDoc = FormatJsDoc(summary, seePath, seeLine, paramDocs, returnsDoc);
                    if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));

                    var paramStr = FormatParameters(parameters);
                    sb.AppendLine($"  {camelName}({paramStr}): {returnType};");
                    sb.AppendLine();
                }
            }
        }

        void EmitStaticMembers(StringBuilder sb, Type type, string className)
        {
            var staticProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => !ShouldSkipMember(p, type))
                .ToList();

            var staticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => !ShouldSkipMember(f, type))
                .ToList();

            var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !ShouldSkipMember(m, type))
                .ToList();

            foreach (var prop in staticProps)
            {
                var camelName = ToCamelCase(prop.Name);
                var tsType = ToTsType(prop.PropertyType);
                var isReadOnly = IsPropertyReadOnly(prop);

                var (summary, seePath, seeLine, _, _) = _docReader.GetDocForMember(type, prop.Name);
                var jsDoc = FormatJsDoc(summary, seePath, seeLine);
                if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));

                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  static {readonlyStr}{camelName}: {tsType};");
                sb.AppendLine();
            }

            foreach (var field in staticFields)
            {
                var camelName = ToCamelCase(field.Name);
                var tsType = ToTsType(field.FieldType);
                var isReadOnly = IsFieldReadOnly(field);

                var (summary, seePath, seeLine, _, _) = _docReader.GetDocForMember(type, field.Name);
                var jsDoc = FormatJsDoc(summary, seePath, seeLine);
                if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));

                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  static {readonlyStr}{camelName}: {tsType};");
                sb.AppendLine();
            }

            // Group by name for overloads
            var groups = staticMethods.GroupBy(m => m.Name);
            foreach (var group in groups)
            {
                foreach (var method in group)
                {
                    var camelName = ToCamelCase(method.Name);
                    var parameters = method.GetParameters();
                    var returnType = ToTsType(method.ReturnType);
                    var (summary, seePath, seeLine, paramDocs, returnsDoc) =
                        _docReader.GetDocForMember(type, method.Name);

                    var jsDoc = FormatJsDoc(summary, seePath, seeLine, paramDocs, returnsDoc);
                    if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));

                    var paramStr = FormatParameters(parameters);
                    sb.AppendLine($"  static {camelName}({paramStr}): {returnType};");
                    sb.AppendLine();
                }
            }
        }

        void EmitConstructors(StringBuilder sb, Type type)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (ctors.Length == 0)
            {
                sb.AppendLine("  constructor();");
                sb.AppendLine();
                return;
            }

            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();
                var paramStr = FormatParameters(parameters);
                sb.AppendLine($"  constructor({paramStr});");
            }
            sb.AppendLine();
        }

        void EmitObjectAliases(StringBuilder sb, Type type, string objectName)
        {
            sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");

            // Instance properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => !ShouldSkipMember(p, type));

            // Include ObservableObject properties
            if (type.BaseType?.FullName?.Contains("ObservableObject") == true)
            {
                var baseProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.DeclaringType?.FullName?.Contains("ObservableObject") == true);
                props = props.Concat(baseProps);
            }

            foreach (var prop in props)
            {
                var alias = PascalToAlias(prop.Name);
                var isReadOnly = IsPropertyReadOnly(prop);
                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  {readonlyStr}{alias}: typeof {objectName}.{ToCamelCase(prop.Name)};");
            }

            // Instance fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !ShouldSkipMember(f, type));
            foreach (var field in fields)
            {
                var alias = PascalToAlias(field.Name);
                var isReadOnly = IsFieldReadOnly(field);
                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  {readonlyStr}{alias}: typeof {objectName}.{ToCamelCase(field.Name)};");
            }

            // Instance methods (deduplicate by name for overloads)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !ShouldSkipMember(m, type))
                .GroupBy(m => m.Name)
                .Select(g => g.First());
            foreach (var method in methods)
            {
                var alias = PascalToAlias(method.Name);
                sb.AppendLine($"  {alias}: typeof {objectName}.{ToCamelCase(method.Name)};");
            }

            sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
        }

        void EmitClassAliases(StringBuilder sb, Type type, string className)
        {
            sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");

            // Instance properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => !ShouldSkipMember(p, type));

            // Include ObservableObject properties
            if (type.BaseType?.FullName?.Contains("ObservableObject") == true)
            {
                var baseProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.DeclaringType?.FullName?.Contains("ObservableObject") == true);
                props = props.Concat(baseProps);
            }

            foreach (var prop in props)
            {
                var alias = PascalToAlias(prop.Name);
                var isReadOnly = IsPropertyReadOnly(prop);
                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  declare {readonlyStr}{alias}: typeof {className}.prototype.{ToCamelCase(prop.Name)};");
            }

            // Instance fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !ShouldSkipMember(f, type));
            foreach (var field in fields)
            {
                var alias = PascalToAlias(field.Name);
                var isReadOnly = IsFieldReadOnly(field);
                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  declare {readonlyStr}{alias}: typeof {className}.prototype.{ToCamelCase(field.Name)};");
            }

            // Instance methods (deduplicate by name for overloads)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !ShouldSkipMember(m, type))
                .GroupBy(m => m.Name)
                .Select(g => g.First());
            foreach (var method in methods)
            {
                var alias = PascalToAlias(method.Name);
                sb.AppendLine($"  declare {alias}: typeof {className}.prototype.{ToCamelCase(method.Name)};");
            }

            // Static properties
            var staticProps = type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(p => !ShouldSkipMember(p, type))
                .GroupBy(p => p.Name)
                .Select(g => g.First());
            foreach (var prop in staticProps)
            {
                var alias = PascalToAlias(prop.Name);
                var isReadOnly = IsPropertyReadOnly(prop);
                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  declare static {readonlyStr}{alias}: typeof {className}.{ToCamelCase(prop.Name)};");
            }

            // Static fields
            var staticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => !ShouldSkipMember(f, type))
                .GroupBy(f => f.Name)
                .Select(g => g.First());
            foreach (var field in staticFields)
            {
                var alias = PascalToAlias(field.Name);
                var isReadOnly = IsFieldReadOnly(field);
                var readonlyStr = isReadOnly ? "readonly " : "";
                sb.AppendLine($"  declare static {readonlyStr}{alias}: typeof {className}.{ToCamelCase(field.Name)};");
            }

            // Static methods (deduplicate by name for overloads)
            var staticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !ShouldSkipMember(m, type))
                .GroupBy(m => m.Name)
                .Select(g => g.First());
            foreach (var method in staticMethods)
            {
                var alias = PascalToAlias(method.Name);
                sb.AppendLine($"  declare static {alias}: typeof {className}.{ToCamelCase(method.Name)};");
            }

            // Nested types
            var nestedTypes = type.GetNestedTypes(BindingFlags.Public);
            foreach (var nested in nestedTypes)
            {
                var alias = PascalToAlias(nested.Name);
                sb.AppendLine($"  declare static readonly {alias}: typeof {nested.Name};");
            }

            sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
        }

        void EmitInterfaces(StringBuilder sb)
        {
            // Emit nested type interfaces (e.g., Http.HttpReponse)
            var httpType = ResolveType("BetterGenshinImpact.Core.Script.Dependence.Http");
            if (httpType != null)
            {
                var nestedTypes = httpType.GetNestedTypes(BindingFlags.Public);
                foreach (var nested in nestedTypes)
                {
                    sb.AppendLine($"/**");
                    sb.AppendLine($" * HTTP 响应");
                    sb.AppendLine($" */");
                    sb.AppendLine($"interface {nested.Name} {{");
                    EmitInterfaceMembers(sb, nested);
                    sb.AppendLine("}");
                    sb.AppendLine();
                }
            }

            // Emit FightFinishDetectConfig as a standalone class (nested in AutoFightParam)
            var autoFightType = ResolveType("BetterGenshinImpact.GameTask.AutoFight.AutoFightParam");
            if (autoFightType != null)
            {
                var nestedTypes = autoFightType.GetNestedTypes(BindingFlags.Public);
                foreach (var nested in nestedTypes)
                {
                    var (typeSummary, _, _) = _docReader.GetDocForType(nested);
                    if (typeSummary != null)
                    {
                        sb.AppendLine("/**");
                        sb.AppendLine($" * {typeSummary}");
                        sb.AppendLine(" */");
                    }
                    sb.AppendLine($"declare class {nested.Name} {{");
                    EmitInstanceProperties(sb, nested, nested.Name);
                    EmitInstanceMethods(sb, nested, nested.Name);
                    EmitClassAliases(sb, nested, nested.Name);
                    sb.AppendLine("}");
                    sb.AppendLine();
                }
            }
        }

        void EmitInterfaceMembers(StringBuilder sb, Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => !ShouldSkipMember(p, type));

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !ShouldSkipMember(f, type));

            foreach (var prop in props)
            {
                var camelName = ToCamelCase(prop.Name);
                var tsType = ToTsType(prop.PropertyType);
                var (summary, seePath, seeLine, _, _) = _docReader.GetDocForMember(type, prop.Name);
                var jsDoc = FormatSingleLineJsDoc(summary, seePath, seeLine);
                if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));
                sb.AppendLine($"  {camelName}: {tsType};");
                sb.AppendLine();
            }

            foreach (var field in fields)
            {
                var camelName = ToCamelCase(field.Name);
                var tsType = ToTsType(field.FieldType);
                var (summary, seePath, seeLine, _, _) = _docReader.GetDocForMember(type, field.Name);
                var jsDoc = FormatSingleLineJsDoc(summary, seePath, seeLine);
                if (jsDoc.Length > 0) sb.AppendLine(Indent(jsDoc, "  "));
                sb.AppendLine($"  {camelName}: {tsType};");
                sb.AppendLine();
            }
        }

        void EmitHostTypes(StringBuilder sb)
        {
            foreach (var typeDef in HostTypes)
            {
                // Skip types with hardcoded declarations
                if (HardcodedTypes.Contains(typeDef.CSharpTypeName))
                {
                    EmitHardcodedType(sb, typeDef);
                    continue;
                }

                var type = ResolveType(typeDef.CSharpTypeName);
                if (type == null)
                {
                    Console.Error.WriteLine($"Warning: Type not found: {typeDef.CSharpTypeName}");
                    continue;
                }

                try
                {
                    EmitSingleHostType(sb, type, typeDef);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Skipping {typeDef.JsName}: {ex.Message}");
                }
            }
        }

        void EmitSingleHostType(StringBuilder sb, Type type, HostTypeDef typeDef)
        {
            var (typeSummary, _, _) = _docReader.GetDocForType(type);
            if (typeSummary != null)
            {
                sb.AppendLine("/**");
                sb.AppendLine($" * {typeSummary}");
                sb.AppendLine(" */");
            }

            var extendsStr = typeDef.Extends != null ? $" extends {typeDef.Extends}" : "";
            sb.AppendLine($"declare class {typeDef.JsName}{extendsStr} {{");

            EmitInstanceProperties(sb, type, typeDef.JsName);
            EmitStaticMembers(sb, type, typeDef.JsName);
            EmitConstructors(sb, type);
            EmitInstanceMethods(sb, type, typeDef.JsName);
            EmitClassAliases(sb, type, typeDef.JsName);

            sb.AppendLine("}");
            sb.AppendLine();
        }

        void EmitHardcodedType(StringBuilder sb, HostTypeDef typeDef)
        {
            switch (typeDef.CSharpTypeName)
            {
                case "System.Threading.CancellationTokenSource":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * 取消令牌源");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class CancellationTokenSource {");
                    sb.AppendLine("  /** 取消令牌 */");
                    sb.AppendLine("  readonly token: CancellationToken;");
                    sb.AppendLine("  /** 是否已请求取消 */");
                    sb.AppendLine("  readonly isCancellationRequested: boolean;");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine();
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 创建关联的令牌源");
                    sb.AppendLine("   * @param tokens 取消令牌列表");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static createLinkedTokenSource(...tokens: CancellationToken[]): CancellationTokenSource;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 取消操作");
                    sb.AppendLine("   */");
                    sb.AppendLine("  cancel(): void;");
                    sb.AppendLine("  cancel(throwOnFirstException: boolean): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 异步取消操作");
                    sb.AppendLine("   */");
                    sb.AppendLine("  cancelAsync(): Promise<void>;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在指定延迟后取消");
                    sb.AppendLine("   * @param millisecondsDelay 延迟时间（毫秒）");
                    sb.AppendLine("   */");
                    sb.AppendLine("  cancelAfter(millisecondsDelay: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 尝试重置令牌源");
                    sb.AppendLine("   * @returns 是否成功");
                    sb.AppendLine("   */");
                    sb.AppendLine("  tryReset(): boolean;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 释放资源");
                    sb.AppendLine("   */");
                    sb.AppendLine("  dispose(): void;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare readonly Token: typeof CancellationTokenSource.prototype.token;");
                    sb.AppendLine();
                    sb.AppendLine("  declare readonly IsCancellationRequested: typeof CancellationTokenSource.prototype.isCancellationRequested;");
                    sb.AppendLine("  declare static CreateLinkedTokenSource: typeof CancellationTokenSource.createLinkedTokenSource;");
                    sb.AppendLine("  declare Cancel: typeof CancellationTokenSource.prototype.cancel;");
                    sb.AppendLine("  declare CancelAsync: typeof CancellationTokenSource.prototype.cancelAsync;");
                    sb.AppendLine("  declare CancelAfter: typeof CancellationTokenSource.prototype.cancelAfter;");
                    sb.AppendLine("  declare TryReset: typeof CancellationTokenSource.prototype.tryReset;");
                    sb.AppendLine("  declare Dispose: typeof CancellationTokenSource.prototype.dispose;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "System.Threading.CancellationToken":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * 取消令牌");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class CancellationToken {");
                    sb.AppendLine("  /** 是否已请求取消 */");
                    sb.AppendLine("  readonly isCancellationRequested: boolean;");
                    sb.AppendLine("  /** 是否可以被取消 */");
                    sb.AppendLine("  readonly canBeCanceled: boolean;");
                    sb.AppendLine("  /** 等待句柄 */");
                    sb.AppendLine("  readonly waitHandle: any;");
                    sb.AppendLine();
                    sb.AppendLine("  static readonly none: any;");
                    sb.AppendLine();
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 注册取消回调");
                    sb.AppendLine("   * @param callback 回调函数");
                    sb.AppendLine("   */");
                    sb.AppendLine("  register(callback: Function): any;");
                    sb.AppendLine("  register(callback: Function, state: any): any;");
                    sb.AppendLine("  register(callback: Function, state: any, useSynchronizationContext: boolean): any;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 注册取消回调（非安全）");
                    sb.AppendLine("   * @param callback 回调函数");
                    sb.AppendLine("   */");
                    sb.AppendLine("  unsafeRegister(callback: Function): any;");
                    sb.AppendLine("  unsafeRegister(callback: Function, state: any): any;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 如果已请求取消则抛出异常");
                    sb.AppendLine("   */");
                    sb.AppendLine("  throwIfCancellationRequested(): void;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare readonly IsCancellationRequested: typeof CancellationToken.prototype.isCancellationRequested;");
                    sb.AppendLine();
                    sb.AppendLine("  declare readonly CanBeCanceled: typeof CancellationToken.prototype.canBeCanceled;");
                    sb.AppendLine("  declare readonly WaitHandle: typeof CancellationToken.prototype.waitHandle;");
                    sb.AppendLine("  declare static readonly None: typeof CancellationToken.none;");
                    sb.AppendLine("  declare Register: typeof CancellationToken.prototype.register;");
                    sb.AppendLine("  declare UnsafeRegister: typeof CancellationToken.prototype.unsafeRegister;");
                    sb.AppendLine("  declare ThrowIfCancellationRequested: typeof CancellationToken.prototype.throwIfCancellationRequested;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "OpenCvSharp.Mat":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * OpenCV 矩阵");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class Mat {");
                    sb.AppendLine("  /** 矩阵宽度 */");
                    sb.AppendLine("  readonly width: number;");
                    sb.AppendLine("  /** 矩阵高度 */");
                    sb.AppendLine("  readonly height: number;");
                    sb.AppendLine("  /** 行数 */");
                    sb.AppendLine("  readonly rows: number;");
                    sb.AppendLine("  /** 列数 */");
                    sb.AppendLine("  readonly cols: number;");
                    sb.AppendLine("  /** 通道数 */");
                    sb.AppendLine("  readonly channels: number;");
                    sb.AppendLine("  /** 元素类型 */");
                    sb.AppendLine("  readonly type: number;");
                    sb.AppendLine("  /** 是否为空 */");
                    sb.AppendLine("  readonly empty: boolean;");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine();
                    sb.AppendLine("  /** 克隆矩阵 */");
                    sb.AppendLine("  clone(): Mat;");
                    sb.AppendLine("  /** 释放资源 */");
                    sb.AppendLine("  dispose(): void;");
                    sb.AppendLine("  /** 保存图像 */");
                    sb.AppendLine("  saveImage(filename: string): boolean;");
                    sb.AppendLine("  /** 调整大小 */");
                    sb.AppendLine("  resize(dsize: any, interpolation?: number): Mat;");
                    sb.AppendLine("  /** 转换颜色空间 */");
                    sb.AppendLine("  convertColor(code: number, dstCn?: number): Mat;");
                    sb.AppendLine("  /** 转为灰度图 */");
                    sb.AppendLine("  cvtColor(code: number, dstCn?: number): Mat;");
                    sb.AppendLine("  /** 转为字节数组 */");
                    sb.AppendLine("  toBytes(): Uint8Array;");
                    sb.AppendLine("  /** 从字节数组创建 */");
                    sb.AppendLine("  static fromBytes(data: Uint8Array): Mat;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare readonly Width: typeof Mat.prototype.width;");
                    sb.AppendLine("  declare readonly Height: typeof Mat.prototype.height;");
                    sb.AppendLine("  declare readonly Rows: typeof Mat.prototype.rows;");
                    sb.AppendLine("  declare readonly Cols: typeof Mat.prototype.cols;");
                    sb.AppendLine("  declare readonly Channels: typeof Mat.prototype.channels;");
                    sb.AppendLine("  declare readonly Type: typeof Mat.prototype.type;");
                    sb.AppendLine("  declare readonly Empty: typeof Mat.prototype.empty;");
                    sb.AppendLine("  declare Clone: typeof Mat.prototype.clone;");
                    sb.AppendLine("  declare Dispose: typeof Mat.prototype.dispose;");
                    sb.AppendLine("  declare SaveImage: typeof Mat.prototype.saveImage;");
                    sb.AppendLine("  declare Resize: typeof Mat.prototype.resize;");
                    sb.AppendLine("  declare ConvertColor: typeof Mat.prototype.convertColor;");
                    sb.AppendLine("  declare CvtColor: typeof Mat.prototype.cvtColor;");
                    sb.AppendLine("  declare ToBytes: typeof Mat.prototype.toBytes;");
                    sb.AppendLine("  declare static FromBytes: typeof Mat.fromBytes;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "OpenCvSharp.Point2f":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * 2D 浮点坐标");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class Point2f {");
                    sb.AppendLine("  /** X 坐标 */");
                    sb.AppendLine("  x: number;");
                    sb.AppendLine("  /** Y 坐标 */");
                    sb.AppendLine("  y: number;");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine("  constructor(x: number, y: number);");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare X: typeof Point2f.prototype.x;");
                    sb.AppendLine("  declare Y: typeof Point2f.prototype.y;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "OpenCvSharp.Rect":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * 矩形区域");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class Rect {");
                    sb.AppendLine("  x: number;");
                    sb.AppendLine("  y: number;");
                    sb.AppendLine("  width: number;");
                    sb.AppendLine("  height: number;");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine("  constructor(x: number, y: number, width: number, height: number);");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare X: typeof Rect.prototype.x;");
                    sb.AppendLine("  declare Y: typeof Rect.prototype.y;");
                    sb.AppendLine("  declare Width: typeof Rect.prototype.width;");
                    sb.AppendLine("  declare Height: typeof Rect.prototype.height;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "OpenCvSharp.Scalar":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * 4 元素向量 (B, G, R, A)");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class Scalar {");
                    sb.AppendLine("  v0: number;");
                    sb.AppendLine("  v1: number;");
                    sb.AppendLine("  v2: number;");
                    sb.AppendLine("  v3: number;");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine("  constructor(v0: number, v1: number, v2: number, v3: number);");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare V0: typeof Scalar.prototype.v0;");
                    sb.AppendLine("  declare V1: typeof Scalar.prototype.v1;");
                    sb.AppendLine("  declare V2: typeof Scalar.prototype.v2;");
                    sb.AppendLine("  declare V3: typeof Scalar.prototype.v3;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "System.Drawing.Color":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * 颜色 (ARGB)");
                    sb.AppendLine(" */");
                    sb.AppendLine("declare class Color {");
                    sb.AppendLine("  readonly a: number;");
                    sb.AppendLine("  readonly r: number;");
                    sb.AppendLine("  readonly g: number;");
                    sb.AppendLine("  readonly b: number;");
                    sb.AppendLine();
                    sb.AppendLine("  /** 从 ARGB 值创建颜色 */");
                    sb.AppendLine("  static fromArgb(alpha: number, red: number, green: number, blue: number): Color;");
                    sb.AppendLine("  static fromArgb(red: number, green: number, blue: number): Color;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare readonly A: typeof Color.prototype.a;");
                    sb.AppendLine("  declare readonly R: typeof Color.prototype.r;");
                    sb.AppendLine("  declare readonly G: typeof Color.prototype.g;");
                    sb.AppendLine("  declare readonly B: typeof Color.prototype.b;");
                    sb.AppendLine("  declare static FromArgb: typeof Color.fromArgb;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "BetterGenshinImpact.GameTask.Model.Area.Region":
                    sb.AppendLine("declare class Region {");
                    sb.AppendLine("  /** X 坐标 */");
                    sb.AppendLine("  x: number;");
                    sb.AppendLine("  /** Y 坐标 */");
                    sb.AppendLine("  y: number;");
                    sb.AppendLine("  /** 宽度 */");
                    sb.AppendLine("  width: number;");
                    sb.AppendLine("  /** 高度 */");
                    sb.AppendLine("  height: number;");
                    sb.AppendLine("  /** 上边界 (Y) */");
                    sb.AppendLine("  top: number;");
                    sb.AppendLine("  /** 下边界 (Y + Height) */");
                    sb.AppendLine("  bottom: number;");
                    sb.AppendLine("  /** 左边界 (X) */");
                    sb.AppendLine("  left: number;");
                    sb.AppendLine("  /** 右边界 (X + Width) */");
                    sb.AppendLine("  right: number;");
                    sb.AppendLine("  /** OCR 识别文本 */");
                    sb.AppendLine("  text: string;");
                    sb.AppendLine("  /** 父区域 */");
                    sb.AppendLine("  readonly prev: Region | null;");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 创建区域");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   * @param width 宽度");
                    sb.AppendLine("   * @param height 高度");
                    sb.AppendLine("   * @param owner 父区域");
                    sb.AppendLine("   * @param converter 坐标转换器");
                    sb.AppendLine("   * @param drawContent 绘制内容");
                    sb.AppendLine("   */");
                    sb.AppendLine("  constructor(x: number, y: number, width: number, height: number, owner?: Region | null, converter?: any, drawContent?: any);");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 从 Rect 创建区域");
                    sb.AppendLine("   * @param rect 矩形");
                    sb.AppendLine("   * @param owner 父区域");
                    sb.AppendLine("   * @param converter 坐标转换器");
                    sb.AppendLine("   */");
                    sb.AppendLine("  constructor(rect: Rect, owner?: Region | null, converter?: any);");
                    sb.AppendLine();
                    sb.AppendLine("  /** 点击区域中心 */");
                    sb.AppendLine("  click(): Region;");
                    sb.AppendLine("  /** 双击区域中心 */");
                    sb.AppendLine("  doubleClick(): Region;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 点击指定坐标");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  clickTo(x: number, y: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 点击矩形中心");
                    sb.AppendLine("   * @param x 矩形 X 坐标");
                    sb.AppendLine("   * @param y 矩形 Y 坐标");
                    sb.AppendLine("   * @param w 矩形宽度");
                    sb.AppendLine("   * @param h 矩形高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  clickTo(x: number, y: number, w: number, h: number): void;");
                    sb.AppendLine("  /** 移动鼠标到区域中心 */");
                    sb.AppendLine("  move(): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 移动鼠标到指定坐标");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  moveTo(x: number, y: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 移动鼠标到矩形中心");
                    sb.AppendLine("   * @param x 矩形 X 坐标");
                    sb.AppendLine("   * @param y 矩形 Y 坐标");
                    sb.AppendLine("   * @param w 矩形宽度");
                    sb.AppendLine("   * @param h 矩形高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  moveTo(x: number, y: number, w: number, h: number): void;");
                    sb.AppendLine("  /** 后台点击区域中心 */");
                    sb.AppendLine("  backgroundClick(): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在覆盖层绘制区域自身");
                    sb.AppendLine("   * @param name 名称");
                    sb.AppendLine("   * @param pen 画笔");
                    sb.AppendLine("   */");
                    sb.AppendLine("  drawSelf(name: string, pen?: any): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在覆盖层绘制矩形");
                    sb.AppendLine("   * @param rect 矩形区域");
                    sb.AppendLine("   * @param name 名称");
                    sb.AppendLine("   * @param pen 画笔");
                    sb.AppendLine("   */");
                    sb.AppendLine("  drawRect(rect: Rect, name: string, pen?: any): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在覆盖层绘制线段");
                    sb.AppendLine("   * @param x1 起点 X");
                    sb.AppendLine("   * @param y1 起点 Y");
                    sb.AppendLine("   * @param x2 终点 X");
                    sb.AppendLine("   * @param y2 终点 Y");
                    sb.AppendLine("   * @param name 名称");
                    sb.AppendLine("   * @param pen 画笔");
                    sb.AppendLine("   */");
                    sb.AppendLine("  drawLine(x1: number, y1: number, x2: number, y2: number, name: string, pen?: any): void;");
                    sb.AppendLine("  /** 转换为 Rect */");
                    sb.AppendLine("  toRect(): Rect;");
                    sb.AppendLine("  /** 转换为 ImageRegion */");
                    sb.AppendLine("  toImageRegion(): ImageRegion;");
                    sb.AppendLine("  /** 检查区域是否为空 */");
                    sb.AppendLine("  isEmpty(): boolean;");
                    sb.AppendLine("  /** 检查区域是否存在 */");
                    sb.AppendLine("  isExist(): boolean;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 派生子区域");
                    sb.AppendLine("   * @param x 相对 X 坐标");
                    sb.AppendLine("   * @param y 相对 Y 坐标");
                    sb.AppendLine("   * @param w 宽度");
                    sb.AppendLine("   * @param h 高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  derive(x: number, y: number, w: number, h: number): Region;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 从 Rect 派生子区域");
                    sb.AppendLine("   * @param rect 矩形区域");
                    sb.AppendLine("   */");
                    sb.AppendLine("  derive(rect: Rect): Region;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 将坐标转换为游戏截图区域坐标");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   * @returns 转换后的坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  convertPositionToGameCaptureRegion(x: number, y: number): any;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 将坐标转换为桌面区域坐标");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   * @returns 转换后的坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  convertPositionToDesktopRegion(x: number, y: number): any;");
                    sb.AppendLine("  /** 释放资源 */");
                    sb.AppendLine("  dispose(): void;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare X: typeof Region.prototype.x;");
                    sb.AppendLine("  declare Y: typeof Region.prototype.y;");
                    sb.AppendLine("  declare Width: typeof Region.prototype.width;");
                    sb.AppendLine("  declare Height: typeof Region.prototype.height;");
                    sb.AppendLine("  declare Top: typeof Region.prototype.top;");
                    sb.AppendLine("  declare Bottom: typeof Region.prototype.bottom;");
                    sb.AppendLine("  declare Left: typeof Region.prototype.left;");
                    sb.AppendLine("  declare Right: typeof Region.prototype.right;");
                    sb.AppendLine("  declare Text: typeof Region.prototype.text;");
                    sb.AppendLine("  declare Prev: typeof Region.prototype.prev;");
                    sb.AppendLine("  declare Click: typeof Region.prototype.click;");
                    sb.AppendLine("  declare DoubleClick: typeof Region.prototype.doubleClick;");
                    sb.AppendLine("  declare ClickTo: typeof Region.prototype.clickTo;");
                    sb.AppendLine("  declare Move: typeof Region.prototype.move;");
                    sb.AppendLine("  declare MoveTo: typeof Region.prototype.moveTo;");
                    sb.AppendLine("  declare BackgroundClick: typeof Region.prototype.backgroundClick;");
                    sb.AppendLine("  declare DrawSelf: typeof Region.prototype.drawSelf;");
                    sb.AppendLine("  declare DrawRect: typeof Region.prototype.drawRect;");
                    sb.AppendLine("  declare DrawLine: typeof Region.prototype.drawLine;");
                    sb.AppendLine("  declare ToRect: typeof Region.prototype.toRect;");
                    sb.AppendLine("  declare ToImageRegion: typeof Region.prototype.toImageRegion;");
                    sb.AppendLine("  declare IsEmpty: typeof Region.prototype.isEmpty;");
                    sb.AppendLine("  declare IsExist: typeof Region.prototype.isExist;");
                    sb.AppendLine("  declare Derive: typeof Region.prototype.derive;");
                    sb.AppendLine("  declare Dispose: typeof Region.prototype.dispose;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "BetterGenshinImpact.GameTask.Model.Area.ImageRegion":
                    sb.AppendLine("declare class ImageRegion extends Region {");
                    sb.AppendLine("  /** 源图像矩阵 */");
                    sb.AppendLine("  readonly srcMat: Mat;");
                    sb.AppendLine("  /** 缓存的灰度图矩阵 */");
                    sb.AppendLine("  readonly cacheGreyMat: Mat;");
                    sb.AppendLine("  /** 缓存的 Image 对象 */");
                    sb.AppendLine("  readonly cacheImage: any;");
                    sb.AppendLine();
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 从 Mat 创建图像区域");
                    sb.AppendLine("   * @param mat 图像矩阵");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   * @param owner 父区域");
                    sb.AppendLine("   * @param converter 坐标转换器");
                    sb.AppendLine("   * @param drawContent 绘制内容");
                    sb.AppendLine("   */");
                    sb.AppendLine("  constructor(mat: Mat, x: number, y: number, owner?: Region | null, converter?: any, drawContent?: any);");
                    sb.AppendLine();
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 裁剪派生新区域");
                    sb.AppendLine("   * @param x 相对 X 坐标");
                    sb.AppendLine("   * @param y 相对 Y 坐标");
                    sb.AppendLine("   * @param w 宽度");
                    sb.AppendLine("   * @param h 高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  deriveCrop(x: number, y: number, w: number, h: number): ImageRegion;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 裁剪派生新区域（使用 Rect）");
                    sb.AppendLine("   * @param rect 矩形区域");
                    sb.AppendLine("   */");
                    sb.AppendLine("  deriveCrop(rect: Rect): ImageRegion;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在区域内查找识别对象");
                    sb.AppendLine("   * @param ro 识别对象");
                    sb.AppendLine("   * @returns 匹配的区域");
                    sb.AppendLine("   */");
                    sb.AppendLine("  find(ro: RecognitionObject): Region;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在区域内查找所有匹配的识别对象");
                    sb.AppendLine("   * @param ro 识别对象");
                    sb.AppendLine("   * @returns 所有匹配的区域");
                    sb.AppendLine("   */");
                    sb.AppendLine("  findMulti(ro: RecognitionObject): Region[];");
                    sb.AppendLine("  /** 释放资源 */");
                    sb.AppendLine("  dispose(): void;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare readonly SrcMat: typeof ImageRegion.prototype.srcMat;");
                    sb.AppendLine("  declare readonly CacheGreyMat: typeof ImageRegion.prototype.cacheGreyMat;");
                    sb.AppendLine("  declare readonly CacheImage: typeof ImageRegion.prototype.cacheImage;");
                    sb.AppendLine("  declare DeriveCrop: typeof ImageRegion.prototype.deriveCrop;");
                    sb.AppendLine("  declare Find: typeof ImageRegion.prototype.find;");
                    sb.AppendLine("  declare FindMulti: typeof ImageRegion.prototype.findMulti;");
                    sb.AppendLine("  declare Dispose: typeof ImageRegion.prototype.dispose;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "BetterGenshinImpact.GameTask.Model.Area.GameCaptureRegion":
                    sb.AppendLine("declare class GameCaptureRegion extends ImageRegion {");
                    sb.AppendLine();
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 从 Mat 创建游戏截图区域");
                    sb.AppendLine("   * @param mat 图像矩阵");
                    sb.AppendLine("   * @param initX 初始 X 坐标");
                    sb.AppendLine("   * @param initY 初始 Y 坐标");
                    sb.AppendLine("   * @param owner 父区域");
                    sb.AppendLine("   * @param converter 坐标转换器");
                    sb.AppendLine("   * @param drawContent 绘制内容");
                    sb.AppendLine("   */");
                    sb.AppendLine("  constructor(mat: Mat, initX: number, initY: number, owner?: Region | null, converter?: any, drawContent?: any);");
                    sb.AppendLine();
                    sb.AppendLine("  /** 缩放派生到 1080P 区域 */");
                    sb.AppendLine("  deriveTo1080P(): ImageRegion;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在游戏截图区域中点击");
                    sb.AppendLine("   * @param posFunc 坐标计算函数 (size, scale) => [x, y]");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static gameRegionClick(posFunc: (size: any, scale: number) => [number, number]): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在游戏截图区域中移动鼠标");
                    sb.AppendLine("   * @param posFunc 坐标计算函数 (size, scale) => [x, y]");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static gameRegionMove(posFunc: (size: any, scale: number) => [number, number]): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在游戏截图区域中相对移动鼠标");
                    sb.AppendLine("   * @param deltaFunc 偏移计算函数 (size, scale) => [dx, dy]");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static gameRegionMoveBy(deltaFunc: (size: any, scale: number) => [number, number]): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 以 1080P 坐标点击");
                    sb.AppendLine("   * @param x 1080P X 坐标");
                    sb.AppendLine("   * @param y 1080P Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static gameRegion1080PPosClick(x: number, y: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 以 1080P 坐标移动鼠标");
                    sb.AppendLine("   * @param x 1080P X 坐标");
                    sb.AppendLine("   * @param y 1080P Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static gameRegion1080PPosMove(x: number, y: number): void;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare DeriveTo1080P: typeof GameCaptureRegion.prototype.deriveTo1080P;");
                    sb.AppendLine("  declare static GameRegionClick: typeof GameCaptureRegion.gameRegionClick;");
                    sb.AppendLine("  declare static GameRegionMove: typeof GameCaptureRegion.gameRegionMove;");
                    sb.AppendLine("  declare static GameRegionMoveBy: typeof GameCaptureRegion.gameRegionMoveBy;");
                    sb.AppendLine("  declare static GameRegion1080PPosClick: typeof GameCaptureRegion.gameRegion1080PPosClick;");
                    sb.AppendLine("  declare static GameRegion1080PPosMove: typeof GameCaptureRegion.gameRegion1080PPosMove;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;

                case "BetterGenshinImpact.GameTask.Model.Area.DesktopRegion":
                    sb.AppendLine("declare class DesktopRegion extends Region {");
                    sb.AppendLine();
                    sb.AppendLine("  constructor();");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 创建桌面区域");
                    sb.AppendLine("   * @param w 宽度");
                    sb.AppendLine("   * @param h 高度");
                    sb.AppendLine("   * @param iMouse 鼠标模拟器");
                    sb.AppendLine("   */");
                    sb.AppendLine("  constructor(w: number, h: number, iMouse?: any);");
                    sb.AppendLine();
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在桌面坐标点击");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  desktopRegionClick(x: number, y: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在桌面坐标点击矩形中心");
                    sb.AppendLine("   * @param x 矩形 X 坐标");
                    sb.AppendLine("   * @param y 矩形 Y 坐标");
                    sb.AppendLine("   * @param w 矩形宽度");
                    sb.AppendLine("   * @param h 矩形高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  desktopRegionClick(x: number, y: number, w: number, h: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在桌面坐标移动鼠标");
                    sb.AppendLine("   * @param x X 坐标");
                    sb.AppendLine("   * @param y Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  desktopRegionMove(x: number, y: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 在桌面坐标移动鼠标到矩形中心");
                    sb.AppendLine("   * @param x 矩形 X 坐标");
                    sb.AppendLine("   * @param y 矩形 Y 坐标");
                    sb.AppendLine("   * @param w 矩形宽度");
                    sb.AppendLine("   * @param h 矩形高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  desktopRegionMove(x: number, y: number, w: number, h: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 静态点击桌面坐标");
                    sb.AppendLine("   * @param cx X 坐标");
                    sb.AppendLine("   * @param cy Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static desktopRegionClick(cx: number, cy: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 静态点击桌面坐标矩形中心");
                    sb.AppendLine("   * @param x 矩形 X 坐标");
                    sb.AppendLine("   * @param y 矩形 Y 坐标");
                    sb.AppendLine("   * @param w 矩形宽度");
                    sb.AppendLine("   * @param h 矩形高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static desktopRegionClick(x: number, y: number, w: number, h: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 静态移动到桌面坐标");
                    sb.AppendLine("   * @param cx X 坐标");
                    sb.AppendLine("   * @param cy Y 坐标");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static desktopRegionMove(cx: number, cy: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 静态移动到桌面坐标矩形中心");
                    sb.AppendLine("   * @param x 矩形 X 坐标");
                    sb.AppendLine("   * @param y 矩形 Y 坐标");
                    sb.AppendLine("   * @param w 矩形宽度");
                    sb.AppendLine("   * @param h 矩形高度");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static desktopRegionMove(x: number, y: number, w: number, h: number): void;");
                    sb.AppendLine("  /**");
                    sb.AppendLine("   * 静态相对移动鼠标");
                    sb.AppendLine("   * @param dx X 偏移");
                    sb.AppendLine("   * @param dy Y 偏移");
                    sb.AppendLine("   */");
                    sb.AppendLine("  static desktopRegionMoveBy(dx: number, dy: number): void;");
                    sb.AppendLine();
                    sb.AppendLine("  // ==== BEGIN AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("  declare DesktopRegionClick: typeof DesktopRegion.prototype.desktopRegionClick;");
                    sb.AppendLine("  declare DesktopRegionMove: typeof DesktopRegion.prototype.desktopRegionMove;");
                    sb.AppendLine("  declare static DesktopRegionClick: typeof DesktopRegion.desktopRegionClick;");
                    sb.AppendLine("  declare static DesktopRegionMove: typeof DesktopRegion.desktopRegionMove;");
                    sb.AppendLine("  declare static DesktopRegionMoveBy: typeof DesktopRegion.desktopRegionMoveBy;");
                    sb.AppendLine("  // ==== END AUTO-GENERATED ALIASES ====");
                    sb.AppendLine("}");
                    sb.AppendLine();
                    break;
            }
        }

        string FormatParameters(ParameterInfo[] parameters)
        {
            var parts = new List<string>();
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var isParams = param.GetCustomAttribute<ParamArrayAttribute>() != null;
                var isOptional = (param.Attributes & ParameterAttributes.HasDefault) != 0;

                // Only mark nullable if the type is actually Nullable<T> (value type)
                var nullableUnderlying = Nullable.GetUnderlyingType(param.ParameterType);
                var isNullable = nullableUnderlying != null;

                var paramName = ToCamelCase(param.Name ?? $"arg{i}");
                var tsType = ToTsType(param.ParameterType);

                if (isParams)
                {
                    parts.Add($"...{paramName}: {tsType}");
                }
                else
                {
                    var optionalStr = (isOptional || isNullable) ? "?" : "";
                    parts.Add($"{paramName}{optionalStr}: {tsType}");
                }
            }
            return string.Join(", ", parts);
        }

        Type? ResolveType(string fullTypeName)
        {
            if (_resolvedTypes.TryGetValue(fullTypeName, out var cached))
                return cached;

            Type? type = null;
            try
            {
                type = _assembly.GetType(fullTypeName);
                if (type == null)
                {
                    // Try to find by simple name
                    var matches = _assembly.GetTypes()
                        .Where(t => t.Name == fullTypeName.Split('.').Last())
                        .ToList();
                    type = matches.Count == 1 ? matches[0] : matches.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: Failed to resolve type '{fullTypeName}': {ex.Message}");
                return null;
            }

            if (type != null)
                _resolvedTypes[fullTypeName] = type;
            return type;
        }
    }

    // ==================== Main ====================

    // 硬编码配置：DLL 相对于项目根目录的路径
    static readonly string DllRelativePath =
        "BetterGenshinImpact/bin/Debug/net8.0-windows10.0.22621.0/BetterGI.dll";
    static readonly string OutputFileName = "bettergi.d.ts";

    static int Main(string[] args)
    {
        // DLL 路径：优先命令行参数，否则用硬编码
        var dllPath = args.Length > 0 ? args[0] : DllRelativePath;

        if (!File.Exists(dllPath))
        {
            Console.Error.WriteLine($"Error: DLL not found: {dllPath}");
            Console.Error.WriteLine("Usage: TypeGen [dll-path]   (default: hardcoded path)");
            return 1;
        }

        Console.WriteLine($"Loading assembly: {dllPath}");

        // Set up assembly resolution from the DLL directory
        var dllDir = Path.GetDirectoryName(Path.GetFullPath(dllPath))!;
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var assemblyName = new AssemblyName(args.Name).Name!;
            var probePath = Path.Combine(dllDir, assemblyName + ".dll");
            if (File.Exists(probePath))
                return Assembly.LoadFrom(probePath);
            probePath = Path.Combine(dllDir, assemblyName + ".ni.dll");
            if (File.Exists(probePath))
                return Assembly.LoadFrom(probePath);
            return null;
        };

        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFrom(dllPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading assembly: {ex.Message}");
            return 1;
        }

        // 自动查找 BetterGenshinImpact 源码目录
        var sourceRoot = ".";
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "BetterGenshinImpact")))
            {
                sourceRoot = Path.Combine(dir, "BetterGenshinImpact");
                break;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }

        Console.WriteLine($"Source root: {sourceRoot}");

        var docReader = new XmlDocReader(sourceRoot);
        var emitter = new TsEmitter(docReader, assembly);
        var output = emitter.Generate();

        File.WriteAllText(OutputFileName, output, Encoding.UTF8);
        Console.WriteLine($"Generated: {Path.GetFullPath(OutputFileName)}");
        return 0;
    }
}
