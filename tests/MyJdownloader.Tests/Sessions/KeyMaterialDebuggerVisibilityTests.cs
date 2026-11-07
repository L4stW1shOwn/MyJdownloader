namespace MyJdownloader.Tests.Sessions;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

[TestFixture]
public sealed class KeyMaterialDebuggerVisibilityTests
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Type[] _keyMaterialTypes = [typeof(byte[]), typeof(Memory<byte>), typeof(ReadOnlyMemory<byte>)];

    private static readonly Type[] _textTypes = [typeof(string), typeof(string[]), typeof(IReadOnlyList<string>), typeof(IEnumerable<string>)];

    private static readonly string[] _secretNameParts = ["Token", "Secret", "Password"];

    [Test]
    public void KeyMaterialMembers_InCoreAssembly_AreHiddenFromDebugger()
    {
        var members = DeclaredMembers(type => _keyMaterialTypes.Contains(type), _ => true).ToList();

        Assert.That(members, Is.Not.Empty);
        Assert.That(VisibleNames(members), Is.Empty);
    }

    [Test]
    public void StringSecretMembers_InCoreAssembly_AreHiddenFromDebugger()
    {
        var members = DeclaredMembers(type => _textTypes.Contains(type), IsSecretName).ToList();

        Assert.That(members, Is.Not.Empty);
        Assert.That(VisibleNames(members), Is.Empty);
    }

    private static bool IsSecretName(string name) =>
        _secretNameParts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));

    private static List<string> VisibleNames(IEnumerable<MemberInfo> members) =>
    [
        .. members
            .Where(member => member.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State != DebuggerBrowsableState.Never)
            .Select(member => $"{member.DeclaringType}.{member.Name}"),
    ];

    private static IEnumerable<MemberInfo> DeclaredMembers(Func<Type, bool> typeMatches, Func<string, bool> nameMatches)
    {
        // Compiler-generated members (backing fields, async state machines) cannot be annotated; they are only
        // visible in raw views and locals, an accepted limit (ADR-006).
        var declaredTypes = typeof(MyJDownloaderClient).Assembly.GetTypes()
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));
        foreach (var type in declaredTypes)
        {
            foreach (var field in type.GetFields(InstanceMembers))
            {
                if (typeMatches(field.FieldType) && nameMatches(field.Name) && !field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                {
                    yield return field;
                }
            }

            foreach (var property in type.GetProperties(InstanceMembers))
            {
                if (typeMatches(property.PropertyType) && nameMatches(property.Name))
                {
                    yield return property;
                }
            }
        }
    }
}
