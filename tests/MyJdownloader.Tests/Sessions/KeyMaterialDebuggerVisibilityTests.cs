namespace MyJdownloader.Tests.Sessions;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

[TestFixture]
public sealed class KeyMaterialDebuggerVisibilityTests
{
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly Type[] _keyMaterialTypes = [typeof(byte[]), typeof(Memory<byte>), typeof(ReadOnlyMemory<byte>)];

    [Test]
    public void KeyMaterialMembers_InCoreAssembly_AreHiddenFromDebugger()
    {
        var members = KeyMaterialMembers().ToList();

        var visible = members
            .Where(member => member.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State != DebuggerBrowsableState.Never)
            .Select(member => $"{member.DeclaringType}.{member.Name}")
            .ToList();

        Assert.That(members, Is.Not.Empty);
        Assert.That(visible, Is.Empty);
    }

    private static IEnumerable<MemberInfo> KeyMaterialMembers()
    {
        // Compiler-generated members (backing fields, async state machines) cannot be annotated; they are only
        // visible in raw views and locals, an accepted limit (ADR-006).
        var declaredTypes = typeof(MyJDownloaderClient).Assembly.GetTypes()
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));
        foreach (var type in declaredTypes)
        {
            foreach (var field in type.GetFields(InstanceMembers))
            {
                if (_keyMaterialTypes.Contains(field.FieldType) && !field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                {
                    yield return field;
                }
            }

            foreach (var property in type.GetProperties(InstanceMembers))
            {
                if (_keyMaterialTypes.Contains(property.PropertyType))
                {
                    yield return property;
                }
            }
        }
    }
}
