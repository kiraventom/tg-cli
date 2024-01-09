using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SettingsPropertyChangedGenerator;

[Generator]
public class SettingsPropertyChangedGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        var settingsInterface = compilation.GetTypeByMetadataName("SettingsManagement.ISettings");
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var symbols =
                syntaxTree.GetRoot().DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(syntax => semanticModel.GetDeclaredSymbol(syntax))
                    .OfType<ITypeSymbol>()
                    .Where(symbol => symbol.Interfaces.Contains(settingsInterface, SymbolEqualityComparer.Default));

            foreach (var symbol in symbols)
            {
                var source = GeneratePropertyChanged(symbol);
                context.AddSource($"{symbol.Name}.Notify.cs", source);
            }
        }
    }

    private static string GeneratePropertyChanged(ITypeSymbol symbol)
    {
        return $@"
using System.ComponentModel;

namespace {symbol.ContainingNamespace};

partial class {symbol.Name}
{{
    {GenerateProperties(symbol)}

    public event PropertyChangedEventHandler? PropertyChanged;
}}
";
    }

    private static string GenerateProperties(ITypeSymbol typeSymbol)
    {
        var sb = new StringBuilder();

        var fields = typeSymbol.GetMembers().OfType<IFieldSymbol>();
        var privateFields = fields.Where(field => field.DeclaredAccessibility is Accessibility.Private);
        var writablePrivateFields = privateFields.Where(field => !field.IsReadOnly);
        var settingsFields =
            writablePrivateFields.Where(field => field.GetAttributes().Any(a => a.AttributeClass.Name == "SaveOnChangeAttribute"));

        foreach (var fieldSymbol in settingsFields)
        {
            var propertyName = char.ToUpper(fieldSymbol.Name[1]) + fieldSymbol.Name[2..];
            sb.AppendLine($@"
public {fieldSymbol.Type} {propertyName}
{{
    get => {fieldSymbol.Name};
    set
    {{
        if (Equals({fieldSymbol.Name}, value))
            return;

        {fieldSymbol.Name} = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof({propertyName})));
        {propertyName}Changed?.Invoke(value);
    }}
}}

public event Action<{fieldSymbol.Type}> {propertyName}Changed;
"
            );
        }

        return sb.ToString();
    }
}
