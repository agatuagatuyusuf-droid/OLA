using System.Text.Json;
using Xunit;
using SkyAuto.Core.Variables;

namespace SkyAuto.Tests;

public class VariableResolverTests
{
    private static VariableContext MakeContext()
    {
        var ctx = new VariableContext
        {
            GlobalVariables = new Dictionary<string, object?>
            {
                ["全局设置"] = "production"
            },
            WorkflowVariables = new Dictionary<string, object?>
            {
                ["账号"] = "admin",
                ["密码"] = "abc123456",
                ["目标路径"] = "C:\\temp"
            },
            RunVariables = new Dictionary<string, object?>(),
            StepOutputs = new Dictionary<string, object?>
            {
                ["step_001"] = new Dictionary<string, object?>
                {
                    ["output"] = "步骤1成功",
                    ["text"] = "步骤1文本",
                    ["result"] = new Dictionary<string, object?>
                    {
                        ["text"] = "步骤1结果文本"
                    }
                },
                ["step_002"] = "步骤2原始输出"
            }
        };

        ctx.WorkflowVariables["用户"] = new Dictionary<string, object?>
        {
            ["name"] = "张三",
            ["phone"] = "13800000000"
        };

        ctx.WorkflowVariables["json用户"] = JsonSerializer.Deserialize<JsonElement>(@"{""name"":""李四"",""phone"":""13900000000""}");

        return ctx;
    }

    [Fact]
    public void Single_Variable_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("账号是 {{账号}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("账号是 admin", result.ResolvedText);
        Assert.Contains("账号", result.UsedKeys);
        Assert.Empty(result.MissingKeys);
    }

    [Fact]
    public void Multiple_Variable_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("账号 {{账号}} 密码 {{密码}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("账号 admin 密码 abc123456", result.ResolvedText);
        Assert.Equal(2, result.UsedKeys.Count);
    }

    [Fact]
    public void Missing_Variable_Returns_Failure()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("账号 {{账号}} 密码 {{密码}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("账号 admin 密码 abc123456", result.ResolvedText);
        Assert.Empty(result.MissingKeys);

        var result2 = resolver.Resolve("不存在的变量 {{不存在的变量}}", ctx);

        Assert.False(result2.Success);
        Assert.Contains("不存在的变量", result2.MissingKeys);
        Assert.Empty(result2.UsedKeys);
    }

    [Fact]
    public void Step_Output_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("输出: {{step_001.output}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("输出: 步骤1成功", result.ResolvedText);
        Assert.Contains("step_001.output", result.UsedKeys);
    }

    [Fact]
    public void Step_Output_Direct_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("直接: {{step_002}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("直接: 步骤2原始输出", result.ResolvedText);
    }

    [Fact]
    public void Step_Output_Property_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("文本: {{step_001.result.text}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("文本: 步骤1结果文本", result.ResolvedText);
    }

    [Fact]
    public void Dictionary_Property_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("姓名: {{用户.name}}, 电话: {{用户.phone}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("姓名: 张三, 电话: 13800000000", result.ResolvedText);
    }

    [Fact]
    public void JsonElement_Property_Replacement()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("姓名: {{json用户.name}}, 电话: {{json用户.phone}}", ctx);

        Assert.True(result.Success);
        Assert.Equal("姓名: 李四, 电话: 13900000000", result.ResolvedText);
    }

    [Fact]
    public void VariableMasker_Masks_Secret_Correctly()
    {
        Assert.Equal(string.Empty, VariableMasker.MaskSecret(null));
        Assert.Equal(string.Empty, VariableMasker.MaskSecret(""));
        Assert.Equal("**", VariableMasker.MaskSecret("ab"));
        Assert.Equal("a***c", VariableMasker.MaskSecret("abc"));
        Assert.Equal("a***3", VariableMasker.MaskSecret("abc123"));
        Assert.Equal("ab******56", VariableMasker.MaskSecret("abc123456"));
    }

    [Fact]
    public void Empty_String_Returns_Success()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("", ctx);

        Assert.True(result.Success);
        Assert.Equal("", result.ResolvedText);
    }

    [Fact]
    public void No_Template_Returns_Original()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("普通字符串没有模板", ctx);

        Assert.True(result.Success);
        Assert.Equal("普通字符串没有模板", result.ResolvedText);
    }

    [Fact]
    public void Malformed_Template_Does_Not_Throw()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var exception = Record.Exception(() => resolver.Resolve("错误模板 {{{{账号}}", ctx));
        Assert.Null(exception);
    }

    [Fact]
    public void Empty_Template_Key_Returns_Failure()
    {
        var resolver = new VariableResolver();
        var ctx = MakeContext();

        var result = resolver.Resolve("空键 {{ }}", ctx);

        Assert.False(result.Success);
        Assert.Contains(result.MissingKeys, k => k == "");
    }

    [Fact]
    public void MaskTextByDefinitions_Replaces_Secrets()
    {
        var ctx = MakeContext();
        var definitions = new List<VariableDefinition>
        {
            new() { Key = "密码", IsSecret = true, Type = VariableType.Password }
        };

        var masked = VariableMasker.MaskTextByDefinitions("密码是 abc123456", definitions, ctx);

        Assert.Equal("密码是 ab******56", masked);
    }
}
